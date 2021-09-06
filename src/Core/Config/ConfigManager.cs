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

        public static ConfigElement<KeyCode> Master_Toggle;
        public static ConfigElement<UIManager.VerticalAnchor> Main_Navbar_Anchor;
        public static ConfigElement<bool> Force_Unlock_Mouse;
        public static ConfigElement<KeyCode> Force_Unlock_Toggle;
        public static ConfigElement<bool> Aggressive_Mouse_Unlock;
        public static ConfigElement<bool> Disable_EventSystem_Override;
        public static ConfigElement<string> Default_Output_Path;
        public static ConfigElement<bool> Log_Unity_Debug;
        public static ConfigElement<bool> Hide_On_Startup;
        public static ConfigElement<float> Startup_Delay_Time;

        public static ConfigElement<string> Reflection_Signature_Blacklist;

        // internal configs
        internal static InternalConfigHandler InternalHandler { get; private set; }

        public static ConfigElement<string> ObjectExplorerData;
        public static ConfigElement<string> InspectorData;
        public static ConfigElement<string> CSConsoleData;
        public static ConfigElement<string> OptionsPanelData;
        public static ConfigElement<string> ConsoleLogData;
        public static ConfigElement<string> HookManagerData;

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
            Master_Toggle = new ConfigElement<KeyCode>("UnityExplorer Toggle",
                "The key to enable or disable UnityExplorer's menu and features.",
                KeyCode.F7);

            Main_Navbar_Anchor = new ConfigElement<UIManager.VerticalAnchor>("Main Navbar Anchor",
                "The vertical anchor of the main UnityExplorer Navbar, in case you want to move it.",
                UIManager.VerticalAnchor.Top);

            Hide_On_Startup = new ConfigElement<bool>("Hide On Startup",
                "Should UnityExplorer be hidden on startup?",
                false);

            Force_Unlock_Mouse = new ConfigElement<bool>("Force Unlock Mouse",
                "Force the Cursor to be unlocked (visible) when the UnityExplorer menu is open.",
                true);

            Force_Unlock_Toggle = new ConfigElement<KeyCode>("Force Unlock Toggle Key",
                "The keybind to toggle the 'Force Unlock Mouse' setting. Only usable when UnityExplorer is open.",
                KeyCode.None);

            Aggressive_Mouse_Unlock = new ConfigElement<bool>("Aggressive Mouse Unlock",
                "Use WaitForEndOfFrame to aggressively force the Mouse to be unlocked.\n<b>Requires restart to take effect.</b>",
                false);

            Disable_EventSystem_Override = new ConfigElement<bool>("Disable EventSystem override",
                "If enabled, UnityExplorer will not override the EventSystem from the game.\n<b>May require restart to take effect.</b>",
                false);

            Log_Unity_Debug = new ConfigElement<bool>("Log Unity Debug",
                "Should UnityEngine.Debug.Log messages be printed to UnityExplorer's log?",
                false);

            Default_Output_Path = new ConfigElement<string>("Default Output Path",
                "The default output path when exporting things from UnityExplorer.",
                Path.Combine(ExplorerCore.Loader.ExplorerFolder, "Output"));

            Startup_Delay_Time = new ConfigElement<float>("Startup Delay Time",
                "The delay on startup before the UI is created.",
                1f);

            Reflection_Signature_Blacklist = new ConfigElement<string>("Member Signature Blacklist",
                "Use this to blacklist certain member signatures if they are known to cause a crash or other issues.\r\n" +
                "Seperate signatures with a semicolon ';'.\r\n" +
                "For example, to blacklist Camera.main, you would add 'UnityEngine.Camera.main;'",
                "");

            // Internal configs (panel save data)

            ObjectExplorerData = new ConfigElement<string>("ObjectExplorer", "", "", true);
            InspectorData = new ConfigElement<string>("Inspector", "", "", true);
            CSConsoleData = new ConfigElement<string>("CSConsole", "", "", true);
            OptionsPanelData = new ConfigElement<string>("OptionsPanel", "", "", true);
            ConsoleLogData = new ConfigElement<string>("ConsoleLog", "", "", true);
            HookManagerData = new ConfigElement<string>("HookManager", "", "", true);
        }
    }
}
