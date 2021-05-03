#if ML
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExplorer.Core;
using UnityExplorer.Core.Config;

// TEMPORARY - JUST REQUIRED UNTIL ML 0.3.1 RELEASED
using MelonLoader.Tomlyn.Model;

// ML 0.3.1 SUPPORT
//using Tomlet;
//using Tomlet.Models;

namespace UnityExplorer.Loader.ML
{
    public class MelonLoaderConfigHandler : ConfigHandler
    {
        internal const string CTG_NAME = "UnityExplorer";

        internal MelonPreferences_Category prefCategory;

        public override void Init()
        {
            prefCategory = MelonPreferences.CreateCategory(CTG_NAME, $"{CTG_NAME} Settings");

            // TEMPORARY - JUST REQUIRED UNTIL ML 0.3.1 RELEASED
            try { MelonPreferences.Mapper.RegisterMapper(KeycodeReader, KeycodeWriter); } catch { }
            //try { MelonPreferences.Mapper.RegisterMapper(MenuPagesReader, MenuPagesWriter); } catch { }

            // ML 0.3.1 SUPPORT
            //try { TomletMain.RegisterMapper(KeycodeWriter, KeycodeReader); } catch { }
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

        public override void RegisterConfigElement<T>(ConfigElement<T> config)
        {
            var entry = prefCategory.CreateEntry(config.Name, config.Value, null, config.IsInternal) as MelonPreferences_Entry<T>;
            
            entry.OnValueChangedUntyped += () => 
            {
                if ((entry.Value == null && config.Value == null) || config.Value.Equals(entry.Value))
                    return;

                config.Value = entry.Value;
            };
        }

        public override void SetConfigValue<T>(ConfigElement<T> config, T value)
        {
            if (prefCategory.GetEntry<T>(config.Name) is MelonPreferences_Entry<T> entry)
            { 
                entry.Value = value;
                entry.Save();
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

        // TEMPORARY - JUST REQUIRED UNTIL ML 0.3.1 RELEASED
        public static KeyCode KeycodeReader(TomlObject value)
        {
            try
            {
                KeyCode kc = (KeyCode)Enum.Parse(typeof(KeyCode), (value as TomlString).Value);

                if (kc == default)
                    throw new Exception();

                return kc;
            }
            catch
            {
                return KeyCode.F7;
            }
        }

        public static TomlObject KeycodeWriter(KeyCode value)
        {
            return MelonPreferences.Mapper.ToToml(value.ToString());
        }

        // ML 0.3.1 SUPPORT
        /*
        public static TomlValue KeycodeWriter(KeyCode value) => TomletMain.ValueFrom(value.ToString());
        public static KeyCode KeycodeReader(TomlValue value)
        {
            try
            {
                KeyCode kc = (KeyCode)Enum.Parse(typeof(KeyCode), value.StringValue);
                if (kc == default)
                    throw new Exception();
                return kc;
            }
            catch { }
            return KeyCode.F7;
        }
        */

        //public static UI.Main.MenuPages MenuPagesReader(TomlObject value)
        //{
        //    try
        //    {
        //        var kc = (UI.Main.MenuPages)Enum.Parse(typeof(UI.Main.MenuPages), (value as TomlString).Value);

        //        if (kc == default)
        //            throw new Exception();

        //        return kc;
        //    }
        //    catch
        //    {
        //        return UI.Main.MenuPages.Home;
        //    }
        //}

        //public static TomlObject MenuPagesWriter(UI.Main.MenuPages value)
        //{
        //    return MelonPreferences.Mapper.ToToml(value.ToString());
        //}
    }
}

#endif