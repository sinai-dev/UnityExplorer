using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.CacheObject;

namespace UnityExplorer.UI.CacheObject.IValues
{
    public class InteractiveEnum : InteractiveValue
    {
        public bool IsFlags;
        public Type EnumType;

        private Type lastType;

        public OrderedDictionary CurrentValues;

        public CachedEnumValue ValueAtIdx(int idx) => (CachedEnumValue)CurrentValues[idx];
        public CachedEnumValue ValueAtKey(object key) => (CachedEnumValue)CurrentValues[key];
        
        private Dropdown enumDropdown;
        private GameObject toggleHolder;
        private readonly List<Toggle> flagToggles = new List<Toggle>();
        private readonly List<Text> flagTexts = new List<Text>();

        // Setting value from owner
        public override void SetValue(object value)
        {
            EnumType = value.GetType();

            if (lastType != EnumType)
            {
                CurrentValues = GetEnumValues(EnumType, out IsFlags);

                if (IsFlags)
                    SetupTogglesForEnumType();
                else
                    SetupDropdownForEnumType();

                lastType = EnumType;
            }

            // setup ui for changes
            if (IsFlags)
                SetTogglesForValue(value);
            else
                SetDropdownForValue(value);
        }

        // Setting value to owner

        private void OnApplyClicked()
        {
            if (IsFlags)
                SetValueFromFlags();
            else
                SetValueFromDropdown();
        }

        private void SetValueFromDropdown()
        {
            try
            {
                CurrentOwner.SetUserValue(ValueAtIdx(enumDropdown.value).ActualValue);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning("Exception setting from dropdown: " + ex);
            }
        }

        private void SetValueFromFlags()
        {
            try
            {
                List<string> values = new List<string>();
                for (int i = 0; i < CurrentValues.Count; i++)
                {
                    if (flagToggles[i].isOn)
                        values.Add(ValueAtIdx(i).Name);
                }

                CurrentOwner.SetUserValue(Enum.Parse(EnumType, string.Join(", ", values.ToArray())));
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning("Exception setting from flag toggles: " + ex);
            }
        }

        // setting UI state for value 

        private void SetDropdownForValue(object value)
        {
            if (CurrentValues.Contains(value))
            {
                var cached = ValueAtKey(value);
                enumDropdown.value = cached.EnumIndex;
                enumDropdown.RefreshShownValue();
            }
            else
                ExplorerCore.LogWarning("CurrentValues does not contain key '" + value?.ToString() ?? "<null>" + "'");
        }

        private void SetTogglesForValue(object value)
        {
            try
            {
                var split = value.ToString().Split(',');
                var set = new HashSet<string>();
                foreach (var s in split)
                    set.Add(s.Trim());

                for (int i = 0; i < CurrentValues.Count; i++)
                    flagToggles[i].isOn = set.Contains(ValueAtIdx(i).Name);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning("Exception setting flag toggles: " + ex);
            }
        }

        // Setting up the UI for the enum type when it changes or is first set

        private void SetupDropdownForEnumType()
        {
            toggleHolder.SetActive(false);
            enumDropdown.gameObject.SetActive(true);

            // create dropdown entries
            enumDropdown.options.Clear();

            foreach (CachedEnumValue entry in CurrentValues.Values)
                enumDropdown.options.Add(new Dropdown.OptionData(entry.Name));

            enumDropdown.value = 0;
            enumDropdown.RefreshShownValue();
        }

        private void SetupTogglesForEnumType()
        {
            toggleHolder.SetActive(true);
            enumDropdown.gameObject.SetActive(false);

            // create / set / hide toggles
            for (int i = 0; i < CurrentValues.Count || i < flagToggles.Count; i++)
            {
                if (i >= CurrentValues.Count)
                {
                    if (i >= flagToggles.Count)
                        break;

                    flagToggles[i].gameObject.SetActive(false);
                    continue;
                }

                if (i >= flagToggles.Count)
                    AddToggleRow();

                flagToggles[i].isOn = false;
                flagTexts[i].text = ValueAtIdx(i).Name;
            }
        }

        private void AddToggleRow()
        {
            var row = UIFactory.CreateUIObject("ToggleRow", toggleHolder);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(row, false, false, true, true, 2);
            UIFactory.SetLayoutElement(row, minHeight: 25, flexibleWidth: 9999);

            var toggleObj = UIFactory.CreateToggle(row, "ToggleObj", out Toggle toggle, out Text toggleText);
            UIFactory.SetLayoutElement(toggleObj, minHeight: 25, flexibleWidth: 9999);

            flagToggles.Add(toggle);
            flagTexts.Add(toggleText);
        }

        // UI Construction

        public override GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(parent, "InteractiveEnum", false, false, true, true, 3, new Vector4(4, 4, 4, 4),
                new Color(0.06f, 0.06f, 0.06f));
            UIFactory.SetLayoutElement(UIRoot, minHeight: 25, flexibleHeight: 9999, flexibleWidth: 9999);

            var hori = UIFactory.CreateUIObject("Hori", UIRoot);
            UIFactory.SetLayoutElement(hori, minHeight: 25, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(hori, false, false, true, true, 2);

            var applyButton = UIFactory.CreateButton(hori, "ApplyButton", "Apply", new Color(0.2f, 0.27f, 0.2f));
            UIFactory.SetLayoutElement(applyButton.Component.gameObject, minHeight: 25, minWidth: 100);
            applyButton.OnClick += OnApplyClicked;

            var dropdownObj = UIFactory.CreateDropdown(hori, out enumDropdown, "not set", 14, null);
            UIFactory.SetLayoutElement(dropdownObj, minHeight: 25, flexibleWidth: 600);

            toggleHolder = UIFactory.CreateUIObject("ToggleHolder", UIRoot);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(toggleHolder, false, false, true, true, 4);
            UIFactory.SetLayoutElement(toggleHolder, minHeight: 25, flexibleWidth: 9999, flexibleHeight: 9999);

            return UIRoot;
        }


        #region Enum cache 

        public struct CachedEnumValue
        {
            public CachedEnumValue(object value, int index, string name)
            {
                EnumIndex = index;
                Name = name;
                ActualValue = value;
            }

            public readonly object ActualValue;
            public int EnumIndex;
            public readonly string Name;
        }

        internal static readonly Dictionary<string, OrderedDictionary> enumCache = new Dictionary<string, OrderedDictionary>();

        internal static OrderedDictionary GetEnumValues(Type enumType, out bool isFlags)
        {
            isFlags = enumType.GetCustomAttributes(typeof(FlagsAttribute), true) is object[] fa && fa.Any();

            if (!enumCache.ContainsKey(enumType.AssemblyQualifiedName))
            {
                var dict = new OrderedDictionary();
                var addedNames = new HashSet<string>();

                int i = 0;
                foreach (var value in Enum.GetValues(enumType))
                {
                    var name = value.ToString();
                    if (addedNames.Contains(name))
                        continue;
                    addedNames.Add(name);

                    dict.Add(value, new CachedEnumValue(value, i, name));
                    i++;
                }

                enumCache.Add(enumType.AssemblyQualifiedName, dict);
            }

            return enumCache[enumType.AssemblyQualifiedName];
        }

        #endregion
    }
}
