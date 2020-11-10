using System;
using System.Linq;
using System.Text;
using UnityExplorer.Input;
using UnityExplorer.Console.Lexer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.UI;
using UnityExplorer.UI.PageModel;
using System.Collections.Generic;
using System.Reflection;
#if CPP
using UnityExplorer.Unstrip;
using UnityExplorer.Helpers;
using UnhollowerRuntimeLib;
#endif

namespace UnityExplorer.Console
{
    // Handles most of the UI side of the C# console, including syntax highlighting.

    public class CodeEditor
    {
        public InputField InputField { get; internal set; }
        public Text InputText { get; internal set; }
        public int CurrentIndent { get; private set; }

        private Text inputHighlightText;
        private Image background;
        private Image scrollbar;

        public string HighlightedText => inputHighlightText.text;
        private readonly CSharpLexer highlightLexer;
        private readonly StringBuilder sbHighlight;

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

            // subscribe to text input changing
#if CPP
            InputField.onValueChanged.AddListener(new Action<string>((string s) => { OnInputChanged(s); }));
#else
            this.InputField.onValueChanged.AddListener((string s) => { OnInputChanged(s); });
#endif
        }

        public void Update()
        {
            // Check for new line
            if (ConsolePage.EnableAutoIndent && InputManager.GetKeyDown(KeyCode.Return))
            {
                AutoIndentCaret();
            }

            if (EventSystem.current?.currentSelectedGameObject?.name == "InputField")
            {
                bool focusKeyPressed = false;

                // Check for any focus key pressed
                foreach (KeyCode key in onFocusKeys)
                {
                    if (InputManager.GetKeyDown(key))
                    {
                        focusKeyPressed = true;
                        break;
                    }
                }

                if (focusKeyPressed || InputManager.GetMouseButton(0))
                {
                    ConsolePage.Instance.OnInputChanged();
                }
            }
        }

