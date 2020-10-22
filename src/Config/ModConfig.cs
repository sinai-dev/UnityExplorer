using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace Explorer.Config
{
    public class ModConfig
    {
        [XmlIgnore] public static readonly XmlSerializer Serializer = new XmlSerializer(typeof(ModConfig));

        [XmlIgnore] private const string EXPLORER_FOLDER = @"Mods\Explorer";
        [XmlIgnore] private const string SETTINGS_PATH = EXPLORER_FOLDER + @"\config.xml";

        [XmlIgnore] public static ModConfig Instance;

        // Actual configs
        public KeyCode Main_Menu_Toggle    = KeyCode.F7;
        public Vector2 Default_Window_Size = new Vector2(550, 700);
        public int     Default_Page_Limit  = 20;
        public bool    Bitwise_Support     = false;
        public bool    Tab_View            = true;
        public string  Default_Output_Path = @"Mods\Explorer";

        public static void OnLoad()
        {
            if (!Directory.Exists(EXPLORER_FOLDER))
            {
                Directory.CreateDirectory(EXPLORER_FOLDER);
            }

            if (LoadSettings()) return;

            Instance = new ModConfig();
            SaveSettings();
        }

        // returns true if settings successfully loaded
        public static bool LoadSettings()
        {
            if (!File.Exists(SETTINGS_PATH))
                return false;

            try
            {
                using (var file = File.OpenRead(SETTINGS_PATH))
                {
                    Instance = (ModConfig)Serializer.Deserialize(file);
                }
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
            {
                Serializer.Serialize(file, Instance);
            }
        }
    }
}
