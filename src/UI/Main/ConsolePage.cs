using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using ExplorerBeta.UI.Main.Console;
using ExplorerBeta.Unstrip.Resources;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if CPP
using UnhollowerRuntimeLib;
#endif

namespace ExplorerBeta.UI.Main
{
    public class ConsolePage : MainMenu.Page
    {
        public override string Name => "C# Console";

        public static ConsolePage Instance { get; private set; }

        public static bool EnableSuggestions { get; set; } = true;
        public static bool EnableAutoIndent { get; set; } = true;

        public CodeEditor m_codeEditor;
        public ScriptEvaluator m_evaluator;

        public static List<Suggestion> AutoCompletes = new List<Suggestion>();
        public static List<string> UsingDirectives;

        public static readonly string[] DefaultUsing = new string[]
        {
            "System",
            "UnityEngine",
            "System.Linq",
            "System.Collections",
            "System.Collections.Generic",
            "System.Reflection"
        };

        public override void Init()
        {
            Instance = this;

            try
            {
                ResetConsole();

                foreach (string use in DefaultUsing)
                {
                    AddUsing(use);
                }

                ConstructUI();

                AutoCompleter.Init();
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"Error setting up console!\r\nMessage: {e.Message}");
                // TODO remove page button from menu
            }
        }

        public override void Update()
        {
            m_codeEditor?.Update();

            AutoCompleter.Update();
        }

        public void AddUsing(string asm)
        {
            if (!UsingDirectives.Contains(asm))
            {
                UsingDirectives.Add(asm);
                Evaluate($"using {asm};", true);
            }
        }

        public void Evaluate(string str, bool suppressWarning = false)
        {
            m_evaluator.Compile(str, out Mono.CSharp.CompiledMethod compiled);

            if (compiled == null)
            {
                if (!suppressWarning)
                {
                    ExplorerCore.LogWarning("Unable to compile the code!");
                }
            }
            else
            {
                try
                {
                    object ret = VoidType.Value;
                    compiled.Invoke(ref ret);
                }
                catch (Exception e)
                {
                    ExplorerCore.LogWarning($"Exception executing code: {e.GetType()}, {e.Message}\r\n{e.StackTrace}");
                }
            }
        }

        public void ResetConsole()
        {
            if (m_evaluator != null)
            {
                m_evaluator.Dispose();
            }

            m_evaluator = new ScriptEvaluator(new StringWriter(new StringBuilder())) { InteractiveBaseClass = typeof(ScriptInteraction) };

            UsingDirectives = new List<string>();
        }

        internal void OnInputChanged()
        {
            AutoCompleter.CheckAutocomplete();

            AutoCompleter.SetSuggestions(AutoCompletes.ToArray());
        }

        public void UseAutocomplete(string suggestion)
        {
            int cursorIndex = m_codeEditor.InputField.caretPosition;
            string input = m_codeEditor.InputField.text;
            input = input.Insert(cursorIndex, suggestion);
            m_codeEditor.InputField.text = input;
            m_codeEditor.InputField.caretPosition += suggestion.Length;

            AutoCompleter.ClearAutocompletes();
        }

        #region UI Construction

        public void ConstructUI()
        {
            Content = UIFactory.CreateUIObject("C# Console", MainMenu.Instance.PageViewport);

            var mainLayout = Content.AddComponent<LayoutElement>();
            mainLayout.preferredHeight = 9900;
            mainLayout.flexibleHeight = 9000;

            var mainGroup = Content.AddComponent<VerticalLayoutGroup>();
            mainGroup.childControlHeight = true;
            mainGroup.childControlWidth = true;
            mainGroup.childForceExpandHeight = true;
            mainGroup.childForceExpandWidth = true;

            #region TOP BAR 

            // Main group object

            var topBarObj = UIFactory.CreateHorizontalGroup(Content);
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
                EnableSuggestions = val;
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
            autoIndentToggle.onValueChanged.AddListener(new Action<bool>((bool val) =>
            {
                EnableAutoIndent = val;
            }));
#else
            autoIndentToggle.onValueChanged.AddListener(OnIndentChanged);

            void OnIndentChanged(bool val) => EnableAutoIndent = val;
#endif
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

            var consoleBase = UIFactory.CreateUIObject("CodeEditor", Content);

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

            var lineHighlight = UIFactory.CreateUIObject("LineHighlight", consoleBase);

            var lineHighlightRect = lineHighlight.GetComponent<RectTransform>();
            lineHighlightRect.pivot = new Vector2(0.5f, 1);
            lineHighlightRect.anchorMin = new Vector2(0, 1);
            lineHighlightRect.anchorMax = new Vector2(1, 1);
            lineHighlightRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 21);

            var lineHighlightImage = lineHighlight.GetComponent<Image>();
            if (!lineHighlightImage)
            {
                lineHighlightImage = lineHighlight.AddComponent<Image>();
            }

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
            mainTextInput.fontSize = 18;

            var placeHolderText = textAreaObj.transform.Find("Placeholder").GetComponent<TextMeshProUGUI>();
            placeHolderText.text = @"Welcome to the Explorer C# Console.

The following helper methods are available:

* <color=#add490>Log(""message"");</color> logs a message to the debug console
* <color=#add490>CurrentTarget();</color> returns the currently inspected target on the Home page
* <color=#add490>AllTargets();</color> returns an object[] array containing all inspected instances
* <color=#add490>Inspect(someObject)</color> to inspect an instance, eg. Inspect(Camera.main);
* <color=#add490>Inspect(typeof(SomeClass))</color> to inspect a Class with static reflection
* <color=#add490>AddUsing(""SomeNamespace"");</color> adds a using directive to the C# console
* <color=#add490>GetUsing();</color> logs the current using directives to the debug console
* <color=#add490>Reset();</color> resets all using directives and variables
";

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
            highlightTextInput.fontSize = 18;

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

            var compileBtnObj = UIFactory.CreateButton(Content);
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
            compileButton.onClick.AddListener(new Action(() =>
            {
                if (!string.IsNullOrEmpty(tmpInput.text))
                {
                    Evaluate(tmpInput.text.Trim());
                }
            }));
#else
            compileButton.onClick.AddListener(CompileCallback);

            void CompileCallback()
            {
                if (!string.IsNullOrEmpty(tmpInput.text))
                {
                    Evaluate(tmpInput.text.Trim());
                }
            }
#endif

            #endregion

            #region FONT

            TMP_FontAsset fontToUse = null;
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
                highlightTextInput.font = fontToUse;
            }

            #endregion

            try
            {
                m_codeEditor = new CodeEditor(inputField, mainTextInput, highlightTextInput, linesTextInput,
                    mainBgImage, lineHighlightImage, linesBgImage, scrollImage);
            }
            catch (Exception e)
            {
                ExplorerCore.Log(e);
            }
        }

        #endregion

        private class VoidType
        {
            public static readonly VoidType Value = new VoidType();
            private VoidType() { }
        }
    }
}
