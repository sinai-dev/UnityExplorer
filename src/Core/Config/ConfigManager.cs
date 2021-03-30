using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExplorer.UI.Main;
using UnityExplorer.UI.Main.Home;

namespace UnityExplorer.Core.Config
{
    public static class ConfigManager
    {
        // Each Loader has its own ConfigHandler.
        // See the UnityExplorer.Loader namespace for the implementations.
        public static IConfigHandler Handler { get; private set; }

        public static ConfigElement<KeyCode> Main_Menu_Toggle;
        public static ConfigElement<int>     Default_Page_Limit;
        public static ConfigElement<string>  Default_Output_Path;
        public static ConfigElement<bool>    Log_Unity_Debug;
        public static ConfigElement<bool>    Hide_On_Startup;
        public static ConfigElement<string>  Last_Window_Anchors;
        public static ConfigElement<int>     Last_Active_Tab;
        public static ConfigElement<bool>    Last_DebugConsole_State;
        public static ConfigElement<bool>    Last_SceneExplorer_State;

        internal static readonly Dictionary<string, IConfigElement> ConfigElements = new Dictionary<string, IConfigElement>();

        public static void Init(IConfigHandler configHandler)
        {
            Handler = configHandler;
            Handler.Init();

            CreateConfigElements();

            Handler.LoadConfig();

            SceneExplorer.OnToggleShow += SceneExplorer_OnToggleShow;
            PanelDragger.OnFinishResize += PanelDragger_OnFinishResize;
            MainMenu.OnActiveTabChanged += MainMenu_OnActiveTabChanged;
            DebugConsole.OnToggleShow += DebugConsole_OnToggleShow;
        }

        internal static void RegisterConfigElement<T>(ConfigElement<T> configElement)
        {
            Handler.RegisterConfigElement(configElement);
            ConfigElements.Add(configElement.Name, configElement);
        }

        private static void CreateConfigElements()
        {
            Main_Menu_Toggle = new ConfigElement<KeyCode>("Main Menu Toggle",
                "The UnityEngine.KeyCode to toggle the UnityExplorer Menu.",
                KeyCode.F7,
                false);

            Default_Page_Limit = new ConfigElement<int>("Default Page Limit",
                "The default maximum number of elements per 'page' in UnityExplorer.",
                25,
                false);

            Default_Output_Path = new ConfigElement<string>("Default Output Path",
                "The default output path when exporting things from UnityExplorer.",
                Path.Combine(ExplorerCore.Loader.ExplorerFolder, "Output"),
                false);

            Log_Unity_Debug = new ConfigElement<bool>("Log Unity Debug",
                "Should UnityEngine.Debug.Log messages be printed to UnityExplorer's log?",
                false,
                false);

            Hide_On_Startup = new ConfigElement<bool>("Hide On Startup",
                "Should UnityExplorer be hidden on startup?",
                false,
                false);

            Last_Window_Anchors = new ConfigElement<string>("Last_Window_Anchors",
                "For internal use, the last anchors of the UnityExplorer window.",
                DEFAULT_WINDOW_ANCHORS,
                true);

            Last_Active_Tab = new ConfigElement<int>("Last_Active_Tab",
                "For internal use, the last active tab index.",
                0,
                true);

            Last_DebugConsole_State = new ConfigElement<bool>("Last_DebugConsole_State",
                "For internal use, the collapsed state of the Debug Console.",
                true,
                true);

            Last_SceneExplorer_State = new ConfigElement<bool>("Last_SceneExplorer_State",
                "For internal use, the collapsed state of the Scene Explorer.",
                true,
                true);
        }

        // Internal config callback listeners

        private static void PanelDragger_OnFinishResize(RectTransform rect)
        {
            Last_Window_Anchors.Value = RectAnchorsToString(rect);
            Handler.SaveConfig();
        }

        private static void MainMenu_OnActiveTabChanged(int page)
        {
            Last_Active_Tab.Value = page;
            Handler.SaveConfig();
        }

        private static void DebugConsole_OnToggleShow(bool showing)
        {
            Last_DebugConsole_State.Value = showing;
            Handler.SaveConfig();
        }

        private static void SceneExplorer_OnToggleShow(bool showing)
        {
            Last_SceneExplorer_State.Value = showing;
            Handler.SaveConfig();
        }

        // Window Anchors helpers

        private const string DEFAULT_WINDOW_ANCHORS = "0.25,0.10,0.78,0.95";

        internal static CultureInfo _enCulture = new CultureInfo("en-US");

        internal static string RectAnchorsToString(this RectTransform rect)
        {
            try
            {
                return string.Format(_enCulture, "{0},{1},{2},{3}", new object[]
                {
                    rect.anchorMin.x,
                    rect.anchorMin.y,
                    rect.anchorMax.x,
                    rect.anchorMax.y
                });
            }
            catch
            {
                return DEFAULT_WINDOW_ANCHORS;
            }
        }

        internal static void SetAnchorsFromString(this RectTransform panel, string stringAnchors)
        {
            Vector4 anchors;
            try
            {
                var split = stringAnchors.Split(',');

                if (split.Length != 4)
                    throw new Exception();

                anchors.x = float.Parse(split[0], _enCulture);
                anchors.y = float.Parse(split[1], _enCulture);
                anchors.z = float.Parse(split[2], _enCulture);
                anchors.w = float.Parse(split[3], _enCulture);
            }
            catch
            {
                anchors = new Vector4(0.25f, 0.1f, 0.78f, 0.95f);
            }

            panel.anchorMin = new Vector2(anchors.x, anchors.y);
            panel.anchorMax = new Vector2(anchors.z, anchors.w);
        }
    }
}
