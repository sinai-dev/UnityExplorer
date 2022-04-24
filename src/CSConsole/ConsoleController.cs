using HarmonyLib;
using Mono.CSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Widgets.AutoComplete;
using UniverseLib;
using UniverseLib.Input;
using UniverseLib.Runtime;
using UniverseLib.UI.Models;
using UniverseLib.Utility;

namespace UnityExplorer.CSConsole
{
    public static class ConsoleController
    {
        public static ScriptEvaluator Evaluator { get; private set; }
        public static LexerBuilder Lexer { get; private set; }
        public static CSAutoCompleter Completer { get; private set; }

        public static bool SRENotSupported { get; private set; }
        public static int LastCaretPosition { get; private set; }
        public static float DefaultInputFieldAlpha { get; set; }

        public static bool EnableCtrlRShortcut { get; private set; } = true;
        public static bool EnableAutoIndent { get; private set; } = true;
        public static bool EnableSuggestions { get; private set; } = true;

        public static CSConsolePanel Panel => UIManager.GetPanel<CSConsolePanel>(UIManager.Panels.CSConsole);
        public static InputFieldRef Input => Panel.Input;

        public static string ScriptsFolder => Path.Combine(ExplorerCore.ExplorerFolder, "Scripts");

        static HashSet<string> usingDirectives;
        static StringBuilder evaluatorOutput;
        static StringWriter evaluatorStringWriter;
        static float timeOfLastCtrlR;

        static bool settingCaretCoroutine;
        static string previousInput;
        static int previousContentLength = 0;

        static readonly string[] DefaultUsing = new string[]
        {
            "System",
            "System.Linq",
            "System.Text",
            "System.Collections",
            "System.Collections.Generic",
            "UnityEngine",
            "UniverseLib",
#if CPP
            "UnhollowerBaseLib",
            "UnhollowerRuntimeLib",
#endif
        };

        const int CSCONSOLE_LINEHEIGHT = 18;

        public static void Init()
        {
            try
            {
                ResetConsole(false);
                // ensure the compiler is supported (if this fails then SRE is probably stripped)
                Evaluator.Compile("0 == 0");
            }
            catch (Exception ex)
            {
                DisableConsole(ex);
                return;
            }

            // Setup console
            Lexer = new LexerBuilder();
            Completer = new CSAutoCompleter();

            SetupHelpInteraction();

            Panel.OnInputChanged += OnInputChanged;
            Panel.InputScroller.OnScroll += OnInputScrolled;
            Panel.OnCompileClicked += Evaluate;
            Panel.OnResetClicked += ResetConsole;
            Panel.OnHelpDropdownChanged += HelpSelected;
            Panel.OnAutoIndentToggled += OnToggleAutoIndent;
            Panel.OnCtrlRToggled += OnToggleCtrlRShortcut;
            Panel.OnSuggestionsToggled += OnToggleSuggestions;
            Panel.OnPanelResized += OnInputScrolled;

            // Run startup script
            try
            {
                if (!Directory.Exists(ScriptsFolder))
                    Directory.CreateDirectory(ScriptsFolder);

                string startupPath = Path.Combine(ScriptsFolder, "startup.cs");
                if (File.Exists(startupPath))
                {
                    ExplorerCore.Log($"Executing startup script from '{startupPath}'...");
                    string text = File.ReadAllText(startupPath);
                    Input.Text = text;
                    Evaluate();
                }
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception executing startup script: {ex}");
            }
        }


        #region Evaluating

        static void GenerateTextWriter()
        {
            evaluatorOutput = new StringBuilder();
            evaluatorStringWriter = new StringWriter(evaluatorOutput);
        }

        public static void ResetConsole() => ResetConsole(true);

