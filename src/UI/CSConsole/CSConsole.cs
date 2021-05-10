using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.Core.CSharp;
using UnityExplorer.Core.Input;
using UnityExplorer.UI.Panels;

namespace UnityExplorer.UI.CSharpConsole
{
    public static class CSConsole
    {
        #region Strings / defaults

        internal const string STARTUP_TEXT = @"Welcome to the UnityExplorer C# Console.

The following helper methods are available:

* <color=#add490>Log(""message"")</color> logs a message to the debug console

* <color=#add490>StartCoroutine(IEnumerator routine)</color> start the IEnumerator as a UnityEngine.Coroutine

* <color=#add490>CurrentTarget()</color> returns the currently inspected target on the Home page

* <color=#add490>AllTargets()</color> returns an object[] array containing all inspected instances

* <color=#add490>Inspect(someObject)</color> to inspect an instance, eg. Inspect(Camera.main);

* <color=#add490>Inspect(typeof(SomeClass))</color> to inspect a Class with static reflection

* <color=#add490>AddUsing(""SomeNamespace"")</color> adds a using directive to the C# console

* <color=#add490>GetUsing()</color> logs the current using directives to the debug console

* <color=#add490>Reset()</color> resets all using directives and variables
";

        internal static readonly string[] DefaultUsing = new string[]
        {
            "System",
            "System.Linq",
            "System.Collections",
            "System.Collections.Generic",
            "System.Reflection",
            "UnityEngine",
#if CPP
            "UnhollowerBaseLib",
            "UnhollowerRuntimeLib",
#endif
        };

        #endregion

        public static ScriptEvaluator Evaluator;
        public static LexerBuilder Lexer;

        private static StringBuilder evaluatorOutput;
        private static HashSet<string> usingDirectives;

        private static CSConsolePanel Panel => UIManager.CSharpConsole;
        private static InputFieldRef Input => Panel.Input;

        // Todo save as config?
        public static bool EnableCtrlRShortcut { get; private set; } = true;
        public static bool EnableAutoIndent { get; private set; } = true;
        public static bool EnableSuggestions { get; private set; } = true;

        public static void Init()
        {
            try
            {
                ResetConsole(false);
                Evaluator.Compile("0 == 0");
            }
            catch
            {
                ExplorerCore.LogWarning("C# Console probably not supported, todo");
                return;
            }

            Lexer = new LexerBuilder();

            Panel.OnInputChanged += OnConsoleInputChanged;
            Panel.InputScroll.OnScroll += ForceOnContentChange;
            // TODO other panel listeners (buttons, etc)
        }

        // Updating and event listeners

        private static readonly KeyCode[] onFocusKeys =
        {
            KeyCode.Return, KeyCode.Backspace, KeyCode.UpArrow,
            KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow
        };

        public static void Update()
        {
            UpdateCaret();

            if (EnableCtrlRShortcut)
            {
                if ((InputManager.GetKey(KeyCode.LeftControl) || InputManager.GetKey(KeyCode.RightControl))
                    && InputManager.GetKeyDown(KeyCode.R))
                {
                    var text = Panel.Input.Text.Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        Evaluate(text);
                        return;
                    }
                }
            }

            //if (EnableAutoIndent && InputManager.GetKeyDown(KeyCode.Return))
            //    DoAutoIndent();

            //if (EnableAutocompletes && InputField.isFocused)
            //{
            //    if (InputManager.GetMouseButton(0) || onFocusKeys.Any(it => InputManager.GetKeyDown(it)))
            //        UpdateAutocompletes();
            //}
        }

        private static void ForceOnContentChange()
        {
            OnConsoleInputChanged(Input.Text);
        }

        // Invoked at most once per frame
        private static void OnConsoleInputChanged(string value)
        {
            // todo auto indent? or only on enter?
            // todo update auto completes


            // syntax highlight
            HighlightVisibleInput(value);
        }

        private static void UpdateCaret()
        {
            LastCaretPosition = Input.InputField.caretPosition;

            // todo check if out of bounds
        }


        #region Evaluating console input

        public static void ResetConsole(bool logSuccess = true)
        {
            if (Evaluator != null)
                Evaluator.Dispose();

            evaluatorOutput = new StringBuilder();
            Evaluator = new ScriptEvaluator(new StringWriter(evaluatorOutput))
            {
                InteractiveBaseClass = typeof(ScriptInteraction)
            };

            usingDirectives = new HashSet<string>();
            foreach (var use in DefaultUsing)
                AddUsing(use);

            if (logSuccess)
                ExplorerCore.Log($"C# Console reset. Using directives:\r\n{Evaluator.GetUsing()}");
        }

