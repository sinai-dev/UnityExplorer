using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityExplorer.Config;
using UnityExplorer.Helpers;
using UnityExplorer.Input;
using UnityExplorer.Inspectors;
using UnityExplorer.UI;
using UnityExplorer.UI.Modules;

namespace UnityExplorer
{
    public class ExplorerCore
    {
        public const string NAME = "UnityExplorer";
        public const string VERSION = "3.1.10";
        public const string AUTHOR = "Sinai";
        public const string GUID = "com.sinai.unityexplorer";

#if ML
        public static string EXPLORER_FOLDER = Path.Combine("Mods", NAME);
#elif BIE
        public static string EXPLORER_FOLDER = Path.Combine(BepInEx.Paths.ConfigPath, NAME);
#elif STANDALONE
        public static string EXPLORER_FOLDER
        {
            get
            {
                if (s_explorerFolder == null)
                {
                    s_explorerFolder = (new Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath;
                    s_explorerFolder = Uri.UnescapeDataString(s_explorerFolder);
                    s_explorerFolder = Path.GetDirectoryName(s_explorerFolder);                    
                }
                
                return s_explorerFolder;
            }
        }
        private static string s_explorerFolder;
#endif

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

                    if (ModConfig.Instance.Hide_On_Startup)
                        ShowMenu = false;

                    // InspectorManager.Instance.Inspect(Tests.TestClass.Instance);
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

#if STANDALONE
        public static Action<string> OnLogMessage;
        public static Action<string> OnLogWarning;
        public static Action<string> OnLogError;
#endif

        public static void Log(object message, bool unity = false)
        {
            DebugConsole.Log(message?.ToString());

            if (unity)
                return;

#if ML
            MelonLoader.MelonLogger.Log(message?.ToString());
#elif BIE
            ExplorerBepInPlugin.Logging?.LogMessage(message?.ToString());
#elif STANDALONE
            OnLogMessage?.Invoke(message?.ToString());
#endif
        }

        public static void LogWarning(object message, bool unity = false)
        {
            DebugConsole.Log(message?.ToString(), "FFFF00");

            if (unity)
                return;

#if ML
            MelonLoader.MelonLogger.LogWarning(message?.ToString());
#elif BIE
            ExplorerBepInPlugin.Logging?.LogWarning(message?.ToString());
#elif STANDALONE
            OnLogWarning?.Invoke(message?.ToString());
#endif
        }

        public static void LogError(object message, bool unity = false)
        {
            DebugConsole.Log(message?.ToString(), "FF0000");

            if (unity)
                return;

#if ML
            MelonLoader.MelonLogger.LogError(message?.ToString());
#elif BIE
            ExplorerBepInPlugin.Logging?.LogError(message?.ToString());
#elif STANDALONE
            OnLogError?.Invoke(message?.ToString());
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
