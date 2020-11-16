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
    public class InteractiveString : InteractiveValue
    {
        public InteractiveString(object value, Type valueType) : base(value, valueType) { }

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
            
            if (m_hiddenObj.gameObject.activeSelf)
                m_hiddenObj.gameObject.SetActive(false);

            // m_baseLabel.text = DefaultLabel;
            m_labelLayout.minWidth = 200;
            m_labelLayout.flexibleWidth = 5000;
        }

        public override void RefreshUIForValue()
        {
            GetDefaultLabel(false);

            if (!Owner.HasEvaluated)
            {
                m_baseLabel.text = DefaultLabel;
                return;
            }

            if (!m_hiddenObj.gameObject.activeSelf)
                m_hiddenObj.gameObject.SetActive(true);

            m_baseLabel.text = m_richValueType;

            if (Value != null)
            {
                var toString = Value.ToString();
                if (toString.Length > 15000)
                    toString = toString.Substring(0, 15000);

                m_valueInput.text = toString;
                m_placeholderText.text = toString;
            }
            else
            {
                m_valueInput.text = "";
                m_placeholderText.text = "null";
            }

            m_labelLayout.minWidth = 50;
            m_labelLayout.flexibleWidth = 0;
        }

        
        internal void OnApplyClicked()
        {
            Value = m_valueInput.text;
            Owner.SetValue();
        }

        internal InputField m_valueInput;
        internal LayoutElement m_labelLayout;
        internal GameObject m_hiddenObj;
        internal Text m_placeholderText;

        public override void ConstructUI(GameObject parent, GameObject subGroup)
        {
            base.ConstructUI(parent, subGroup);

            GetDefaultLabel(false);
            m_richValueType = UISyntaxHighlight.ParseFullSyntax(FallbackType, false);

            m_labelLayout = m_baseLabel.gameObject.GetComponent<LayoutElement>();

            m_hiddenObj = UIFactory.CreateLabel(m_valueContent, TextAnchor.MiddleLeft);
            m_hiddenObj.SetActive(false);
            var hiddenText = m_hiddenObj.GetComponent<Text>();
            hiddenText.color = Color.clear;
            hiddenText.fontSize = 14;
            hiddenText.raycastTarget = false;
            var hiddenFitter = m_hiddenObj.AddComponent<ContentSizeFitter>();
            hiddenFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var hiddenLayout = m_hiddenObj.AddComponent<LayoutElement>();
            hiddenLayout.minHeight = 25;
            hiddenLayout.flexibleHeight = 500;
            hiddenLayout.minWidth = 250;
            hiddenLayout.flexibleWidth = 9000;
            var hiddenGroup = m_hiddenObj.AddComponent<HorizontalLayoutGroup>();
            hiddenGroup.childForceExpandWidth = true;
            hiddenGroup.childControlWidth = true;
            hiddenGroup.childForceExpandHeight = true;
            hiddenGroup.childControlHeight = true;

            var inputObj = UIFactory.CreateInputField(m_hiddenObj, 14, 3);
            var inputLayout = inputObj.AddComponent<LayoutElement>();
            inputLayout.minWidth = 120;
            inputLayout.minHeight = 25;
            inputLayout.flexibleWidth = 5000;
            inputLayout.flexibleHeight = 5000;

            m_valueInput = inputObj.GetComponent<InputField>();
            m_valueInput.lineType = InputField.LineType.MultiLineNewline;

            m_placeholderText = m_valueInput.placeholder.GetComponent<Text>();

            m_valueInput.onValueChanged.AddListener((string val) => 
            {
                hiddenText.text = val;
                LayoutRebuilder.ForceRebuildLayoutImmediate(Owner.m_mainRect);
            });

            if (Owner.CanWrite)
            {
                var applyBtnObj = UIFactory.CreateButton(m_valueContent, new Color(0.2f, 0.2f, 0.2f));
                var applyLayout = applyBtnObj.AddComponent<LayoutElement>();
                applyLayout.minWidth = 50;
                applyLayout.minHeight = 25;
                applyLayout.flexibleWidth = 0;
                var applyBtn = applyBtnObj.GetComponent<Button>();
                applyBtn.onClick.AddListener(OnApplyClicked);

                var applyText = applyBtnObj.GetComponentInChildren<Text>();
                applyText.text = "Apply";
            }
            else
            {
                m_valueInput.readOnly = true;
            }
        }
    }
}