        public static void ResetConsole(bool logSuccess = true)
        {
            if (SRENotSupported)
                return;

            if (Evaluator != null)
                Evaluator.Dispose();

            GenerateTextWriter();
            Evaluator = new ScriptEvaluator(evaluatorStringWriter)
            {
                InteractiveBaseClass = typeof(ScriptInteraction)
            };

            usingDirectives = new HashSet<string>();
            foreach (string use in DefaultUsing)
                AddUsing(use);

            if (logSuccess)
                ExplorerCore.Log($"C# Console reset");//. Using directives:\r\n{Evaluator.GetUsing()}");
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

            if (evaluatorStringWriter == null || evaluatorOutput == null)
            {
                GenerateTextWriter();
                Evaluator._textWriter = evaluatorStringWriter;
            }

            try
            {
                // Compile the code. If it returned a CompiledMethod, it is REPL.
                CompiledMethod repl = Evaluator.Compile(input);

                if (repl != null)
                {
                    // Valid REPL, we have a delegate to the evaluation.
                    try
                    {
                        object ret = null;
                        repl.Invoke(ref ret);
                        string result = ret?.ToString();
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
                    // The compiled code was not REPL, so it was a using directive or it defined classes.

                    string output = Evaluator._textWriter.ToString();
                    string[] outputSplit = output.Split('\n');
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


        #region Update loop and event listeners

        public static void Update()
        {
            if (SRENotSupported)
                return;

            if (InputManager.GetKeyDown(KeyCode.Home))
                JumpToStartOrEndOfLine(true);
            else if (InputManager.GetKeyDown(KeyCode.End))
                JumpToStartOrEndOfLine(false);

            UpdateCaret(out bool caretMoved);

            if (!settingCaretCoroutine && EnableSuggestions)
            {
                if (AutoCompleteModal.CheckEscape(Completer))
                {
                    OnAutocompleteEscaped();
                    return;
                }

                if (caretMoved)
                    AutoCompleteModal.Instance.ReleaseOwnership(Completer);
            }

            if (EnableCtrlRShortcut
                && (InputManager.GetKey(KeyCode.LeftControl) || InputManager.GetKey(KeyCode.RightControl))
                && InputManager.GetKeyDown(KeyCode.R)
                && timeOfLastCtrlR.OccuredEarlierThanDefault())
            {
                timeOfLastCtrlR = Time.realtimeSinceStartup;
                Evaluate(Panel.Input.Text);
            }
        }

        static void OnInputScrolled() => HighlightVisibleInput(out _);

        static void OnInputChanged(string value)
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
                OnAutocompleteEnter();

            if (!settingCaretCoroutine)
            {
                if (EnableAutoIndent)
                    DoAutoIndent();
            }

            HighlightVisibleInput(out bool inStringOrComment);

            if (!settingCaretCoroutine)
            {
                if (EnableSuggestions)
                {
                    if (inStringOrComment)
                        AutoCompleteModal.Instance.ReleaseOwnership(Completer);
                    else
                        Completer.CheckAutocompletes();
                }
            }

            UpdateCaret(out _);
        }

        static void OnToggleAutoIndent(bool value)
        {
            EnableAutoIndent = value;
        }

        static void OnToggleCtrlRShortcut(bool value)
        {
            EnableCtrlRShortcut = value;
        }

        static void OnToggleSuggestions(bool value)
        {
            EnableSuggestions = value;
        }

        #endregion


        #region Caret position

        static void UpdateCaret(out bool caretMoved)
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
                UICharInfo charInfo = Input.TextGenerator.characters[LastCaretPosition];
                float charTop = charInfo.cursorPos.y;
                float charBot = charTop - CSCONSOLE_LINEHEIGHT;

                float viewportMin = Input.Transform.rect.height - Input.Transform.anchoredPosition.y - (Input.Transform.rect.height * 0.5f);
                float viewportMax = viewportMin - Panel.InputScroller.ViewportRect.rect.height;

                float diff = 0f;
                if (charTop > viewportMin)
                    diff = charTop - viewportMin;
                else if (charBot < viewportMax)
                    diff = charBot - viewportMax;

                if (Math.Abs(diff) > 1)
                {
                    RectTransform rect = Input.Transform;
                    rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y - diff);
                }
            }
        }

        public static void SetCaretPosition(int caretPosition)
        {
            Input.Component.caretPosition = caretPosition;

            // Fix to make sure we always really set the caret position.
            // Yields a frame and fixes text-selection issues.
            settingCaretCoroutine = true;
            Input.Component.readOnly = true;
            RuntimeHelper.StartCoroutine(DoSetCaretCoroutine(caretPosition));
        }

        static IEnumerator DoSetCaretCoroutine(int caretPosition)
        {
            Color color = Input.Component.selectionColor;
            color.a = 0f;
            Input.Component.selectionColor = color;

            EventSystemHelper.SetSelectionGuard(false);
            Input.Component.Select();

            yield return null; // ~~~~~~~ YIELD FRAME ~~~~~~~~~

            Input.Component.caretPosition = caretPosition;
            Input.Component.selectionFocusPosition = caretPosition;
            LastCaretPosition = Input.Component.caretPosition;

            color.a = DefaultInputFieldAlpha;
            Input.Component.selectionColor = color;

            Input.Component.readOnly = false;
            settingCaretCoroutine = false;
        }

