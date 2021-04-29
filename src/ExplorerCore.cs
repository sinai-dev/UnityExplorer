using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Input;
using UnityExplorer.Core.Runtime;
using UnityExplorer.Tests;
using UnityExplorer.UI;
using UnityExplorer.UI.Inspectors;
using UnityExplorer.UI.Panels;

namespace UnityExplorer
{
    public static class ExplorerCore
    {
        public const string NAME = "UnityExplorer";
        public const string VERSION = "4.0.0";
        public const string AUTHOR = "Sinai";
        public const string GUID = "com.sinai.unityexplorer";

        public static IExplorerLoader Loader { get; private set; }
        public static RuntimeContext Context { get; internal set; }

        /// <summary>
        /// Initialize UnityExplorer with the provided Loader implementation.
        /// </summary>
        public static void Init(IExplorerLoader loader)
        {
            if (Loader != null)
            {
                LogWarning("UnityExplorer is already loaded!");
                return;
            }

            // TEMP DEBUG TEST FOR TRANSFORM TREE

            var stressTest = new GameObject("StressTest");
            for (int i = 0; i < 100; i++)
            {
                var obj = new GameObject($"Parent_{i}");
                obj.transform.parent = stressTest.transform;
                for (int j = 0; j < 100; j++)
                {
                    var obj2 = new GameObject($"Child_{j}");
                    obj2.transform.parent = obj.transform;
                }
            }

            // END

            Loader = loader;

            if (!Directory.Exists(Loader.ExplorerFolder))
                Directory.CreateDirectory(Loader.ExplorerFolder);

            ConfigManager.Init(Loader.ConfigHandler);

            RuntimeProvider.Init();
            SceneHandler.Init();
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

            UIManager.InitUI();

            InspectorManager.Inspect(typeof(TestClass));
            //InspectorManager.Inspect(UIManager.CanvasRoot.gameObject.GetComponent<GraphicRaycaster>());
            //InspectorManager.InspectType(typeof(ReflectionUtility));
        }

        /// <summary>
        /// Should be called once per frame.
        /// </summary>
        public static void Update()
        {
            RuntimeProvider.Instance.Update();

            UIManager.Update();
        }

#region LOGGING

        public static void Log(object message) 
            => Log(message, LogType.Log);

        public static void LogWarning(object message) 
            => Log(message, LogType.Warning);

        public static void LogError(object message) 
            => Log(message, LogType.Error);

        public static void LogUnity(object message, LogType logType)
        {
            if (!ConfigManager.Log_Unity_Debug.Value)
                return;

            Log($"[Unity] {message}", logType);
        }

        private static void Log(object message, LogType logType)
        {
            string log = message?.ToString() ?? "";

            switch (logType)
            {
                case LogType.Assert:
                case LogType.Log:
                    Loader.OnLogMessage(log);
                    //DebugConsole.Log(log, Color.white);
                    break;

                case LogType.Warning:
                    Loader.OnLogWarning(log);
                    //DebugConsole.Log(log, Color.yellow);
                    break;

                case LogType.Error:
                case LogType.Exception:
                    Loader.OnLogError(log);
                    //DebugConsole.Log(log, Color.red);
                    break;
            }
        }

#endregion
    }
}
