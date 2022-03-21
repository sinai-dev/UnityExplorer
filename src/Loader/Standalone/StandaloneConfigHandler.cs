#if STANDALONE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityExplorer.Config;
using System.IO;
using UnityEngine;
using Tomlet;
using Tomlet.Models;

namespace UnityExplorer.Loader.Standalone
{
    public class StandaloneConfigHandler : ConfigHandler
    {
        internal static string CONFIG_PATH;

        public override void Init()
        {
            CONFIG_PATH = Path.Combine(ExplorerCore.Loader.ExplorerFolder, "config.cfg");
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

                var document = TomlParser.ParseFile(CONFIG_PATH);
                foreach (var key in document.Keys)
                {
                    var config = ConfigManager.ConfigElements[key];
                    config.BoxedValue = StringToConfigValue(document.GetValue(key).StringValue, config.ElementType);
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
            var document = TomlDocument.CreateEmpty();
            foreach (var config in ConfigManager.ConfigElements)
                document.Put(config.Key, config.Value.BoxedValue.ToString());

            if (!Directory.Exists(ExplorerCore.Loader.ExplorerFolder))
                Directory.CreateDirectory(ExplorerCore.Loader.ExplorerFolder);

            File.WriteAllText(CONFIG_PATH, document.SerializedValue);
        }
    }
}

#endif