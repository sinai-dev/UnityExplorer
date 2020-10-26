using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Explorer.Unstrip.ColorUtility;
using ExplorerBeta.Input;
using ExplorerBeta.Unstrip.Resources;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if CPP
using UnhollowerRuntimeLib;
#endif

namespace ExplorerBeta.UI.Main
{
    public class DebugConsole
    {
        public static DebugConsole Instance { get; private set; }

        public static bool LogUnity { get; set; } = true;

        public static GameObject CanvasRoot;

        public readonly List<string> AllMessages;
        public readonly List<Text> MessageHolders;

        private TMP_InputField m_textInput;

        public DebugConsole(GameObject parent)
        {
            Instance = this;

            AllMessages = new List<string>();
            MessageHolders = new List<Text>();

            try
            {
                ConstructUI(parent);
            }
            catch (Exception e)
            {
                ExplorerCore.Log(e);
            }
        }

        // todo: get scrollbar working with inputfield somehow

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
            mainLayout.minHeight = 40;
            mainLayout.preferredHeight = 230;
            mainLayout.flexibleHeight = 0;

#region LOG AREA
            var logAreaObj = UIFactory.CreateHorizontalGroup(mainObj);
            var logAreaGroup = logAreaObj.GetComponent<HorizontalLayoutGroup>();
            logAreaGroup.childControlHeight = true;
            logAreaGroup.childControlWidth = true;
            logAreaGroup.childForceExpandHeight = true;
            logAreaGroup.childForceExpandWidth = true;

            var logAreaLayout = logAreaObj.AddComponent<LayoutElement>();
            logAreaLayout.preferredHeight = 300;
            logAreaLayout.flexibleHeight = 50;

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

            tmpInput.readOnly = true;

            m_textInput = inputObj.GetComponent<TMP_InputField>();

#endregion

#region BOTTOM BAR

            var bottomBarObj = UIFactory.CreateHorizontalGroup(mainObj);
            var topBarLayout = bottomBarObj.AddComponent<LayoutElement>();
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
                    mainLayout.preferredHeight = 40;
                }
                else
                {
                    logAreaObj.SetActive(true);
                    hideBtnText.text = "Hide";
                    mainLayout.preferredHeight = 230;
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
            unityToggle.onValueChanged.AddListener(new Action<bool>(ToggleCallback));
#else
            unityToggle.onValueChanged.AddListener(ToggleCallback);
#endif

            void ToggleCallback(bool val)
            {
                LogUnity = val;
            }

            unityToggleText.text = "Print Unity Debug?";
            unityToggleText.alignment = TextAnchor.MiddleLeft;

            var unityToggleLayout = unityToggleObj.AddComponent<LayoutElement>();
            unityToggleLayout.minWidth = 200;
            unityToggleLayout.flexibleWidth = 0;

            var unityToggleRect = unityToggleObj.transform.Find("Background").GetComponent<RectTransform>();
            var pos = unityToggleRect.localPosition;
            pos.y = -8;
            unityToggleRect.localPosition = pos;

#endregion
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
            if (Instance == null)
                return;

            Instance.AllMessages.Add(message);

            if (Instance.m_textInput)
            {
                if (hexColor != null)
                    message = $"<color=#{hexColor}>{message}</color>";

                Instance.m_textInput.text = $"{message}\n{Instance.m_textInput.text}";
            }
        }
    }
}
