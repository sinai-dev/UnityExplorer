using System;
using System.IO;
using UnityEngine;
using IniParser;
using IniParser.Parser;
using UnityExplorer.UI;
using System.Globalization;
using UnityExplorer.Core.Inspectors;
using UnityExplorer.UI.Main;

namespace UnityExplorer.Core.Config
{
    public class ExplorerConfig
    {
        public static ExplorerConfig Instance;

        internal static readonly IniDataParser _parser = new IniDataParser();
        internal static readonly string INI_PATH = Path.Combine(ExplorerCore.Loader.ConfigFolder, "config.ini");

        internal static CultureInfo _enCulture = new CultureInfo("en-US");

        // Actual configs
        public KeyCode  Main_Menu_Toggle     = KeyCode.F7;
        public bool     Force_Unlock_Mouse   = true;
        public int      Default_Page_Limit   = 25;
        public string   Default_Output_Path  = Path.Combine(ExplorerCore.EXPLORER_FOLDER, "Output");
        public bool     Log_Unity_Debug      = false;
        public bool     Hide_On_Startup      = false;
        public string   Window_Anchors       = DEFAULT_WINDOW_ANCHORS;
        public int      Active_Tab           = 0;
        public bool     DebugConsole_Hidden  = false;
        public bool     SceneExplorer_Hidden = false;

        private const string DEFAULT_WINDOW_ANCHORS = "0.25,0.10,0.78,0.95";

        public static event Action OnConfigChanged;

        internal static void InvokeConfigChanged()
        {
            OnConfigChanged?.Invoke();
        }

        public static void OnLoad()
        {
            Instance = new ExplorerConfig();
            _parser.Configuration.CommentString = "#";

            PanelDragger.OnFinishResize += PanelDragger_OnFinishResize;
            SceneExplorer.OnToggleShow += SceneExplorer_OnToggleShow;
            DebugConsole.OnToggleShow += DebugConsole_OnToggleShow;
            MainMenu.OnActiveTabChanged += MainMenu_OnActiveTabChanged;

            if (LoadSettings())
                return;

            SaveSettings();
        }

        public static bool LoadSettings()
        {
            if (!File.Exists(INI_PATH))
                return false;

            string ini = File.ReadAllText(INI_PATH);

            var data = _parser.Parse(ini);

            foreach (var config in data.Sections["Config"])
            {
                switch (config.KeyName)
                {
                    case nameof(Main_Menu_Toggle):
                        Instance.Main_Menu_Toggle = (KeyCode)Enum.Parse(typeof(KeyCode), config.Value);
                        break;
                    case nameof(Force_Unlock_Mouse):
                        Instance.Force_Unlock_Mouse = bool.Parse(config.Value);
                        break;
                    case nameof(Default_Page_Limit):
                        Instance.Default_Page_Limit = int.Parse(config.Value);
                        break;
                    case nameof(Log_Unity_Debug):
                        Instance.Log_Unity_Debug = bool.Parse(config.Value);
                        break;
                    case nameof(Default_Output_Path):
                        Instance.Default_Output_Path = config.Value;
                        break;
                    case nameof(Hide_On_Startup):
                        Instance.Hide_On_Startup = bool.Parse(config.Value);
                        break;
                    case nameof(Window_Anchors):
                        Instance.Window_Anchors = config.Value;
                        break;
                    case nameof(Active_Tab):
                        Instance.Active_Tab = int.Parse(config.Value);
                        break;
                    case nameof(DebugConsole_Hidden):
                        Instance.DebugConsole_Hidden = bool.Parse(config.Value);
                        break;
                    case nameof(SceneExplorer_Hidden):
                        Instance.SceneExplorer_Hidden = bool.Parse(config.Value);
                        break;
                }
            }

            return true;
        }

        public static void SaveSettings()
        {
            var data = new IniParser.Model.IniData();

            data.Sections.AddSection("Config");

            var sec = data.Sections["Config"];
            sec.AddKey(nameof(Main_Menu_Toggle),     Instance.Main_Menu_Toggle.ToString());
            sec.AddKey(nameof(Force_Unlock_Mouse),   Instance.Force_Unlock_Mouse.ToString());
            sec.AddKey(nameof(Default_Page_Limit),   Instance.Default_Page_Limit.ToString());
            sec.AddKey(nameof(Log_Unity_Debug),      Instance.Log_Unity_Debug.ToString());
            sec.AddKey(nameof(Default_Output_Path),  Instance.Default_Output_Path);
            sec.AddKey(nameof(Hide_On_Startup),      Instance.Hide_On_Startup.ToString());
            sec.AddKey(nameof(Window_Anchors),       GetWindowAnchorsString());
            sec.AddKey(nameof(Active_Tab),           Instance.Active_Tab.ToString());
            sec.AddKey(nameof(DebugConsole_Hidden),  Instance.DebugConsole_Hidden.ToString());
            sec.AddKey(nameof(SceneExplorer_Hidden), Instance.SceneExplorer_Hidden.ToString());

            if (!Directory.Exists(ExplorerCore.Loader.ConfigFolder))
                Directory.CreateDirectory(ExplorerCore.Loader.ConfigFolder);

            File.WriteAllText(INI_PATH, data.ToString());
        }

        private static void SceneExplorer_OnToggleShow()
        {
            Instance.SceneExplorer_Hidden = SceneExplorer.UI.Hiding;
            SaveSettings();
        }

        private static void DebugConsole_OnToggleShow()
        {
            Instance.DebugConsole_Hidden = DebugConsole.Hiding;
            SaveSettings();
        }

        private static void MainMenu_OnActiveTabChanged(int page)
        {
            Instance.Active_Tab = page;
            SaveSettings();
        }

        // ============ Window Anchors specific stuff ============== //

        private static void PanelDragger_OnFinishResize()
        {
            Instance.Window_Anchors = GetWindowAnchorsString();
            SaveSettings();
        }

        internal Vector4 GetWindowAnchorsVector()
        {
            try
            {
                var split = Window_Anchors.Split(',');

                if (split.Length != 4)
                    throw new Exception();

                Vector4 ret = Vector4.zero;
                ret.x = float.Parse(split[0], _enCulture);
                ret.y = float.Parse(split[1], _enCulture);
                ret.z = float.Parse(split[2], _enCulture);
                ret.w = float.Parse(split[3], _enCulture);
                return ret;
            }
            catch
            {
                return DefaultWindowAnchors();
            }
        }

        internal static string GetWindowAnchorsString()
        {
            try
            {
                var rect = PanelDragger.Instance.Panel;
                return string.Format(_enCulture, "{0},{1},{2},{3}", new object[]
                {
                    rect.anchorMin.x,
                    rect.anchorMin.y,
                    rect.anchorMax.x,
                    rect.anchorMax.y
                });
            }
            catch
            {
                return DEFAULT_WINDOW_ANCHORS;
            }
        }

        internal static Vector4 DefaultWindowAnchors()
        {
            Instance.Window_Anchors = DEFAULT_WINDOW_ANCHORS;
            return new Vector4(0.25f, 0.1f, 0.78f, 0.95f);
        }
    }
}
