#if ML
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExplorer.Config;

namespace UnityExplorer.Loader.ML
{
    public class MelonLoaderConfigHandler : ConfigHandler
    {
        internal const string CTG_NAME = "UnityExplorer";

        internal MelonPreferences_Category prefCategory;

        public override void Init()
        {
            prefCategory = MelonPreferences.CreateCategory(CTG_NAME, $"{CTG_NAME} Settings", false, true);
        }

        public override void LoadConfig()
        {
            foreach (var entry in ConfigManager.ConfigElements)
            {
                var key = entry.Key;
                if (prefCategory.GetEntry(key) is MelonPreferences_Entry)
                {
                    var config = entry.Value;
                    config.BoxedValue = config.GetLoaderConfigValue();
                }
            }
        }

        // This wrapper exists to handle the "LemonAction" delegates which ML now uses in 0.4.4+.
        // Reflection is required since the delegate type changed between 0.4.3 and 0.4.4.
        // A wrapper class is required to link the MelonPreferences_Entry and the delegate instance.
        public class EntryDelegateWrapper<T>
        {
            public MelonPreferences_Entry<T> entry;
            public ConfigElement<T> config;

            public EntryDelegateWrapper(MelonPreferences_Entry<T> entry, ConfigElement<T> config)
            {
                this.entry = entry;
                this.config = config;
                var evt = entry.GetType().GetEvent("OnValueChangedUntyped");
                evt.AddEventHandler(entry, Delegate.CreateDelegate(evt.EventHandlerType, this, GetType().GetMethod("OnChanged")));
            }

            public void OnChanged()
            {
                if ((entry.Value == null && config.Value == null) || config.Value.Equals(entry.Value))
                    return;
                config.Value = entry.Value;
            }
        }

        public override void RegisterConfigElement<T>(ConfigElement<T> config)
        {
            var entry = prefCategory.CreateEntry(config.Name, config.Value, null, config.Description, config.IsInternal, false);
            new EntryDelegateWrapper<T>(entry, config);
        }

        public override void SetConfigValue<T>(ConfigElement<T> config, T value)
        {
            if (prefCategory.GetEntry<T>(config.Name) is MelonPreferences_Entry<T> entry)
            { 
                entry.Value = value;
                //entry.Save();
            }
        }

        public override T GetConfigValue<T>(ConfigElement<T> config)
        {
            if (prefCategory.GetEntry<T>(config.Name) is MelonPreferences_Entry<T> entry)
                return entry.Value;

            return default;
        }

        public override void OnAnyConfigChanged()
        {
        }

        public override void SaveConfig()
        {
            MelonPreferences.Save();
        }
    }
}
#endif