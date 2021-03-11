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
        public const string VERSION = "3.2.1";
        public const string AUTHOR = "Sinai";
        public const string GUID = "com.sinai.unityexplorer";

        public static ExplorerCore Instance { get; private set; }

        private static IExplorerLoader s_loader;
        public static IExplorerLoader Loader => s_loader
#if ML
                                             ?? (s_loader = ExplorerMelonMod.Instance);   
#elif BIE
                                             ?? (s_loader = ExplorerBepInPlugin.Instance);
#elif STANDALONE
                                             ?? (s_loader = ExplorerStandalone.Instance);
#endif

        public static string ExplorerFolder => Loader.ExplorerFolder;

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

            if (!Directory.Exists(ExplorerFolder))
                Directory.CreateDirectory(ExplorerFolder);

            ExplorerConfig.OnLoad();

            InputManager.Init();
            ForceUnlockCursor.Init();

            SetupEvents();

            UIManager.ShowMenu = true;

            Log($"{NAME} {VERSION} initialized.");
        }

        public static void Update()
        {
            UIManager.CheckUIInit();

            if (MouseInspector.Enabled)
                MouseInspector.UpdateInspect();
            else
                UIManager.Update();
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
            catch
            {
               // exceptions here are non-fatal, just ignore. 
            }
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

            Loader.OnLogMessage(message);
        }

        public static void LogWarning(object message, bool unity = false)
        {
            DebugConsole.Log(message?.ToString(), "FFFF00");

            if (unity)
                return;

            Loader.OnLogWarning(message);
        }

        public static void LogError(object message, bool unity = false)
        {
            DebugConsole.Log(message?.ToString(), "FF0000");

            if (unity)
                return;

            Loader.OnLogError(message);
        }
    }
}
