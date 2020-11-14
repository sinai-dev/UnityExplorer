using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.Helpers;
using UnityExplorer.UI;

namespace UnityExplorer.Inspectors.Reflection
{
    public class InteractiveValue
    {
        public InteractiveValue(Type valueType)
        {
            this.ValueType = valueType;
        }

        public CacheObjectBase OwnerCacheObject;

        public object Value { get; set; }
        public readonly Type ValueType;

        // might not need
        public virtual bool HasSubContent => false;

        public string RichTextValue => m_richValue ?? GetLabelForValue();
        internal string m_richValue;
        internal string m_richValueType;

        public MethodInfo ToStringMethod => m_toStringMethod ?? GetToStringMethod();
        internal MethodInfo m_toStringMethod;

        public virtual void Init()
        {
            OnValueUpdated();
        }

        public virtual void OnValueUpdated()
        {
            if (!m_text)
                return;

            if (OwnerCacheObject is CacheMember ownerMember && !string.IsNullOrEmpty(ownerMember.ReflectionException))
            {
                m_text.text = "<color=red>" + ownerMember.ReflectionException + "</color>";
                return;
            }

            GetLabelForValue();
            m_text.text = RichTextValue;

            bool shouldShowInspect = !Value.IsNullOrDestroyed(true);
            if (m_inspectButton.activeSelf != shouldShowInspect)
                m_inspectButton.SetActive(shouldShowInspect);
        }

        public string GetLabelForValue()
        {
            var valueType = Value?.GetType() ?? this.ValueType;

            m_richValueType = UISyntaxHighlight.ParseFullSyntax(valueType, true);

            if (OwnerCacheObject is CacheMember cm && !cm.HasEvaluated)
                return $"<i><color=grey>Not yet evaluated</color> ({m_richValueType})</i>";

            if (Value == null) return $"<color=grey>null</color> ({m_richValueType})";

            string label;

            if (valueType == typeof(TextAsset) && Value is TextAsset textAsset)
            {
                label = textAsset.text;

                if (label.Length > 10)
                    label = $"{label.Substring(0, 10)}...";

                label = $"\"{label}\" {textAsset.name} ({m_richValueType})";
            }
            else if (valueType == typeof(EventSystem))
            {
                label = m_richValueType;
            }
            else
            {
                var toString = (string)ToStringMethod.Invoke(Value, null);

                var fullnametemp = valueType.ToString();
                if (fullnametemp.StartsWith("Il2CppSystem"))
                    fullnametemp = fullnametemp.Substring(6, fullnametemp.Length - 6);

                var temp = toString.Replace(fullnametemp, "").Trim();

                if (string.IsNullOrEmpty(temp))
                {
                    label = m_richValueType;
                }
                else
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

            return m_richValue = label;
        }

        private MethodInfo GetToStringMethod()
        {
            try
            {
                m_toStringMethod = ReflectionHelpers.GetActualType(Value).GetMethod("ToString", new Type[0])
                                   ?? typeof(object).GetMethod("ToString", new Type[0]);

                // test invoke
                m_toStringMethod.Invoke(Value, null);
            }
            catch
            {
                m_toStringMethod = typeof(object).GetMethod("ToString", new Type[0]);
            }
            return m_toStringMethod;
        }

        #region UI CONSTRUCTION

        internal GameObject m_mainContent;
        internal GameObject m_inspectButton;
        internal Text m_text;
        internal GameObject m_subContentParent;

        public virtual void ConstructUI(GameObject parent, GameObject subGroup)
        {
            m_mainContent = UIFactory.CreateHorizontalGroup(parent, new Color(1, 1, 1, 0));
            var mainGroup = m_mainContent.GetComponent<HorizontalLayoutGroup>();

            mainGroup.childForceExpandWidth = true;
            mainGroup.childControlWidth = true;
            mainGroup.childForceExpandHeight = false;
            mainGroup.childControlHeight = true;
            mainGroup.spacing = 4;
            mainGroup.childAlignment = TextAnchor.UpperLeft;
            var mainLayout = m_mainContent.AddComponent<LayoutElement>();
            mainLayout.flexibleWidth = 9000;
            mainLayout.minWidth = 175;
            mainLayout.minHeight = 25;
            mainLayout.flexibleHeight = 0;

            // inspect button

            m_inspectButton = UIFactory.CreateButton(m_mainContent, new Color(0.3f, 0.3f, 0.3f, 0.2f));
            var inspectLayout = m_inspectButton.AddComponent<LayoutElement>();
            inspectLayout.minWidth = 60;
            inspectLayout.minHeight = 25;
            inspectLayout.flexibleHeight = 0;
            inspectLayout.flexibleWidth = 0;
            var inspectText = m_inspectButton.GetComponentInChildren<Text>();
            inspectText.text = "Inspect";
            var inspectBtn = m_inspectButton.GetComponent<Button>();

            inspectBtn.onClick.AddListener(OnInspectClicked);
            void OnInspectClicked()
            {
                if (!Value.IsNullOrDestroyed())
                    InspectorManager.Instance.Inspect(this.Value);
            }

            m_inspectButton.SetActive(false);

            // value label / tostring

            var labelObj = UIFactory.CreateLabel(m_mainContent, TextAnchor.MiddleLeft);
            m_text = labelObj.GetComponent<Text>();
            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 9000;
            labelLayout.minHeight = 25;

            m_subContentParent = subGroup;
        }

#endregion
    }
}
