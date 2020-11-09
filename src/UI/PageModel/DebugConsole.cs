using System;
using System.Collections.Generic;
using UnityExplorer.Unstrip;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Config;

namespace UnityExplorer.UI.PageModel
{
    public class DebugConsole
    {
        public static DebugConsole Instance { get; private set; }

        public static bool LogUnity { get; set; } = ModConfig.Instance.Log_Unity_Debug;

        public static readonly List<string> AllMessages = new List<string>();
        public static readonly List<Text> MessageHolders = new List<Text>();

        internal static readonly List<string> s_preInitMessages = new List<string>();

        private TMP_InputField m_textInput;

        public DebugConsole(GameObject parent)
        {
            Instance = this;

            //AllMessages = new List<string>();
            //MessageHolders = new List<Text>();

            ConstructUI(parent);

            string preAppend = "";
            for (int i = s_preInitMessages.Count - 1; i >= 0; i--)
            {
                var msg = s_preInitMessages[i];
                if (preAppend != "")
                    preAppend += "\r\n";
                preAppend += msg;
            }
            m_textInput.text = preAppend;
        }

        public static void Log(string message)
        {
            Log(message, null);
        }

        public static void Log(string message, Color color)
        {
            Log(message, color.ToHex());
        }

        public static void Log(string message, string hexColor)
        {
            message = $"{AllMessages.Count}: {message}";

            AllMessages.Add(message);

            if (hexColor != null)
                message = $"<color=#{hexColor}>{message}</color>";
            
            if (Instance?.m_textInput)
                Instance.m_textInput.text = $"{message}\n{Instance.m_textInput.text}";
            else
                s_preInitMessages.Add(message);
        }

