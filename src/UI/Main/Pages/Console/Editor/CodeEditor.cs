using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Text;
using System.Reflection;
using ExplorerBeta.Input;
using Explorer.UI.Main.Pages.Console.Lexer;
using ExplorerBeta;

namespace Explorer.UI.Main.Pages.Console
{
    public class CodeEditor
    {
        public CodeEditor(TMP_InputField inputField, TextMeshProUGUI inputText, TextMeshProUGUI inputHighlightText, TextMeshProUGUI lineText, 
            Image background, Image lineHighlight, Image lineNumberBackground, Image scrollbar)
        {
            this.InputField = inputField;
            this.inputText = inputText;
            this.inputHighlightText = inputHighlightText;
            this.lineText = lineText;
            this.background = background;
            this.lineHighlight = lineHighlight;
            this.lineNumberBackground = lineNumberBackground;
            this.scrollbar = scrollbar;

            var highlightTextRect = inputHighlightText.GetComponent<RectTransform>();
            highlightTextRect.anchorMin = Vector2.zero;
            highlightTextRect.anchorMax = Vector2.one;
            highlightTextRect.offsetMin = Vector2.zero;
            highlightTextRect.offsetMax = Vector2.zero;

            if (!AllReferencesAssigned())
            {
                throw new Exception("CodeEditor: Components are missing!");
            }

            this.inputTextTransform = inputText.GetComponent<RectTransform>();
            this.lineHighlightTransform = lineHighlight.GetComponent<RectTransform>();

            ApplyTheme();
            ApplyLanguage();

            // subscribe to text input changing
            InputField.onValueChanged.AddListener(new Action<string>((string s) => { Refresh(); }));
        }

        private static readonly KeyCode[] lineChangeKeys =
        { 
            KeyCode.Return, KeyCode.Backspace, KeyCode.UpArrow, 
            KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow 
        };
        
        private static readonly StringBuilder highlightedBuilder = new StringBuilder(4096);
        private static readonly StringBuilder lineBuilder = new StringBuilder();

        private readonly InputStringLexer lexer = new InputStringLexer();
        private readonly RectTransform inputTextTransform;
        private readonly RectTransform lineHighlightTransform;
        private string lastText;
        private bool lineHighlightLocked;

        public readonly TMP_InputField InputField;
        private readonly TextMeshProUGUI inputText;
        private readonly TextMeshProUGUI inputHighlightText;
        private readonly TextMeshProUGUI lineText;
        private readonly Image background;
        private readonly Image lineHighlight;
        private readonly Image lineNumberBackground;
        private readonly Image scrollbar;

        private bool lineNumbers = true;
        private int lineNumbersSize = 20;

        public int LineCount { get; private set; } = 0;
        public int CurrentLine { get; private set; } = 0;
        public int CurrentColumn { get; private set; } = 0;
        public int CurrentIndent { get; private set; } = 0;

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

        public string HighlightedText => inputHighlightText.text;

        public bool LineNumbers
        {
            get { return lineNumbers; }
            set
            {
                lineNumbers = value;

                //RectTransform inputFieldTransform = InputField.transform as RectTransform;
                //RectTransform lineNumberBackgroudTransform = lineNumberBackground.transform as RectTransform;

                //// Check for line numbers
                //if (lineNumbers == true)
                //{
                //    // Enable line numbers
                //    lineNumberBackground.gameObject.SetActive(true);
                //    lineText.gameObject.SetActive(true);

                //    // Set left value
                //    inputFieldTransform.offsetMin = new Vector2(lineNumbersSize, inputFieldTransform.offsetMin.y);
                //    lineNumberBackgroudTransform.sizeDelta = new Vector2(lineNumbersSize + 15, lineNumberBackgroudTransform.sizeDelta.y);
                //}
                //else
                //{
                //    // Disable line numbers
                //    lineNumberBackground.gameObject.SetActive(false);
                //    lineText.gameObject.SetActive(false);

                //    // Set left value
                //    inputFieldTransform.offsetMin = new Vector2(0, inputFieldTransform.offsetMin.y);
                //}
            }
        }

        // todo maybe not needed
        public int LineNumbersSize
        {
            get { return lineNumbersSize; }
            set
            {
                lineNumbersSize = value;

                // Update the line numbers
                LineNumbers = lineNumbers;
            }
        }

