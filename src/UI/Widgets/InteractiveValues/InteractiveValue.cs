using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.Core;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.CacheObject;
using UnityExplorer.UI.Inspectors;

namespace UnityExplorer.UI.InteractiveValues
{
    public class InteractiveValue
    {
        /// <summary>
        /// Get the <see cref="InteractiveValue"/> subclass which supports the provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> which you want the <see cref="InteractiveValue"/> Type for.</param>
        /// <returns>The best subclass of <see cref="InteractiveValue"/> which supports the provided <paramref name="type"/>.</returns>
        public static Type GetIValueForType(Type type)
        {
            // rather ugly but I couldn't think of a cleaner way that was worth it.
            // switch-case doesn't really work here.

            // arbitrarily check some types, fastest methods first.
            if (type == typeof(bool))
                return typeof(InteractiveBool);
            // if type is primitive then it must be a number if its not a bool. Also check for decimal.
            else if (type.IsPrimitive || type == typeof(decimal))
                return typeof(InteractiveNumber);
            // check for strings
            else if (type == typeof(string))
                return typeof(InteractiveString);
            // check for enum/flags
            else if (typeof(Enum).IsAssignableFrom(type))
            {
                // NET 3.5 doesn't have "GetCustomAttribute", gotta use the multiple version.
                if (type.GetCustomAttributes(typeof(FlagsAttribute), true) is object[] fa && fa.Any())
                    return typeof(InteractiveFlags);
                else
                    return typeof(InteractiveEnum);
            }
            // check for unity struct types
            else if (typeof(Color).IsAssignableFrom(type))
                return typeof(InteractiveColor);
            else if (InteractiveFloatStruct.IsTypeSupported(type))
                return typeof(InteractiveFloatStruct);
            // check Transform, force InteractiveValue so they dont become InteractiveEnumerables.
            else if (typeof(Transform).IsAssignableFrom(type))
                return typeof(InteractiveValue);
            // check Dictionaries before Enumerables
            else if (ReflectionUtility.IsDictionary(type))
                return typeof(InteractiveDictionary);
            // finally check for Enumerables
            else if (ReflectionUtility.IsEnumerable(type))
                return typeof(InteractiveEnumerable);
            // fallback to default
            else
                return typeof(InteractiveValue);
        }

        public static InteractiveValue Create(object value, Type fallbackType)
        {
            var type = ReflectionUtility.GetActualType(value) ?? fallbackType;
            var iType = GetIValueForType(type);

            return (InteractiveValue)Activator.CreateInstance(iType, new object[] { value, type });
        }

        // ~~~~~~~~~ Instance ~~~~~~~~~

        public InteractiveValue(object value, Type valueType)
        {
            this.Value = value;
            this.FallbackType = valueType;
        }

        public CacheObjectBase Owner;

        public object Value;
        public readonly Type FallbackType;

        public virtual bool HasSubContent => false;
        public virtual bool SubContentWanted => false;
        public virtual bool WantInspectBtn => true;

        public string DefaultLabel => m_defaultLabel ?? GetDefaultLabel();
        internal string m_defaultLabel;
        internal string m_richValueType;

        public bool m_UIConstructed;

        public virtual void OnDestroy()
        {
            if (this.m_mainContent)
            {
                m_mainContent.transform.SetParent(null, false);
                m_mainContent.SetActive(false); 
                GameObject.Destroy(this.m_mainContent.gameObject);
            }

            DestroySubContent();
        }

        public virtual void DestroySubContent()
        {
            if (this.m_subContentParent && HasSubContent)
            {
                for (int i = 0; i < this.m_subContentParent.transform.childCount; i++)
                {
                    var child = m_subContentParent.transform.GetChild(i);
                    if (child)
                        GameObject.Destroy(child.gameObject);
                }
            }

            m_subContentConstructed = false;
        }

        public virtual void OnValueUpdated()
        {
            if (!m_UIConstructed)
                ConstructUI(m_mainContentParent, m_subContentParent);

            if (Owner is CacheMember ownerMember && !string.IsNullOrEmpty(ownerMember.ReflectionException))
                OnException(ownerMember);
            else
                RefreshUIForValue();
        }

        public virtual void OnException(CacheMember member)
        {
            if (m_UIConstructed)
                m_baseLabel.text = "<color=red>" + member.ReflectionException + "</color>";
                
            Value = null;
        }

        public virtual void RefreshUIForValue()
        {
            GetDefaultLabel();
            m_baseLabel.text = DefaultLabel;
        }

        public void RefreshElementsAfterUpdate()
        {
            if (WantInspectBtn)
            {
                bool shouldShowInspect = !Value.IsNullOrDestroyed();

                if (m_inspectButton.activeSelf != shouldShowInspect)
                    m_inspectButton.SetActive(shouldShowInspect);
            }

            bool subContentWanted = SubContentWanted;
            if (Owner is CacheMember cm && (!cm.HasEvaluated || !string.IsNullOrEmpty(cm.ReflectionException)))
                subContentWanted = false;

            if (HasSubContent)
            {
                if (m_subExpandBtn.gameObject.activeSelf != subContentWanted)
                    m_subExpandBtn.gameObject.SetActive(subContentWanted);

                if (!subContentWanted && m_subContentParent.activeSelf)
                    ToggleSubcontent();
            }
        }

        public virtual void ConstructSubcontent()
        {
            m_subContentConstructed = true;
        }