        // For Home and End keys
        static void JumpToStartOrEndOfLine(bool toStart)
        {
            // Determine the current and next line
            UILineInfo thisline = default;
            UILineInfo nextLine = default;
            for (int i = 0; i < Input.Component.cachedInputTextGenerator.lineCount; i++)
            {
                UILineInfo line = Input.Component.cachedInputTextGenerator.lines[i];

                if (line.startCharIdx > LastCaretPosition)
                {
                    nextLine = line;
                    break;
                }
                thisline = line;
            }

            if (toStart)
            {
                // Determine where the non-whitespace text begins
                int nonWhitespaceStartIdx = thisline.startCharIdx;
                while (char.IsWhiteSpace(Input.Text[nonWhitespaceStartIdx]))
                    nonWhitespaceStartIdx++;

                // Jump to either the true start or the non-whitespace position,
                // depending on which one we are not at.
                if (LastCaretPosition == nonWhitespaceStartIdx)
                    SetCaretPosition(thisline.startCharIdx);
                else // jump to the next line start index - 1, ie. end of this line
                    SetCaretPosition(nonWhitespaceStartIdx);
            }
            else
            {
                // If there is no next line, jump to the end of this line (+1, to the invisible next character position)
                if (nextLine.startCharIdx <= 0)
                    SetCaretPosition(Input.Text.Length);
                else
                    SetCaretPosition(nextLine.startCharIdx - 1);
            }
        }

        #endregion


        #region Lexer Highlighting

