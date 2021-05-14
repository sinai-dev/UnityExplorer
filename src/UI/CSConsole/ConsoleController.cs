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
        public static ScriptEvaluator Evaluator;
        public static LexerBuilder Lexer;
        public static CSAutoCompleter Completer;

        private static HashSet<string> usingDirectives;
        private static StringBuilder evaluatorOutput;

        public static CSConsolePanel Panel => UIManager.GetPanel<CSConsolePanel>(UIManager.Panels.CSConsole);
        public static InputFieldRef Input => Panel.Input;

        public static int LastCaretPosition { get; private set; }
        internal static float defaultInputFieldAlpha;

        // Todo save as config?
        public static bool EnableCtrlRShortcut { get; private set; } = true;
        public static bool EnableAutoIndent { get; private set; } = true;
        public static bool EnableSuggestions { get; private set; } = true;

        internal static readonly string[] DefaultUsing = new string[]
        {
            "System",
            "System.Linq",
            "System.Text",
            "System.Collections.Generic",
            "UnityEngine",
#if CPP
            "UnhollowerBaseLib",
            "UnhollowerRuntimeLib",
#endif
        };

        public static void Init()
        {
            try
            {
                ResetConsole(false);
                // ensure the compiler is supported (if this fails then SRE is probably stubbed)
                Evaluator.Compile("0 == 0");
            }
            catch (Exception ex)
            {
                DisableConsole(ex);
                return;
            }

            Lexer = new LexerBuilder();
            Completer = new CSAutoCompleter();

            SetupHelpInteraction();

            Panel.OnInputChanged += OnInputChanged;
            Panel.InputScroll.OnScroll += OnInputScrolled;
            Panel.OnCompileClicked += Evaluate;
            Panel.OnResetClicked += ResetConsole;
            Panel.OnHelpDropdownChanged += HelpSelected;
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


        #region Evaluating

        public static void ResetConsole() => ResetConsole(true);

        public static void ResetConsole(bool logSuccess = true)
        {
            if (SRENotSupported)
                return;

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
            if (SRENotSupported)
                return;

            Evaluate(Input.Text);
        }

        public static void Evaluate(string input, bool supressLog = false)
        {
            if (SRENotSupported)
                return;

            try
            {
                // Try to "Compile" the code (tries to interpret it as REPL)
                var evaluation = Evaluator.Compile(input);
                if (evaluation != null)
                {
                    // Valid REPL, we have a delegate to the evaluation.
                    try
                    {
                        object ret = null;
                        evaluation.Invoke(ref ret);
                        var result = ret?.ToString();
                        if (!string.IsNullOrEmpty(result))
                            ExplorerCore.Log($"Invoked REPL, result: {ret}");
                        else
                            ExplorerCore.Log($"Invoked REPL (no return value)");
                    }
                    catch (Exception ex)
                    {
                        ExplorerCore.LogWarning($"Exception invoking REPL: {ex}");
                    }
                }
                else
                {
                    // The input was not recognized as an evaluation. Compile the code.

                    Evaluator.Run(input);

                    string output = ScriptEvaluator._textWriter.ToString();
                    var outputSplit = output.Split('\n');
                    if (outputSplit.Length >= 2)
                        output = outputSplit[outputSplit.Length - 2];
                    evaluatorOutput.Clear();

                    if (ScriptEvaluator._reportPrinter.ErrorsCount > 0)
                        throw new FormatException($"Unable to compile the code. Evaluator's last output was:\r\n{output}");
                    else if (!supressLog)
                        ExplorerCore.Log($"Code compiled without errors.");
                }
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


        // Updating and event listeners

        private static bool settingCaretCoroutine;

        private static void OnInputScrolled() => HighlightVisibleInput();

        private static string previousInput;

        // Invoked at most once per frame
        private static void OnInputChanged(string value)
        {
            if (SRENotSupported)
                return;

            // prevent escape wiping input
            if (InputManager.GetKeyDown(KeyCode.Escape))
            {
                Input.Text = previousInput;
                
                if (EnableSuggestions && AutoCompleteModal.CheckEscape(Completer))
                    OnAutocompleteEscaped();

                return;
            }

            previousInput = value;

            if (EnableSuggestions && AutoCompleteModal.CheckEnter(Completer))
            {
                OnAutocompleteEnter();
            }
            else if (!settingCaretCoroutine)
            {
                if (EnableSuggestions)
                    Completer.CheckAutocompletes();

                if (EnableAutoIndent)
                    DoAutoIndent();
            }

            HighlightVisibleInput();
        }

        public static void Update()
        {
            if (SRENotSupported)
                return;

            UpdateCaret(out bool caretMoved);

            if (!settingCaretCoroutine && EnableSuggestions && AutoCompleteModal.CheckEscape(Completer))
            {
                OnAutocompleteEscaped();
                return;
            }

            if (!settingCaretCoroutine && EnableSuggestions && caretMoved)
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

            // Override up/down arrow movement when autocompleting
            if (EnableSuggestions && AutoCompleteModal.CheckNavigation(Completer))
            {
                Input.Component.caretPosition = LastCaretPosition;
                return;
            }

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

        private static void SetCaretPosition(int caretPosition)
        {
            settingCaretCoroutine = true;
            RuntimeProvider.Instance.StartCoroutine(SetAutocompleteCaretCoro(caretPosition));
        }

        private static IEnumerator SetAutocompleteCaretCoro(int caretPosition)
        {
            var color = Input.Component.selectionColor;
            color.a = 0f;
            Input.Component.selectionColor = color;
            EventSystem.current.SetSelectedGameObject(null, null);
            yield return null;

            EventSystem.current.SetSelectedGameObject(Input.UIRoot, null);
            Input.Component.Select();
            yield return null;

            Input.Component.caretPosition = caretPosition;
            Input.Component.selectionFocusPosition = caretPosition;
            LastCaretPosition = Input.Component.caretPosition;

            color.a = defaultInputFieldAlpha;
            Input.Component.selectionColor = color;

            settingCaretCoroutine = false;
        }


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
            settingCaretCoroutine = true;
            Input.Text = Input.Text.Insert(LastCaretPosition, suggestion);

            SetCaretPosition(LastCaretPosition + suggestion.Length);
            LastCaretPosition = Input.Component.caretPosition;
        }

        private static void OnAutocompleteEnter()
        {
            // Remove the new line
            int lastIdx = Input.Component.caretPosition - 1;
            Input.Text = Input.Text.Remove(lastIdx, 1);

            // Use the selected suggestion
            Input.Component.caretPosition = LastCaretPosition;
            Completer.OnSuggestionClicked(AutoCompleteModal.SelectedSuggestion);
        }

        private static void OnAutocompleteEscaped()
        {
            AutoCompleteModal.Instance.ReleaseOwnership(Completer);
            SetCaretPosition(LastCaretPosition);
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


        #region "Help" interaction

        private static bool SRENotSupported;

        private static void DisableConsole(Exception ex)
        {
            SRENotSupported = true;
            Input.Component.readOnly = true;
            Input.Component.textComponent.color = "5d8556".ToColor();

            if (ex is NotSupportedException)
            {
                Input.Text = $@"The C# Console has been disabled because System.Reflection.Emit threw an exception: {ex.ReflectionExToString()}

If the game was built with Unity's stubbed netstandard 2.0 runtime, you can fix this with UnityDoorstop:
    * Download the Unity Editor version that the game uses
    * Navigate to the folder:
      - Editor\Data\PlaybackEngines\windowsstandalonesupport\Variations\mono\Managed
      - or, Editor\Data\MonoBleedingEdge\lib\mono\4.5
    * Copy the mscorlib.dll and System.Reflection.Emit DLLs from the folder
    * Make a subfolder in the folder that contains doorstop_config.ini
    * Put the DLLs inside the subfolder
    * Set the 'dllSearchPathOverride' in doorstop_config.ini to the subfolder name";
            }
            else
            {
                Input.Text = $@"The C# Console has been disabled because of an unknown error.
{ex}";
            }
        }

        private static readonly Dictionary<string, string> helpDict = new Dictionary<string, string>();

        public static void SetupHelpInteraction()
        {
            var drop = Panel.HelpDropdown;

            helpDict.Add("Help", "");
            helpDict.Add("Usings", HELP_USINGS);
            helpDict.Add("REPL", HELP_REPL);
            helpDict.Add("Classes", HELP_CLASSES);
            helpDict.Add("Coroutines", HELP_COROUTINES);

            foreach (var opt in helpDict)
                drop.options.Add(new Dropdown.OptionData(opt.Key));
        }

        public static void HelpSelected(int index)
        {
            if (index == 0)
                return;

            var helpText = helpDict.ElementAt(index);

            Input.Text = helpText.Value;

            Panel.HelpDropdown.value = 0;
        }


        internal const string STARTUP_TEXT = @"<color=#5d8556>// Welcome to the UnityExplorer C# Console!

// It is recommended to use the Log panel (or a console log window) while using this tool.
// Use the Help dropdown to see detailed examples of how to use the console.</color>";

        internal const string HELP_USINGS = @"// You can add a using directive to any namespace, but you must compile for it to take effect.
// It will remain in effect until you Reset the console.
using UnityEngine.UI;

// To see your current usings, use the ""GetUsing();"" helper.
// Note: You cannot add usings and evaluate REPL at the same time.";

        internal const string HELP_REPL = @"/* REPL (Read-Evaluate-Print-Loop) is a way to execute code immediately.
 * REPL code cannot contain any using directives or classes.
 * The return value of the last line of your REPL will be printed to the log.
 * Variables defined in REPL will exist until you Reset the console.
*/

// eg: This code would print 'Hello, World!', and then print 6 as the return value.
Log(""Hello, world!"");
var x = 5;
++x;

/* The following helpers are available in REPL mode:
 * GetUsing();        - prints the current using directives to the console log
 * GetVars();         - prints the names and values of the REPL variables you have defined
 * GetClasses();      - prints the names and members of the classes you have defined
 * Log(obj);          - prints a message to the console log
 * CurrentTarget;     - System.Object, the target of the active Inspector tab
 * AllTargets;        - System.Object[], the targets of all Inspector tabs
 * Inspect(obj);      - inspect the object with the Inspector
 * Inspect(someType); - inspect a Type with static reflection
 * Start(enumerator); - starts the IEnumerator as a Coroutine
 * help;              - the default REPL help command, contains additional helpers.
*/";

        internal const string HELP_CLASSES = @"// Classes you compile will exist until the application closes.
// You can soft-overwrite a class by compiling it again with the same name. The old class will still technically exist in memory.

// Compiled classes can be accessed from both inside and outside this console.
// Note: in IL2CPP, injecting these classes with ClassInjector may crash the game!

public class HelloWorld
{
    public static void Main()
    {
        UnityExplorer.ExplorerCore.Log(""Hello, world!"");
    }
}

// In REPL, you could call the example method above with ""HelloWorld.Main();""
// Note: The compiler does not allow you to run REPL code and define classes at the same time.

// In REPL, use the ""GetClasses();"" helper to see the classes you have defined since the last Reset.";

        internal const string HELP_COROUTINES = @"// To start a Coroutine directly, use ""Start(SomeCoroutine());"" in REPL mode.

// To define a coroutine, you will need to compile it seperately. For example:
public class MyCoro
{
    public static IEnumerator Main()
    {
        yield return null;
        UnityExplorer.ExplorerCore.Log(""Hello, world after one frame!"");
    }
}
// To run this Coroutine in REPL, it would look like ""Start(MyCoro.Main());""";

        #endregion
    }
}
