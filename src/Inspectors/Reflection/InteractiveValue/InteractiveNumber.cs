using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Helpers;
using UnityExplorer.UI;

namespace UnityExplorer.Inspectors.Reflection
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

            m_baseLabel.text = UISyntaxHighlight.ParseFullSyntax(FallbackType, false);
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
                ExplorerCore.LogWarning("Could not parse input! " + ReflectionHelpers.ExceptionToString(e, true));
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

            var inputObj = UIFactory.CreateInputField(m_valueContent);
            var inputLayout = inputObj.AddComponent<LayoutElement>();
            inputLayout.minWidth = 120;
            inputLayout.minHeight = 25;
            inputLayout.flexibleWidth = 0;

            m_valueInput = inputObj.GetComponent<InputField>();
            m_valueInput.gameObject.SetActive(false);

            if (Owner.CanWrite)
            {
                var applyBtnObj = UIFactory.CreateButton(m_valueContent, new Color(0.2f, 0.2f, 0.2f));
                var applyLayout = applyBtnObj.AddComponent<LayoutElement>();
                applyLayout.minWidth = 50;
                applyLayout.minHeight = 25;
                applyLayout.flexibleWidth = 0;
                m_applyBtn = applyBtnObj.GetComponent<Button>();
                m_applyBtn.onClick.AddListener(OnApplyClicked);

                var applyText = applyBtnObj.GetComponentInChildren<Text>();
                applyText.text = "Apply";
            }
        }
    }
}
