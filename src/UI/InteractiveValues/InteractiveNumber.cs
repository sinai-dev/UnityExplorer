using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Unity;
using UnityExplorer.UI;
using UnityExplorer.Core;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.CacheObject;

namespace UnityExplorer.UI.InteractiveValues
{
    public class InteractiveNumber : InteractiveValue
    {
        public InteractiveNumber(object value, Type valueType) : base(value, valueType) { }

        public override bool HasSubContent => false;
        public override bool SubContentWanted => false;
        public override bool WantInspectBtn => false;

        public override void OnValueUpdated()
        {
            base.OnValueUpdated();
        }

        public override void OnException(CacheMember member)
        {
            base.OnException(member);

            if (m_valueInput.gameObject.activeSelf)
                m_valueInput.gameObject.SetActive(false);

            if (Owner.CanWrite)
            {
                if (m_applyBtn.gameObject.activeSelf)
                    m_applyBtn.gameObject.SetActive(false);
            }
        }

        public override void RefreshUIForValue()
        {
            if (!Owner.HasEvaluated)
            {
                GetDefaultLabel();
                m_baseLabel.text = DefaultLabel;
                return;
            }

            m_baseLabel.text = SignatureHighlighter.ParseFullSyntax(FallbackType, false);
            m_valueInput.text = Value.ToString();

            var type = Value.GetType();
            if (type == typeof(float)
                || type == typeof(double)
                || type == typeof(decimal))
            {
                m_valueInput.characterValidation = InputField.CharacterValidation.Decimal;
            }
            else
            {
                m_valueInput.characterValidation = InputField.CharacterValidation.Integer;
            }

            if (Owner.CanWrite)
            {
                if (!m_applyBtn.gameObject.activeSelf)
                    m_applyBtn.gameObject.SetActive(true);
            }

            if (!m_valueInput.gameObject.activeSelf)
                m_valueInput.gameObject.SetActive(true);
        }

        public MethodInfo ParseMethod => m_parseMethod ?? (m_parseMethod = Value.GetType().GetMethod("Parse", new Type[] { typeof(string) }));
        private MethodInfo m_parseMethod;

        internal void OnApplyClicked()
        {
            try
            {
                Value = ParseMethod.Invoke(null, new object[] { m_valueInput.text });
                Owner.SetValue();
                RefreshUIForValue();
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning("Could not parse input! " + ReflectionUtility.ReflectionExToString(e, true));
            }
        }

        internal InputField m_valueInput;
        internal Button m_applyBtn;

        public override void ConstructUI(GameObject parent, GameObject subGroup)
        {
            base.ConstructUI(parent, subGroup);

            var labelLayout = m_baseLabel.gameObject.GetComponent<LayoutElement>();
            labelLayout.minWidth = 50;
            labelLayout.flexibleWidth = 0;

            var inputObj = UIFactory.CreateInputField(m_mainContent, "InteractiveNumberInput", "...");
            UIFactory.SetLayoutElement(inputObj, minWidth: 120, minHeight: 25, flexibleWidth: 0);

            m_valueInput = inputObj.GetComponent<InputField>();
            m_valueInput.gameObject.SetActive(false);

            if (Owner.CanWrite)
            {
                m_applyBtn = UIFactory.CreateButton(m_mainContent, "ApplyButton", "Apply", OnApplyClicked, new Color(0.2f, 0.2f, 0.2f));
                UIFactory.SetLayoutElement(m_applyBtn.gameObject, minWidth: 50, minHeight: 25, flexibleWidth: 0);
            }
        }
    }
}
