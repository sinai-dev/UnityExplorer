using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Widgets.AutoComplete;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.Utility;

namespace UnityExplorer.UI.Widgets
{
    public class ParameterHandler : BaseArgumentHandler
    {
        private ParameterInfo paramInfo;
        private Type paramType;

        internal EnumCompleter enumCompleter;
        private ButtonRef enumHelperButton;

        private bool usingBasicLabel;
        private object basicValue;
        private GameObject basicLabelHolder;
        private Text basicLabel;
        private ButtonRef pasteButton;

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
                usingBasicLabel = false;

                this.inputField.Component.gameObject.SetActive(true);
                this.basicLabelHolder.SetActive(false);
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
                usingBasicLabel = true;

                this.inputField.Component.gameObject.SetActive(false);
                this.basicLabelHolder.SetActive(true);
                this.typeCompleter.Enabled = false;
                this.enumCompleter.Enabled = false;
                this.enumHelperButton.Component.gameObject.SetActive(false);

                SetDisplayedValueFromPaste();
            }
        }

        public void OnReturned()
        {
            this.evaluator = null;
            this.paramInfo = null;

            this.enumCompleter.Enabled = false;
            this.typeCompleter.Enabled = false;

            this.inputField.Text = "";

            this.usingBasicLabel = false;
            this.basicValue = null;
        }

        public object Evaluate()
        {
            if (usingBasicLabel)
                return basicValue;

            string input = this.inputField.Text;

            if (typeof(Type).IsAssignableFrom(paramType))
                return ReflectionUtility.GetTypeByName(input);

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

        private void OnPasteClicked()
        {
            if (ClipboardPanel.TryPaste(this.paramType, out object paste))
            {
                basicValue = paste;
                SetDisplayedValueFromPaste();
            }
        }

        private void SetDisplayedValueFromPaste()
        {
            if (usingBasicLabel)
                basicLabel.text = ToStringUtility.ToStringWithType(basicValue, paramType, false);
            else
            {
                if (typeof(Type).IsAssignableFrom(paramType))
                    inputField.Text = (basicValue as Type).FullDescription();
                else
                    inputField.Text = ParseUtility.ToStringForInput(basicValue, paramType);
            }
        }

        public override void CreateSpecialContent()
        {
            enumCompleter = new(paramType, this.inputField)
            {
                Enabled = false
            };

            enumHelperButton = UIFactory.CreateButton(UIRoot, "EnumHelper", "▼");
            UIFactory.SetLayoutElement(enumHelperButton.Component.gameObject, minWidth: 25, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            enumHelperButton.OnClick += enumCompleter.HelperButtonClicked;

            basicLabelHolder = UIFactory.CreateHorizontalGroup(UIRoot, "BasicLabelHolder", true, true, true, true, bgColor: new(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(basicLabelHolder, minHeight: 25, flexibleHeight: 50, minWidth: 100, flexibleWidth: 1000);
            basicLabel = UIFactory.CreateLabel(basicLabelHolder, "BasicLabel", "null", TextAnchor.MiddleLeft);
            basicLabel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            pasteButton = UIFactory.CreateButton(UIRoot, "PasteButton", "Paste", new Color(0.13f, 0.13f, 0.13f, 1f));
            UIFactory.SetLayoutElement(pasteButton.Component.gameObject, minHeight: 25, minWidth: 28, flexibleWidth: 0);
            pasteButton.ButtonText.color = Color.green;
            pasteButton.ButtonText.fontSize = 10;
            pasteButton.OnClick += OnPasteClicked;
        }
    }
}
