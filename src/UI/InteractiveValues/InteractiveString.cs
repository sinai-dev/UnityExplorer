using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Unity;
using UnityExplorer.UI;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.CacheObject;

namespace UnityExplorer.UI.InteractiveValues
{
    public class InteractiveString : InteractiveValue
    {
        public InteractiveString(object value, Type valueType) : base(value, valueType) { }

        public override bool HasSubContent => true;
        public override bool SubContentWanted => true;

        public override bool WantInspectBtn => false;

        public override void OnValueUpdated()
        {
            base.OnValueUpdated();
        }

        public override void OnException(CacheMember member)
        {
            base.OnException(member); 
            
            if (m_subContentConstructed && m_hiddenObj.gameObject.activeSelf)
                m_hiddenObj.gameObject.SetActive(false);

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

            m_baseLabel.text = m_richValueType;

            if (m_subContentConstructed)
            {
                if (!m_hiddenObj.gameObject.activeSelf)
                    m_hiddenObj.gameObject.SetActive(true);
            }

            if (!string.IsNullOrEmpty((string)Value))
            {
                var toString = (string)Value;
                if (toString.Length > 15000)
                    toString = toString.Substring(0, 15000);

                m_readonlyInput.text = toString;

                if (m_subContentConstructed)
                {
                    m_valueInput.text = toString;
                    m_placeholderText.text = toString;
                }
            }
            else
            {
                string s = Value == null 
                            ? "null" 
                            : "empty";

                m_readonlyInput.text = $"<i><color=grey>{s}</color></i>";

                if (m_subContentConstructed)
                {
                    m_valueInput.text = "";
                    m_placeholderText.text = s;
                }
            }

            m_labelLayout.minWidth = 50;
            m_labelLayout.flexibleWidth = 0;
        }

        internal void OnApplyClicked()
        {
            Value = m_valueInput.text;
            Owner.SetValue();
            RefreshUIForValue();
        }

        // for the default label
        internal LayoutElement m_labelLayout;

        //internal InputField m_readonlyInput;
        internal Text m_readonlyInput;

        // for input
        internal InputField m_valueInput;
        internal GameObject m_hiddenObj;
        internal Text m_placeholderText;

        public override void ConstructUI(GameObject parent, GameObject subGroup)
        {
            base.ConstructUI(parent, subGroup);

            GetDefaultLabel(false);
            m_richValueType = SignatureHighlighter.ParseFullSyntax(FallbackType, false);

            m_labelLayout = m_baseLabel.gameObject.GetComponent<LayoutElement>();

            m_readonlyInput = UIFactory.CreateLabel(m_valueContent, "ReadonlyLabel", "", TextAnchor.MiddleLeft);
            m_readonlyInput.horizontalOverflow = HorizontalWrapMode.Overflow;

            var testFitter = m_readonlyInput.gameObject.AddComponent<ContentSizeFitter>();
            testFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

            UIFactory.SetLayoutElement(m_readonlyInput.gameObject, minHeight: 25, preferredHeight: 25, flexibleHeight: 0);
        }

        public override void ConstructSubcontent()
        {
            base.ConstructSubcontent();

            var groupObj = UIFactory.CreateVerticalGroup(m_subContentParent, "SubContent", false, false, true, true, 4, new Vector4(3,3,3,3),
                new Color(1, 1, 1, 0));

            m_hiddenObj = UIFactory.CreateLabel(groupObj, "HiddenLabel", "", TextAnchor.MiddleLeft).gameObject;
            m_hiddenObj.SetActive(false);
            var hiddenText = m_hiddenObj.GetComponent<Text>();
            hiddenText.color = Color.clear;
            hiddenText.fontSize = 14;
            hiddenText.raycastTarget = false;
            hiddenText.supportRichText = false;
            var hiddenFitter = m_hiddenObj.AddComponent<ContentSizeFitter>();
            hiddenFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UIFactory.SetLayoutElement(m_hiddenObj, minHeight: 25, flexibleHeight: 500, minWidth: 250, flexibleWidth: 9000);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(m_hiddenObj, true, true, true, true);

            var inputObj = UIFactory.CreateInputField(m_hiddenObj, "StringInputField", "...", 14, 3);
            UIFactory.SetLayoutElement(inputObj, minWidth: 120, minHeight: 25, flexibleWidth: 5000, flexibleHeight: 5000);

            m_valueInput = inputObj.GetComponent<InputField>();
            m_valueInput.lineType = InputField.LineType.MultiLineNewline;

            m_placeholderText = m_valueInput.placeholder.GetComponent<Text>();

            m_placeholderText.supportRichText = false;
            m_valueInput.textComponent.supportRichText = false;

            m_valueInput.onValueChanged.AddListener((string val) =>
            {
                hiddenText.text = val ?? "";
                LayoutRebuilder.ForceRebuildLayoutImmediate(Owner.m_mainRect);
            });

            if (Owner.CanWrite)
            {
                var apply = UIFactory.CreateButton(groupObj, "ApplyButton", "Apply", OnApplyClicked, new Color(0.2f, 0.2f, 0.2f));
                UIFactory.SetLayoutElement(apply.gameObject, minWidth: 50, minHeight: 25, flexibleWidth: 0);
            }
            else
            {
                m_valueInput.readOnly = true;
            }

            RefreshUIForValue();
        }
    }
}