        public void Update()
        {
            // Auto indent
            if (AutoIndent.autoIndentMode != AutoIndent.IndentMode.None)
            {
                // Check for new line
                if (InputManager.GetKeyDown(KeyCode.Return))
                {
                    AutoIndentCaret();
                }
            }

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
                UpdateCurrentLineHighlight();
            }
        }

        public void Refresh(bool forceUpdate = false)
        {
            // Trigger a content change event
            DisplayedContentChanged(InputField.text, forceUpdate);
        }

        public void SetLineHighlight(int lineNumber, bool lockLineHighlight)
        {
            // Check if code editor is not active
            if (lineNumber < 1 || lineNumber > LineCount)
                return;

            //int lineOffset = 0;
            //int lineIndex = lineNumber - 1;

            // Highlight the current line
            lineHighlightTransform.anchoredPosition = new Vector2(5,
                (inputText.textInfo.lineInfo[inputText.textInfo.characterInfo[0].lineNumber].lineHeight *
                -(lineNumber - 1))  - 4f +
                inputTextTransform.anchoredPosition.y);

            // Lock the line highlight so it cannot be moved
            if (lockLineHighlight == true)
                LockLineHighlight();
            else
                UnlockLineHighlight();
        }

        public void LockLineHighlight()
        {
            lineHighlightLocked = true;
        }

        public void UnlockLineHighlight()
        {
            lineHighlightLocked = false;
        }

        private void DisplayedContentChanged(string newText, bool forceUpdate)
        {
            // Update caret position
            UpdateCurrentLineColumnIndent();

            // Check for change
            if ((!forceUpdate && lastText == newText) || string.IsNullOrEmpty(newText))
            {
                if (string.IsNullOrEmpty(newText))
                {
                    inputHighlightText.text = string.Empty;
                }

                // Its possible the text was cleared so we need to sync numbers and highlighter
                UpdateCurrentLineNumbers();
                UpdateCurrentLineHighlight();
                return;
            }

            inputHighlightText.text = SyntaxHighlightContent(newText);

            // Sync line numbers and update the line highlight
            UpdateCurrentLineNumbers();
            UpdateCurrentLineHighlight();

            this.lastText = newText;
        }

        private void UpdateCurrentLineNumbers()
        {
            // Get the line count
            int currentLineCount = inputText.textInfo.lineCount;

            int currentLineNumber = 1;

            // Check for a change in line
            if (currentLineCount != LineCount)
            {
                try
                {
                    // Update line numbers
                    lineBuilder.Length = 0;

                    // Build line numbers string
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

                    // Update displayed line numbers
                    lineText.text = lineBuilder.ToString();
                    LineCount = currentLineCount;
                }
                catch { }
            }
        }

        private void UpdateCurrentLineColumnIndent()
        {
            // Get the current line number
            int caret = InputField.caretPosition;
            
            if (caret < 0 || caret >= inputText.textInfo.characterInfo.Count)
            {
                while (caret >= 0 && caret >= inputText.textInfo.characterInfo.Count)
                    caret--;

                if (caret < 0 || caret >= inputText.textInfo.characterInfo.Count)
                {
                    return;
                }
            }

            CurrentLine = inputText.textInfo.characterInfo[caret].lineNumber;

            // Get the total character count
            int charCount = 0;
            for (int i = 0; i < CurrentLine; i++)
                charCount += inputText.textInfo.lineInfo[i].characterCount;

            // Get the column position
            CurrentColumn = caret - charCount;

            CurrentIndent = 0;

            // Check for auto indent allowed
            if (AutoIndent.allowAutoIndent)
            {
                for (int i = 0; i < caret && i < InputField.text.Length; i++)
                {
                    char character = InputField.text[i];

                    // Check for opening indents
                    if (character == AutoIndent.indentIncreaseCharacter)
                        CurrentIndent++;

                    // Check for closing indents
                    if (character == AutoIndent.indentDecreaseCharacter)
                        CurrentIndent--;
                }

                // Dont allow negative indents
                if (CurrentIndent < 0)
                    CurrentIndent = 0;
            }
        }

