using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Helpers;
using UnityExplorer.UI;

namespace UnityExplorer.Inspectors.Reflection
{
    public class InteractiveBool : InteractiveValue
    {
        public InteractiveBool(object value, Type valueType) : base(value, valueType) { }

        public override bool HasSubContent => false;
        public override bool SubContentWanted => false;
        public override bool WantInspectBtn => false;

        internal Toggle m_toggle;

        public override void OnValueUpdated()
        {
            base.OnValueUpdated();

            if (!Value.IsNullOrDestroyed())
            {
                if (OwnerCacheObject.CanWrite)
                {
                    if (!m_toggle.gameObject.activeSelf)
                        m_toggle.gameObject.SetActive(true);

                    var val = (bool)Value;
                    if (m_toggle.isOn != val)
                        m_toggle.isOn = val;
                }

                RefreshUIElements();
            }
        }

        internal void RefreshUIElements()
        {
            if (m_baseLabel)
            {
                var val = (bool)Value;
                var color = val
                    ? "00FF00"  // on
                    : "FF0000"; // off

                m_baseLabel.text = $"<color=#{color}>{val}</color> ({m_richValueType})";
            }
        }

        internal void OnToggleValueChanged(bool val)
        {
            Value = val;
            OwnerCacheObject.SetValue();
            RefreshUIElements();
        }

        public override void ConstructUI(GameObject parent, GameObject subGroup)
        {
            base.ConstructUI(parent, subGroup);

            if (OwnerCacheObject.CanWrite)
            {
                var toggleObj = UIFactory.CreateToggle(m_valueContent, out m_toggle, out _, new Color(0.1f, 0.1f, 0.1f));
                toggleObj.SetActive(false);
                var toggleLayout = toggleObj.AddComponent<LayoutElement>();
                toggleLayout.minWidth = 24;

                m_toggle.onValueChanged.AddListener(OnToggleValueChanged);

                m_baseLabel.transform.SetAsLastSibling();
            }
        }
    }
}