        public void OnInputChanged(string newInput, bool forceUpdate = false)
        {
            string newText = newInput;

            UpdateIndent(newInput);

            if (!forceUpdate && string.IsNullOrEmpty(newText))
            {
                inputHighlightText.text = string.Empty;
            }
            else
            {
                inputHighlightText.text = SyntaxHighlightContent(newText);
            }

            ConsolePage.Instance.OnInputChanged();
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

            for (int i = 0; i < caret && i < newText.Length; i++)
            {
                char character = newText[i];

                if (character == CSharpLexer.indentOpen)
                    CurrentIndent++;

                if (character == CSharpLexer.indentClose)
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
            topBarLabelLayout.preferredWidth = 800;
            topBarLabelLayout.flexibleWidth = 10;
            var topBarText = topBarLabel.GetComponent<Text>();
            topBarText.text = "C# Console";
            topBarText.fontSize = 20;

            // Enable Suggestions toggle

            var suggestToggleObj = UIFactory.CreateToggle(topBarObj, out Toggle suggestToggle, out Text suggestToggleText);
#if CPP
            suggestToggle.onValueChanged.AddListener(new Action<bool>(SuggestToggleCallback));
#else
            suggestToggle.onValueChanged.AddListener(SuggestToggleCallback);
#endif
            void SuggestToggleCallback(bool val)
            {
                ConsolePage.EnableAutocompletes = val;
                AutoCompleter.Update();
            }

            suggestToggleText.text = "Suggestions";
            suggestToggleText.alignment = TextAnchor.UpperLeft;
            var suggestTextPos = suggestToggleText.transform.localPosition;
            suggestTextPos.y = -14;
            suggestToggleText.transform.localPosition = suggestTextPos;

            var suggestLayout = suggestToggleObj.AddComponent<LayoutElement>();
            suggestLayout.minWidth = 120;
            suggestLayout.flexibleWidth = 0;

            var suggestRect = suggestToggleObj.transform.Find("Background");
            var suggestPos = suggestRect.localPosition;
            suggestPos.y = -14;
            suggestRect.localPosition = suggestPos;

            // Enable Auto-indent toggle

            var autoIndentToggleObj = UIFactory.CreateToggle(topBarObj, out Toggle autoIndentToggle, out Text autoIndentToggleText);
#if CPP
            autoIndentToggle.onValueChanged.AddListener(new Action<bool>(OnIndentChanged));
#else
            autoIndentToggle.onValueChanged.AddListener(OnIndentChanged);
#endif
            void OnIndentChanged(bool val) => ConsolePage.EnableAutoIndent = val;

            autoIndentToggleText.text = "Auto-indent";
            autoIndentToggleText.alignment = TextAnchor.UpperLeft;
            var autoIndentTextPos = autoIndentToggleText.transform.localPosition;
            autoIndentTextPos.y = -14;
            autoIndentToggleText.transform.localPosition = autoIndentTextPos;

            var autoIndentLayout = autoIndentToggleObj.AddComponent<LayoutElement>();
            autoIndentLayout.minWidth = 120;
            autoIndentLayout.flexibleWidth = 0;

            var autoIndentRect = autoIndentToggleObj.transform.Find("Background");
            suggestPos = autoIndentRect.localPosition;
            suggestPos.y = -14;
            autoIndentRect.localPosition = suggestPos;

            #endregion

            #region CONSOLE INPUT

            var consoleBase = UIFactory.CreateUIObject("CodeEditor", ConsolePage.Instance.Content);

            var consoleLayout = consoleBase.AddComponent<LayoutElement>();
            consoleLayout.preferredHeight = 500;
            consoleLayout.flexibleHeight = 50;

            consoleBase.AddComponent<RectMask2D>();

            var mainRect = consoleBase.GetComponent<RectTransform>();
            mainRect.pivot = Vector2.one * 0.5f;
            mainRect.anchorMin = Vector2.zero;
            mainRect.anchorMax = Vector2.one;
            mainRect.offsetMin = Vector2.zero;
            mainRect.offsetMax = Vector2.zero;

            var mainBg = UIFactory.CreateUIObject("MainBackground", consoleBase);

            var mainBgRect = mainBg.GetComponent<RectTransform>();
            mainBgRect.pivot = new Vector2(0, 1);
            mainBgRect.anchorMin = Vector2.zero;
            mainBgRect.anchorMax = Vector2.one;
            mainBgRect.offsetMin = Vector2.zero;
            mainBgRect.offsetMax = Vector2.zero;

            var mainBgImage = mainBg.AddGraphic<Image>();

            var inputObj = UIFactory.CreateInputField(consoleBase, 14, 0);

            var inputField = inputObj.GetComponent<InputField>();
            //inputField.richText = false;
            //inputField.restoreOriginalTextOnEscape = false;

            var inputRect = inputObj.GetComponent<RectTransform>();
            inputRect.pivot = new Vector2(0, 1);
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = Vector2.one;
            inputRect.offsetMin = new Vector2(20, 0);
            inputRect.offsetMax = new Vector2(14, 0);

            var mainTextObj = inputField.textComponent.gameObject;

            var mainTextInput = mainTextObj.GetComponent<Text>();

            var placeHolderText = inputField.placeholder.GetComponent<Text>();
            placeHolderText.text = STARTUP_TEXT;

            var highlightTextObj = UIFactory.CreateUIObject("HighlightText", mainTextObj.gameObject);
            var highlightTextRect = highlightTextObj.GetComponent<RectTransform>();
            highlightTextRect.pivot = new Vector2(0, 1);
            highlightTextRect.anchorMin = Vector2.zero;
            highlightTextRect.anchorMax = Vector2.one;
            highlightTextRect.offsetMin = new Vector2(20, 0);
            highlightTextRect.offsetMax = new Vector2(14, 0);

            var highlightTextInput = highlightTextObj.AddGraphic<Text>();
            highlightTextInput.supportRichText = true;

            var scroll = UIFactory.CreateScrollbar(consoleBase);

            var scrollRect = scroll.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(1, 0);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.pivot = new Vector2(0.5f, 1);
            scrollRect.offsetMin = new Vector2(-25f, 0);

            var scroller = scroll.GetComponent<Scrollbar>();
            scroller.direction = Scrollbar.Direction.TopToBottom;
            var scrollColors = scroller.colors;
            scrollColors.normalColor = new Color(0.6f, 0.6f, 0.6f, 1.0f);
            scroller.colors = scrollColors;

            var scrollImage = scroll.GetComponent<Image>();

            inputField.GetComponentInChildren<RectMask2D>().enabled = false;
            inputObj.GetComponent<Image>().enabled = false;

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
#if CPP
            compileButton.onClick.AddListener(new Action(CompileCallback));
#else
            compileButton.onClick.AddListener(CompileCallback);
#endif
            void CompileCallback()
            {
                if (!string.IsNullOrEmpty(inputField.text))
                {
                    ConsolePage.Instance.Evaluate(inputField.text.Trim());
                }
            }

            #endregion

            mainTextInput.supportRichText = false;

            mainTextInput.font = UIManager.ConsoleFont;
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
            this.background = mainBgImage;
            this.scrollbar = scrollImage;

            // set some colors
            InputField.caretColor = Color.white;
            InputText.color = new Color(1, 1, 1, 0.51f);
            inputHighlightText.color = Color.white;
            background.color = new Color32(37, 37, 37, 255);
            scrollbar.color = new Color32(45, 50, 50, 255);
        }
    }
}
