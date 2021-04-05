using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityExplorer.Core.CSharp;
using System.Linq;
using UnityExplorer.Core.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.UI.Main.CSConsole;
using UnityExplorer.Core;
using UnityExplorer.Core.Unity;
using UnityExplorer.UI.Utility;
#if CPP
using UnityExplorer.Core.Runtime.Il2Cpp;
#endif

namespace UnityExplorer.UI.Main.CSConsole
{
    public class CSharpConsole : BaseMenuPage
    {
        public override string Name => "C# Console";

        public static CSharpConsole Instance { get; private set; }

        public ScriptEvaluator Evaluator;
        internal StringBuilder m_evalLogBuilder;

        public static List<string> UsingDirectives;

        public static readonly string[] DefaultUsing = new string[]
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

        public override bool Init()
        {
            Instance = this;

            try
            {
                InitConsole();

                AutoCompleter.Init();
#if MONO
                DummyBehaviour.Setup();
#endif

                ResetConsole(false);
                // Make sure compiler is supported on this platform
                Evaluator.Compile("new object();");

                return true;
            }
            catch (Exception e)
            {
                string info = "The C# Console has been disabled because";
                if (e is NotSupportedException && e.TargetSite?.Name == "DefineDynamicAssembly")
                    info += " Reflection.Emit is not supported.";
                else
                    info += $" of an unknown error.\r\n({e.ReflectionExToString()})";

                ExplorerCore.LogWarning(info);

                this.RefNavbarButton.GetComponentInChildren<Text>().text += " (disabled)";

                return false;
            }
        }

        public void ResetConsole(bool log = true)
        {
            if (Evaluator != null)
                Evaluator.Dispose();

            m_evalLogBuilder = new StringBuilder();

            Evaluator = new ScriptEvaluator(new StringWriter(m_evalLogBuilder)) { InteractiveBaseClass = typeof(ScriptInteraction) };

            UsingDirectives = new List<string>();

            foreach (string use in DefaultUsing)
                AddUsing(use);

            if (log)
                ExplorerCore.Log($"C# Console reset. Using directives:\r\n{Evaluator.GetUsing()}");
        }

        public override void Update()
        {
            UpdateConsole();

            AutoCompleter.Update();
#if CPP
            Il2CppCoroutine.Process();
#endif
        }

        public void AddUsing(string asm)
        {
            if (!UsingDirectives.Contains(asm))
            {
                Evaluate($"using {asm};", true);
                UsingDirectives.Add(asm);
            }
        }

