using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Panels
{
    // TODO move the logic out of this class into a LogUtil class (also move ExplorerCore.Log into that)

    public class LogPanel : UIPanel, ICellPoolDataSource<ConsoleLogCell>
    {
        public struct LogInfo
        {
            public string message;
            public LogType type;

            public LogInfo(string message, LogType type) { this.message = message; this.type = type; }
        }

        private static readonly List<LogInfo> Logs = new List<LogInfo>();
        private static string CurrentStreamPath;

        public override string Name => "Log";
        public override UIManager.Panels PanelType => UIManager.Panels.ConsoleLog;

        public override int MinWidth => 350;
        public override int MinHeight => 75;
        public override bool ShouldSaveActiveState => true;
        public override bool ShowByDefault => true;

        public int ItemCount => Logs.Count;

        private static ScrollPool<ConsoleLogCell> logScrollPool;

        public LogPanel()
        {
            SetupIO();
        }

        private static bool DoneScrollPoolInit;

        public override void SetActive(bool active)
        {
            base.SetActive(active);

            if (active && !DoneScrollPoolInit)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(this.Rect);
                logScrollPool.Initialize(this);
                DoneScrollPoolInit = true;
            }

            logScrollPool.Refresh(true, false);
        }

        private void SetupIO()
        {
            var path = Path.Combine(ExplorerCore.Loader.ExplorerFolder, "Logs");
            //path = IOUtility.EnsureValidFilePath(path);

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

            var fileName = $"UnityExplorer {DateTime.Now:u}.txt";
            fileName = IOUtility.EnsureValidFilename(fileName);

            CurrentStreamPath = IOUtility.EnsureValidFilePath(Path.Combine(path, fileName));

            File.WriteAllLines(CurrentStreamPath, Logs.Select(it => it.message).ToArray());
        }

        // Logging

        public static void Log(string message, LogType type)
        {
            Logs.Add(new LogInfo(message, type));

            if (CurrentStreamPath != null)
                File.AppendAllText(CurrentStreamPath, '\n' + message);

            if (logScrollPool != null)
                logScrollPool.Refresh(true, false);
        }

        private static void ClearLogs()
        {
            Logs.Clear();
            logScrollPool.Refresh(true, true);
        }

        private static void OpenLogFile()
        {
            if (File.Exists(CurrentStreamPath))
                Process.Start(CurrentStreamPath);
        }

        // Cell pool

        private static readonly Dictionary<LogType, Color> logColors = new Dictionary<LogType, Color>
        {
            { LogType.Log,       Color.white },
            { LogType.Warning,   Color.yellow },
            { LogType.Assert,    Color.yellow },
            { LogType.Error,     Color.red },
            { LogType.Exception, Color.red },
        };

        private readonly Color logEvenColor = new Color(0.34f, 0.34f, 0.34f);
        private readonly Color logOddColor = new Color(0.28f, 0.28f, 0.28f);

        public void OnCellBorrowed(ConsoleLogCell cell) { }

        public void SetCell(ConsoleLogCell cell, int index)
        {
            if (index >= Logs.Count)
            {
                cell.Disable();
                return;
            }

            // Logs are displayed in reverse order (newest at top)
            index = Logs.Count - index - 1;

            var log = Logs[index];
            cell.IndexLabel.text = $"{index}:";
            cell.Input.Text = log.message;
            cell.Input.Component.textComponent.color = logColors[log.type];

            var color = index % 2 == 0 ? logEvenColor : logOddColor;
            RuntimeProvider.Instance.SetColorBlock(cell.Input.Component, color);
        }

        // Panel save data

        public override string GetSaveDataFromConfigManager()
        {
            return ConfigManager.ConsoleLogData.Value;
        }

        public override void DoSaveToConfigElement()
        {
            ConfigManager.ConsoleLogData.Value = this.ToSaveData();
        }

        protected internal override void DoSetDefaultPosAndAnchors()
        {
            Rect.localPosition = Vector2.zero;
            Rect.pivot = new Vector2(0f, 1f);
            Rect.anchorMin = new Vector2(0.5f, 0.03f);
            Rect.anchorMax = new Vector2(0.9f, 0.2f);
        }

        // UI Construction

        public override void ConstructPanelContent()
        {
            // Log scroll pool

            logScrollPool = UIFactory.CreateScrollPool<ConsoleLogCell>(this.content, "Logs", out GameObject scrollObj,
                out GameObject scrollContent, new Color(0.03f, 0.03f, 0.03f));
            UIFactory.SetLayoutElement(scrollObj, flexibleWidth: 9999, flexibleHeight: 9999);

            // Buttons and toggles

            var optionsRow = UIFactory.CreateUIObject("OptionsRow", this.content);
            UIFactory.SetLayoutElement(optionsRow, minHeight: 25, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(optionsRow, false, false, true, true, 5, 2, 2, 2, 2);

            var clearButton = UIFactory.CreateButton(optionsRow, "ClearButton", "Clear", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(clearButton.Component.gameObject, minHeight: 23, flexibleHeight: 0, minWidth: 60);
            clearButton.OnClick += ClearLogs;
            clearButton.Component.transform.SetSiblingIndex(1);

            var fileButton = UIFactory.CreateButton(optionsRow, "FileButton", "Open Log File", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(fileButton.Component.gameObject, minHeight: 23, flexibleHeight: 0, minWidth: 100);
            fileButton.OnClick += OpenLogFile;
            fileButton.Component.transform.SetSiblingIndex(2);

            var unityToggle = UIFactory.CreateToggle(optionsRow, "UnityLogToggle", out var toggle, out var toggleText);
            UIFactory.SetLayoutElement(unityToggle, minHeight: 25, minWidth: 150);
            toggleText.text = "Log Unity Debug?";
            toggle.isOn = ConfigManager.Log_Unity_Debug.Value;
            ConfigManager.Log_Unity_Debug.OnValueChanged += (bool val) => toggle.isOn = val;
            toggle.onValueChanged.AddListener((bool val) => ConfigManager.Log_Unity_Debug.Value = val);
        }
    }

    #region Log Cell View

    public class ConsoleLogCell : ICell
    {
        public Text IndexLabel;
        public InputFieldRef Input;

        public RectTransform Rect { get; set; }
        public GameObject UIRoot { get; set; }

        public float DefaultHeight => 25;

        public bool Enabled => UIRoot.activeInHierarchy;
        public void Enable() => UIRoot.SetActive(true);
        public void Disable() => UIRoot.SetActive(false);


        public GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateUIObject("LogCell", parent, new Vector2(25, 25));
            Rect = UIRoot.GetComponent<RectTransform>();
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(UIRoot, false, false, true, true, 3);
            UIFactory.SetLayoutElement(UIRoot, minHeight: 25, minWidth: 50, flexibleWidth: 9999);

            IndexLabel = UIFactory.CreateLabel(UIRoot, "IndexLabel", "i:", TextAnchor.MiddleCenter, Color.grey, false, 12);
            UIFactory.SetLayoutElement(IndexLabel.gameObject, minHeight: 25, minWidth: 30, flexibleWidth: 40);

            Input = UIFactory.CreateInputField(UIRoot, "Input", "");
            //Input.Component.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UIFactory.SetLayoutElement(Input.UIRoot, minHeight: 25, flexibleWidth: 9999);
            RuntimeProvider.Instance.SetColorBlock(Input.Component, new Color(0.1f, 0.1f, 0.1f), new Color(0.13f, 0.13f, 0.13f),
                new Color(0.07f, 0.07f, 0.07f));
            Input.Component.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

            Input.Component.readOnly = true;
            Input.Component.textComponent.supportRichText = true;
            Input.Component.lineType = InputField.LineType.MultiLineNewline;
            Input.Component.textComponent.font = UIManager.ConsoleFont;
            Input.PlaceholderText.font = UIManager.ConsoleFont;

            return UIRoot;
        }
    }

    #endregion
}
