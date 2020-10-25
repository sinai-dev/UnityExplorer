using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Explorer.UI.Main.Pages.Console;
using ExplorerBeta;
using ExplorerBeta.UI;
using ExplorerBeta.UI.Main;
using ExplorerBeta.Unstrip.Resources;
using TMPro;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;

namespace Explorer.UI.Main.Pages
{
    public class ConsolePage : BaseMenuPage
    {
        public override string Name => "C# Console";

        public static ConsolePage Instance { get; private set; }

        private CodeEditor codeEditor;

        private ScriptEvaluator m_evaluator;

        public static List<AutoComplete> AutoCompletes = new List<AutoComplete>();
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

                foreach (var use in DefaultUsing)
                {
                    AddUsing(use);
                }

                ConstructUI();
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"Error setting up console!\r\nMessage: {e.Message}");
                // TODO
            }
        }

        public override void Update()
        {
            codeEditor?.Update();
        }

        internal string AsmToUsing(string asm, bool richText = false)
        {
            if (richText)
            {
                return $"<color=#569cd6>using</color> {asm};";
            }
            return $"using {asm};";
        }

        public void AddUsing(string asm)
        {
            if (!UsingDirectives.Contains(asm))
            {
                UsingDirectives.Add(asm);
                Evaluate(AsmToUsing(asm), true);
            }
        }

        public object Evaluate(string str, bool suppressWarning = false)
        {
            object ret = VoidType.Value;

            m_evaluator.Compile(str, out var compiled);

            try
            {
                if (compiled == null)
                {
                    throw new Exception("Mono.Csharp Service was unable to compile the code provided.");
                }

                compiled.Invoke(ref ret);
            }
            catch (Exception e)
            {
                if (!suppressWarning)
                {
                    ExplorerCore.LogWarning(e.GetType() + ", " + e.Message);
                }
            }

            return ret;
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

        #region UI Construction

        public void ConstructUI()
        {
            Content = UIFactory.CreateUIObject("C# Console", MainMenu.Instance.PageViewport);

            var mainLayout = Content.AddComponent<LayoutElement>();
            mainLayout.preferredHeight = 300;
            mainLayout.flexibleHeight = 4;

            var mainGroup = Content.AddComponent<VerticalLayoutGroup>();
            mainGroup.childControlHeight = true;
            mainGroup.childControlWidth = true;
            mainGroup.childForceExpandHeight = true;
            mainGroup.childForceExpandWidth = true;

            var topBarObj = UIFactory.CreateHorizontalGroup(Content);
            var topBarLayout = topBarObj.AddComponent<LayoutElement>();
            topBarLayout.preferredHeight = 50;
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

            var topBarLabel = UIFactory.CreateLabel(topBarObj, TextAnchor.MiddleLeft);
            var topBarLabelLayout = topBarLabel.AddComponent<LayoutElement>();
            topBarLabelLayout.preferredWidth = 800;
            topBarLabelLayout.flexibleWidth = 10;
            var topBarText = topBarLabel.GetComponent<Text>();
            topBarText.text = "C# Console";
            topBarText.fontSize = 20;

            var compileBtnObj = UIFactory.CreateButton(topBarObj);
            var compileBtnLayout = compileBtnObj.AddComponent<LayoutElement>();
            compileBtnLayout.preferredWidth = 80;
            compileBtnLayout.flexibleWidth = 0;
            var compileButton = compileBtnObj.GetComponent<Button>();
            var compileBtnColors = compileButton.colors;
            compileBtnColors.normalColor = new Color(14f/255f, 106f/255f, 14f/255f);
            compileButton.colors = compileBtnColors;
            var btnText = compileBtnObj.GetComponentInChildren<Text>();
            btnText.text = ">";
            btnText.fontSize = 25;
            btnText.color = Color.white;

            var consoleBase = UIFactory.CreateUIObject("CodeEditor", Content);

            var consoleLayout = consoleBase.AddComponent<LayoutElement>();
            consoleLayout.preferredHeight = 500;
            consoleLayout.flexibleHeight = 5;

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
                lineHighlightImage = lineHighlight.AddComponent<Image>();

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

            // setup button callbacks

            compileButton.onClick.AddListener(new Action(() => 
            {
                if (!string.IsNullOrEmpty(tmpInput.text))
                {
                    Evaluate(tmpInput.text.Trim());
                }
            }));

            TMP_FontAsset fontToUse = null;
#if CPP
            var fonts = ResourcesUnstrip.FindObjectsOfTypeAll(Il2CppType.Of<TMP_FontAsset>());
            foreach (var font in fonts)
            {
                var fontCast = font.Il2CppCast(typeof(TMP_FontAsset)) as TMP_FontAsset;

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
                fontToUse.faceInfo.tabWidth = 10;
                fontToUse.tabSize = 10;
                var faceInfo = fontToUse.faceInfo;
                faceInfo.tabWidth = 10;
                fontToUse.faceInfo = faceInfo;

                tmpInput.fontAsset = fontToUse;
                mainTextInput.font = fontToUse;
                highlightTextInput.font = fontToUse;
            }

            try
            {
                codeEditor = new CodeEditor(inputField, mainTextInput, highlightTextInput, linesTextInput,
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