        private void UpdateCurrentLineHighlight()
        {
            if (lineHighlightLocked)
                return;

            try
            {
                // unity 2018.2 and older may need lineOffset as 0? not sure
                //int lineOffset = 1;

                int caret = InputField.caretPosition - 1;

                var lineHeight = inputText.textInfo.lineInfo[inputText.textInfo.characterInfo[0].lineNumber].lineHeight;
                var lineNumber = inputText.textInfo.characterInfo[caret].lineNumber;
                var offset = lineNumber + inputTextTransform.anchoredPosition.y;

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
            if (!InputTheme.allowSyntaxHighlighting)
                return inputText;

            int offset = 0;

            highlightedBuilder.Length = 0;

            foreach (var match in lexer.LexInputString(inputText))
            {
                // Copy text before the match
                for (int i = offset; i < match.startIndex; i++)
                    highlightedBuilder.Append(inputText[i]);

                // Add the opening color tag
                highlightedBuilder.Append(match.htmlColor);

                // Copy text inbetween the match boundaries
                for (int i = match.startIndex; i < match.endIndex; i++)
                    highlightedBuilder.Append(inputText[i]);

                // Add the closing color tag
                highlightedBuilder.Append(CLOSE_COLOR_TAG);

                // Update offset
                offset = match.endIndex;
            }

            // Copy remaining text
            for (int i = offset; i < inputText.Length; i++)
                highlightedBuilder.Append(inputText[i]);

            // Convert to string
            inputText = highlightedBuilder.ToString();

            return inputText;
        }

        // todo param is probably pointless
        private void AutoIndentCaret()
        {
            if (CurrentIndent > 0)
            {
                var indent = GetAutoIndentTab(CurrentIndent);

                if (indent.Length > 0)
                {
                    var caretPos = InputField.caretPosition;

                    var indentMinusOne = indent.Substring(0, indent.Length - 1);

                    // get last index of {
                    // check it on the next line if its not already
                    var text = InputField.text;
                    var sub = InputField.text.Substring(0, InputField.caretPosition);
                    var lastIndex = sub.LastIndexOf("{");
                    var offset = lastIndex - 1;
                    if (offset >= 0 && text[offset] != '\n' && text[offset] != '\t')
                    {
                        var open = "\n" + indentMinusOne;

                        InputField.text = text.Insert(offset + 1, open);

                        caretPos += open.Length;
                    }

                    // check if should add auto-close }
                    int numOpen = 0;
                    int numClose = 0;
                    char prevChar = default;
                    foreach (var _char in InputField.text)
                    {
                        if (_char == '{')
                        {
                            if (prevChar != default && (prevChar == '\\' || prevChar == '{'))
                            {
                                if (prevChar == '{')
                                    numOpen--;
                            }
                            else
                            {
                                numOpen++;
                            }
                        }
                        else if (_char == '}')
                        {
                            if (prevChar != default && (prevChar == '\\' || prevChar == '}'))
                            {
                                if (prevChar == '}')
                                    numClose--;
                            }
                            else
                            {
                                numClose++;
                            }
                        }
                        prevChar = _char;
                    }
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
            UpdateCurrentLineColumnIndent();

            inputText.text = InputField.text;
            inputText.SetText(InputField.text, true);
            inputText.Rebuild(CanvasUpdate.Prelayout);
            InputField.ForceLabelUpdate();
            InputField.Rebuild(CanvasUpdate.Prelayout);
            Refresh(true);
        }

        private string GetAutoIndentTab(int amount)
        {
            string tab = string.Empty;

            for (int i = 0; i < amount; i++)
                tab += "\t";

            return tab;
        }

        private void ApplyTheme()
        {
            // Check for missing references
            if (!AllReferencesAssigned())
                throw new Exception("Cannot apply theme because one or more required component references are missing. ");

            // Apply theme colors
            InputField.caretColor = InputTheme.caretColor;
            inputText.color = InputTheme.textColor;
            inputHighlightText.color = InputTheme.textColor;
            background.color = InputTheme.backgroundColor;
            lineHighlight.color = InputTheme.lineHighlightColor;
            lineNumberBackground.color = InputTheme.lineNumberBackgroundColor;
            lineText.color = InputTheme.lineNumberTextColor;
            scrollbar.color = InputTheme.scrollbarColor;
        }

        private void ApplyLanguage()
        {
            lexer.UseMatchers(CodeTheme.DelimiterSymbols, CodeTheme.Matchers);
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
