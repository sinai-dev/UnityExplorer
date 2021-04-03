using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using System.IO;
using System.Linq;
using UnityExplorer.Core.Unity;
using UnityExplorer.UI.Utility;

namespace UnityExplorer.UI.Main
{
    public class DebugConsole
    {
        public static DebugConsole Instance { get; private set; }

        public static bool LogUnity { get; set; }
        //public static bool SaveToDisk { get; set; } = ModConfig.Instance.Save_Logs_To_Disk;

        internal static StreamWriter s_streamWriter;

        public static readonly List<string> AllMessages = new List<string>();
        public static readonly List<Text> MessageHolders = new List<Text>();

        // logs that occured before the actual UI was ready.
        // these ones include the hex color codes.
        internal static readonly List<string> s_preInitMessages = new List<string>();

        private InputField m_textInput;
        internal const int MAX_TEXT_LEN = 10000;

        public DebugConsole(GameObject parent)
        {
            Instance = this;
            LogUnity = ConfigManager.Log_Unity_Debug.Value;

            ConstructUI(parent);

            if (!ConfigManager.Last_DebugConsole_State.Value)
                ToggleShow();

            // append messages that logged before we were set up
            string preAppend = "";
            for (int i = s_preInitMessages.Count - 1; i >= 0; i--)
            {
                var msg = s_preInitMessages[i];
                if (preAppend != "")
                    preAppend += "\r\n";
                preAppend += msg;
            }
            m_textInput.text = preAppend;

            // set up IO

            var path = Path.Combine(ExplorerCore.Loader.ExplorerFolder, "Logs");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            // clean old log(s)
            var files = Directory.GetFiles(path);
            if (files.Length >= 10)
            {
                var sorted = files.ToList();
                // sort by 'datetime.ToString("u")' will put the oldest ones first
                sorted.Sort();
                for (int i = 0; i < files.Length - 9; i++)
                    File.Delete(files[i]);
            }

            var fileName = "UnityExplorer " + DateTime.Now.ToString("u") + ".txt";
            fileName = RemoveInvalidFilenameChars(fileName);

            var stream = File.Create(path + @"\" + fileName);
            s_streamWriter = new StreamWriter(stream)
            {
                AutoFlush = true
            };

            foreach (var msg in AllMessages)
                s_streamWriter.WriteLine(msg);
        }

        public static bool Hiding;

        private GameObject m_logAreaObj;
        private Text m_hideBtnText;
        private LayoutElement m_mainLayout;

        public static Action<bool> OnToggleShow;

        public void ToggleShow()
        {
            if (m_logAreaObj.activeSelf)
            {
                Hiding = true;
                m_logAreaObj.SetActive(false);
                m_hideBtnText.text = "Show";
                m_mainLayout.minHeight = 30;
            }
            else
            {
                Hiding = false;
                m_logAreaObj.SetActive(true);
                m_hideBtnText.text = "Hide";
                m_mainLayout.minHeight = 190;
            }

            OnToggleShow?.Invoke(!Hiding);
        }

        public static string RemoveInvalidFilenameChars(string s)
        {
            var invalid = Path.GetInvalidFileNameChars();
            foreach (var c in invalid)
            {
                s = s.Replace(c.ToString(), "");
            }
            return s;
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
            s_streamWriter?.WriteLine(message);

            if (hexColor != null)
                message = $"<color=#{hexColor}>{message}</color>";

            if (Instance?.m_textInput)
            {
                var input = Instance.m_textInput;
                var wanted = $"{message}\n{input.text}";

                if (wanted.Length > MAX_TEXT_LEN)
                    wanted = wanted.Substring(0, MAX_TEXT_LEN);

                input.text = wanted;
            }
            else
                s_preInitMessages.Add(message);
        }

        public void ConstructUI(GameObject parent)
        {
            var mainObj = UIFactory.CreateVerticalGroup(parent, "DebugConsole", true, true, true, true, 0, default, new Color(0.1f, 0.1f, 0.1f, 1.0f));
            var mainImage = mainObj.GetComponent<Image>();
            mainImage.maskable = true;
            var mask = mainObj.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            m_mainLayout = mainObj.AddComponent<LayoutElement>();
            m_mainLayout.minHeight = 190;
            m_mainLayout.flexibleHeight = 0;

            #region LOG AREA
            m_logAreaObj = UIFactory.CreateHorizontalGroup(mainObj, "LogArea", true, true, true, true);
            UIFactory.SetLayoutElement(m_logAreaObj, preferredHeight: 190, flexibleHeight: 0);

            var inputScrollerObj = UIFactory.CreateSrollInputField(m_logAreaObj, 
                "DebugConsoleOutput",
                "<no output>",
                out InputFieldScroller inputScroll,
                14, 
                new Color(0.05f, 0.05f, 0.05f));

            inputScroll.inputField.textComponent.font = UIManager.ConsoleFont;
            inputScroll.inputField.readOnly = true;

            m_textInput = inputScroll.inputField;
            #endregion

            #region BOTTOM BAR

            var bottomBarObj = UIFactory.CreateHorizontalGroup(mainObj, "BottomBar", false, true, true, true, 10, new Vector4(2,2,10,10),
                default, TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(bottomBarObj, minHeight: 30, flexibleHeight: 0);

            // Debug Console label

            var bottomLabel = UIFactory.CreateLabel(bottomBarObj, "DebugConsoleLabel", "Debug Console", TextAnchor.MiddleLeft);
            bottomLabel.fontStyle = FontStyle.Bold;
            UIFactory.SetLayoutElement(bottomLabel.gameObject, minWidth: 100, flexibleWidth: 0);

            // Hide button

            var hideButton = UIFactory.CreateButton(bottomBarObj, "HideButton", "Hide", ToggleShow);
            UIFactory.SetLayoutElement(hideButton.gameObject, minWidth: 80, flexibleWidth: 0);
            m_hideBtnText = hideButton.GetComponentInChildren<Text>();

            // Clear button

            var clearButton = UIFactory.CreateButton(bottomBarObj, "ClearButton", "Clear", () =>
            {
                m_textInput.text = "";
                AllMessages.Clear();
            });
            UIFactory.SetLayoutElement(clearButton.gameObject, minWidth: 80, flexibleWidth: 0);

            // Unity log toggle

            var unityToggleObj = UIFactory.CreateToggle(bottomBarObj, "UnityLogToggle", out Toggle unityToggle, out Text unityToggleText);

            unityToggle.onValueChanged.AddListener((bool val) =>
            {
                LogUnity = val;
                ConfigManager.Log_Unity_Debug.Value = val;
            });

            ConfigManager.Log_Unity_Debug.OnValueChanged += (bool val) => { unityToggle.isOn = val; };

            unityToggle.isOn = LogUnity;
            unityToggleText.text = "Log Unity Debug?";
            unityToggleText.alignment = TextAnchor.MiddleLeft;

            UIFactory.SetLayoutElement(unityToggleObj, minWidth: 170, flexibleWidth: 0);

            var unityToggleRect = unityToggleObj.transform.Find("Background").GetComponent<RectTransform>();
            var pos = unityToggleRect.localPosition;
            pos.y = -4;
            unityToggleRect.localPosition = pos;

            #endregion
        }
    }
}
