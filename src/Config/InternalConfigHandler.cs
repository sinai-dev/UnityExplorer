using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExplorer.UI;
using Tomlet;
using Tomlet.Models;

namespace UnityExplorer.Config
{
    public class InternalConfigHandler : ConfigHandler
    {
        internal static string CONFIG_PATH;

        public override void Init()
        {
            CONFIG_PATH = Path.Combine(ExplorerCore.ExplorerFolder, "data.cfg");
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

        // Not necessary, just return the value.
        public override T GetConfigValue<T>(ConfigElement<T> element) => element.Value;

        // Always just auto-save.
        public override void OnAnyConfigChanged() => SaveConfig();

        public bool TryLoadConfig()
        {
            try
            {
                if (!File.Exists(CONFIG_PATH))
                    return false;

                TomlDocument document = TomlParser.ParseFile(CONFIG_PATH);
                foreach (var key in document.Keys)
                {
                    var panelKey = (UIManager.Panels)Enum.Parse(typeof(UIManager.Panels), key);
                    ConfigManager.GetPanelSaveData(panelKey).Value = document.GetString(key);
                }

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

            var tomlDocument = TomlDocument.CreateEmpty();
            foreach (var entry in ConfigManager.InternalConfigs)
                tomlDocument.Put(entry.Key, entry.Value.BoxedValue as string, false);

            File.WriteAllText(CONFIG_PATH, tomlDocument.SerializedValue);
        }
    }
}
