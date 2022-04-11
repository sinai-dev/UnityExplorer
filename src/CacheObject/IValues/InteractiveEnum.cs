using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Widgets.AutoComplete;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.Utility;

namespace UnityExplorer.CacheObject.IValues
{
    public class InteractiveEnum : InteractiveValue
    {
        public bool IsFlags;
        public Type EnumType;

        private Type lastType;

        public OrderedDictionary CurrentValues;

        private InputFieldRef inputField;
        private ButtonRef enumHelperButton;
        private EnumCompleter enumCompleter;

        private GameObject toggleHolder;
        private readonly List<Toggle> flagToggles = new();
        private readonly List<Text> flagTexts = new();

        public CachedEnumValue ValueAtIndex(int idx) => (CachedEnumValue)CurrentValues[idx];
        public CachedEnumValue ValueAtKey(object key) => (CachedEnumValue)CurrentValues[key];

        // Setting value from owner
        public override void SetValue(object value)
        {
            EnumType = value.GetType();

            if (lastType != EnumType)
            {
                CurrentValues = GetEnumValues(EnumType);

                IsFlags = EnumType.GetCustomAttributes(typeof(FlagsAttribute), true) is object[] fa && fa.Any();
                if (IsFlags)
                    SetupTogglesForEnumType();
                else
                {
                    inputField.Component.gameObject.SetActive(true);
                    enumHelperButton.Component.gameObject.SetActive(true);
                    toggleHolder.SetActive(false);
                }

                enumCompleter.EnumType = EnumType;
                enumCompleter.CacheEnumValues();

                lastType = EnumType;
            }

            if (!IsFlags)
                inputField.Text = value.ToString();
            else
                SetTogglesForValue(value);

            this.enumCompleter.chosenSuggestion = value.ToString();
            AutoCompleteModal.Instance.ReleaseOwnership(this.enumCompleter);
        }

        private void SetTogglesForValue(object value)
        {
            try
            {
                for (int i = 0; i < CurrentValues.Count; i++)
                    flagToggles[i].isOn = (value as Enum).HasFlag(ValueAtIndex(i).ActualValue as Enum);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning("Exception setting flag toggles: " + ex);
            }
        }

        // Setting value to owner

        private void OnApplyClicked()
        {
            try
            {
                if (!IsFlags)
                {
                    if (ParseUtility.TryParse(this.inputField.Text, EnumType, out object value, out Exception ex))
                        CurrentOwner.SetUserValue(value);
                    else
                        throw ex;
                }
                else
                {
                    SetValueFromFlags();
                }
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
                List<string> values = new();
                for (int i = 0; i < CurrentValues.Count; i++)
                {
                    if (flagToggles[i].isOn)
                        values.Add(ValueAtIndex(i).Name);
                }

                CurrentOwner.SetUserValue(Enum.Parse(EnumType, string.Join(", ", values.ToArray())));
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning("Exception setting from flag toggles: " + ex);
            }
        }

        // UI Construction

        private void EnumHelper_OnClick()
        {
            enumCompleter.HelperButtonClicked();
        }

        public override GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(parent, "InteractiveEnum", false, false, true, true, 3, new Vector4(4, 4, 4, 4),
                new Color(0.06f, 0.06f, 0.06f));
            UIFactory.SetLayoutElement(UIRoot, minHeight: 25, flexibleHeight: 9999, flexibleWidth: 9999);

            GameObject hori = UIFactory.CreateUIObject("Hori", UIRoot);
            UIFactory.SetLayoutElement(hori, minHeight: 25, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(hori, false, false, true, true, 2);

            ButtonRef applyButton = UIFactory.CreateButton(hori, "ApplyButton", "Apply", new Color(0.2f, 0.27f, 0.2f));
            UIFactory.SetLayoutElement(applyButton.Component.gameObject, minHeight: 25, minWidth: 100);
            applyButton.OnClick += OnApplyClicked;

            inputField = UIFactory.CreateInputField(hori, "InputField", "Enter name or underlying value...");
            UIFactory.SetLayoutElement(inputField.UIRoot, minHeight: 25, flexibleHeight: 50, minWidth: 100, flexibleWidth: 1000);
            inputField.Component.lineType = InputField.LineType.MultiLineNewline;
            inputField.UIRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            enumHelperButton = UIFactory.CreateButton(hori, "EnumHelper", "▼");
            UIFactory.SetLayoutElement(enumHelperButton.Component.gameObject, minWidth: 25, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            enumHelperButton.OnClick += EnumHelper_OnClick;

            enumCompleter = new EnumCompleter(this.EnumType, this.inputField);

            toggleHolder = UIFactory.CreateUIObject("ToggleHolder", UIRoot);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(toggleHolder, false, false, true, true, 4);
            UIFactory.SetLayoutElement(toggleHolder, minHeight: 25, flexibleWidth: 9999, flexibleHeight: 9999);

            return UIRoot;
        }

        private void SetupTogglesForEnumType()
        {
            toggleHolder.SetActive(true);
            inputField.Component.gameObject.SetActive(false);
            enumHelperButton.Component.gameObject.SetActive(false);

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
                flagTexts[i].text = ValueAtIndex(i).Name;
            }
        }

        private void AddToggleRow()
        {
            GameObject row = UIFactory.CreateUIObject("ToggleRow", toggleHolder);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(row, false, false, true, true, 2);
            UIFactory.SetLayoutElement(row, minHeight: 25, flexibleWidth: 9999);

            GameObject toggleObj = UIFactory.CreateToggle(row, "ToggleObj", out Toggle toggle, out Text toggleText);
            UIFactory.SetLayoutElement(toggleObj, minHeight: 25, flexibleWidth: 9999);

            flagToggles.Add(toggle);
            flagTexts.Add(toggleText);
        }

        #region Enum cache 

        internal static readonly Dictionary<string, OrderedDictionary> enumCache = new();

        internal static OrderedDictionary GetEnumValues(Type enumType)
        {
            //isFlags = enumType.GetCustomAttributes(typeof(FlagsAttribute), true) is object[] fa && fa.Any();

            if (!enumCache.ContainsKey(enumType.AssemblyQualifiedName))
            {
                OrderedDictionary dict = new();
                HashSet<string> addedNames = new();

                int i = 0;
                foreach (object value in Enum.GetValues(enumType))
                {
                    string name = value.ToString();
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
}