        public void ToggleSubcontent()
        {
            if (!this.m_subContentParent.activeSelf)
            {
                this.m_subContentParent.SetActive(true);
                this.m_subContentParent.transform.SetAsLastSibling();
                m_subExpandBtn.GetComponentInChildren<Text>().text = "▼";
            }
            else
            {
                this.m_subContentParent.SetActive(false);
                m_subExpandBtn.GetComponentInChildren<Text>().text = "▲";
            }

            OnToggleSubcontent(m_subContentParent.activeSelf);

            RefreshElementsAfterUpdate();
        }

        internal virtual void OnToggleSubcontent(bool toggle)
        {
            if (!m_subContentConstructed)
                ConstructSubcontent();
        }

        internal MethodInfo m_toStringMethod;
        internal MethodInfo m_toStringFormatMethod;
        internal bool m_gotToStringMethods;

        public string GetDefaultLabel(bool updateType = true)
        {
            var valueType = Value?.GetType() ?? this.FallbackType;
            if (updateType)
                m_richValueType = SignatureHighlighter.ParseFullSyntax(valueType, true);

            if (!Owner.HasEvaluated)
                return m_defaultLabel = $"<i><color=grey>Not yet evaluated</color> ({m_richValueType})</i>";

            if (Value.IsNullOrDestroyed())
                return m_defaultLabel = $"<color=grey>null</color> ({m_richValueType})";

            string label;

            // Two dirty fixes for TextAsset and EventSystem, which can have very long ToString results.
            if (Value is TextAsset textAsset)
            {
                label = textAsset.text;

                if (label.Length > 10)
                    label = $"{label.Substring(0, 10)}...";

                label = $"\"{label}\" {textAsset.name} ({m_richValueType})";
            }
            else if (Value is EventSystem)
            {
                label = m_richValueType;
            }
            else // For everything else...
            {
                if (!m_gotToStringMethods)
                {
                    m_gotToStringMethods = true;

                    m_toStringMethod = valueType.GetMethod("ToString", new Type[0]);
                    m_toStringFormatMethod = valueType.GetMethod("ToString", new Type[] { typeof(string) });

                    // test format method actually works
                    try
                    {
                        m_toStringFormatMethod.Invoke(Value, new object[] { "F3" });
                    }
                    catch
                    {
                        m_toStringFormatMethod = null;
                    }
                }

                string toString;
                if (m_toStringFormatMethod != null)
                    toString = (string)m_toStringFormatMethod.Invoke(Value, new object[] { "F3" });
                else
                    toString = (string)m_toStringMethod.Invoke(Value, new object[0]);

                toString = toString ?? "";

                string typeName = valueType.FullName;
                if (typeName.StartsWith("Il2CppSystem."))
                    typeName = typeName.Substring(6, typeName.Length - 6);

                toString = ReflectionProvider.Instance.ProcessTypeFullNameInString(valueType, toString, ref typeName);

                // If the ToString is just the type name, use our syntax highlighted type name instead.
                if (toString == typeName)
                {
                    label = m_richValueType;
                }
                else // Otherwise, parse the result and put our highlighted name in.
                {
                    if (toString.Length > 200)
                        toString = toString.Substring(0, 200) + "...";

                    label = toString;

                    var unityType = $"({valueType.FullName})";
                    if (Value is UnityEngine.Object && label.Contains(unityType))
                        label = label.Replace(unityType, $"({m_richValueType})");
                    else
                        label += $" ({m_richValueType})";
                }
            }

            return m_defaultLabel = label;
        }

        #region UI CONSTRUCTION

        internal GameObject m_mainContentParent;
        internal GameObject m_subContentParent;

        internal GameObject m_mainContent;
        internal GameObject m_inspectButton;
        internal Text m_baseLabel;

        internal Button m_subExpandBtn;
        internal bool m_subContentConstructed;

        public virtual void ConstructUI(GameObject parent, GameObject subGroup)
        {
            m_UIConstructed = true;

            m_mainContent = UIFactory.CreateHorizontalGroup(parent, $"InteractiveValue_{this.GetType().Name}", false, false, true, true, 4, default, 
                new Color(1, 1, 1, 0), TextAnchor.UpperLeft);

            var mainRect = m_mainContent.GetComponent<RectTransform>();
            mainRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 25);

            UIFactory.SetLayoutElement(m_mainContent, flexibleWidth: 9000, minWidth: 175, minHeight: 25, flexibleHeight: 0);

            // subcontent expand button
            if (HasSubContent)
            {
                m_subExpandBtn = UIFactory.CreateButton(m_mainContent, "ExpandSubcontentButton", "▲", ToggleSubcontent, new Color(0.3f, 0.3f, 0.3f));
                UIFactory.SetLayoutElement(m_subExpandBtn.gameObject, minHeight: 25, minWidth: 25, flexibleWidth: 0, flexibleHeight: 0);
            }

            // inspect button

            var inspectBtn = UIFactory.CreateButton(m_mainContent, 
                "InspectButton", 
                "Inspect", 
                () => 
                {
                    if (!Value.IsNullOrDestroyed(false))
                        InspectorManager.Inspect(this.Value, this.Owner);
                }, 
                new Color(0.3f, 0.3f, 0.3f, 0.2f));

            m_inspectButton = inspectBtn.gameObject;
            UIFactory.SetLayoutElement(m_inspectButton, minWidth: 60, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);

            m_inspectButton.SetActive(false);

            // value label

            m_baseLabel = UIFactory.CreateLabel(m_mainContent, "ValueLabel", "", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(m_baseLabel.gameObject, flexibleWidth: 9000, minHeight: 25);

            m_subContentParent = subGroup;
        }

#endregion
    }
}