        public void ConstructUI(GameObject parent)
        {
            var mainObj = UIFactory.CreateVerticalGroup(parent, new Color(0.1f, 0.1f, 0.1f, 1.0f));

            var mainGroup = mainObj.GetComponent<VerticalLayoutGroup>();
            mainGroup.childControlHeight = true;
            mainGroup.childControlWidth = true;
            mainGroup.childForceExpandHeight = true;
            mainGroup.childForceExpandWidth = true;

            var mainImage = mainObj.GetComponent<Image>();
            mainImage.maskable = true;

            var mask = mainObj.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            var mainLayout = mainObj.AddComponent<LayoutElement>();
            mainLayout.minHeight = 190;
            mainLayout.flexibleHeight = 0;

            #region LOG AREA
            var logAreaObj = UIFactory.CreateHorizontalGroup(mainObj);
            var logAreaGroup = logAreaObj.GetComponent<HorizontalLayoutGroup>();
            logAreaGroup.childControlHeight = true;
            logAreaGroup.childControlWidth = true;
            logAreaGroup.childForceExpandHeight = true;
            logAreaGroup.childForceExpandWidth = true;

            var logAreaLayout = logAreaObj.AddComponent<LayoutElement>();
            logAreaLayout.preferredHeight = 190;
            logAreaLayout.flexibleHeight = 0;

            var inputObj = UIFactory.CreateTMPInput(logAreaObj);

            var mainInputGroup = inputObj.GetComponent<VerticalLayoutGroup>();
            mainInputGroup.padding.left = 8;
            mainInputGroup.padding.right = 8;
            mainInputGroup.padding.top = 5;
            mainInputGroup.padding.bottom = 5;

            var inputLayout = inputObj.AddComponent<LayoutElement>();
            inputLayout.preferredWidth = 500;
            inputLayout.flexibleWidth = 9999;

            var inputImage = inputObj.GetComponent<Image>();
            inputImage.color = new Color(0.05f, 0.05f, 0.05f, 1.0f);

            var scroll = UIFactory.CreateScrollbar(logAreaObj);

            var scrollLayout = scroll.AddComponent<LayoutElement>();
            scrollLayout.preferredWidth = 25;
            scrollLayout.flexibleWidth = 0;

            var scroller = scroll.GetComponent<Scrollbar>();
            scroller.direction = Scrollbar.Direction.TopToBottom;
            var scrollColors = scroller.colors;
            scrollColors.normalColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            scroller.colors = scrollColors;

            var tmpInput = inputObj.GetComponent<TMP_InputField>();
            tmpInput.scrollSensitivity = 15;
            tmpInput.verticalScrollbar = scroller;

            if (UIManager.ConsoleFont != null)
            {
                tmpInput.textComponent.font = UIManager.ConsoleFont;
#if MONO
                (tmpInput.placeholder as TextMeshProUGUI).font = UIManager.ConsoleFont;
#else
                tmpInput.placeholder.TryCast<TextMeshProUGUI>().font = UIManager.ConsoleFont;
#endif
            }

            tmpInput.readOnly = true;

            m_textInput = inputObj.GetComponent<TMP_InputField>();

#endregion

            #region BOTTOM BAR

            var bottomBarObj = UIFactory.CreateHorizontalGroup(mainObj);
            LayoutElement topBarLayout = bottomBarObj.AddComponent<LayoutElement>();
            topBarLayout.minHeight = 40;
            topBarLayout.flexibleHeight = 0;

            var bottomGroup = bottomBarObj.GetComponent<HorizontalLayoutGroup>();
            bottomGroup.padding.left = 10;
            bottomGroup.padding.right = 10;
            bottomGroup.padding.top = 2;
            bottomGroup.padding.bottom = 2;
            bottomGroup.spacing = 10;
            bottomGroup.childForceExpandHeight = true;
            bottomGroup.childForceExpandWidth = false;
            bottomGroup.childControlWidth = true;
            bottomGroup.childControlHeight = true;
            bottomGroup.childAlignment = TextAnchor.MiddleLeft;

            // Debug Console label

            var bottomLabel = UIFactory.CreateLabel(bottomBarObj, TextAnchor.MiddleLeft);
            var topBarLabelLayout = bottomLabel.AddComponent<LayoutElement>();
            topBarLabelLayout.minWidth = 100;
            topBarLabelLayout.flexibleWidth = 0;
            var topBarText = bottomLabel.GetComponent<Text>();
            topBarText.fontStyle = FontStyle.Bold;
            topBarText.text = "Debug Console";
            topBarText.fontSize = 14;

            // Hide button

            var hideButtonObj = UIFactory.CreateButton(bottomBarObj);

            var hideBtnText = hideButtonObj.GetComponentInChildren<Text>();
            hideBtnText.text = "Hide";

            var hideButton = hideButtonObj.GetComponent<Button>();
#if CPP
            hideButton.onClick.AddListener(new Action(HideCallback));
#else
            hideButton.onClick.AddListener(HideCallback);
#endif
            void HideCallback()
            {
                if (logAreaObj.activeSelf)
                {
                    logAreaObj.SetActive(false);
                    hideBtnText.text = "Show";
                    mainLayout.minHeight = 40;
                }
                else
                {
                    logAreaObj.SetActive(true);
                    hideBtnText.text = "Hide";
                    mainLayout.minHeight = 190;
                }
            }

            var hideBtnColors = hideButton.colors;
            //hideBtnColors.normalColor = new Color(160f / 255f, 140f / 255f, 40f / 255f);
            hideButton.colors = hideBtnColors;

            var hideBtnLayout = hideButtonObj.AddComponent<LayoutElement>();
            hideBtnLayout.minWidth = 80;
            hideBtnLayout.flexibleWidth = 0;

            // Clear button

            var clearButtonObj = UIFactory.CreateButton(bottomBarObj);

            var clearBtnText = clearButtonObj.GetComponentInChildren<Text>();
            clearBtnText.text = "Clear";

            var clearButton = clearButtonObj.GetComponent<Button>();
#if CPP
            clearButton.onClick.AddListener(new Action(ClearCallback));
#else
            clearButton.onClick.AddListener(ClearCallback);
#endif

            void ClearCallback()
            {
                m_textInput.text = "";
                AllMessages.Clear();
            }

            var clearBtnColors = clearButton.colors;
            //clearBtnColors.normalColor = new Color(160f/255f, 140f/255f, 40f/255f);
            clearButton.colors = clearBtnColors;

            var clearBtnLayout = clearButtonObj.AddComponent<LayoutElement>();
            clearBtnLayout.minWidth = 80;
            clearBtnLayout.flexibleWidth = 0;

            // Unity log toggle

            var unityToggleObj = UIFactory.CreateToggle(bottomBarObj, out Toggle unityToggle, out Text unityToggleText);
#if CPP
            unityToggle.onValueChanged.AddListener(new Action<bool>(ToggleLogUnity));
#else
            unityToggle.onValueChanged.AddListener(ToggleLogUnity);
#endif
            unityToggle.isOn = LogUnity;
            unityToggleText.text = "Print Unity Debug?";
            unityToggleText.alignment = TextAnchor.MiddleLeft;

            void ToggleLogUnity(bool val)
            {
                LogUnity = val;
                ModConfig.Instance.Log_Unity_Debug = val;
                ModConfig.SaveSettings();
            }

            var unityToggleLayout = unityToggleObj.AddComponent<LayoutElement>();
            unityToggleLayout.minWidth = 200;
            unityToggleLayout.flexibleWidth = 0;

            var unityToggleRect = unityToggleObj.transform.Find("Background").GetComponent<RectTransform>();
            var pos = unityToggleRect.localPosition;
            pos.y = -8;
            unityToggleRect.localPosition = pos;

#endregion
        }
    }
}
