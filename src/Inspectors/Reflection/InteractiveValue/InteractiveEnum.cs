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
    public class InteractiveEnum : InteractiveValue
    {
        internal static Dictionary<Type, KeyValuePair<int,string>[]> s_enumNamesCache = new Dictionary<Type, KeyValuePair<int, string>[]>();

        public InteractiveEnum(object value, Type valueType) : base(value, valueType)
        {
            GetNames();
        }

        public override bool HasSubContent => true;
        public override bool SubContentWanted => Owner.CanWrite;
        public override bool WantInspectBtn => false;

        internal KeyValuePair<int,string>[] m_values = new KeyValuePair<int, string>[0];

        internal Type m_lastEnumType;

        internal void GetNames()
        {
            var type = Value?.GetType() ?? FallbackType;

            if (m_lastEnumType == type)
                return;

            m_lastEnumType = type;

            if (m_subContentConstructed)
            {
                // changing types, destroy subcontent
                for (int i = 0; i < m_subContentParent.transform.childCount; i++)
                {
                    var child = m_subContentParent.transform.GetChild(i);
                    GameObject.Destroy(child.gameObject);
                }

                m_subContentConstructed = false;
            }

            if (!s_enumNamesCache.ContainsKey(type))
            {
                // using GetValues not GetNames, to catch instances of weird enums (eg CameraClearFlags)
                var values = Enum.GetValues(type);

                var list = new List<KeyValuePair<int, string>>();
                var set = new HashSet<string>();
                foreach (var value in values)
                {
                    var name = value.ToString();
                    if (set.Contains(name)) 
                        continue;
                    set.Add(name);
                    list.Add(new KeyValuePair<int, string>((int)value, name));
                }

                s_enumNamesCache.Add(type, list.ToArray());
            }

            m_values = s_enumNamesCache[type];
        }

        public override void OnValueUpdated()
        {
            GetNames();

            base.OnValueUpdated();
        }

        public override void RefreshUIForValue()
        {
            base.RefreshUIForValue();

            if (m_subContentConstructed)
            {
                m_dropdownText.text = Value?.ToString() ?? "<no value set>";
            }
        }

        internal override void OnToggleSubcontent(bool toggle)
        {
            base.OnToggleSubcontent(toggle);

            RefreshUIForValue();
        }

        private void SetValueFromDropdown()
        {
            var type = Value?.GetType() ?? FallbackType;
            var value = Enum.Parse(type, m_dropdownText.text);
            if (value != null)
            {
                Value = value;
                Owner.SetValue();
                RefreshUIForValue();
            }
        }

        internal Dropdown m_dropdown;
        internal Text m_dropdownText;

        public override void ConstructUI(GameObject parent, GameObject subGroup)
        {
            base.ConstructUI(parent, subGroup);
        }

        public override void ConstructSubcontent()
        {
            base.ConstructSubcontent();

            if (Owner.CanWrite)
            {
                var groupObj = UIFactory.CreateHorizontalGroup(m_subContentParent, new Color(1, 1, 1, 0));
                var group = groupObj.GetComponent<HorizontalLayoutGroup>();
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
                applyBtn.onClick.AddListener(SetValueFromDropdown);

                // dropdown

                var dropdownObj = UIFactory.CreateDropdown(groupObj, out m_dropdown);
                var dropLayout = dropdownObj.AddComponent<LayoutElement>();
                dropLayout.minWidth = 150;
                dropLayout.minHeight = 25;
                dropLayout.flexibleWidth = 120;

                foreach (var kvp in m_values)
                {
                    m_dropdown.options.Add(new Dropdown.OptionData
                    {
                        text = $"{kvp.Key}: {kvp.Value}"
                    });
                }

                m_dropdownText = m_dropdown.transform.Find("Label").GetComponent<Text>();
            }
        }
    }
}
