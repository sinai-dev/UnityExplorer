using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Input;
using UnityExplorer.Core.Runtime;
using UnityExplorer.Tests;
using UnityExplorer.UI;
using UnityExplorer.Inspectors;
using UnityExplorer.ObjectExplorer;
using UnityExplorer.UI.Panels;
using HarmonyLib;

namespace UnityExplorer
{
    public static class ExplorerCore
    {
        public const string NAME = "UnityExplorer";
        public const string VERSION = "4.2.0";
        public const string AUTHOR = "Sinai";
        public const string GUID = "com.sinai.unityexplorer";

        public static IExplorerLoader Loader { get; private set; }
        public static RuntimeContext Context { get; internal set; }

        public static HarmonyLib.Harmony Harmony { get; } = new HarmonyLib.Harmony(GUID);

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
            Loader = loader;

            Log($"{NAME} {VERSION} initializing...");

            ExplorerBehaviour.Setup();

            if (!Directory.Exists(Loader.ExplorerFolder))
                Directory.CreateDirectory(Loader.ExplorerFolder);

            ConfigManager.Init(Loader.ConfigHandler);
            ReflectionUtility.Init();
            RuntimeProvider.Init();
            
            SceneHandler.Init();
            InputManager.Init();

            RuntimeProvider.Instance.StartCoroutine(SetupCoroutine());

            Log($"Finished core setup, waiting for UI setup...");
        }

        // Do a delayed setup so that objects aren't destroyed instantly.
        // This can happen for a multitude of reasons.
        // Default delay is 1 second which is usually enough.
        private static IEnumerator SetupCoroutine()
        {
            yield return null;

            float start = Time.realtimeSinceStartup;
            float delay = ConfigManager.Startup_Delay_Time.Value;

            while (delay > 0)
            {
                float diff = Math.Max(Time.deltaTime, Time.realtimeSinceStartup - start);
                delay -= diff;
                yield return null;
            }

            Log($"Creating UI...");

            UIManager.InitUI();

            Log($"{NAME} {VERSION} initialized.");
        }

        /// <summary>
        /// Should be called once per frame.
        /// </summary>
        public static void Update()
        {
            RuntimeProvider.Instance.Update();

            UIManager.Update();
        }

        public static void FixedUpdate()
        {
            RuntimeProvider.Instance.ProcessFixedUpdate();
        }

        public static void OnPostRender()
        {
            RuntimeProvider.Instance.ProcessOnPostRender();
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

            LogPanel.Log(log, logType);

            switch (logType)
            {
                case LogType.Assert:
                case LogType.Log:
                    Loader.OnLogMessage(log);
                    break;

                case LogType.Warning:
                    Loader.OnLogWarning(log);
                    break;

                case LogType.Error:
                case LogType.Exception:
                    Loader.OnLogError(log);
                    break;
            }
        }

        #endregion
    }
}
