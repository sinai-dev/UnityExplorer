using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;

namespace Explorer
{
    public class ModConfig
    {
        [XmlIgnore] public static readonly XmlSerializer Serializer = new XmlSerializer(typeof(ModConfig));

        [XmlIgnore] private const string EXPLORER_FOLDER = @"Mods\CppExplorer";
        [XmlIgnore] private const string SETTINGS_PATH = EXPLORER_FOLDER + @"\config.xml";

        [XmlIgnore] public static ModConfig Instance;

        public KeyCode Main_Menu_Toggle = KeyCode.F7;
        public Vector2 Default_Window_Size = new Vector2(550, 700);

        public static void OnLoad()
        {
            if (!Directory.Exists(EXPLORER_FOLDER))
            {
                Directory.CreateDirectory(EXPLORER_FOLDER);
            }

            if (File.Exists(SETTINGS_PATH))
            {
                LoadSettings(false);
            }
            else
            {
                Instance = new ModConfig();
                SaveSettings(false);
            }
        }

        public static void LoadSettings(bool checkExist = true)
        {
            if (checkExist && !File.Exists(SETTINGS_PATH))
                return;

            var file = File.OpenRead(SETTINGS_PATH);
            Instance = (ModConfig)Serializer.Deserialize(file);
            file.Close();
        }

        public static void SaveSettings(bool checkExist = true)
        {
            if (checkExist && File.Exists(SETTINGS_PATH))
                File.Delete(SETTINGS_PATH);

            FileStream file = File.Create(SETTINGS_PATH);
            Serializer.Serialize(file, Instance);
            file.Close();
        }
    }
}