        public void Evaluate(string code, bool supressLog = false)
        {
            try
            {
                Evaluator.Run(code);

                string output = ScriptEvaluator._textWriter.ToString();
                var outputSplit = output.Split('\n');
                if (outputSplit.Length >= 2)
                    output = outputSplit[outputSplit.Length - 2];
                m_evalLogBuilder.Clear();

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

        // =================================================================================================

        // UI stuff

        public InputField InputField { get; internal set; }
        public Text InputText { get; internal set; }
        public int CurrentIndent { get; private set; }

        public static bool EnableCtrlRShortcut { get; set; } = true;
        public static bool EnableAutoIndent { get; set; } = true;
        public static bool EnableAutocompletes { get; set; } = true;
        public static List<Suggestion> AutoCompletes = new List<Suggestion>();

        public string HighlightedText => inputHighlightText.text;
        private Text inputHighlightText;

        private CSLexerHighlighter highlightLexer;

        internal int m_lastCaretPos;
        internal int m_fixCaretPos;
        internal bool m_fixwanted;
        internal float m_lastSelectAlpha;

        private static readonly KeyCode[] onFocusKeys =
        {
            KeyCode.Return, KeyCode.Backspace, KeyCode.UpArrow,
            KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow
        };

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

        public void InitConsole()
        {
            highlightLexer = new CSLexerHighlighter();

            ConstructUI();

            InputField.onValueChanged.AddListener((string s) => { OnInputChanged(s); });
        }

        internal static bool IsUserCopyPasting()
        {
            return (InputManager.GetKey(KeyCode.LeftControl) || InputManager.GetKey(KeyCode.RightControl))
                && InputManager.GetKeyDown(KeyCode.V);
        }

        public void UpdateConsole()
        {
            if (s_copyPasteBuffer != null)
            {
                if (!IsUserCopyPasting())
                {
                    OnInputChanged(s_copyPasteBuffer);

                    s_copyPasteBuffer = null;
                }
            }

            if (EnableCtrlRShortcut)
            {
                if ((InputManager.GetKey(KeyCode.LeftControl) || InputManager.GetKey(KeyCode.RightControl))
                    && InputManager.GetKeyDown(KeyCode.R))
                {
                    var text = InputField.text.Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        Evaluate(text);
                        return;
                    }
                }
            }

            if (EnableAutoIndent && InputManager.GetKeyDown(KeyCode.Return))
                AutoIndentCaret();

            if (EnableAutocompletes && InputField.isFocused)
            {
                if (InputManager.GetMouseButton(0) || onFocusKeys.Any(it => InputManager.GetKeyDown(it)))
                    UpdateAutocompletes();
            }

            if (m_fixCaretPos > 0)
            {
                if (!m_fixwanted)
                {
                    EventSystem.current.SetSelectedGameObject(InputField.gameObject, null);
                    m_fixwanted = true;
                }
                else
                {
                    InputField.caretPosition = m_fixCaretPos;
                    InputField.selectionFocusPosition = m_fixCaretPos;

                    m_fixwanted = false;
                    m_fixCaretPos = -1;

                    var color = InputField.selectionColor;
                    color.a = m_lastSelectAlpha;
                    InputField.selectionColor = color;
                }
            }
            else if (InputField.caretPosition > 0)
            {
                m_lastCaretPos = InputField.caretPosition;
            }
        }

        internal void UpdateAutocompletes()
        {
            AutoCompleter.CheckAutocomplete();
            AutoCompleter.SetSuggestions(AutoCompletes.ToArray());
        }

        public void UseAutocomplete(string suggestion)
        {
            string input = InputField.text;
            input = input.Insert(m_lastCaretPos, suggestion);
            InputField.text = input;

            m_fixCaretPos = m_lastCaretPos += suggestion.Length;

            var color = InputField.selectionColor;
            m_lastSelectAlpha = color.a;
            color.a = 0f;
            InputField.selectionColor = color;

            AutoCompleter.ClearAutocompletes();
        }

        internal static string s_copyPasteBuffer;

        public void OnInputChanged(string newText, bool forceUpdate = false)
        {
            if (IsUserCopyPasting())
            {
                //Console.WriteLine("Copy+Paste detected!");
                s_copyPasteBuffer = newText;
                return;
            }

            if (EnableAutoIndent)
                UpdateIndent(newText);

            if (!forceUpdate && string.IsNullOrEmpty(newText))
                inputHighlightText.text = string.Empty;
            else
                inputHighlightText.text = SyntaxHighlightContent(newText);

            if (EnableAutocompletes)
                UpdateAutocompletes();
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

            bool stringState = false;

            for (int i = 0; i < caret && i < newText.Length; i++)
            {
                char character = newText[i];

                if (character == '"')
                    stringState = !stringState;
                else if (!stringState && character == CSLexerHighlighter.indentOpen)
                    CurrentIndent++;
                else if (!stringState && character == CSLexerHighlighter.indentClose)
                    CurrentIndent--;
            }

            if (CurrentIndent < 0)
                CurrentIndent = 0;
        }

        private const string CLOSE_COLOR_TAG = "</color>";

        private string SyntaxHighlightContent(string inputText)
        {
            int offset = 0;

            //Console.WriteLine("Highlighting input text:\r\n" + inputText);

            string ret = "";

            foreach (var match in highlightLexer.GetMatches(inputText))
            {
                for (int i = offset; i < match.startIndex; i++)
                    ret += inputText[i];

                ret += $"{match.htmlColor}";

                for (int i = match.startIndex; i < match.endIndex; i++)
                    ret += inputText[i];

                ret += CLOSE_COLOR_TAG;

                offset = match.endIndex;
            }

            for (int i = offset; i < inputText.Length; i++)
                ret += inputText[i];

            return ret;
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
                    int numOpen = InputField.text.Where(x => x == CSLexerHighlighter.indentOpen).Count();
                    int numClose = InputField.text.Where(x => x == CSLexerHighlighter.indentClose).Count();

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
            Content = UIFactory.CreateVerticalGroup(MainMenu.Instance.PageViewport, "CSharpConsole", true, true, true, true);
            UIFactory.SetLayoutElement(Content, preferredHeight: 500, flexibleHeight: 9000);

            #region TOP BAR 

            // Main group object

            var topBarObj = UIFactory.CreateHorizontalGroup(Content, "TopBar", true, true, true, true, 10, new Vector4(8, 8, 30, 30), 
                default, TextAnchor.LowerCenter);
            UIFactory.SetLayoutElement(topBarObj, minHeight: 50, flexibleHeight: 0);

            // Top label

            var topBarLabel = UIFactory.CreateLabel(topBarObj, "TopLabel", "C# Console", TextAnchor.MiddleLeft, default, true, 25);
            UIFactory.SetLayoutElement(topBarLabel.gameObject, preferredWidth: 150, flexibleWidth: 5000);

            // Enable Ctrl+R toggle

            var ctrlRToggleObj = UIFactory.CreateToggle(topBarObj, "CtrlRToggle", out Toggle ctrlRToggle, out Text ctrlRToggleText);
            ctrlRToggle.onValueChanged.AddListener((bool val) => { EnableCtrlRShortcut = val; });

            ctrlRToggleText.text = "Run on Ctrl+R";
            ctrlRToggleText.alignment = TextAnchor.UpperLeft;
            UIFactory.SetLayoutElement(ctrlRToggleObj, minWidth: 140, flexibleWidth: 0, minHeight: 25);

            // Enable Suggestions toggle

            var suggestToggleObj = UIFactory.CreateToggle(topBarObj, "SuggestionToggle", out Toggle suggestToggle, out Text suggestToggleText);
            suggestToggle.onValueChanged.AddListener((bool val) =>
            {
                EnableAutocompletes = val;
                AutoCompleter.Update();
            });

            suggestToggleText.text = "Suggestions";
            suggestToggleText.alignment = TextAnchor.UpperLeft;

            UIFactory.SetLayoutElement(suggestToggleObj, minWidth: 120, flexibleWidth: 0, minHeight: 25);

            // Enable Auto-indent toggle

            var autoIndentToggleObj = UIFactory.CreateToggle(topBarObj, "IndentToggle", out Toggle autoIndentToggle, out Text autoIndentToggleText);
            autoIndentToggle.onValueChanged.AddListener((bool val) => EnableAutoIndent = val);

            autoIndentToggleText.text = "Auto-indent on Enter";
            autoIndentToggleText.alignment = TextAnchor.UpperLeft;

            UIFactory.SetLayoutElement(autoIndentToggleObj, minWidth: 180, flexibleWidth: 0, minHeight: 25);

            #endregion

            #region CONSOLE INPUT

            int fontSize = 16;

            var inputObj = UIFactory.CreateSrollInputField(Content, "ConsoleInput", STARTUP_TEXT, out InputFieldScroller consoleScroll, fontSize);

            var inputField = consoleScroll.inputField;

            var mainTextObj = inputField.textComponent.gameObject;
            var mainTextInput = inputField.textComponent;
            mainTextInput.supportRichText = false;
            mainTextInput.color = new Color(1, 1, 1, 0.5f);

            var placeHolderText = inputField.placeholder.GetComponent<Text>();
            placeHolderText.fontSize = fontSize;

            var highlightTextObj = UIFactory.CreateUIObject("HighlightText", mainTextObj.gameObject);
            var highlightTextRect = highlightTextObj.GetComponent<RectTransform>();
            highlightTextRect.pivot = new Vector2(0, 1);
            highlightTextRect.anchorMin = Vector2.zero;
            highlightTextRect.anchorMax = Vector2.one;
            highlightTextRect.offsetMin = new Vector2(20, 0);
            highlightTextRect.offsetMax = new Vector2(14, 0);

            var highlightTextInput = highlightTextObj.AddComponent<Text>();
            highlightTextInput.supportRichText = true;
            highlightTextInput.fontSize = fontSize;

            #endregion

            #region COMPILE BUTTON BAR

            var horozGroupObj = UIFactory.CreateHorizontalGroup(Content, "BigButtons", true, true, true, true, 0, new Vector4(2,2,2,2),
                new Color(1, 1, 1, 0));

            var resetButton = UIFactory.CreateButton(horozGroupObj, "ResetButton", "Reset", () => ResetConsole(), "666666".ToColor());
            var resetBtnText = resetButton.GetComponentInChildren<Text>();
            resetBtnText.fontSize = 18;
            UIFactory.SetLayoutElement(resetButton.gameObject, preferredWidth: 80, flexibleWidth: 0, minHeight: 45, flexibleHeight: 0);

            var compileButton = UIFactory.CreateButton(horozGroupObj, "CompileButton", "Compile", CompileCallback, 
                new Color(14f / 255f, 80f / 255f, 14f / 255f));
            var btnText = compileButton.GetComponentInChildren<Text>();
            btnText.fontSize = 18;
            UIFactory.SetLayoutElement(compileButton.gameObject, preferredWidth: 80, flexibleWidth: 0, minHeight: 45, flexibleHeight: 0);

            void CompileCallback()
            {
                if (!string.IsNullOrEmpty(inputField.text))
                    Evaluate(inputField.text.Trim());
                else
                    ExplorerCore.Log("Cannot evaluate empty input!");
            }

            #endregion

            //mainTextInput.supportRichText = false;

            mainTextInput.font = UIManager.ConsoleFont;
            placeHolderText.font = UIManager.ConsoleFont;
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
        }



        // ================================================================================================

        private class VoidType
        {
            public static readonly VoidType Value = new VoidType();
            private VoidType() { }
        }
    }
}
