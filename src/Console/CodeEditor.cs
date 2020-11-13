using System;
using System.Linq;
using System.Text;
using UnityExplorer.Input;
using UnityExplorer.Console.Lexer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.UI;
using UnityExplorer.UI.Modules;
using System.Collections.Generic;
using System.Reflection;
using UnityExplorer.UI.Shared;
using UnityExplorer.Helpers;

namespace UnityExplorer.Console
{
    // Handles most of the UI side of the C# console, including syntax highlighting.

    public class CodeEditor
    {
        public InputField InputField { get; internal set; }
        public Text InputText { get; internal set; }
        public int CurrentIndent { get; private set; }

        public static bool EnableCtrlRShortcut { get; set; } = true;
        public static bool EnableAutoIndent { get; set; } = true;
        public static bool EnableAutocompletes { get; set; } = true;
        public static List<Suggestion> AutoCompletes = new List<Suggestion>();

        public string HighlightedText => inputHighlightText.text;
        private Text inputHighlightText;

        private readonly CSharpLexer highlightLexer;
        private readonly StringBuilder sbHighlight;

        internal int m_lastCaretPos;
        internal int m_fixCaretPos;
        internal bool m_fixwanted;
        internal float m_lastSelectAlpha;

        private static readonly KeyCode[] onFocusKeys =
        {
            KeyCode.Return, KeyCode.Backspace, KeyCode.UpArrow,
            KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow
        };

        internal const string STARTUP_TEXT = @"Welcome to the UnityExplorer C# Console.

The following helper methods are available:

* <color=#add490>Log(""message"")</color> logs a message to the debug console

* <color=#add490>CurrentTarget()</color> returns the currently inspected target on the Home page

* <color=#add490>AllTargets()</color> returns an object[] array containing all inspected instances

* <color=#add490>Inspect(someObject)</color> to inspect an instance, eg. Inspect(Camera.main);

* <color=#add490>Inspect(typeof(SomeClass))</color> to inspect a Class with static reflection

* <color=#add490>AddUsing(""SomeNamespace"")</color> adds a using directive to the C# console

* <color=#add490>GetUsing()</color> logs the current using directives to the debug console

* <color=#add490>Reset()</color> resets all using directives and variables
";

        public CodeEditor()
        {
            sbHighlight = new StringBuilder();
            highlightLexer = new CSharpLexer();

            ConstructUI();

            InputField.onValueChanged.AddListener((string s) => { OnInputChanged(s); });
        }

        public void Update()
        {
            if (EnableCtrlRShortcut)
            {
                if ((InputManager.GetKey(KeyCode.LeftControl) || InputManager.GetKey(KeyCode.RightControl))
                    && InputManager.GetKeyDown(KeyCode.R))
                {
                    var text = InputField.text.Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        ConsolePage.Instance.Evaluate(text);
                        return;
                    }
                }
            }

            if (EnableAutoIndent && InputManager.GetKeyDown(KeyCode.Return))
                AutoIndentCaret();

            if (EnableAutocompletes && InputField.isFocused)
            {
                if (InputManager.GetMouseButton(0) || onFocusKeys.Any(it => InputManager.GetKeyDown(it)))
                    UpdateAutocompletes();
            }

            if (m_fixCaretPos > 0)
            {
                if (!m_fixwanted)
                    m_fixwanted = true;
                else
                {
                    InputField.caretPosition = m_fixCaretPos;
                    InputField.selectionFocusPosition = m_fixCaretPos;

                    m_fixwanted = false;
                    m_fixCaretPos = -1;

                    var color = InputField.selectionColor;
                    color.a = m_lastSelectAlpha;
                    InputField.selectionColor = color;
                }
            }
            else if (InputField.caretPosition > 0)
            {
                m_lastCaretPos = InputField.caretPosition;
            }
        }

        internal void UpdateAutocompletes()
        {
            AutoCompleter.CheckAutocomplete();
            AutoCompleter.SetSuggestions(AutoCompletes.ToArray());
        }

