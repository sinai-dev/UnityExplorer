using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Unity;
using UnityExplorer.Core.Input;
using UnityExplorer.Core.Inspectors;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI;
using UnityExplorer.UI.Main;
using UnityExplorer.UI.Utility;

namespace UnityExplorer
{
    public class ExplorerCore
    {
        public const string NAME = "UnityExplorer";
        public const string VERSION = "3.2.8";
        public const string AUTHOR = "Sinai";
        public const string GUID = "com.sinai.unityexplorer";

        public static ExplorerCore Instance { get; private set; }

        public static IExplorerLoader Loader =>
#if ML
            ExplorerMelonMod.Instance;
#elif BIE
            ExplorerBepInPlugin.Instance;
#elif STANDALONE
            ExplorerStandalone.Instance;
#endif

        public static string EXPLORER_FOLDER => Loader.ExplorerFolder;

        public ExplorerCore()
        {
            if (Instance != null)
            {
                Log("An instance of Explorer is already active!");
                return;
            }

            Instance = this;

            if (!Directory.Exists(EXPLORER_FOLDER))
                Directory.CreateDirectory(EXPLORER_FOLDER);

            ExplorerConfig.OnLoad();

            RuntimeProvider.Init();

            InputManager.Init();

            CursorUnlocker.Init();

            UIManager.ShowMenu = true;

            Log($"{NAME} {VERSION} initialized.");
        }

        public static void Update()
        {
            UIManager.CheckUIInit();

            if (InspectUnderMouse.Enabled)
                InspectUnderMouse.UpdateInspect();
            else
                UIManager.Update();
        }

        public void OnUnityLog(string message, string stackTrace, LogType type)
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
