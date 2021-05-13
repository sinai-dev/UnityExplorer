using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.UI.CSConsole;
using UnityExplorer.UI.Utility;

namespace UnityExplorer.UI.Panels
{
    public class CSConsolePanel : UIPanel
    {
        public override string Name => "C# Console";
        public override UIManager.Panels PanelType => UIManager.Panels.CSConsole;
        public override int MinWidth => 400;
        public override int MinHeight => 300;

        public InputFieldScroller InputScroll { get; private set; }
        public InputFieldRef Input => InputScroll.InputField;
        public Text InputText { get; private set; }
        public Text HighlightText { get; private set; }

        public Action<string> OnInputChanged;

        public Action OnResetClicked;
        public Action OnCompileClicked;
        public Action<bool> OnCtrlRToggled;
        public Action<bool> OnSuggestionsToggled;
        public Action<bool> OnAutoIndentToggled;

        private void InvokeOnValueChanged(string value)
        {
            // Todo show a label instead of just logging
            if (value.Length == UIManager.MAX_INPUTFIELD_CHARS)
                ExplorerCore.LogWarning($"Reached maximum InputField character length! ({UIManager.MAX_INPUTFIELD_CHARS})");

            OnInputChanged?.Invoke(value);
        }

        public override void Update()
        {
            base.Update();

            ConsoleController.Update();
        }

        // Saving

        public override void DoSaveToConfigElement()
        {
            ConfigManager.CSConsoleData.Value = this.ToSaveData();
        }

        public override string GetSaveDataFromConfigManager() => ConfigManager.CSConsoleData.Value;

        // UI Construction

        protected internal override void DoSetDefaultPosAndAnchors()
        {
            mainPanelRect.localPosition = Vector2.zero;
            mainPanelRect.pivot = new Vector2(0f, 1f);
            mainPanelRect.anchorMin = new Vector2(0.4f, 0.1f);
            mainPanelRect.anchorMax = new Vector2(0.9f, 0.85f);
        }

        public override void ConstructPanelContent()
        {
            #region TOP BAR 

            // Main group object

            var topBarObj = UIFactory.CreateHorizontalGroup(this.content, "TopBar", true, true, true, true, 10, new Vector4(8, 8, 30, 30),
                default, TextAnchor.LowerCenter);
            UIFactory.SetLayoutElement(topBarObj, minHeight: 50, flexibleHeight: 0);

            //// Top label

            //var topBarLabel = UIFactory.CreateLabel(topBarObj, "TopLabel", "C# Console", TextAnchor.MiddleLeft, default, true, 25);
            //UIFactory.SetLayoutElement(topBarLabel.gameObject, preferredWidth: 150, flexibleWidth: 5000);

            // Enable Ctrl+R toggle

            var ctrlRToggleObj = UIFactory.CreateToggle(topBarObj, "CtrlRToggle", out var CtrlRToggle, out Text ctrlRToggleText);
            UIFactory.SetLayoutElement(ctrlRToggleObj, minWidth: 140, flexibleWidth: 0, minHeight: 25);
            ctrlRToggleText.alignment = TextAnchor.UpperLeft;
            ctrlRToggleText.text = "Run on Ctrl+R";
            CtrlRToggle.onValueChanged.AddListener((bool val) => { OnCtrlRToggled?.Invoke(val); });

            // Enable Suggestions toggle

            var suggestToggleObj = UIFactory.CreateToggle(topBarObj, "SuggestionToggle", out var SuggestionsToggle, out Text suggestToggleText);
            UIFactory.SetLayoutElement(suggestToggleObj, minWidth: 120, flexibleWidth: 0, minHeight: 25);
            suggestToggleText.alignment = TextAnchor.UpperLeft;
            suggestToggleText.text = "Suggestions";
            SuggestionsToggle.onValueChanged.AddListener((bool val) => { OnSuggestionsToggled?.Invoke(val); });

            // Enable Auto-indent toggle

            var autoIndentToggleObj = UIFactory.CreateToggle(topBarObj, "IndentToggle", out var AutoIndentToggle, out Text autoIndentToggleText);
            UIFactory.SetLayoutElement(autoIndentToggleObj, minWidth: 180, flexibleWidth: 0, minHeight: 25);
            autoIndentToggleText.alignment = TextAnchor.UpperLeft;
            autoIndentToggleText.text = "Auto-indent";
            AutoIndentToggle.onValueChanged.AddListener((bool val) => { OnAutoIndentToggled?.Invoke(val); });

            #endregion

            #region CONSOLE INPUT

            int fontSize = 16;

            var inputObj = UIFactory.CreateSrollInputField(this.content, "ConsoleInput", ScriptInteraction.STARTUP_TEXT, out var inputScroller, fontSize);
            InputScroll = inputScroller;
            ConsoleController.defaultInputFieldAlpha = Input.Component.selectionColor.a;
            Input.OnValueChanged += InvokeOnValueChanged;

            InputText = Input.Component.textComponent;
            InputText.supportRichText = false;
            InputText.color = Color.white;
            Input.PlaceholderText.fontSize = fontSize;

            // Lexer highlight text overlay
            var highlightTextObj = UIFactory.CreateUIObject("HighlightText", InputText.gameObject);
            var highlightTextRect = highlightTextObj.GetComponent<RectTransform>();
            highlightTextRect.pivot = new Vector2(0, 1);
            highlightTextRect.anchorMin = Vector2.zero;
            highlightTextRect.anchorMax = Vector2.one;
            highlightTextRect.offsetMin = Vector2.zero;
            highlightTextRect.offsetMax = Vector2.zero;

            HighlightText = highlightTextObj.AddComponent<Text>();
            HighlightText.color = Color.clear;
            HighlightText.supportRichText = true;
            HighlightText.fontSize = fontSize;

            // Set fonts
            InputText.font = UIManager.ConsoleFont;
            Input.PlaceholderText.font = UIManager.ConsoleFont;
            HighlightText.font = UIManager.ConsoleFont;

            #endregion

            #region COMPILE BUTTON BAR

            var horozGroupObj = UIFactory.CreateHorizontalGroup(this.content, "BigButtons", true, true, true, true, 0, new Vector4(2, 2, 2, 2),
                new Color(1, 1, 1, 0));

            var resetButton = UIFactory.CreateButton(horozGroupObj, "ResetButton", "Reset", new Color(0.33f, 0.33f, 0.33f));
            UIFactory.SetLayoutElement(resetButton.Component.gameObject, minHeight: 45, minWidth: 80, flexibleHeight: 0);
            resetButton.ButtonText.fontSize = 18;
            resetButton.OnClick += OnResetClicked;

            var compileButton = UIFactory.CreateButton(horozGroupObj, "CompileButton", "Compile", new Color(0.33f, 0.5f, 0.33f));
            UIFactory.SetLayoutElement(compileButton.Component.gameObject, minHeight: 45, minWidth: 80, flexibleHeight: 0);
            compileButton.ButtonText.fontSize = 18;
            compileButton.OnClick += OnCompileClicked;

            #endregion

        }
    }
}
