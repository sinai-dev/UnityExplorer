using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Explorer.Unstrip.ColorUtility;
using ExplorerBeta.Input;
using ExplorerBeta.Unstrip.Resources;
using TMPro;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;

namespace ExplorerBeta.UI.Main
{
    // TODO: 
    // - Maybe hook into Unity's debug logs
    // - Buttons for clear, save to file, etc..?

    public class DebugConsole
    {
        public static DebugConsole Instance { get; private set; }

        public static GameObject CanvasRoot;
        //private static GameObject Panel;

        public readonly List<string> AllMessages;
        public readonly List<Text> MessageHolders;

        private TMP_InputField m_textInput;

        // todo probably put this in UImanager, use for C# console too
        //internal static Font m_consoleFont;
        //private const int MAX_MESSAGES = 100;

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
            var obj = UIFactory.CreateHorizontalGroup(parent, new Color(0.1f, 0.1f, 0.1f, 1.0f));
            var mainGroup = obj.GetComponent<HorizontalLayoutGroup>();
            mainGroup.childControlHeight = true;
            mainGroup.childControlWidth = true;
            mainGroup.childForceExpandHeight = true;
            mainGroup.childForceExpandWidth = true;

            var mainImage = obj.GetComponent<Image>();
            mainImage.maskable = true;

            var mask = obj.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            var mainLayout = obj.AddComponent<LayoutElement>();
            mainLayout.preferredHeight = 230;
            mainLayout.flexibleHeight = 0;

            var input = UIFactory.CreateTMPInput(obj);

            var inputLayout = input.AddComponent<LayoutElement>();
            inputLayout.preferredWidth = 500;
            inputLayout.flexibleWidth = 9999;

            var scroll = UIFactory.CreateScrollbar(obj);

            var scrollLayout = scroll.AddComponent<LayoutElement>();
            scrollLayout.preferredWidth = 25;
            scrollLayout.flexibleWidth = 0;

            var scroller = scroll.GetComponent<Scrollbar>();
            scroller.direction = Scrollbar.Direction.TopToBottom;
            var scrollColors = scroller.colors;
            scrollColors.normalColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            //try { scrollColors.selectedColor = scrollColors.normalColor; } catch { }
            scroller.colors = scrollColors;

            var tmpInput = input.GetComponent<TMP_InputField>();
            tmpInput.scrollSensitivity = 15;
            tmpInput.verticalScrollbar = scroller;

            m_textInput = input.GetComponent<TMP_InputField>();
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
