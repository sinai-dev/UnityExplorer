using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Unity;
using UnityExplorer.UI;

namespace UnityExplorer.Core.Inspectors.Reflection
{
    public class InteractiveBool : InteractiveValue
    {
        public InteractiveBool(object value, Type valueType) : base(value, valueType) { }

        public override bool HasSubContent => false;
        public override bool SubContentWanted => false;
        public override bool WantInspectBtn => false;

        internal Toggle m_toggle;
        internal Button m_applyBtn;

        public override void OnValueUpdated()
        {
            base.OnValueUpdated();
        }

        public override void RefreshUIForValue()
        {
            GetDefaultLabel();

            if (Owner.HasEvaluated)
            {
                var val = (bool)Value;

                if (Owner.CanWrite)
                {
                    if (!m_toggle.gameObject.activeSelf)
                        m_toggle.gameObject.SetActive(true);

                    if (!m_applyBtn.gameObject.activeSelf)
                        m_applyBtn.gameObject.SetActive(true);

                    if (val != m_toggle.isOn)
                        m_toggle.isOn = val;
                }

                var color = val
                    ? "6bc981"  // on
                    : "c96b6b"; // off

                m_baseLabel.text = $"<color=#{color}>{val}</color>";
            }
            else
            {
                m_baseLabel.text = DefaultLabel;
            }
        }

        public override void OnException(CacheMember member)
        {
            base.OnException(member);

            if (Owner.CanWrite)
            {
                if (m_toggle.gameObject.activeSelf)
                    m_toggle.gameObject.SetActive(false);

                if (m_applyBtn.gameObject.activeSelf)
                    m_applyBtn.gameObject.SetActive(false);
            }
        }

        internal void OnToggleValueChanged(bool val)
        {
            Value = val;
            RefreshUIForValue();
        }

        public override void ConstructUI(GameObject parent, GameObject subGroup)
        {
            base.ConstructUI(parent, subGroup);

            var baseLayout = m_baseLabel.gameObject.GetComponent<LayoutElement>();
            baseLayout.flexibleWidth = 0;
            baseLayout.minWidth = 50;

            if (Owner.CanWrite)
            {
                var toggleObj = UIFactory.CreateToggle(m_valueContent, out m_toggle, out _, new Color(0.1f, 0.1f, 0.1f));
                var toggleLayout = toggleObj.AddComponent<LayoutElement>();
                toggleLayout.minWidth = 24;

                m_toggle.onValueChanged.AddListener(OnToggleValueChanged);

                m_baseLabel.transform.SetAsLastSibling();

                var applyBtnObj = UIFactory.CreateButton(m_valueContent, new Color(0.2f, 0.2f, 0.2f));
                var applyLayout = applyBtnObj.AddComponent<LayoutElement>();
                applyLayout.minWidth = 50;
                applyLayout.minHeight = 25;
                applyLayout.flexibleWidth = 0;
                m_applyBtn = applyBtnObj.GetComponent<Button>();
                m_applyBtn.onClick.AddListener(() => { Owner.SetValue(); });

                var applyText = applyBtnObj.GetComponentInChildren<Text>();
                applyText.text = "Apply";

                toggleObj.SetActive(false);
                applyBtnObj.SetActive(false);
            }
        }
    }
}
