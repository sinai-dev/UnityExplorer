using System;
using System.Linq;
using System.Text;
using UnityExplorer.Input;
using UnityExplorer.Console.Lexer;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.UI;
using UnityExplorer.UI.PageModel;
using System.Collections.Generic;
using System.Reflection;
#if CPP
using UnityExplorer.Unstrip.Resources;
using UnityExplorer.Helpers;
using UnhollowerRuntimeLib;
#endif

namespace UnityExplorer.Console
{
    public class CodeEditor
    {
        private readonly InputLexer inputLexer = new InputLexer();

        public TMP_InputField InputField { get; internal set; }

        public TextMeshProUGUI inputText;
        private TextMeshProUGUI inputHighlightText;
        private TextMeshProUGUI lineText;
        private Image background;
        private Image lineNumberBackground;
        private Image scrollbar;

        //private readonly RectTransform inputTextTransform;
        //private readonly RectTransform lineHighlightTransform;
        //private bool lineHighlightLocked;
        //private Image lineHighlight;

        public int LineCount { get; private set; }
        public int CurrentLine { get; private set; }
        public int CurrentColumn { get; private set; }
        public int CurrentIndent { get; private set; }

        private static readonly StringBuilder highlightedBuilder = new StringBuilder(4096);
        private static readonly StringBuilder lineBuilder = new StringBuilder();

        private static readonly KeyCode[] lineChangeKeys =
        {
            KeyCode.Return, KeyCode.Backspace, KeyCode.UpArrow,
            KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow
        };

        public string HighlightedText => inputHighlightText.text;

        public string Text
        {
            get { return InputField.text; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    InputField.text = value;
                    inputHighlightText.text = value;
                }
                else
                {
                    InputField.text = string.Empty;
                    inputHighlightText.text = string.Empty;
                }

                inputText.ForceMeshUpdate(false);
            }
        }

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
            ConstructUI();

            if (!AllReferencesAssigned())
            {
                throw new Exception("References are missing!");
            }

            //inputTextTransform = inputText.GetComponent<RectTransform>();
            //lineHighlightTransform = lineHighlight.GetComponent<RectTransform>();

            ApplyTheme();
            inputLexer.UseMatchers(CSharpLexer.DelimiterSymbols, CSharpLexer.Matchers);

