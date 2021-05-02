using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Input;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI;
using UnityExplorer.UI.Inspectors;
using UnityExplorer.UI.Main;

namespace UnityExplorer
{
    public class ExplorerCore
    {
        public const string NAME = "UnityExplorer";
        public const string VERSION = "3.3.15";
        public const string AUTHOR = "Sinai";
        public const string GUID = "com.sinai.unityexplorer";

        public static ExplorerCore Instance { get; private set; }

        public static IExplorerLoader Loader { get; private set; }

        // Prevent using ctor, must use Init method.
        private ExplorerCore() { }

        public static void Init(IExplorerLoader loader)
        {
            if (Instance != null)
            {
                Log("An instance of UnityExplorer is already active!");
                return;
            }

            Loader = loader;
            Instance = new ExplorerCore();

            if (!Directory.Exists(Loader.ExplorerFolder))
                Directory.CreateDirectory(Loader.ExplorerFolder);

            ConfigManager.Init(Loader.ConfigHandler);

            RuntimeProvider.Init();

            InputManager.Init();

            Log($"{NAME} {VERSION} initialized.");

            RuntimeProvider.Instance.StartCoroutine(SetupCoroutine());
        }

        // Do a delayed setup so that objects aren't destroyed instantly.
        // This can happen for a multitude of reasons.
        // Default delay is 1 second which is usually enough.
        private static IEnumerator SetupCoroutine()
        {
            float f = Time.realtimeSinceStartup;
            float delay = ConfigManager.Startup_Delay_Time.Value;
            while (Time.realtimeSinceStartup - f < delay)
                yield return null;

            Log($"Creating UI, after delay of {delay} second(s).");
            UIManager.Init();

            //InspectorManager.Instance.Inspect(typeof(TestClass));
        }

        public static void Update()
        {
            RuntimeProvider.Instance.Update();

            UIManager.Update();
        }

        public static void Log(object message) 
            => Log(message, LogType.Log, false);

        public static void LogWarning(object message) 
            => Log(message, LogType.Warning, false);

        public static void LogError(object message) 
            => Log(message, LogType.Error, false);

        internal static void Log(object message, LogType logType, bool isFromUnity = false)
        {
            if (isFromUnity && !ConfigManager.Log_Unity_Debug.Value)
                return;

            string log = message?.ToString() ?? "";

            switch (logType)
            {
                case LogType.Assert:
                case LogType.Log:
                    Loader.OnLogMessage(log);
                    DebugConsole.Log(log, Color.white);
                    break;

                case LogType.Warning:
                    Loader.OnLogWarning(log);
                    DebugConsole.Log(log, Color.yellow);
                    break;

                case LogType.Error:
                case LogType.Exception:
                    Loader.OnLogError(log);
                    DebugConsole.Log(log, Color.red);
                    break;
            }
        }
    }
}
