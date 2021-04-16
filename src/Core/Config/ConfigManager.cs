using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityExplorer.UI;

namespace UnityExplorer.Core.Config
{
    public static class ConfigManager
    {
        // Each Mod Loader has its own ConfigHandler.
        // See the UnityExplorer.Loader namespace for the implementations.
        public static ConfigHandler Handler { get; private set; }

        public static ConfigElement<KeyCode>    Main_Menu_Toggle;
        public static ConfigElement<bool>       Force_Unlock_Mouse;
        public static ConfigElement<KeyCode>    Force_Unlock_Keybind;
        public static ConfigElement<bool>       Aggressive_Force_Unlock;
        //public static ConfigElement<MenuPages>  Default_Tab;
        public static ConfigElement<int>        Default_Page_Limit;
        public static ConfigElement<string>     Default_Output_Path;
        public static ConfigElement<bool>       Log_Unity_Debug;
        public static ConfigElement<bool>       Hide_On_Startup;
        public static ConfigElement<float>      Startup_Delay_Time;

        // internal configs
        internal static InternalConfigHandler InternalHandler { get; private set; }

        public static ConfigElement<string> SceneExplorerData;
        public static ConfigElement<string> GameObjectInspectorData;
        public static ConfigElement<string> MainWindowData;
        public static ConfigElement<string> DebugConsoleData;

        internal static readonly Dictionary<string, IConfigElement> ConfigElements = new Dictionary<string, IConfigElement>();
        internal static readonly Dictionary<string, IConfigElement> InternalConfigs = new Dictionary<string, IConfigElement>();

        public static void Init(ConfigHandler configHandler)
        {
            Handler = configHandler;
            Handler.Init();

            InternalHandler = new InternalConfigHandler();
            InternalHandler.Init();

            CreateConfigElements();

            Handler.LoadConfig();
            InternalHandler.LoadConfig();

            //InitConsoleCallback();
        }

        internal static void RegisterConfigElement<T>(ConfigElement<T> configElement)
        {
            if (!configElement.IsInternal)
            {
                Handler.RegisterConfigElement(configElement);
                ConfigElements.Add(configElement.Name, configElement);
            }
            else
            {
                InternalHandler.RegisterConfigElement(configElement);
                InternalConfigs.Add(configElement.Name, configElement);
            }
        }

        private static void CreateConfigElements()
        {
            Main_Menu_Toggle = new ConfigElement<KeyCode>("Main Menu Toggle",
                "The UnityEngine.KeyCode to toggle the UnityExplorer Menu.",
                KeyCode.F7);

            Hide_On_Startup = new ConfigElement<bool>("Hide On Startup",
                "Should UnityExplorer be hidden on startup?",
                false);

            Force_Unlock_Mouse = new ConfigElement<bool>("Force Unlock Mouse",
                "Force the Cursor to be unlocked (visible) when the UnityExplorer menu is open.",
                true);

            Force_Unlock_Keybind = new ConfigElement<KeyCode>("Force Unlock Keybind",
                "The keybind to toggle the 'Force Unlock Mouse' setting. Only usable when UnityExplorer is open.",
                KeyCode.F6);

            Aggressive_Force_Unlock = new ConfigElement<bool>("Aggressive Mouse Unlock",
                "Use Camera.onPostRender callback to aggressively force the Mouse to be unlocked (requires game restart).",
                false);

            //Default_Tab = new ConfigElement<MenuPages>("Default Tab",
            //    "The default menu page when starting the game.",
            //    MenuPages.Home);

            Log_Unity_Debug = new ConfigElement<bool>("Log Unity Debug",
                "Should UnityEngine.Debug.Log messages be printed to UnityExplorer's log?",
                false);

            Default_Page_Limit = new ConfigElement<int>("Default Page Limit",
                "The default maximum number of elements per 'page' in UnityExplorer.",
                25);

            Default_Output_Path = new ConfigElement<string>("Default Output Path",
                "The default output path when exporting things from UnityExplorer.",
                Path.Combine(ExplorerCore.Loader.ExplorerFolder, "Output"));

            Startup_Delay_Time = new ConfigElement<float>("Startup Delay Time",
                "The delay on startup before the UI is created.",
                1f);

            // Internal configs

            SceneExplorerData = new ConfigElement<string>("SceneExplorer", "", "", true);
            GameObjectInspectorData = new ConfigElement<string>("GameObjectInspector", "", "", true);
            MainWindowData = new ConfigElement<string>("MainWindow", "", "", true);
            DebugConsoleData = new ConfigElement<string>("DebugConsole", "", "", true);
        }
    }
}
