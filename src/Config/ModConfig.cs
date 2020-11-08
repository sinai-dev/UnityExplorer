using System;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace UnityExplorer.Config
{
    public class ModConfig
    {
        [XmlIgnore] public static readonly XmlSerializer Serializer = new XmlSerializer(typeof(ModConfig));

        //[XmlIgnore] private const string EXPLORER_FOLDER = @"Mods\UnityExplorer";
        [XmlIgnore] private const string SETTINGS_PATH = ExplorerCore.EXPLORER_FOLDER + @"\config.xml";

        [XmlIgnore] public static ModConfig Instance;

        // Actual configs
        public KeyCode  Main_Menu_Toggle    = KeyCode.F7;
        public bool     Force_Unlock_Mouse  = true;
        public int      Default_Page_Limit  = 20;
        public string   Default_Output_Path = ExplorerCore.EXPLORER_FOLDER;
        public bool     Log_Unity_Debug     = false;

        public static event Action OnConfigChanged;

        internal static void InvokeConfigChanged()
        {
            OnConfigChanged?.Invoke();
        }

        public static void OnLoad()
        {
            if (LoadSettings())
                return;

            Instance = new ModConfig();
            SaveSettings();
        }

        public static bool LoadSettings()
        {
            if (!File.Exists(SETTINGS_PATH))
                return false;

            try
            {
                using (var file = File.OpenRead(SETTINGS_PATH))
                    Instance = (ModConfig)Serializer.Deserialize(file);
            }
            catch
            {
                return false;
            }

            return Instance != null;
        }

        public static void SaveSettings()
        {
            if (File.Exists(SETTINGS_PATH))
                File.Delete(SETTINGS_PATH);

            using (var file = File.Create(SETTINGS_PATH))
                Serializer.Serialize(file, Instance);
        }
    }
}
