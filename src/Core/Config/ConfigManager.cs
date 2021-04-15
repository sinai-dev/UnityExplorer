using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace UnityExplorer.Core.Config
{
    public static class ConfigManager
    {
        // Each Mod Loader has its own ConfigHandler.
        // See the UnityExplorer.Loader namespace for the implementations.
        public static ConfigHandler Handler { get; private set; }

        public static ConfigElement<KeyCode>    Main_Menu_Toggle;
        public static ConfigElement<bool>       Force_Unlock_Mouse;
        //public static ConfigElement<MenuPages>  Default_Tab;
        public static ConfigElement<int>        Default_Page_Limit;
        public static ConfigElement<string>     Default_Output_Path;
        public static ConfigElement<bool>       Log_Unity_Debug;
        public static ConfigElement<bool>       Hide_On_Startup;
        public static ConfigElement<float>      Startup_Delay_Time;

        public static ConfigElement<string>     Last_Window_Anchors;
        public static ConfigElement<string>     Last_Window_Position;
        public static ConfigElement<bool>       Last_DebugConsole_State;
        public static ConfigElement<bool>       Last_SceneExplorer_State;

        internal static readonly Dictionary<string, IConfigElement> ConfigElements = new Dictionary<string, IConfigElement>();

        public static void Init(ConfigHandler configHandler)
        {
            Handler = configHandler;
            Handler.Init();

            CreateConfigElements();

            Handler.LoadConfig();

            //SceneExplorer.OnToggleShow += SceneExplorer_OnToggleShow;
            //PanelDragger.OnFinishResize += PanelDragger_OnFinishResize;
            //PanelDragger.OnFinishDrag += PanelDragger_OnFinishDrag;
            //DebugConsole.OnToggleShow += DebugConsole_OnToggleShow;

            InitConsoleCallback();
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
                KeyCode.F7);

            Hide_On_Startup = new ConfigElement<bool>("Hide On Startup",
                "Should UnityExplorer be hidden on startup?",
                false);

            //Default_Tab = new ConfigElement<MenuPages>("Default Tab",
            //    "The default menu page when starting the game.",
            //    MenuPages.Home);

            Log_Unity_Debug = new ConfigElement<bool>("Log Unity Debug",
                "Should UnityEngine.Debug.Log messages be printed to UnityExplorer's log?",
                false);

            Force_Unlock_Mouse = new ConfigElement<bool>("Force Unlock Mouse",
                "Force the Cursor to be unlocked (visible) when the UnityExplorer menu is open.",
                true);

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

            Last_Window_Anchors = new ConfigElement<string>("Last_Window_Anchors",
                "For internal use, the last anchors of the UnityExplorer window.",
                DEFAULT_WINDOW_ANCHORS,
                true);

            Last_Window_Position = new ConfigElement<string>("Last_Window_Position",
                "For internal use, the last position of the UnityExplorer window.",
                DEFAULT_WINDOW_POSITION,
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
            Last_Window_Anchors.Value = rect.RectAnchorsToString();
            PanelDragger_OnFinishDrag(rect);
        }

        private static void PanelDragger_OnFinishDrag(RectTransform rect)
        {
            Last_Window_Position.Value = rect.RectPositionToString();
        }

        private static void DebugConsole_OnToggleShow(bool showing)
        {
            Last_DebugConsole_State.Value = showing;
        }

        private static void SceneExplorer_OnToggleShow(bool showing)
        {
            Last_SceneExplorer_State.Value = showing;
        }

        #region CONSOLE ONEXIT CALLBACK

        internal static void InitConsoleCallback()
        {
            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            SetConsoleCtrlHandler(handler, true);
        }

        static bool ConsoleEventCallback(int eventType)
        {
            // 2 is Console Quit
            if (eventType == 2)
                Handler.SaveConfig();

            return false;
        }

        static ConsoleEventDelegate handler;
        private delegate bool ConsoleEventDelegate(int eventType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        #endregion

        #region WINDOW ANCHORS / POSITION HELPERS

        // Window Anchors helpers

        private const string DEFAULT_WINDOW_ANCHORS = "0.25,0.10,0.78,0.95";
        private const string DEFAULT_WINDOW_POSITION = "0,0";

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

        internal static string RectPositionToString(this RectTransform rect)
        {
            return string.Format(_enCulture, "{0},{1}", new object[]
            {
                rect.localPosition.x, rect.localPosition.y
            });
        }

        internal static void SetPositionFromString(this RectTransform rect, string stringPosition)
        {
            try
            {
                var split = stringPosition.Split(',');

                if (split.Length != 2)
                    throw new Exception();

                Vector3 vector = rect.localPosition;
                vector.x = float.Parse(split[0], _enCulture);
                vector.y = float.Parse(split[1], _enCulture);
                rect.localPosition = vector;
            }
            catch //(Exception ex)
            {
                //ExplorerCore.LogWarning("Exception setting window position: " + ex);
            }
        }

        #endregion
    }
}
