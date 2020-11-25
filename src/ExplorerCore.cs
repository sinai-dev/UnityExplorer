using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityExplorer.Config;
using UnityExplorer.Input;
using UnityExplorer.Inspectors;
using UnityExplorer.UI;
using UnityExplorer.UI.Modules;
#if CPP
using UnityExplorer.Helpers;
#endif

namespace UnityExplorer
{
    public class ExplorerCore
    {
        public const string NAME = "UnityExplorer";
        public const string VERSION = "3.0.5";
        public const string AUTHOR = "Sinai";
        public const string GUID = "com.sinai.unityexplorer";
        public const string EXPLORER_FOLDER = @"Mods\UnityExplorer";

        public static ExplorerCore Instance { get; private set; }

        public static bool ShowMenu
        {
            get => s_showMenu;
            set => SetShowMenu(value);
        }
        public static bool s_showMenu;

        private static bool s_doneUIInit;
        private static float s_timeSinceStartup;

        public ExplorerCore()
        {
            if (Instance != null)
            {
                Log("An instance of Explorer is already active!");
                return;
            }

            Instance = this;

#if CPP
            ReflectionHelpers.TryLoadGameModules();
#endif

            if (!Directory.Exists(EXPLORER_FOLDER))
                Directory.CreateDirectory(EXPLORER_FOLDER);

            ModConfig.OnLoad();

            InputManager.Init();
            ForceUnlockCursor.Init();

            SetupEvents();

            ShowMenu = true;

            Log($"{NAME} {VERSION} initialized.");
        }

        public static void Update()
        {
            if (!s_doneUIInit)
                CheckUIInit();

            if (MouseInspector.Enabled)
                MouseInspector.UpdateInspect();
            else
            {
                if (InputManager.GetKeyDown(ModConfig.Instance.Main_Menu_Toggle))
                    ShowMenu = !ShowMenu;

                if (ShowMenu && s_doneUIInit)
                    UIManager.Update();
            }
        }

        private static void CheckUIInit()
        {
            s_timeSinceStartup += Time.deltaTime;

            if (s_timeSinceStartup > 0.1f)
            {
                s_doneUIInit = true;
                try
                {
                    UIManager.Init();
                    Log("Initialized UnityExplorer UI.");
                }
                catch (Exception e)
                {
                    LogWarning($"Exception setting up UI: {e}");
                }
            }
        }

        private void SetupEvents()
        {
#if CPP
            try
            {
                Application.add_logMessageReceived(new Action<string, string, LogType>(OnUnityLog));
                SceneManager.add_sceneLoaded(new Action<Scene, LoadSceneMode>((Scene a, LoadSceneMode b) => { OnSceneLoaded(); }));
                SceneManager.add_activeSceneChanged(new Action<Scene, Scene>((Scene a, Scene b) => { OnSceneLoaded(); }));
            }
            catch { }
#else
            Application.logMessageReceived += OnUnityLog;
            SceneManager.sceneLoaded += (Scene a, LoadSceneMode b) => { OnSceneLoaded(); }; 
            SceneManager.activeSceneChanged += (Scene a, Scene b) => { OnSceneLoaded(); };
#endif
        }

        internal void OnSceneLoaded()
        {
            UIManager.OnSceneChange();
        }

        private static void SetShowMenu(bool show)
        {
            s_showMenu = show;

            if (UIManager.CanvasRoot)
            {
                UIManager.CanvasRoot.SetActive(show);

                if (show)
                    ForceUnlockCursor.SetEventSystem();
                else
                    ForceUnlockCursor.ReleaseEventSystem();
            }

            ForceUnlockCursor.UpdateCursorControl();
        }

        private void OnUnityLog(string message, string stackTrace, LogType type)
        {
            if (!DebugConsole.LogUnity)
                return;

            message = $"[UNITY] {message}";

            switch (type)
            {
                case LogType.Assert:
                case LogType.Log:
                    Log(message, true);
                    break;
                case LogType.Warning:
                    LogWarning(message, true);
                    break;
                case LogType.Exception:
                case LogType.Error:
                    LogError(message, true);
                    break;
            }
        }

        public static void Log(object message, bool unity = false)
        {
            DebugConsole.Log(message?.ToString());

            if (unity)
                return;

#if ML
            MelonLoader.MelonLogger.Log(message?.ToString());
#else
            ExplorerBepInPlugin.Logging?.LogMessage(message?.ToString());
#endif
        }

        public static void LogWarning(object message, bool unity = false)
        {
            DebugConsole.Log(message?.ToString(), "FFFF00");

            if (unity)
                return;

#if ML
            MelonLoader.MelonLogger.LogWarning(message?.ToString());
#else
                        ExplorerBepInPlugin.Logging?.LogWarning(message?.ToString());
#endif
        }

        public static void LogError(object message, bool unity = false)
        {
            DebugConsole.Log(message?.ToString(), "FF0000");

            if (unity)
                return;

#if ML
            MelonLoader.MelonLogger.LogError(message?.ToString());
#else
                        ExplorerBepInPlugin.Logging?.LogError(message?.ToString());
#endif
        }


        public static string RemoveInvalidFilenameChars(string s)
        {
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            foreach (var c in invalid)
            {
                s = s.Replace(c.ToString(), "");
            }
            return s;
        }
    }
}