        public static void AddUsing(string assemblyName)
        {
            if (!usingDirectives.Contains(assemblyName))
            {
                Evaluate($"using {assemblyName};", true);
                usingDirectives.Add(assemblyName);
            }
        }

        public static void Evaluate(string input, bool supressLog = false)
        {
            try
            {
                Evaluator.Run(input);

                string output = ScriptEvaluator._textWriter.ToString();
                var outputSplit = output.Split('\n');
                if (outputSplit.Length >= 2)
                    output = outputSplit[outputSplit.Length - 2];
                evaluatorOutput.Clear();

                if (ScriptEvaluator._reportPrinter.ErrorsCount > 0)
                    throw new FormatException($"Unable to compile the code. Evaluator's last output was:\r\n{output}");

                if (!supressLog)
                    ExplorerCore.Log("Code executed successfully.");
            }
            catch (FormatException fex)
            {
                if (!supressLog)
                    ExplorerCore.LogWarning(fex.Message);
            }
            catch (Exception ex)
            {
                if (!supressLog)
                    ExplorerCore.LogWarning(ex);
            }
        }

        #endregion


        #region Lexer Highlighting

        private static void HighlightVisibleInput(string value)
        {
            int startLine = 0;
            int endLine = Input.TextGenerator.lineCount - 1;

            // Calculate visible text if necessary
            if (Input.Rect.rect.height > Panel.InputScroll.ViewportRect.rect.height)
            {
                // This was mostly done through trial and error, it probably depends on the anchoring.
                int topLine = -1;
                int bottomLine = -1;
                var heightCorrection = Input.Rect.rect.height * 0.5f;

                var viewportMin = Input.Rect.rect.height - Input.Rect.anchoredPosition.y;
                var viewportMax = viewportMin - Panel.InputScroll.ViewportRect.rect.height;

                for (int i = 0; i < Input.TextGenerator.lineCount; i++)
                {
                    var line = Input.TextGenerator.lines[i];
                    var pos = line.topY + heightCorrection;

                    // if top of line is below the viewport top
                    if (topLine == -1 && pos <= viewportMin)
                        topLine = i;

                    // if bottom of line is below the viewport bottom
                    if ((pos - line.height) >= viewportMax)
                        bottomLine = i;
                }

                startLine = Math.Max(0, topLine - 1);
                endLine = Math.Min(Input.TextGenerator.lineCount - 1, bottomLine + 1);
            }

            int startIdx = Input.TextGenerator.lines[startLine].startCharIdx;
            int endIdx;
            if (endLine >= Input.TextGenerator.lineCount - 1)
                endIdx = value.Length - 1;
            else
                endIdx = Math.Min(value.Length - 1, Input.TextGenerator.lines[endLine + 1].startCharIdx);


            // Highlight the visible text with the LexerBuilder
            Panel.HighlightText.text = Lexer.BuildHighlightedString(value, startIdx, endIdx, startLine);
        }

        #endregion


        #region Autocompletes

        public static void UseSuggestion(string suggestion)
        {
            string input = Input.Text;
            input = input.Insert(LastCaretPosition, suggestion);
            Input.Text = input;

            RuntimeProvider.Instance.StartCoroutine(SetAutocompleteCaret(LastCaretPosition += suggestion.Length));
        }

        public static int LastCaretPosition { get; private set; }
        internal static float defaultInputFieldAlpha;

        private static IEnumerator SetAutocompleteCaret(int caretPosition)
        {
            var color = Input.InputField.selectionColor;
            color.a = 0f;
            Input.InputField.selectionColor = color;
            yield return null;

            EventSystem.current.SetSelectedGameObject(Panel.Input.UIRoot, null);
            yield return null;

            Input.InputField.caretPosition = caretPosition;
            Input.InputField.selectionFocusPosition = caretPosition;
            color.a = defaultInputFieldAlpha;
            Input.InputField.selectionColor = color;
        }

        #endregion


        // TODO indenting 
        #region AUTO INDENT TODO

        //private static void DoAutoIndent()
        //{
        //    int caret = Panel.LastCaretPosition;
        //    Panel.InputField.Text = Lexer.AutoIndentOnEnter(InputField.text, ref caret);
        //    InputField.caretPosition = caret;
        //    
        //    Panel.InputText.Rebuild(CanvasUpdate.Prelayout);
        //    InputField.ForceLabelUpdate();
        //    InputField.Rebuild(CanvasUpdate.Prelayout);
        //    
        //    OnConsoleInputChanged(InputField.text);
        //}


        // Auto-indenting

        //public int GetIndentLevel(string input, int toIndex)
        //{
        //    bool stringState = false;
        //    int indent = 0;
        //
        //    for (int i = 0; i < toIndex && i < input.Length; i++)
        //    {
        //        char character = input[i];
        //
        //        if (character == '"')
        //            stringState = !stringState;
        //        else if (!stringState && character == INDENT_OPEN)
        //            indent++;
        //        else if (!stringState && character == INDENT_CLOSE)
        //            indent--;
        //    }
        //
        //    if (indent < 0)
        //        indent = 0;
        //
        //    return indent;
        //}

