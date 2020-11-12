using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.Helpers;
using UnityExplorer.UI;
using UnityExplorer.UI.Shared;
using UnityExplorer.Unstrip;

namespace UnityExplorer.Inspectors.Reflection
{
    public class InteractiveValue
    {
        public CacheObjectBase OwnerCacheObject;

        public object Value { get; set; }
        public Type ValueType;

        public string RichTextValue => m_richValue ?? GetRichTextValue();
        internal string m_richValue;
        internal string m_richValueType;

        public MethodInfo ToStringMethod => m_toStringMethod ?? GetToStringMethod();
        internal MethodInfo m_toStringMethod;

        public virtual void Init()
        {
            UpdateValue();
        }

        public virtual void UpdateValue()
        {
            if (!m_text)
                return;

            if (OwnerCacheObject is CacheMember ownerMember && !string.IsNullOrEmpty(ownerMember.ReflectionException))
            {
                m_text.text = "<color=red>" + ownerMember.ReflectionException + "</color>";
                return;
            }

            GetRichTextValue();

            m_text.text = RichTextValue;

            //if (Value == null)
            //    m_text.text = $"<color=red>null</color> {m_richValueType}";
            //else
            //    m_text.text = RichTextValue;
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


        public string GetRichTextValue()
        {
            if (Value != null)
                ValueType = Value.GetType();

            m_richValueType = UISyntaxHighlight.GetHighlight(ValueType, true);

            if (Value == null) return $"<color=grey>null</color> ({m_richValueType})";

            string label;

            if (ValueType == typeof(TextAsset))
            {
                var textAsset = Value as TextAsset;

                label = textAsset.text;

                if (label.Length > 10)
                    label = $"{label.Substring(0, 10)}...";

                label = $"\"{label}\" {textAsset.name} ({m_richValueType})";
            }
            else if (ValueType == typeof(EventSystem))
            {
                label = m_richValueType;
            }
            else
            {
                var toString = (string)ToStringMethod.Invoke(Value, null);

                var temp = toString.Replace(ValueType.FullName, "").Trim();

                if (string.IsNullOrEmpty(temp))
                {
                    label = m_richValueType;
                }
                else
                {
                    if (toString.Length > 200)
                        toString = toString.Substring(0, 200) + "...";

                    label = toString;

                    var unityType = $"({ValueType.FullName})";
                    if (Value is UnityEngine.Object && label.Contains(unityType))
                        label = label.Replace(unityType, $"({m_richValueType})");
                    else
                        label += $" ({m_richValueType})";
                }
            }

            return m_richValue = label;
        }

#region UI CONSTRUCTION

        internal GameObject m_UIContent;
        internal Text m_text;

        public void ConstructUI(GameObject parent)
        {
            m_UIContent = UIFactory.CreateLabel(parent, TextAnchor.MiddleLeft);
            var mainLayout = m_UIContent.AddComponent<LayoutElement>();
            mainLayout.minWidth = 200;
            mainLayout.flexibleWidth = 5000;
            mainLayout.minHeight = 25;
            m_text = m_UIContent.GetComponent<Text>();

            GetRichTextValue();
            m_text.text = $"<i><color=grey>Not yet evaluated</color> ({m_richValueType})</i>";
        }

#endregion
    }
}
