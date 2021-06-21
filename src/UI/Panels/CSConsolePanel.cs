using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.UI.CSConsole;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Panels
{
    public class CSConsolePanel : UIPanel
    {
        public override string Name => "C# Console";
        public override UIManager.Panels PanelType => UIManager.Panels.CSConsole;
        public override int MinWidth => 750;
        public override int MinHeight => 300;

        public InputFieldScroller InputScroller { get; private set; }
        public InputFieldRef Input => InputScroller.InputField;
        public Text InputText { get; private set; }
        public Text HighlightText { get; private set; }
        public Text LineNumberText { get; private set; }

        public Dropdown HelpDropdown { get; private set; }

        // events
        public Action<string> OnInputChanged;
        public Action OnResetClicked;
        public Action OnCompileClicked;
        public Action<int> OnHelpDropdownChanged;
        public Action<bool> OnCtrlRToggled;
        public Action<bool> OnSuggestionsToggled;
        public Action<bool> OnAutoIndentToggled;
        public Action OnPanelResized;

        private void InvokeOnValueChanged(string value)
        {
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

        public override void OnFinishResize(RectTransform panel)
        {
            OnPanelResized?.Invoke();
        }

        protected internal override void DoSetDefaultPosAndAnchors()
        {
            Rect.localPosition = Vector2.zero;
            Rect.pivot = new Vector2(0f, 1f);
            Rect.anchorMin = new Vector2(0.4f, 0.175f);
            Rect.anchorMax = new Vector2(0.85f, 0.925f);
        }

        public override void ConstructPanelContent()
        {
            // Tools Row

            var toolsRow = UIFactory.CreateHorizontalGroup(this.content, "ToggleRow", false, false, true, true, 5, new Vector4(8, 8, 10, 5),
                default, TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(toolsRow, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            // Buttons

            var compileButton = UIFactory.CreateButton(toolsRow, "CompileButton", "Compile", new Color(0.33f, 0.5f, 0.33f));
            UIFactory.SetLayoutElement(compileButton.Component.gameObject, minHeight: 28, minWidth: 130, flexibleHeight: 0);
            compileButton.ButtonText.fontSize = 15;
            compileButton.OnClick += () => { OnCompileClicked?.Invoke(); };

            var resetButton = UIFactory.CreateButton(toolsRow, "ResetButton", "Reset", new Color(0.33f, 0.33f, 0.33f));
            UIFactory.SetLayoutElement(resetButton.Component.gameObject, minHeight: 28, minWidth: 80, flexibleHeight: 0);
            resetButton.ButtonText.fontSize = 15;
            resetButton.OnClick += () => { OnResetClicked?.Invoke(); };

            // Help dropdown

            var helpDrop = UIFactory.CreateDropdown(toolsRow, out var dropdown, "Help", 14, null);
            UIFactory.SetLayoutElement(helpDrop, minHeight: 25, minWidth: 100);
            HelpDropdown = dropdown;
            HelpDropdown.onValueChanged.AddListener((int val) => { this.OnHelpDropdownChanged?.Invoke(val); });

            // Enable Ctrl+R toggle

            var ctrlRToggleObj = UIFactory.CreateToggle(toolsRow, "CtrlRToggle", out var CtrlRToggle, out Text ctrlRToggleText);
            UIFactory.SetLayoutElement(ctrlRToggleObj, minWidth: 150, flexibleWidth: 0, minHeight: 25);
            ctrlRToggleText.alignment = TextAnchor.UpperLeft;
            ctrlRToggleText.text = "Compile on Ctrl+R";
            CtrlRToggle.onValueChanged.AddListener((bool val) => { OnCtrlRToggled?.Invoke(val); });

            // Enable Suggestions toggle

            var suggestToggleObj = UIFactory.CreateToggle(toolsRow, "SuggestionToggle", out var SuggestionsToggle, out Text suggestToggleText);
            UIFactory.SetLayoutElement(suggestToggleObj, minWidth: 120, flexibleWidth: 0, minHeight: 25);
            suggestToggleText.alignment = TextAnchor.UpperLeft;
            suggestToggleText.text = "Suggestions";
            SuggestionsToggle.onValueChanged.AddListener((bool val) => { OnSuggestionsToggled?.Invoke(val); });

            // Enable Auto-indent toggle

            var autoIndentToggleObj = UIFactory.CreateToggle(toolsRow, "IndentToggle", out var AutoIndentToggle, out Text autoIndentToggleText);
            UIFactory.SetLayoutElement(autoIndentToggleObj, minWidth: 120, flexibleWidth: 0, minHeight: 25);
            autoIndentToggleText.alignment = TextAnchor.UpperLeft;
            autoIndentToggleText.text = "Auto-indent";
            AutoIndentToggle.onValueChanged.AddListener((bool val) => { OnAutoIndentToggled?.Invoke(val); });

            // Console Input

            var inputArea = UIFactory.CreateUIObject("InputGroup", content);
            UIFactory.SetLayoutElement(inputArea, flexibleWidth: 9999, flexibleHeight: 9999);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(inputArea, false, true, true, true);
            inputArea.AddComponent<Image>().color = Color.white;
            inputArea.AddComponent<Mask>().showMaskGraphic = false;

            // line numbers

            var linesHolder = UIFactory.CreateUIObject("LinesHolder", inputArea);
            var linesRect = linesHolder.GetComponent<RectTransform>();
            linesRect.pivot = new Vector2(0, 1);
            linesRect.anchorMin = new Vector2(0, 0);
            linesRect.anchorMax = new Vector2(0, 1);
            linesRect.sizeDelta = new Vector2(0, 305000);
            linesRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, 50);
            linesHolder.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(linesHolder, true, true, true, true);

            LineNumberText = UIFactory.CreateLabel(linesHolder, "LineNumbers", "1", TextAnchor.UpperCenter, Color.grey, fontSize: 16);
            LineNumberText.font = UIManager.ConsoleFont;

            // input field

            int fontSize = 16;

            var inputObj = UIFactory.CreateScrollInputField(inputArea, "ConsoleInput", ConsoleController.STARTUP_TEXT, 
                out var inputScroller, fontSize);
            InputScroller = inputScroller;
            ConsoleController.defaultInputFieldAlpha = Input.Component.selectionColor.a;
            Input.OnValueChanged += InvokeOnValueChanged;

            // move line number text with input field
            linesRect.transform.SetParent(inputObj.transform.Find("Viewport"), false);
            inputScroller.Slider.Scrollbar.onValueChanged.AddListener((float val) => { SetLinesPosition(); });
            inputScroller.Slider.Slider.onValueChanged.AddListener((float val) => { SetLinesPosition(); });
            void SetLinesPosition()
            {
                linesRect.anchoredPosition = new Vector2(linesRect.anchoredPosition.x, inputScroller.ContentRect.anchoredPosition.y);
                //SetInputLayout();
            }

            InputText = Input.Component.textComponent;
            InputText.supportRichText = false;
            InputText.color = Color.clear;
            Input.Component.customCaretColor = true;
            Input.Component.caretColor = Color.white;
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
            HighlightText.color = Color.white;
            HighlightText.supportRichText = true;
            HighlightText.fontSize = fontSize;

            // Set fonts
            InputText.font = UIManager.ConsoleFont;
            Input.PlaceholderText.font = UIManager.ConsoleFont;
            HighlightText.font = UIManager.ConsoleFont;

            RuntimeProvider.Instance.StartCoroutine(DelayedLayoutSetup());
        }

        private IEnumerator DelayedLayoutSetup()
        {
            yield return null;
            SetInputLayout();
        }

        public void SetInputLayout()
        {
            Input.Rect.offsetMin = new Vector2(52, Input.Rect.offsetMin.y);
            Input.Rect.offsetMax = new Vector2(2, Input.Rect.offsetMax.y);
        }
    }
}
