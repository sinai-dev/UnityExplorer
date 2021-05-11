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
using UnityExplorer.UI.Widgets.AutoComplete;

namespace UnityExplorer.UI.CSharpConsole
{
    public static class CSConsole
    {
        #region Strings / defaults

        internal const string STARTUP_TEXT = @"Welcome to the UnityExplorer C# Console.

The following helper methods are available:

* <color=#add490>Log(""message"")</color> logs a message to the debug console

* <color=#add490>StartCoroutine(IEnumerator routine)</color> start the IEnumerator as a UnityEngine.Coroutine

* <color=#add490>CurrentTarget()</color> returns the target of the active Inspector tab as System.Object

* <color=#add490>AllTargets()</color> returns a System.Object[] array containing the targets of all active tabs

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
        public static CSAutoCompleter Completer;

        private static HashSet<string> usingDirectives;
        private static StringBuilder evaluatorOutput;

        public static CSConsolePanel Panel => UIManager.CSharpConsole;
        public static InputFieldRef Input => Panel.Input;

        public static int LastCaretPosition { get; private set; }
        internal static float defaultInputFieldAlpha;

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
            Completer = new CSAutoCompleter();

            Panel.OnInputChanged += OnConsoleInputChanged;
            Panel.InputScroll.OnScroll += OnInputScrolled;
            Panel.OnCompileClicked += Evaluate;
            Panel.OnResetClicked += ResetConsole;
            Panel.OnAutoIndentToggled += OnToggleAutoIndent;
            Panel.OnCtrlRToggled += OnToggleCtrlRShortcut;
            Panel.OnSuggestionsToggled += OnToggleSuggestions;

        }

        // Updating and event listeners

        private static void OnInputScrolled() => HighlightVisibleInput(Input.Text);

        // Invoked at most once per frame
        private static void OnConsoleInputChanged(string value)
        {
            LastCaretPosition = Input.Component.caretPosition;

            if (EnableSuggestions)
                Completer.CheckAutocompletes();

            HighlightVisibleInput(value);
        }

        public static void Update()
        {
            int lastCaretPos = LastCaretPosition;
            UpdateCaret();
            bool caretMoved = lastCaretPos != LastCaretPosition;

            if (EnableSuggestions && caretMoved)
            {
                Completer.CheckAutocompletes();
            }

            //if (EnableAutoIndent && caretMoved)
            //    DoAutoIndent();

            if (EnableCtrlRShortcut
                && (InputManager.GetKey(KeyCode.LeftControl) || InputManager.GetKey(KeyCode.RightControl))
                && InputManager.GetKeyDown(KeyCode.R))
            {
                Evaluate(Panel.Input.Text);
            }
        }

        private static void UpdateCaret()
        {
            LastCaretPosition = Input.Component.caretPosition;

            // todo check if out of bounds, move content if so
        }


        #region Evaluating

        public static void ResetConsole() => ResetConsole(true);

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

        public static void Evaluate()
        {
            Evaluate(Input.Text);
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
            int startIdx = 0;
            int endIdx = value.Length - 1;
            int topLine = 0;

            // Calculate visible text if necessary
            if (Input.Rect.rect.height > Panel.InputScroll.ViewportRect.rect.height)
            {
                topLine = -1;
                int bottomLine = -1;

                // the top and bottom position of the viewport in relation to the text height
                // they need the half-height adjustment to normalize against the 'line.topY' value.
                var viewportMin = Input.Rect.rect.height - Input.Rect.anchoredPosition.y - (Input.Rect.rect.height * 0.5f);
                var viewportMax = viewportMin - Panel.InputScroll.ViewportRect.rect.height - (Input.Rect.rect.height * 0.5f);

                for (int i = 0; i < Input.TextGenerator.lineCount; i++)
                {
                    var line = Input.TextGenerator.lines[i];
                    // if not set the top line yet, and top of line is below the viewport top
                    if (topLine == -1 && line.topY <= viewportMin)
                        topLine = i;
                    // if bottom of line is below the viewport bottom
                    if ((line.topY - line.height) >= viewportMax)
                        bottomLine = i;
                }
                // make sure lines are valid
                topLine = Math.Max(0, topLine - 1);
                bottomLine = Math.Min(Input.TextGenerator.lineCount - 1, bottomLine + 1);

                startIdx = Input.TextGenerator.lines[topLine].startCharIdx;
                endIdx = bottomLine == Input.TextGenerator.lineCount
                    ? value.Length - 1
                    : (Input.TextGenerator.lines[bottomLine + 1].startCharIdx - 1);
            }


            // Highlight the visible text with the LexerBuilder
            Panel.HighlightText.text = Lexer.BuildHighlightedString(value, startIdx, endIdx, topLine);
        }

        #endregion


        #region Autocompletes

        public static void InsertSuggestionAtCaret(string suggestion)
        {
            string input = Input.Text;
            input = input.Insert(LastCaretPosition, suggestion);
            Input.Text = input;

            RuntimeProvider.Instance.StartCoroutine(SetAutocompleteCaret(LastCaretPosition += suggestion.Length));
        }

        private static IEnumerator SetAutocompleteCaret(int caretPosition)
        {
            var color = Input.Component.selectionColor;
            color.a = 0f;
            Input.Component.selectionColor = color;
            yield return null;

            EventSystem.current.SetSelectedGameObject(Panel.Input.UIRoot, null);
            yield return null;

            Input.Component.caretPosition = caretPosition;
            Input.Component.selectionFocusPosition = caretPosition;
            color.a = defaultInputFieldAlpha;
            Input.Component.selectionColor = color;
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


        #region UI Listeners and options

        private static void OnToggleAutoIndent(bool value)
        {
            // TODO
        }

        private static void OnToggleCtrlRShortcut(bool value)
        {
            // TODO
        }

        private static void OnToggleSuggestions(bool value)
        {
            // TODO
        }

        #endregion

    }
}
