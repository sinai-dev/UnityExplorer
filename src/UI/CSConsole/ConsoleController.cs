using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.UI.CSConsole;
using UnityExplorer.Core.Input;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Widgets.AutoComplete;

namespace UnityExplorer.UI.CSConsole
{
    public static class ConsoleController
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

            Panel.OnInputChanged += OnInputChanged;
            Panel.InputScroll.OnScroll += OnInputScrolled;
            Panel.OnCompileClicked += Evaluate;
            Panel.OnResetClicked += ResetConsole;
            Panel.OnAutoIndentToggled += OnToggleAutoIndent;
            Panel.OnCtrlRToggled += OnToggleCtrlRShortcut;
            Panel.OnSuggestionsToggled += OnToggleSuggestions;

        }

        #region UI Listeners and options

        // TODO save

        private static void OnToggleAutoIndent(bool value)
        {
            EnableAutoIndent = value;
        }

        private static void OnToggleCtrlRShortcut(bool value)
        {
            EnableCtrlRShortcut = value;
        }

        private static void OnToggleSuggestions(bool value)
        {
            EnableSuggestions = value;
        }

        #endregion

        // Updating and event listeners

        private static bool settingAutoCompletion;

        private static void OnInputScrolled() => HighlightVisibleInput();

        // Invoked at most once per frame
        private static void OnInputChanged(string value)
        {
            if (!settingAutoCompletion && EnableSuggestions)
                Completer.CheckAutocompletes();

            if (!settingAutoCompletion && EnableAutoIndent)
                DoAutoIndent();

            HighlightVisibleInput();
        }

        public static void Update()
        {
            UpdateCaret(out bool caretMoved);

            if (!settingAutoCompletion && EnableSuggestions && caretMoved)
            {
                Completer.CheckAutocompletes();
            }

            if (EnableCtrlRShortcut
                && (InputManager.GetKey(KeyCode.LeftControl) || InputManager.GetKey(KeyCode.RightControl))
                && InputManager.GetKeyDown(KeyCode.R))
            {
                Evaluate(Panel.Input.Text);
            }
        }

        private const int CSCONSOLE_LINEHEIGHT = 18;

        private static void UpdateCaret(out bool caretMoved)
        {
            int prevCaret = LastCaretPosition;
            caretMoved = false;

            if (Input.Component.isFocused)
            {
                LastCaretPosition = Input.Component.caretPosition;
                caretMoved = LastCaretPosition != prevCaret;
            }

            if (Input.Text.Length == 0)
                return;

            // If caret moved, ensure caret is visible in the viewport
            if (caretMoved)
            {
                var charInfo = Input.TextGenerator.characters[LastCaretPosition];
                var charTop = charInfo.cursorPos.y;
                var charBot = charTop - CSCONSOLE_LINEHEIGHT;

                var viewportMin = Input.Rect.rect.height - Input.Rect.anchoredPosition.y - (Input.Rect.rect.height * 0.5f);
                var viewportMax = viewportMin - Panel.InputScroll.ViewportRect.rect.height;

                float diff = 0f;
                if (charTop > viewportMin)
                    diff = charTop - viewportMin;
                else if (charBot < viewportMax)
                    diff = charBot - viewportMax;

                if (Math.Abs(diff) > 1)
                {
                    var rect = Input.Rect;
                    rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y - diff);
                }
            }
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

        private static void HighlightVisibleInput()
        {
            int startIdx = 0;
            int endIdx = Input.Text.Length - 1;
            int topLine = 0;

            // Calculate visible text if necessary
            if (Input.Rect.rect.height > Panel.InputScroll.ViewportRect.rect.height)
            {
                topLine = -1;
                int bottomLine = -1;

                // the top and bottom position of the viewport in relation to the text height
                // they need the half-height adjustment to normalize against the 'line.topY' value.
                var viewportMin = Input.Rect.rect.height - Input.Rect.anchoredPosition.y - (Input.Rect.rect.height * 0.5f);
                var viewportMax = viewportMin - Panel.InputScroll.ViewportRect.rect.height;

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

                topLine = Math.Max(0, topLine - 1);
                bottomLine = Math.Min(Input.TextGenerator.lineCount - 1, bottomLine + 1);

                startIdx = Input.TextGenerator.lines[topLine].startCharIdx;
                endIdx = (bottomLine >= Input.TextGenerator.lineCount - 1)
                    ? Input.Text.Length - 1
                    : (Input.TextGenerator.lines[bottomLine + 1].startCharIdx - 1);
            }

            // Highlight the visible text with the LexerBuilder
            Panel.HighlightText.text = Lexer.BuildHighlightedString(Input.Text, startIdx, endIdx, topLine);
        }

        #endregion


        #region Autocompletes

        public static void InsertSuggestionAtCaret(string suggestion)
        {
            settingAutoCompletion = true;
            Input.Text = Input.Text.Insert(LastCaretPosition, suggestion);

            RuntimeProvider.Instance.StartCoroutine(SetAutocompleteCaret(LastCaretPosition + suggestion.Length));
            LastCaretPosition = Input.Component.caretPosition;
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
            LastCaretPosition = Input.Component.caretPosition;

            color.a = defaultInputFieldAlpha;
            Input.Component.selectionColor = color;

            settingAutoCompletion = false;
        }


        #endregion


        #region Auto indenting

        private static int prevContentLen = 0;

        private static void DoAutoIndent()
        {
            if (Input.Text.Length > prevContentLen)
            {
                int inc = Input.Text.Length - prevContentLen;

                if (inc == 1)
                {
                    int caret = Input.Component.caretPosition;
                    Input.Text = Lexer.IndentCharacter(Input.Text, ref caret);
                    Input.Component.caretPosition = caret;
                    LastCaretPosition = caret;
                }
                else
                {
                    // todo indenting for copy+pasted content

                    //ExplorerCore.Log("Content increased by " + inc);
                    //var comp = Input.Text.Substring(PreviousCaretPosition, inc);
                    //ExplorerCore.Log("composition string: " + comp);
                }
            }

            prevContentLen = Input.Text.Length;
        }

        #endregion



    }
}
