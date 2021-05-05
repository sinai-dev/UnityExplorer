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

        public static CSConsolePanel Instance { get; private set; }

        public InputField InputField { get; private set; }
        public Text InputText { get; private set; }
        public Text HighlightText { get; private set; }

        public Action<string> OnInputChanged;
        private float m_timeOfLastInputInvoke;

        public Action OnResetClicked;
        public Action OnCompileClicked;
        public Action<bool> OnCtrlRToggled;
        public Action<bool> OnSuggestionsToggled;
        public Action<bool> OnAutoIndentToggled;

        private int m_lastCaretPosition;
        private int m_desiredCaretFix;
        private bool m_fixWaiting;
        private float m_defaultInputFieldAlpha;

        public void UseSuggestion(string suggestion)
        {
            string input = InputField.text;
            input = input.Insert(m_lastCaretPosition, suggestion);
            InputField.text = input;

            m_desiredCaretFix = m_lastCaretPosition += suggestion.Length;

            var color = InputField.selectionColor;
            color.a = 0f;
            InputField.selectionColor = color;
        }

        private void InvokeOnValueChanged(string value)
        {
            if (value.Length == UIManager.MAX_INPUTFIELD_CHARS)
                ExplorerCore.LogWarning($"Reached maximum InputField character length! ({UIManager.MAX_INPUTFIELD_CHARS})");

            if (m_timeOfLastInputInvoke.OccuredEarlierThanDefault())
                return;

            m_timeOfLastInputInvoke = Time.realtimeSinceStartup;
            OnInputChanged?.Invoke(value);
        }

        public override void Update()
        {
            base.Update();

            if (m_desiredCaretFix >= 0)
            {
                if (!m_fixWaiting)
                {
                    EventSystem.current.SetSelectedGameObject(InputField.gameObject, null);
                    m_fixWaiting = true;
                }
                else
                {
                    InputField.caretPosition = m_desiredCaretFix;
                    InputField.selectionFocusPosition = m_desiredCaretFix;
                    var color = InputField.selectionColor;
                    color.a = m_defaultInputFieldAlpha;
                    InputField.selectionColor = color;

                    m_fixWaiting = false;
                    m_desiredCaretFix = -1;
                }
            }
            else if (InputField.caretPosition > 0)
                m_lastCaretPosition = InputField.caretPosition;
        }

        // Saving

        public override void DoSaveToConfigElement()
        {
            ConfigManager.CSConsoleData.Value = this.ToSaveData();
        }

        public override string GetSaveData() => ConfigManager.CSConsoleData.Value;

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
            //Content = UIFactory.CreateVerticalGroup(MainMenu.Instance.PageViewport, "CSharpConsole", true, true, true, true);
            //UIFactory.SetLayoutElement(Content, preferredHeight: 500, flexibleHeight: 9000);

            #region TOP BAR 

            // Main group object

            var topBarObj = UIFactory.CreateHorizontalGroup(this.content, "TopBar", true, true, true, true, 10, new Vector4(8, 8, 30, 30),
                default, TextAnchor.LowerCenter);
            UIFactory.SetLayoutElement(topBarObj, minHeight: 50, flexibleHeight: 0);

            // Top label

            var topBarLabel = UIFactory.CreateLabel(topBarObj, "TopLabel", "C# Console", TextAnchor.MiddleLeft, default, true, 25);
            UIFactory.SetLayoutElement(topBarLabel.gameObject, preferredWidth: 150, flexibleWidth: 5000);

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
            autoIndentToggleText.text = "Auto-indent on Enter";
            AutoIndentToggle.onValueChanged.AddListener((bool val) => { OnAutoIndentToggled?.Invoke(val); });

            #endregion

            #region CONSOLE INPUT

            int fontSize = 16;

            //var inputObj = UIFactory.CreateSrollInputField(this.content, "ConsoleInput", CSConsoleManager.STARTUP_TEXT, 
            //    out InputFieldScroller consoleScroll, fontSize);

            var inputObj = UIFactory.CreateSrollInputField(this.content, "ConsoleInput", CSConsoleManager.STARTUP_TEXT, out var inputField, fontSize);
            InputField = inputField.InputField;
            m_defaultInputFieldAlpha = InputField.selectionColor.a;
            InputField.onValueChanged.AddListener(InvokeOnValueChanged);

            var placeHolderText = InputField.placeholder.GetComponent<Text>();
            placeHolderText.fontSize = fontSize;

            InputText = InputField.textComponent;
            InputText.supportRichText = false;
            InputText.color = new Color(1, 1, 1, 0.5f);

            var mainTextObj = InputText.gameObject;
            var highlightTextObj = UIFactory.CreateUIObject("HighlightText", mainTextObj.gameObject);
            var highlightTextRect = highlightTextObj.GetComponent<RectTransform>();
            highlightTextRect.pivot = new Vector2(0, 1);
            highlightTextRect.anchorMin = Vector2.zero;
            highlightTextRect.anchorMax = Vector2.one;
            highlightTextRect.offsetMin = new Vector2(20, 0);
            highlightTextRect.offsetMax = new Vector2(14, 0);

            HighlightText = highlightTextObj.AddComponent<Text>();
            HighlightText.supportRichText = true;
            HighlightText.fontSize = fontSize;

            #endregion

            #region COMPILE BUTTON BAR

            var horozGroupObj = UIFactory.CreateHorizontalGroup(this.content, "BigButtons", true, true, true, true, 0, new Vector4(2, 2, 2, 2),
                new Color(1, 1, 1, 0));

            var resetButton = UIFactory.CreateButton(horozGroupObj, "ResetButton", "Reset", new Color(0.33f, 0.33f, 0.33f));
            UIFactory.SetLayoutElement(resetButton.Button.gameObject, minHeight: 45, minWidth: 80, flexibleHeight: 0);
            resetButton.ButtonText.fontSize = 18;
            resetButton.OnClick += OnResetClicked;

            var compileButton = UIFactory.CreateButton(horozGroupObj, "CompileButton", "Compile", new Color(0.33f, 0.5f, 0.33f));
            UIFactory.SetLayoutElement(compileButton.Button.gameObject, minHeight: 45, minWidth: 80, flexibleHeight: 0);
            compileButton.ButtonText.fontSize = 18;
            compileButton.OnClick += OnCompileClicked;

            #endregion

            InputText.font = UIManager.ConsoleFont;
            placeHolderText.font = UIManager.ConsoleFont;
            HighlightText.font = UIManager.ConsoleFont;

            // reset this after formatting finalized
            highlightTextRect.anchorMin = Vector2.zero;
            highlightTextRect.anchorMax = Vector2.one;
            highlightTextRect.offsetMin = Vector2.zero;
            highlightTextRect.offsetMax = Vector2.zero;
        }
    }
}