        //// TODO not quite correct, but almost there.
        //
        //public string AutoIndentOnEnter(string input, ref int caretPos)
        //{
        //    var sb = new StringBuilder(input);
        //
        //    bool inString = false;
        //    bool inChar = false;
        //    int currentIndent = 0;
        //    int curLineIndent = 0;
        //    bool prevWasNewLine = true;
        //
        //    // process before caret position
        //    for (int i = 0; i < caretPos; i++)
        //    {
        //        char c = sb[i];
        //
        //        ExplorerCore.Log(i + ": " + c);
        //
        //        // update string/char state
        //        if (!inChar && c == '\"')
        //            inString = !inString;
        //        else if (!inString && c == '\'')
        //            inChar = !inChar;
        //
        //        // continue if inside string or char
        //        if (inString || inChar)
        //            continue;
        //
        //        // check for new line
        //        if (c == '\n')
        //        {
        //            ExplorerCore.Log("new line, resetting line counts");
        //            curLineIndent = 0;
        //            prevWasNewLine = true;
        //        }
        //        // check for indent
        //        else if (c == '\t' && prevWasNewLine)
        //        {
        //            ExplorerCore.Log("its a tab");
        //            if (curLineIndent > currentIndent)
        //            {
        //                ExplorerCore.Log("too many tabs, removing");
        //                // already reached the indent we should have
        //                sb.Remove(i, 1);
        //                i--;
        //                caretPos--;
        //                curLineIndent--;
        //            }
        //            else
        //                curLineIndent++;
        //        }
        //        // remove spaces on new lines
        //        else if (c == ' ' && prevWasNewLine)
        //        {
        //            ExplorerCore.Log("removing newline-space");
        //            sb.Remove(i, 1);
        //            i--;
        //            caretPos--;
        //        }
        //        else
        //        {
        //            if (c == INDENT_CLOSE)
        //                currentIndent--;
        //
        //            if (prevWasNewLine && curLineIndent < currentIndent)
        //            {
        //                ExplorerCore.Log("line is not indented enough");
        //                // line is not indented enough
        //                int diff = currentIndent - curLineIndent;
        //                sb.Insert(i, new string('\t', diff));
        //                caretPos += diff;
        //                i += diff;
        //            }
        //
        //            // check for brackets
        //            if ((c == INDENT_CLOSE || c == INDENT_OPEN) && !prevWasNewLine)
        //            {
        //                ExplorerCore.Log("bracket needs new line");
        //
        //                // need to put it on a new line
        //                sb.Insert(i, $"\n{new string('\t', currentIndent)}");
        //                caretPos += 1 + currentIndent;
        //                i += 1 + currentIndent;
        //            }
        //
        //            if (c == INDENT_OPEN)
        //                currentIndent++;
        //
        //            prevWasNewLine = false;
        //        }
        //    }
        //
        //    // todo put caret on new line after previous bracket if needed
        //    // indent caret to current indent
        //
        //    // process after caret position, make sure there are equal opened/closed brackets
        //    ExplorerCore.Log("-- after caret --");
        //    for (int i = caretPos; i < sb.Length; i++)
        //    {
        //        char c = sb[i];
        //        ExplorerCore.Log(i + ": " + c);
        //
        //        // update string/char state
        //        if (!inChar && c == '\"')
        //            inString = !inString;
        //        else if (!inString && c == '\'')
        //            inChar = !inChar;
        //
        //        if (inString || inChar)
        //            continue;
        //
        //        if (c == INDENT_OPEN)
        //            currentIndent++;
        //        else if (c == INDENT_CLOSE)
        //            currentIndent--;
        //    }
        //
        //    if (currentIndent > 0)
        //    {
        //        ExplorerCore.Log("there are not enough closing brackets, curIndent is " + currentIndent);
        //        // There are not enough close brackets
        //
        //        // TODO this should append in reverse indent order (small indents inserted first, then biggest).
        //        while (currentIndent > 0)
        //        {
        //            ExplorerCore.Log("Inserting closing bracket with " + currentIndent + " indent");
        //            // append the indented '}' on a new line
        //            sb.Insert(caretPos, $"\n{new string('\t', currentIndent - 1)}}}");
        //
        //            currentIndent--;
        //        }
        //
        //    }
        //    //else if (currentIndent < 0)
        //    //{
        //    //    // There are too many close brackets
        //    //
        //    //    // todo?
        //    //}
        //
        //    return sb.ToString();
        //}

        #endregion

    }
}
