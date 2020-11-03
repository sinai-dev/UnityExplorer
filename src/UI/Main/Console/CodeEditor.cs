using System;
using System.Linq;
using System.Text;
using UnityExplorer.Input;
using UnityExplorer.UI.Main.Console.Lexer;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityExplorer.UI.Main.Console
{
    public class CodeEditor
    {
        private readonly InputLexer inputLexer = new InputLexer();

        public TMP_InputField InputField { get; }

        public readonly TextMeshProUGUI inputText;
        private readonly TextMeshProUGUI inputHighlightText;
        private readonly TextMeshProUGUI lineText;
        private readonly Image background;
        private readonly Image lineHighlight;
        private readonly Image lineNumberBackground;
        private readonly Image scrollbar;

        private bool lineHighlightLocked;
        private readonly RectTransform inputTextTransform;
        private readonly RectTransform lineHighlightTransform;

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

        public CodeEditor(TMP_InputField inputField, TextMeshProUGUI inputText, TextMeshProUGUI inputHighlightText, TextMeshProUGUI lineText,
            Image background, Image lineHighlight, Image lineNumberBackground, Image scrollbar)
        {
            InputField = inputField;
            this.inputText = inputText;
            this.inputHighlightText = inputHighlightText;
            this.lineText = lineText;
            this.background = background;
            this.lineHighlight = lineHighlight;
            this.lineNumberBackground = lineNumberBackground;
            this.scrollbar = scrollbar;

            if (!AllReferencesAssigned())
            {
                throw new Exception("References are missing!");
            }

            InputField.restoreOriginalTextOnEscape = false;

            inputTextTransform = inputText.GetComponent<RectTransform>();
            lineHighlightTransform = lineHighlight.GetComponent<RectTransform>();

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
                    UpdateHighlight();
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
            UpdateHighlight();

            ConsolePage.Instance.OnInputChanged();
        }

        public void SetLineHighlight(int lineNumber, bool lockLineHighlight)
        {
            if (lineNumber < 1 || lineNumber > LineCount)
            {
                return;
            }

            lineHighlightTransform.anchoredPosition = new Vector2(5,
                (inputText.textInfo.lineInfo[inputText.textInfo.characterInfo[0].lineNumber].lineHeight *
                -(lineNumber - 1)) - 4f +
                inputTextTransform.anchoredPosition.y);

            lineHighlightLocked = lockLineHighlight;
        }

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

        private void UpdateHighlight()
        {
            if (lineHighlightLocked)
            {
                return;
            }

            try
            {
                int caret = InputField.caretPosition - 1;

                float lineHeight = inputText.textInfo.lineInfo[inputText.textInfo.characterInfo[0].lineNumber].lineHeight;
                int lineNumber = inputText.textInfo.characterInfo[caret].lineNumber;
                float offset = lineNumber + inputTextTransform.anchoredPosition.y;

                lineHighlightTransform.anchoredPosition = new Vector2(5, -(offset * lineHeight));
            }
            catch //(Exception e)
            {
                //ExplorerCore.LogWarning("Exception on Update Line Highlight: " + e);
            }
        }

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
            lineHighlight.color = lineHighlightColor;
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
                !lineHighlight ||
                !lineNumberBackground ||
                !scrollbar)
            {
                // One or more references are not assigned
                return false;
            }
            return true;
        }
    }
}
