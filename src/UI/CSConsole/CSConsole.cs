using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
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
                Lexer = new LexerBuilder();

                ResetConsole(false);
                Evaluator.Compile("0 == 0");

                Panel.OnInputChanged += OnConsoleInputChanged;
                Panel.InputScroll.OnScroll += ForceOnContentChange;
                // TODO other panel listeners (buttons, etc)

            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning(ex);
            }
        }

        // Updating and event listeners

        private static readonly KeyCode[] onFocusKeys =
        {
            KeyCode.Return, KeyCode.Backspace, KeyCode.UpArrow,
            KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow
        };

        public static void Update()
        {
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

            // syntax highlight
            LexerHighlightAndSet(value);


            // todo update auto completes
            // ...
        }

        private static void LexerHighlightAndSet(string value)
        {
            int startLine = 0;
            int endLine = Input.TextGenerator.lineCount - 1;

            if (Input.Rect.rect.height > Panel.InputScroll.ViewportRect.rect.height)
            {
                // Not all text is displayed.
                // Only syntax highlight what we need to.

                int topLine = -1;
                int bottomLine = -1;
                var half = Input.Rect.rect.height * 0.5f;

                var top = Input.Rect.rect.height - Input.Rect.anchoredPosition.y;
                var bottom = top - Panel.InputScroll.ViewportRect.rect.height;

                for (int i = 0; i < Input.TextGenerator.lineCount; i++)
                {
                    var line = Input.TextGenerator.lines[i];
                    var pos = line.topY + half;

                    if (topLine == -1 && pos <= top)
                        topLine = i;

                    if ((pos - line.height) >= bottom)
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

            var sb = new StringBuilder();
            for (int i = 0; i < startLine; i++)
                sb.Append('\n');
            for (int i = startIdx; i <= endIdx; i++)
                sb.Append(value[i]);

            Panel.HighlightText.text = Lexer.SyntaxHighlight(sb.ToString());
        }


        // TODO indenting 

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

    }
}