        public void UseAutocomplete(string suggestion)
        {
            EventSystem.current.SetSelectedGameObject(ConsolePage.Instance.m_codeEditor.InputField.gameObject, null);

            string input = InputField.text;
            input = input.Insert(m_lastCaretPos, suggestion);
            InputField.text = input;

            m_fixCaretPos = m_lastCaretPos += suggestion.Length;

            var color = InputField.selectionColor;
            m_lastSelectAlpha = color.a;
            color.a = 0f;
            InputField.selectionColor = color;

            AutoCompleter.ClearAutocompletes();
        }

        public void OnInputChanged(string newInput, bool forceUpdate = false)
        {
            string newText = newInput;

            UpdateIndent(newInput);

            if (!forceUpdate && string.IsNullOrEmpty(newText))
                inputHighlightText.text = string.Empty;
            else
                inputHighlightText.text = SyntaxHighlightContent(newText);

            UpdateAutocompletes();
        }

        private void UpdateIndent(string newText)
        {
            int caret = InputField.caretPosition;

            int len = newText.Length;
            if (caret < 0 || caret >= len)
            {
                while (caret >= 0 && caret >= len)
                    caret--;

                if (caret < 0)
                    return;
            }

            CurrentIndent = 0;

            bool stringState = false;

            for (int i = 0; i < caret && i < newText.Length; i++)
            {
                char character = newText[i];

                if (character == '"')
                    stringState = !stringState;
                else if (!stringState && character == CSharpLexer.indentOpen)
                    CurrentIndent++;
                else if (!stringState && character == CSharpLexer.indentClose)
                    CurrentIndent--;
            }

            if (CurrentIndent < 0)
                CurrentIndent = 0;
        }

        private const string CLOSE_COLOR_TAG = "</color>";

        private string SyntaxHighlightContent(string inputText)
        {
            int offset = 0;

            sbHighlight.Length = 0;

            foreach (LexerMatchInfo match in highlightLexer.GetMatches(inputText))
            {
                for (int i = offset; i < match.startIndex; i++)
                {
                    sbHighlight.Append(inputText[i]);
                }

                sbHighlight.Append($"{match.htmlColor}");

                for (int i = match.startIndex; i < match.endIndex; i++)
                {
                    sbHighlight.Append(inputText[i]);
                }

                sbHighlight.Append(CLOSE_COLOR_TAG);

                offset = match.endIndex;
            }

            for (int i = offset; i < inputText.Length; i++)
            {
                sbHighlight.Append(inputText[i]);
            }

            inputText = sbHighlight.ToString();

            return inputText;
        }

        private void AutoIndentCaret()
        {
            if (CurrentIndent > 0)
            {
                string indent = GetAutoIndentTab(CurrentIndent);

                if (indent.Length > 0)
                {
                    int caretPos = InputField.caretPosition;

                    string indentMinusOne = indent.Substring(0, indent.Length - 1);

                    // get last index of {
                    // chuck it on the next line if its not already
                    string text = InputField.text;
                    string sub = InputField.text.Substring(0, InputField.caretPosition);
                    int lastIndex = sub.LastIndexOf("{");
                    int offset = lastIndex - 1;
                    if (offset >= 0 && text[offset] != '\n' && text[offset] != '\t')
                    {
                        string open = "\n" + indentMinusOne;

                        InputField.text = text.Insert(offset + 1, open);

                        caretPos += open.Length;
                    }

                    // check if should add auto-close }
                    int numOpen = InputField.text.Where(x => x == CSharpLexer.indentOpen).Count();
                    int numClose = InputField.text.Where(x => x == CSharpLexer.indentClose).Count();

                    if (numOpen > numClose)
                    {
                        // add auto-indent closing
                        indentMinusOne = $"\n{indentMinusOne}}}";
                        InputField.text = InputField.text.Insert(caretPos, indentMinusOne);
                    }

                    // insert the actual auto indent now
                    InputField.text = InputField.text.Insert(caretPos, indent);

                    //InputField.stringPosition = caretPos + indent.Length;
                    InputField.caretPosition = caretPos + indent.Length;
                }
            }

            // Update line column and indent positions
            UpdateIndent(InputField.text);

            InputText.text = InputField.text;
            //inputText.SetText(InputField.text, true);
            InputText.Rebuild(CanvasUpdate.Prelayout);
            InputField.ForceLabelUpdate();
            InputField.Rebuild(CanvasUpdate.Prelayout);

            OnInputChanged(InputText.text, true);
        }

