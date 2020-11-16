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
    public class InteractiveFlags : InteractiveEnum
    {
        public InteractiveFlags(object value, Type valueType) : base(value, valueType) 
        {
            m_toggles = new Toggle[m_values.Length];
            m_enabledFlags = new bool[m_values.Length];
        }

        public override bool HasSubContent => true;
        public override bool SubContentWanted => Owner.CanWrite;
        public override bool WantInspectBtn => false;

        internal bool[] m_enabledFlags;
        internal Toggle[] m_toggles;

        public override void OnValueUpdated()
        {
            base.OnValueUpdated();

            if (Owner.CanWrite)
            {
                var enabledNames = new List<string>();

                var enabled = Value?.ToString().Split(',').Select(it => it.Trim());
                if (enabled != null)
                    enabledNames.AddRange(enabled);

                for (int i = 0; i < m_values.Length; i++)
                {
                    m_enabledFlags[i] = enabledNames.Contains(m_values[i].Value);
                }
            }
        }

        public override void RefreshUIForValue()
        {
            //base.RefreshUIForValue();

            GetDefaultLabel();
            m_baseLabel.text = DefaultLabel;

            if (m_subContentConstructed)
            {
                for (int i = 0; i < m_values.Length; i++)
                {
                    var toggle = m_toggles[i];
                    if (toggle.isOn != m_enabledFlags[i])
                        toggle.isOn = m_enabledFlags[i];
                }
            }
        }

        private void SetValueFromToggles()
        {
            string val = "";
            for (int i = 0; i < m_values.Length; i++)
            {
                if (m_enabledFlags[i])
                {
                    if (val != "") val += ", ";
                    val += m_values[i].Value;
                }
            }
            var type = Value?.GetType() ?? FallbackType;
            Value = Enum.Parse(type, val);
            RefreshUIForValue();
            Owner.SetValue();
        }

        internal override void OnToggleSubcontent(bool toggle)
        {
            base.OnToggleSubcontent(toggle);

            RefreshUIForValue();
        }

        public override void ConstructUI(GameObject parent, GameObject subGroup)
        {
            base.ConstructUI(parent, subGroup);
        }

        public override void ConstructSubcontent()
        {
            m_subContentConstructed = true;

            if (Owner.CanWrite)
            {
                var groupObj = UIFactory.CreateVerticalGroup(m_subContentParent, new Color(1, 1, 1, 0));
                var group = groupObj.GetComponent<VerticalLayoutGroup>();
                group.childForceExpandHeight = true;
                group.childForceExpandWidth = false;
                group.childControlHeight = true;
                group.childControlWidth = true;
                group.padding.top = 3;
                group.padding.left = 3;
                group.padding.right = 3;
                group.padding.bottom = 3;
                group.spacing = 5;

                // apply button

                var applyObj = UIFactory.CreateButton(groupObj, new Color(0.3f, 0.3f, 0.3f));
                var applyLayout = applyObj.AddComponent<LayoutElement>();
                applyLayout.minHeight = 25;
                applyLayout.minWidth = 50;
                var applyText = applyObj.GetComponentInChildren<Text>();
                applyText.text = "Apply";
                var applyBtn = applyObj.GetComponent<Button>();
                applyBtn.onClick.AddListener(SetValueFromToggles);

                // toggles

                for (int i = 0; i < m_values.Length; i++)
                {
                    AddToggle(i, groupObj);
                }
            }
        }

        internal void AddToggle(int index, GameObject groupObj)
        {
            var value = m_values[index];

            var toggleObj = UIFactory.CreateToggle(groupObj, out Toggle toggle, out Text text, new Color(0.1f, 0.1f, 0.1f));
            var toggleLayout = toggleObj.AddComponent<LayoutElement>();
            toggleLayout.minWidth = 100;
            toggleLayout.flexibleWidth = 2000;
            toggleLayout.minHeight = 25;

            m_toggles[index] = toggle;

            toggle.onValueChanged.AddListener((bool val) => { m_enabledFlags[index] = val; });

            text.text = $"{value.Key}: {value.Value}";
        }
    }
}