        /// <summary>
        /// Returns true if caret is inside string or comment, false otherwise
        /// </summary>
        private static void HighlightVisibleInput(out bool inStringOrComment)
        {
            inStringOrComment = false;
            if (string.IsNullOrEmpty(Input.Text))
            {
                Panel.HighlightText.text = "";
                Panel.LineNumberText.text = "1";
                return;
            }

            // Calculate the visible lines

            int topLine = -1;
            int bottomLine = -1;

            // the top and bottom position of the viewport in relation to the text height
            // they need the half-height adjustment to normalize against the 'line.topY' value.
            float viewportMin = Input.Transform.rect.height - Input.Transform.anchoredPosition.y - (Input.Transform.rect.height * 0.5f);
            float viewportMax = viewportMin - Panel.InputScroller.ViewportRect.rect.height;

            for (int i = 0; i < Input.TextGenerator.lineCount; i++)
            {
                UILineInfo line = Input.TextGenerator.lines[i];
                // if not set the top line yet, and top of line is below the viewport top
                if (topLine == -1 && line.topY <= viewportMin)
                    topLine = i;
                // if bottom of line is below the viewport bottom
                if ((line.topY - line.height) >= viewportMax)
                    bottomLine = i;
            }

            topLine = Math.Max(0, topLine - 1);
            bottomLine = Math.Min(Input.TextGenerator.lineCount - 1, bottomLine + 1);

            int startIdx = Input.TextGenerator.lines[topLine].startCharIdx;
            int endIdx = (bottomLine >= Input.TextGenerator.lineCount - 1)
                ? Input.Text.Length - 1
                : (Input.TextGenerator.lines[bottomLine + 1].startCharIdx - 1);


            // Highlight the visible text with the LexerBuilder

            Panel.HighlightText.text = Lexer.BuildHighlightedString(Input.Text, startIdx, endIdx, topLine, LastCaretPosition, out inStringOrComment);

            // Set the line numbers

            // determine true starting line number (not the same as the cached TextGenerator line numbers)
            int realStartLine = 0;
            for (int i = 0; i < startIdx; i++)
            {
                if (LexerBuilder.IsNewLine(Input.Text[i]))
                    realStartLine++;
            }
            realStartLine++;
            char lastPrev = '\n';

            StringBuilder sb = new();

            // append leading new lines for spacing (no point rendering line numbers we cant see)
            for (int i = 0; i < topLine; i++)
                sb.Append('\n');

            // append the displayed line numbers
            for (int i = topLine; i <= bottomLine; i++)
            {
                if (i > 0)
                    lastPrev = Input.Text[Input.TextGenerator.lines[i].startCharIdx - 1];

                // previous line ended with a newline character, this is an actual new line.
                if (LexerBuilder.IsNewLine(lastPrev))
                {
                    sb.Append(realStartLine.ToString());
                    realStartLine++;
                }

                sb.Append('\n');
            }

            Panel.LineNumberText.text = sb.ToString();

            return;
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

        private static void DoAutoIndent()
        {
            if (Input.Text.Length > previousContentLength)
            {
                int inc = Input.Text.Length - previousContentLength;

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

            previousContentLength = Input.Text.Length;
        }

        #endregion


        #region "Help" interaction

        private static void DisableConsole(Exception ex)
        {
            SRENotSupported = true;
            Input.Component.readOnly = true;
            Input.Component.textComponent.color = "5d8556".ToColor();

            if (ex is NotSupportedException)
            {
                Input.Text = $@"The C# Console has been disabled because System.Reflection.Emit threw a NotSupportedException.

Easy, dirty fix: (will likely break on game updates)
    * Download the corlibs for the game's Unity version from here: https://unity.bepinex.dev/corlibs/
    * Unzip and copy mscorlib.dll (and System.Reflection.Emit DLLs, if present) from the folder
    * Paste and overwrite the files into <Game>_Data/Managed/

With UnityDoorstop: (BepInEx only, or if you use UnityDoorstop + Standalone release):
    * Download the corlibs for the game's Unity version from here: https://unity.bepinex.dev/corlibs/
    * Unzip and copy mscorlib.dll (and System.Reflection.Emit DLLs, if present) from the folder
    * Find the folder which contains doorstop_config.ini (the game folder, or your r2modman/ThunderstoreModManager profile folder)
    * Make a subfolder called 'corlibs' inside this folder.
    * Paste the DLLs inside the corlibs folder.
    * In doorstop_config.ini, set 'dllSearchPathOverride=corlibs'.

Doorstop example:
- <Game>\
    - <Game>_Data\...
    - BepInEx\...
    - corlibs\
        - mscorlib.dll
    - doorstop_config.ini (with dllSearchPathOverride=corlibs)
    - <Game>.exe
    - winhttp.dll";
            }
            else
            {
                Input.Text = $"The C# Console has been disabled. {ex}";
            }
        }

        private static readonly Dictionary<string, string> helpDict = new();

        public static void SetupHelpInteraction()
        {
            Dropdown drop = Panel.HelpDropdown;

            helpDict.Add("Help", "");
            helpDict.Add("Usings", HELP_USINGS);
            helpDict.Add("REPL", HELP_REPL);
            helpDict.Add("Classes", HELP_CLASSES);
            helpDict.Add("Coroutines", HELP_COROUTINES);

            foreach (KeyValuePair<string, string> opt in helpDict)
                drop.options.Add(new Dropdown.OptionData(opt.Key));
        }

        public static void HelpSelected(int index)
        {
            if (index == 0)
                return;

            KeyValuePair<string, string> helpText = helpDict.ElementAt(index);

            Input.Text = helpText.Value;

            Panel.HelpDropdown.value = 0;
        }


        internal const string STARTUP_TEXT = @"<color=#5d8556>// Welcome to the UnityExplorer C# Console!

// It is recommended to use the Log panel (or a console log window) while using this tool.
// Use the Help dropdown to see detailed examples of how to use the console.

// To execute a script automatically on startup, put the script at 'sinai-dev-UnityExplorer\Scripts\startup.cs'</color>";

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
 * CurrentTarget;     - System.Object, the target of the active Inspector tab
 * AllTargets;        - System.Object[], the targets of all Inspector tabs
 * Log(obj);          - prints a message to the console log
 * Inspect(obj);      - inspect the object with the Inspector
 * Inspect(someType); - inspect a Type with static reflection
 * Start(enumerator); - Coroutine, starts the IEnumerator as a Coroutine, and returns the Coroutine.
 * Stop(coroutine);   - stop the Coroutine ONLY if it was started with Start(ienumerator).
 * Copy(obj);         - copies the object to the UnityExplorer Clipboard
 * Paste();           - System.Object, the contents of the Clipboard.
 * GetUsing();        - prints the current using directives to the console log
 * GetVars();         - prints the names and values of the REPL variables you have defined
 * GetClasses();      - prints the names and members of the classes you have defined
 * help;              - the default REPL help command, contains additional helpers.
*/";

        internal const string HELP_CLASSES = @"// Classes you compile will exist until the application closes.
// You can soft-overwrite a class by compiling it again with the same name. The old class will still technically exist in memory.

// Compiled classes can be accessed from both inside and outside this console.
// Note: in IL2CPP, you must declare a Namespace to inject these classes with ClassInjector or it will crash the game.

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

// To declare a coroutine, you will need to compile it separately. For example:
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
