using System;
using System.IO;
using UnityEngine;
using IniParser;
using IniParser.Parser;
using UnityExplorer.UI;

namespace UnityExplorer.Core.Config
{
    public class ExplorerConfig
    {
        public static ExplorerConfig Instance;

        internal static readonly IniDataParser _parser = new IniDataParser();
        internal static readonly string INI_PATH = Path.Combine(ExplorerCore.Loader.ConfigFolder, "config.ini");

        static ExplorerConfig()
        {
            _parser.Configuration.CommentString = "#";

            PanelDragger.OnFinishResize += PanelDragger_OnFinishResize;
        }

        // Actual configs
        public KeyCode  Main_Menu_Toggle    = KeyCode.F7;
        public bool     Force_Unlock_Mouse  = true;
        public int      Default_Page_Limit  = 25;
        public string   Default_Output_Path = Path.Combine(ExplorerCore.EXPLORER_FOLDER, "Output");
        public bool     Log_Unity_Debug     = false;
        public bool     Hide_On_Startup     = false;
        public string   Window_Anchors      = DEFAULT_WINDOW_ANCHORS;

        private const string DEFAULT_WINDOW_ANCHORS = "0.25,0.1,0.78,0.95";

        public static event Action OnConfigChanged;

        internal static void InvokeConfigChanged()
        {
            OnConfigChanged?.Invoke();
        }

        public static void OnLoad()
        {
            Instance = new ExplorerConfig();

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
                }
            }

            return true;
        }

        public static void SaveSettings()
        {
            var data = new IniParser.Model.IniData();

            data.Sections.AddSection("Config");

            var sec = data.Sections["Config"];
            sec.AddKey(nameof(Main_Menu_Toggle),    Instance.Main_Menu_Toggle.ToString());
            sec.AddKey(nameof(Force_Unlock_Mouse),  Instance.Force_Unlock_Mouse.ToString());
            sec.AddKey(nameof(Default_Page_Limit),  Instance.Default_Page_Limit.ToString());
            sec.AddKey(nameof(Log_Unity_Debug),     Instance.Log_Unity_Debug.ToString());
            sec.AddKey(nameof(Default_Output_Path), Instance.Default_Output_Path);
            sec.AddKey(nameof(Hide_On_Startup),     Instance.Hide_On_Startup.ToString());
            sec.AddKey(nameof(Window_Anchors),      GetWindowAnchorsString());

            if (!Directory.Exists(ExplorerCore.Loader.ConfigFolder))
                Directory.CreateDirectory(ExplorerCore.Loader.ConfigFolder);

            File.WriteAllText(INI_PATH, data.ToString());
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
                Vector4 ret = Vector4.zero;
                ret.x = float.Parse(split[0]);
                ret.y = float.Parse(split[1]);
                ret.z = float.Parse(split[2]);
                ret.w = float.Parse(split[3]);
                return ret;
            }
            catch
            {
                Window_Anchors = DEFAULT_WINDOW_ANCHORS;
                return GetWindowAnchorsVector();
            }
        }

        internal static string GetWindowAnchorsString()
        {
            try
            {
                var rect = PanelDragger.Instance.Panel;
                return $"{rect.anchorMin.x},{rect.anchorMin.y},{rect.anchorMax.x},{rect.anchorMax.y}";
            }
            catch
            {
                return DEFAULT_WINDOW_ANCHORS;
            }
        }
    }
}