            // subscribe to text input changing
#if CPP
            InputField.onValueChanged.AddListener(new Action<string>((string s) => { OnInputChanged(); }));
#else
            this.InputField.onValueChanged.AddListener((string s) => { OnInputChanged(); });
#endif
        }

        public void Update()
        {
            // Check for new line
            if (ConsolePage.EnableAutoIndent && InputManager.GetKeyDown(KeyCode.Return))
            {
                AutoIndentCaret();
            }

            if (EventSystem.current?.currentSelectedGameObject?.name == "InputField (TMP)")
            {
                bool focusKeyPressed = false;

                // Check for any focus key pressed
                foreach (KeyCode key in lineChangeKeys)
                {
                    if (InputManager.GetKeyDown(key))
                    {
                        focusKeyPressed = true;
                        break;
                    }
                }

                // Update line highlight
                if (focusKeyPressed || InputManager.GetMouseButton(0))
                {
                    //UpdateHighlight();
                    ConsolePage.Instance.OnInputChanged();
                }
            }
        }

        public void OnInputChanged(bool forceUpdate = false)
        {
            string newText = InputField.text;

            UpdateIndent();

            if (!forceUpdate && string.IsNullOrEmpty(newText))
            {
                inputHighlightText.text = string.Empty;
            }
            else
            {
                inputHighlightText.text = SyntaxHighlightContent(newText);
            }

            UpdateLineNumbers();
            //UpdateHighlight();

            ConsolePage.Instance.OnInputChanged();
        }

        //public void SetLineHighlight(int lineNumber, bool lockLineHighlight)
        //{
        //    if (lineNumber < 1 || lineNumber > LineCount)
        //    {
        //        return;
        //    }

        //    lineHighlightTransform.anchoredPosition = new Vector2(5,
        //        (inputText.textInfo.lineInfo[inputText.textInfo.characterInfo[0].lineNumber].lineHeight *
        //        //-(lineNumber - 1)) - 4f +
        //        -(lineNumber - 1)) +
        //        inputTextTransform.anchoredPosition.y);

        //    lineHighlightLocked = lockLineHighlight;
        //}

        private void UpdateLineNumbers()
        {
            int currentLineCount = inputText.textInfo.lineCount;

            int currentLineNumber = 1;

            if (currentLineCount != LineCount)
            {
                try
                {
                    lineBuilder.Length = 0;

                    for (int i = 1; i < currentLineCount + 2; i++)
                    {
                        if (i - 1 > 0 && i - 1 < currentLineCount - 1)
                        {
                            int characterStart = inputText.textInfo.lineInfo[i - 1].firstCharacterIndex;
                            int characterCount = inputText.textInfo.lineInfo[i - 1].characterCount;

                            if (characterStart >= 0 && characterStart < inputText.text.Length &&
                                characterCount != 0 && !inputText.text.Substring(characterStart, characterCount).Contains("\n"))
                            {
                                lineBuilder.Append("\n");
                                continue;
                            }
                        }

                        lineBuilder.Append(currentLineNumber);
                        lineBuilder.Append('\n');

                        currentLineNumber++;

                        if (i - 1 == 0 && i - 1 < currentLineCount - 1)
                        {
                            int characterStart = inputText.textInfo.lineInfo[i - 1].firstCharacterIndex;
                            int characterCount = inputText.textInfo.lineInfo[i - 1].characterCount;

                            if (characterStart >= 0 && characterStart < inputText.text.Length &&
                                characterCount != 0 && !inputText.text.Substring(characterStart, characterCount).Contains("\n"))
                            {
                                lineBuilder.Append("\n");
                                continue;
                            }
                        }
                    }

                    lineText.text = lineBuilder.ToString();
                    LineCount = currentLineCount;
                }
                catch { }
            }
        }

        private void UpdateIndent()
        {
            int caret = InputField.caretPosition;

            if (caret < 0 || caret >= inputText.textInfo.characterInfo.Length)
            {
                while (caret >= 0 && caret >= inputText.textInfo.characterInfo.Length)
                {
                    caret--;
                }

                if (caret < 0 || caret >= inputText.textInfo.characterInfo.Length)
                {
                    return;
                }
            }

            CurrentLine = inputText.textInfo.characterInfo[caret].lineNumber;

            int charCount = 0;
            for (int i = 0; i < CurrentLine; i++)
            {
                charCount += inputText.textInfo.lineInfo[i].characterCount;
            }

            CurrentColumn = caret - charCount;
            CurrentIndent = 0;

            for (int i = 0; i < caret && i < InputField.text.Length; i++)
            {
                char character = InputField.text[i];

                if (character == CSharpLexer.indentIncreaseCharacter)
                {
                    CurrentIndent++;
                }

                if (character == CSharpLexer.indentDecreaseCharacter)
                {
                    CurrentIndent--;
                }
            }

            if (CurrentIndent < 0)
            {
                CurrentIndent = 0;
            }
        }

        //private void UpdateHighlight()
        //{
        //    if (lineHighlightLocked)
        //    {
        //        return;
        //    }

        //    try
        //    {
        //        int caret = InputField.caretPosition - 1;

        //        float lineHeight = inputText.textInfo.lineInfo[inputText.textInfo.characterInfo[0].lineNumber].lineHeight;
        //        int lineNumber = inputText.textInfo.characterInfo[caret].lineNumber;
        //        float offset = lineNumber + inputTextTransform.anchoredPosition.y;

        //        lineHighlightTransform.anchoredPosition = new Vector2(5, -(offset * lineHeight));
        //    }
        //    catch //(Exception e)
        //    {
        //        //ExplorerCore.LogWarning("Exception on Update Line Highlight: " + e);
        //    }
        //}

        private const string CLOSE_COLOR_TAG = "</color>";

        private string SyntaxHighlightContent(string inputText)
        {
            int offset = 0;

            highlightedBuilder.Length = 0;

            foreach (LexerMatchInfo match in inputLexer.LexInputString(inputText))
            {
                for (int i = offset; i < match.startIndex; i++)
                {
                    highlightedBuilder.Append(inputText[i]);
                }

                highlightedBuilder.Append(match.htmlColor);

                for (int i = match.startIndex; i < match.endIndex; i++)
                {
                    highlightedBuilder.Append(inputText[i]);
                }

                highlightedBuilder.Append(CLOSE_COLOR_TAG);

                offset = match.endIndex;
            }

            for (int i = offset; i < inputText.Length; i++)
            {
                highlightedBuilder.Append(inputText[i]);
            }

            inputText = highlightedBuilder.ToString();

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
                    int numOpen = InputField.text.Where(x => x == CSharpLexer.indentIncreaseCharacter).Count();
                    int numClose = InputField.text.Where(x => x == CSharpLexer.indentDecreaseCharacter).Count();

                    if (numOpen > numClose)
                    {
                        // add auto-indent closing
                        indentMinusOne = $"\n{indentMinusOne}}}";
                        InputField.text = InputField.text.Insert(caretPos, indentMinusOne);
                    }

                    // insert the actual auto indent now
                    InputField.text = InputField.text.Insert(caretPos, indent);

                    InputField.stringPosition = caretPos + indent.Length;
                }
            }

            // Update line column and indent positions
            UpdateIndent();

            inputText.text = InputField.text;
            inputText.SetText(InputField.text, true);
            inputText.Rebuild(CanvasUpdate.Prelayout);
            InputField.ForceLabelUpdate();
            InputField.Rebuild(CanvasUpdate.Prelayout);

            OnInputChanged(true);
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

        // ============== Theme ============== //

        private static Color caretColor = new Color32(255, 255, 255, 255);
        private static Color textColor = new Color32(255, 255, 255, 255);
        private static Color backgroundColor = new Color32(37, 37, 37, 255);
        private static Color lineHighlightColor = new Color32(50, 50, 50, 255);
        private static Color lineNumberBackgroundColor = new Color32(25, 25, 25, 255);
        private static Color lineNumberTextColor = new Color32(180, 180, 180, 255);
        private static Color scrollbarColor = new Color32(45, 50, 50, 255);

        private void ApplyTheme()
        {
            var highlightTextRect = inputHighlightText.GetComponent<RectTransform>();
            highlightTextRect.anchorMin = Vector2.zero;
            highlightTextRect.anchorMax = Vector2.one;
            highlightTextRect.offsetMin = Vector2.zero;
            highlightTextRect.offsetMax = Vector2.zero;

            InputField.caretColor = caretColor;
            inputText.color = textColor;
            inputHighlightText.color = textColor;
            background.color = backgroundColor;
            //lineHighlight.color = lineHighlightColor;
            lineNumberBackground.color = lineNumberBackgroundColor;
            lineText.color = lineNumberTextColor;
            scrollbar.color = scrollbarColor;
        }

        private bool AllReferencesAssigned()
        {
            if (!InputField ||
                !inputText ||
                !inputHighlightText ||
                !lineText ||
                !background ||
                //!lineHighlight ||
                !lineNumberBackground ||
                !scrollbar)
            {
                // One or more references are not assigned
                return false;
            }
            return true;
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

            var mainBgImage = mainBg.AddComponent<Image>();

            //var lineHighlight = UIFactory.CreateUIObject("LineHighlight", consoleBase);

            //var lineHighlightRect = lineHighlight.GetComponent<RectTransform>();
            //lineHighlightRect.pivot = new Vector2(0.5f, 1);
            //lineHighlightRect.anchorMin = new Vector2(0, 1);
            //lineHighlightRect.anchorMax = new Vector2(1, 1);
            //lineHighlightRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 21);

            //var lineHighlightImage = lineHighlight.GetComponent<Image>();
            //if (!lineHighlightImage)
            //{
            //    lineHighlightImage = lineHighlight.AddComponent<Image>();
            //}

            var linesBg = UIFactory.CreateUIObject("LinesBackground", consoleBase);
            var linesBgRect = linesBg.GetComponent<RectTransform>();
            linesBgRect.anchorMin = Vector2.zero;
            linesBgRect.anchorMax = new Vector2(0, 1);
            linesBgRect.offsetMin = new Vector2(-17.5f, 0);
            linesBgRect.offsetMax = new Vector2(17.5f, 0);
            linesBgRect.sizeDelta = new Vector2(65, 0);

            var linesBgImage = linesBg.AddComponent<Image>();

            var inputObj = UIFactory.CreateTMPInput(consoleBase);

            var inputField = inputObj.GetComponent<TMP_InputField>();
            inputField.richText = false;
            inputField.restoreOriginalTextOnEscape = false;

            var inputRect = inputObj.GetComponent<RectTransform>();
            inputRect.pivot = new Vector2(0.5f, 0.5f);
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = new Vector2(0.92f, 1);
            inputRect.offsetMin = new Vector2(20, 0);
            inputRect.offsetMax = new Vector2(14, 0);
            inputRect.anchoredPosition = new Vector2(40, 0);

            var textAreaObj = inputObj.transform.Find("TextArea");
            var textAreaRect = textAreaObj.GetComponent<RectTransform>();
            textAreaRect.pivot = new Vector2(0.5f, 0.5f);
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;

            var mainTextObj = textAreaObj.transform.Find("Text");
            var mainTextRect = mainTextObj.GetComponent<RectTransform>();
            mainTextRect.pivot = new Vector2(0.5f, 0.5f);
            mainTextRect.anchorMin = Vector2.zero;
            mainTextRect.anchorMax = Vector2.one;
            mainTextRect.offsetMin = Vector2.zero;
            mainTextRect.offsetMax = Vector2.zero;

            var mainTextInput = mainTextObj.GetComponent<TextMeshProUGUI>();
            //mainTextInput.fontSize = 18;

            var placeHolderText = textAreaObj.transform.Find("Placeholder").GetComponent<TextMeshProUGUI>();
            placeHolderText.text = CodeEditor.STARTUP_TEXT;

            var linesTextObj = UIFactory.CreateUIObject("LinesText", mainTextObj.gameObject);
            var linesTextRect = linesTextObj.GetComponent<RectTransform>();

            var linesTextInput = linesTextObj.AddComponent<TextMeshProUGUI>();
            linesTextInput.fontSize = 18;

            var highlightTextObj = UIFactory.CreateUIObject("HighlightText", mainTextObj.gameObject);
            var highlightTextRect = highlightTextObj.GetComponent<RectTransform>();
            highlightTextRect.anchorMin = Vector2.zero;
            highlightTextRect.anchorMax = Vector2.one;
            highlightTextRect.offsetMin = Vector2.zero;
            highlightTextRect.offsetMax = Vector2.zero;

            var highlightTextInput = highlightTextObj.AddComponent<TextMeshProUGUI>();
            //highlightTextInput.fontSize = 18;

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

            var tmpInput = inputObj.GetComponent<TMP_InputField>();
            tmpInput.scrollSensitivity = 15;
            tmpInput.verticalScrollbar = scroller;

            // set lines text anchors here after UI is fleshed out
            linesTextRect.pivot = Vector2.zero;
            linesTextRect.anchorMin = new Vector2(0, 0);
            linesTextRect.anchorMax = new Vector2(1, 1);
            linesTextRect.offsetMin = Vector2.zero;
            linesTextRect.offsetMax = Vector2.zero;
            linesTextRect.anchoredPosition = new Vector2(-40, 0);

            tmpInput.GetComponentInChildren<RectMask2D>().enabled = false;
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
                if (!string.IsNullOrEmpty(tmpInput.text))
                {
                    ConsolePage.Instance.Evaluate(tmpInput.text.Trim());
                }
            }

            #endregion

            #region FONT

            TMP_FontAsset fontToUse = UIManager.ConsoleFont;
            if (fontToUse == null)
            {
#if CPP
                UnityEngine.Object[] fonts = ResourcesUnstrip.FindObjectsOfTypeAll(Il2CppType.Of<TMP_FontAsset>());
                foreach (UnityEngine.Object font in fonts)
                {
                    TMP_FontAsset fontCast = font.Il2CppCast(typeof(TMP_FontAsset)) as TMP_FontAsset;

                    if (fontCast.name.Contains("LiberationSans"))
                    {
                        fontToUse = fontCast;
                        break;
                    }
                }
#else
                var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                foreach (var font in fonts)
                {
                    if (font.name.Contains("LiberationSans"))
                    {
                        fontToUse = font;
                        break;
                    }
                }
#endif
            }

            if (fontToUse != null)
            {
                UnityEngine.TextCore.FaceInfo faceInfo = fontToUse.faceInfo;
                fontToUse.tabSize = 10;
                faceInfo.tabWidth = 10;
#if CPP
                fontToUse.faceInfo = faceInfo;
#else
                typeof(TMP_FontAsset)
                    .GetField("m_FaceInfo", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(fontToUse, faceInfo);
#endif

                tmpInput.fontAsset = fontToUse;

                mainTextInput.font = fontToUse;
                mainTextInput.fontSize = 18;

                highlightTextInput.font = fontToUse;
                highlightTextInput.fontSize = 18;
            }
            #endregion

            // assign references

            this.InputField = inputField;

            this.inputText = mainTextInput;
            this.inputHighlightText = highlightTextInput;
            this.lineText = linesTextInput;
            this.background = mainBgImage;
            //this.lineHighlight = lineHighlightImage;
            this.lineNumberBackground = linesBgImage;
            this.scrollbar = scrollImage;
        }
    }
}
