using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Unity;
using UnityExplorer.UI;

namespace UnityExplorer.UI.InteractiveValues
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
                DestroySubContent();
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

                    var backingType = Enum.GetUnderlyingType(type);
                    int intValue;
                    try
                    {
                        // this approach is necessary, a simple '(int)value' is not sufficient.

                        var unbox = Convert.ChangeType(value, backingType);

                        intValue = (int)Convert.ChangeType(unbox, typeof(int));
                    }
                    catch (Exception ex)
                    {
                        ExplorerCore.LogWarning("[InteractiveEnum] Could not Unbox underlying type " + backingType.Name + " from " + type.FullName);
                        ExplorerCore.Log(ex.ToString());
                        continue;
                    }

                    list.Add(new KeyValuePair<int, string>(intValue, name));
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

            if (m_subContentConstructed && !(this is InteractiveFlags))
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
            var index = m_dropdown.value;

            var value = Enum.Parse(type, s_enumNamesCache[type][index].Value);

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
                var groupObj = UIFactory.CreateHorizontalGroup(m_subContentParent, "InteractiveEnumGroup", false, true, true, true, 5, 
                    new Vector4(3,3,3,3),new Color(1, 1, 1, 0));

                // apply button

                var apply = UIFactory.CreateButton(groupObj, "ApplyButton", "Apply", SetValueFromDropdown, new Color(0.3f, 0.3f, 0.3f));
                UIFactory.SetLayoutElement(apply.gameObject, minHeight: 25, minWidth: 50);

                // dropdown

                var dropdownObj = UIFactory.CreateDropdown(groupObj, out m_dropdown, "", 14, null);
                UIFactory.SetLayoutElement(dropdownObj, minWidth: 150, minHeight: 25, flexibleWidth: 120);

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
