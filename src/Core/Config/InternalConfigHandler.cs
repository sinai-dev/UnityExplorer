using IniParser.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExplorer.UI;

namespace UnityExplorer.Core.Config
{
    public class InternalConfigHandler : ConfigHandler
    {
        internal static IniDataParser _parser;
        internal static string INI_PATH;

        public override void Init()
        {
            INI_PATH = Path.Combine(ExplorerCore.Loader.ExplorerFolder, "data.ini");
            _parser = new IniDataParser();
            _parser.Configuration.CommentString = "#";
        }

        public override void LoadConfig()
        {
            if (!TryLoadConfig())
                SaveConfig();
        }

        public override void RegisterConfigElement<T>(ConfigElement<T> element)
        {
            // Not necessary
        }

        public override void SetConfigValue<T>(ConfigElement<T> element, T value)
        {
            // Not necessary
        }

        public override T GetConfigValue<T>(ConfigElement<T> element)
        {
            // Not necessary, just return the value.
            return element.Value;
        }

        public override void OnAnyConfigChanged()
        {
            SaveConfig();
        }

        public bool TryLoadConfig()
        {
            try
            {
                ExplorerCore.Log("Loading internal data");

                if (!File.Exists(INI_PATH))
                    return false;

                string ini = File.ReadAllText(INI_PATH);

                var data = _parser.Parse(ini);

                foreach (var config in data.Sections["Config"])
                {
                    if (ConfigManager.InternalConfigs.TryGetValue(config.KeyName, out IConfigElement configElement))
                        configElement.BoxedValue = StringToConfigValue(config.Value, configElement.ElementType);
                }

                ExplorerCore.Log("Loaded");

                return true;
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning("Error loading internal data: " + ex.ToString());
                return false;
            }
        }

        public override void SaveConfig()
        {
            if (UIManager.Initializing)
                return;

            var data = new IniParser.Model.IniData();

            data.Sections.AddSection("Config");
            var sec = data.Sections["Config"];

            foreach (var entry in ConfigManager.InternalConfigs)
                sec.AddKey(entry.Key, entry.Value.BoxedValue.ToString());

            if (!Directory.Exists(ExplorerCore.Loader.ConfigFolder))
                Directory.CreateDirectory(ExplorerCore.Loader.ConfigFolder);

            File.WriteAllText(INI_PATH, data.ToString());
        }

        public object StringToConfigValue(string value, Type elementType)
        {
            if (elementType.IsEnum)
                return Enum.Parse(elementType, value);
            else if (elementType == typeof(bool))
                return bool.Parse(value);
            else if (elementType == typeof(int))
                return int.Parse(value);
            else
                return value;
        }
    }
}
