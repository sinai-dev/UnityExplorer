#if STANDALONE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityExplorer.Core.Config;
using IniParser.Parser;
using System.IO;
using UnityEngine;

namespace UnityExplorer.Loader.STANDALONE
{
    public class StandaloneConfigHandler : ConfigHandler
    {
        internal static IniDataParser _parser;
        internal static string CONFIG_PATH;

        public override void Init()
        {
            CONFIG_PATH = Path.Combine(ExplorerCore.Loader.ExplorerFolder, "config.ini");
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
            // Not necessary, just save.
            SaveConfig();
        }

        public override T GetConfigValue<T>(ConfigElement<T> element)
        {
            // Not necessary, just return the value.
            return element.Value;
        }

        public bool TryLoadConfig()
        {
            try
            {
                if (!File.Exists(CONFIG_PATH))
                    return false;

                string ini = File.ReadAllText(CONFIG_PATH);

                var data = _parser.Parse(ini);

                foreach (var config in data.Sections["Config"])
                {
                    if (ConfigManager.ConfigElements.TryGetValue(config.KeyName, out IConfigElement configElement))
                        configElement.BoxedValue = StringToConfigValue(config.Value, configElement.ElementType);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public object StringToConfigValue(string value, Type elementType)
        {
            if (elementType == typeof(KeyCode))
                return (KeyCode)Enum.Parse(typeof(KeyCode), value);
            else if (elementType == typeof(bool))
                return bool.Parse(value);
            else if (elementType == typeof(int))
                return int.Parse(value);
            else 
                return value;
        }

        public override void OnAnyConfigChanged()
        {
            SaveConfig();
        }

        public override void SaveConfig()
        {
            var data = new IniParser.Model.IniData();

            data.Sections.AddSection("Config");
            var sec = data.Sections["Config"];

            foreach (var entry in ConfigManager.ConfigElements)
                sec.AddKey(entry.Key, entry.Value.BoxedValue.ToString());

            if (!Directory.Exists(ExplorerCore.Loader.ExplorerFolder))
                Directory.CreateDirectory(ExplorerCore.Loader.ExplorerFolder);

            File.WriteAllText(CONFIG_PATH, data.ToString());
        }
    }
}

#endif