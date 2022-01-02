using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using UnityEngine.UI;
using UnityExplorer.CacheObject.IValues;
using UnityExplorer.UI.Widgets.AutoComplete;
using UniverseLib;
using UniverseLib.UI;

namespace UnityExplorer.UI.Widgets
{
    public class ParameterHandler : BaseArgumentHandler
    {
        private ParameterInfo paramInfo;
        private Type paramType;

        internal Dropdown dropdown;
        private bool usingDropdown;
        internal EnumCompleter enumCompleter;
        private ButtonRef enumHelperButton;

        public void OnBorrowed(EvaluateWidget evaluator, ParameterInfo paramInfo)
        {
            this.evaluator = evaluator;
            this.paramInfo = paramInfo;

            this.paramType = paramInfo.ParameterType;
            if (paramType.IsByRef)
                paramType = paramType.GetElementType();

            this.argNameLabel.text = 
                $"{SignatureHighlighter.Parse(paramType, false)} <color={SignatureHighlighter.LOCAL_ARG}>{paramInfo.Name}</color>";

            if (ParseUtility.CanParse(paramType) || typeof(Type).IsAssignableFrom(paramType))
            {
                this.inputField.Component.gameObject.SetActive(true);
                this.dropdown.gameObject.SetActive(false);
                this.typeCompleter.Enabled = typeof(Type).IsAssignableFrom(paramType);
                this.enumCompleter.Enabled = paramType.IsEnum;
                this.enumHelperButton.Component.gameObject.SetActive(paramType.IsEnum);

                if (!typeCompleter.Enabled)
                {
                    if (paramType == typeof(string))
                        inputField.PlaceholderText.text = "...";
                    else
                        inputField.PlaceholderText.text = $"eg. {ParseUtility.GetExampleInput(paramType)}";
                }
                else
                {
                    inputField.PlaceholderText.text = "Enter a Type name...";
                    this.typeCompleter.BaseType = typeof(object);
                    this.typeCompleter.CacheTypes();
                }

                if (enumCompleter.Enabled)
                {
                    enumCompleter.EnumType = paramType;
                    enumCompleter.CacheEnumValues();
                }
            }
            else
            {
                // non-parsable, and not a Type
                this.inputField.Component.gameObject.SetActive(false);
                this.dropdown.gameObject.SetActive(true);
                this.typeCompleter.Enabled = false;
                this.enumCompleter.Enabled = false;
                this.enumHelperButton.Component.gameObject.SetActive(false);

                usingDropdown = true;
                PopulateDropdown();

                InspectorManager.OnInspectedTabsChanged += PopulateDropdown;
            }
        }

        public void OnReturned()
        {
            this.evaluator = null;
            this.paramInfo = null;

            usingDropdown = false;

            this.enumCompleter.Enabled = false;
            this.typeCompleter.Enabled = false;

            this.inputField.Text = "";

            InspectorManager.OnInspectedTabsChanged -= PopulateDropdown;
        }

        public object Evaluate()
        {
            if (!usingDropdown)
            {
                var input = this.inputField.Text;

                if (paramType == typeof(string))
                    return input;

                if (string.IsNullOrEmpty(input))
                {
                    if (paramInfo.IsOptional)
                        return paramInfo.DefaultValue;
                    else
                        return null;
                }

                if (!ParseUtility.TryParse(input, paramType, out object parsed, out Exception ex))
                {
                    ExplorerCore.LogWarning($"Cannot parse argument '{paramInfo.Name}' ({paramInfo.ParameterType.Name})" +
                        $"{(ex == null ? "" : $", {ex.GetType().Name}: {ex.Message}")}");
                    return null;
                }
                else
                    return parsed;
            }
            else
            {
                if (dropdown.value == 0)
                    return null;
                else
                    return dropdownUnderlyingValues[dropdown.value];
            }
        }

        private object[] dropdownUnderlyingValues;

        internal void PopulateDropdown()
        {
            if (!usingDropdown)
                return;

            dropdown.options.Clear();
            var underlyingValues = new List<object>();

            dropdown.options.Add(new Dropdown.OptionData("null"));
            underlyingValues.Add(null);

            var argType = paramType;

            int tabIndex = 0;
            foreach (var tab in InspectorManager.Inspectors)
            {
                tabIndex++;

                if (argType.IsAssignableFrom(tab.Target.GetActualType()))
                {
                    dropdown.options.Add(new Dropdown.OptionData($"Tab {tabIndex}: {tab.Tab.TabText.text}"));
                    underlyingValues.Add(tab.Target);
                }
            }

            dropdownUnderlyingValues = underlyingValues.ToArray();
        }

        private void EnumHelper_OnClick()
        {
            enumCompleter.HelperButtonClicked();
        }

        public override void CreateSpecialContent()
        {
            enumHelperButton = UIFactory.CreateButton(UIRoot, "EnumHelper", "▼");
            UIFactory.SetLayoutElement(enumHelperButton.Component.gameObject, minWidth: 25, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            enumHelperButton.OnClick += EnumHelper_OnClick;

            var dropdownObj = UIFactory.CreateDropdown(UIRoot, out dropdown, "Select argument...", 14, (int val) =>
            {
                //ArgDropdownChanged(val);
            });
            UIFactory.SetLayoutElement(dropdownObj, minHeight: 25, flexibleHeight: 50, minWidth: 100, flexibleWidth: 1000);
            dropdownObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            enumCompleter = new EnumCompleter(paramType, this.inputField);
            enumCompleter.Enabled = false;
        }

        //private void ArgDropdownChanged(int value)
        //{
        //    // not needed
        //}
    }
}