        private string GetAutoIndentTab(int amount)
        {
            string tab = string.Empty;

            for (int i = 0; i < amount; i++)
            {
                tab += "\t";
            }

            return tab;
        }

        // ========== UI CONSTRUCTION =========== //

        public void ConstructUI()
        {
            ConsolePage.Instance.Content = UIFactory.CreateUIObject("C# Console", MainMenu.Instance.PageViewport);

            var mainLayout = ConsolePage.Instance.Content.AddComponent<LayoutElement>();
            mainLayout.preferredHeight = 9900;
            mainLayout.flexibleHeight = 9000;

            var mainGroup = ConsolePage.Instance.Content.AddComponent<VerticalLayoutGroup>();
            mainGroup.childControlHeight = true;
            mainGroup.childControlWidth = true;
            mainGroup.childForceExpandHeight = true;
            mainGroup.childForceExpandWidth = true;

            #region TOP BAR 

            // Main group object

            var topBarObj = UIFactory.CreateHorizontalGroup(ConsolePage.Instance.Content);
            LayoutElement topBarLayout = topBarObj.AddComponent<LayoutElement>();
            topBarLayout.minHeight = 50;
            topBarLayout.flexibleHeight = 0;

            var topBarGroup = topBarObj.GetComponent<HorizontalLayoutGroup>();
            topBarGroup.padding.left = 30;
            topBarGroup.padding.right = 30;
            topBarGroup.padding.top = 8;
            topBarGroup.padding.bottom = 8;
            topBarGroup.spacing = 10;
            topBarGroup.childForceExpandHeight = true;
            topBarGroup.childForceExpandWidth = true;
            topBarGroup.childControlWidth = true;
            topBarGroup.childControlHeight = true;
            topBarGroup.childAlignment = TextAnchor.LowerCenter;

            var topBarLabel = UIFactory.CreateLabel(topBarObj, TextAnchor.MiddleLeft);
            var topBarLabelLayout = topBarLabel.AddComponent<LayoutElement>();
            topBarLabelLayout.preferredWidth = 150;
            topBarLabelLayout.flexibleWidth = 5000;
            var topBarText = topBarLabel.GetComponent<Text>();
            topBarText.text = "C# Console";
            topBarText.fontSize = 20;

            // Enable Ctrl+R toggle

            var ctrlRToggleObj = UIFactory.CreateToggle(topBarObj, out Toggle ctrlRToggle, out Text ctrlRToggleText);
            ctrlRToggle.onValueChanged.AddListener(CtrlRToggleCallback);
            void CtrlRToggleCallback(bool val)
            {
                EnableCtrlRShortcut = val;
            }

            ctrlRToggleText.text = "Run on Ctrl+R";
            ctrlRToggleText.alignment = TextAnchor.UpperLeft;
            var ctrlRLayout = ctrlRToggleObj.AddComponent<LayoutElement>();
            ctrlRLayout.minWidth = 140;
            ctrlRLayout.flexibleWidth = 0;
            ctrlRLayout.minHeight = 25;

            // Enable Suggestions toggle

            var suggestToggleObj = UIFactory.CreateToggle(topBarObj, out Toggle suggestToggle, out Text suggestToggleText);
            suggestToggle.onValueChanged.AddListener(SuggestToggleCallback);
            void SuggestToggleCallback(bool val)
            {
                EnableAutocompletes = val;
                AutoCompleter.Update();
            }

            suggestToggleText.text = "Suggestions";
            suggestToggleText.alignment = TextAnchor.UpperLeft;
            var suggestLayout = suggestToggleObj.AddComponent<LayoutElement>();
            suggestLayout.minWidth = 120;
            suggestLayout.flexibleWidth = 0;
            suggestLayout.minHeight = 25;

            // Enable Auto-indent toggle

            var autoIndentToggleObj = UIFactory.CreateToggle(topBarObj, out Toggle autoIndentToggle, out Text autoIndentToggleText);
            autoIndentToggle.onValueChanged.AddListener(OnIndentChanged);
            void OnIndentChanged(bool val) => EnableAutoIndent = val;

            autoIndentToggleText.text = "Auto-indent";
            autoIndentToggleText.alignment = TextAnchor.UpperLeft;

            var autoIndentLayout = autoIndentToggleObj.AddComponent<LayoutElement>();
            autoIndentLayout.minWidth = 120;
            autoIndentLayout.flexibleWidth = 0;
            autoIndentLayout.minHeight = 25;

            #endregion

            #region CONSOLE INPUT

            int fontSize = 16;

            var inputObj = UIFactory.CreateSrollInputField(ConsolePage.Instance.Content, out InputFieldScroller consoleScroll, fontSize);

            var inputField = consoleScroll.inputField;

            var mainTextObj = inputField.textComponent.gameObject;
            var mainTextInput = inputField.textComponent;
            mainTextInput.supportRichText = false;
            mainTextInput.color = new Color(1, 1, 1, 0.5f);

            var placeHolderText = inputField.placeholder.GetComponent<Text>();
            placeHolderText.text = STARTUP_TEXT;
            placeHolderText.fontSize = fontSize;

            var highlightTextObj = UIFactory.CreateUIObject("HighlightText", mainTextObj.gameObject);
            var highlightTextRect = highlightTextObj.GetComponent<RectTransform>();
            highlightTextRect.pivot = new Vector2(0, 1);
            highlightTextRect.anchorMin = Vector2.zero;
            highlightTextRect.anchorMax = Vector2.one;
            highlightTextRect.offsetMin = new Vector2(20, 0);
            highlightTextRect.offsetMax = new Vector2(14, 0);

            var highlightTextInput = highlightTextObj.AddComponent<Text>();
            highlightTextInput.supportRichText = true;
            highlightTextInput.fontSize = fontSize;

            #endregion

            #region COMPILE BUTTON

            var compileBtnObj = UIFactory.CreateButton(ConsolePage.Instance.Content);
            var compileBtnLayout = compileBtnObj.AddComponent<LayoutElement>();
            compileBtnLayout.preferredWidth = 80;
            compileBtnLayout.flexibleWidth = 0;
            compileBtnLayout.minHeight = 45;
            compileBtnLayout.flexibleHeight = 0;
            var compileButton = compileBtnObj.GetComponent<Button>();
            var compileBtnColors = compileButton.colors;
            compileBtnColors.normalColor = new Color(14f / 255f, 80f / 255f, 14f / 255f);
            compileButton.colors = compileBtnColors;
            var btnText = compileBtnObj.GetComponentInChildren<Text>();
            btnText.text = "Run";
            btnText.fontSize = 18;
            btnText.color = Color.white;

            // Set compile button callback now that we have the Input Field reference
            compileButton.onClick.AddListener(CompileCallback);
            void CompileCallback()
            {
                if (!string.IsNullOrEmpty(inputField.text))
                {
                    ConsolePage.Instance.Evaluate(inputField.text.Trim());
                }
            }

            #endregion

            //mainTextInput.supportRichText = false;

            mainTextInput.font = UIManager.ConsoleFont;
            placeHolderText.font = UIManager.ConsoleFont;
            highlightTextInput.font = UIManager.ConsoleFont;

            // reset this after formatting finalized
            highlightTextRect.anchorMin = Vector2.zero;
            highlightTextRect.anchorMax = Vector2.one;
            highlightTextRect.offsetMin = Vector2.zero;
            highlightTextRect.offsetMax = Vector2.zero;

            // assign references

            this.InputField = inputField;

            this.InputText = mainTextInput;
            this.inputHighlightText = highlightTextInput;
        }
    }
}
