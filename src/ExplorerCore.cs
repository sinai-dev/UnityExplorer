using System;
using System.Collections;
using System.Linq;
using ExplorerBeta.Config;
using ExplorerBeta.Input;
using ExplorerBeta.UI;
using ExplorerBeta.UI.Main;
using System.Reflection;
using UnityEngine;

namespace ExplorerBeta
{
    public class ExplorerCore
    {
        public const string NAME    = "Explorer " + VERSION + " (" + PLATFORM + ", " + MODLOADER + ")";
        public const string VERSION = "3.0.0b";
        public const string AUTHOR  = "Sinai";
        public const string GUID    = "com.sinai.explorer";

        public const string PLATFORM =
#if CPP
            "Il2Cpp";
#else
            "Mono";
#endif
        public const string MODLOADER =
#if ML
            "MelonLoader";
#else
            "BepInEx";
#endif

        public static ExplorerCore Instance { get; private set; }

        public static bool ShowMenu
        {
            get => m_showMenu;
            set => SetShowMenu(value);
        }
        public static bool m_showMenu;

        private static bool m_doneUIInit;
        private static float m_timeSinceStartup;

        public ExplorerCore()
        {
            if (Instance != null)
            {
                Log("An instance of Explorer is already active!");
                return;
            }

            Instance = this;

            ModConfig.OnLoad();

            InputManager.Init();
            ForceUnlockCursor.Init();

#if CPP
            Application.add_logMessageReceived(new Action<string, string, LogType>(LogCallback));
#else
            Application.logMessageReceived += LogCallback;
#endif

            ShowMenu = true;

            Log($"{NAME} initialized.");
        }

        private static void SetShowMenu(bool show)
        {
            m_showMenu = show;
           
            if (UIManager.CanvasRoot)
            {
                UIManager.CanvasRoot.SetActive(show);

                if (show)
                {
                    ForceUnlockCursor.SetEventSystem();
                }
                else
                {
                    ForceUnlockCursor.ReleaseEventSystem();
                }
            }

            ForceUnlockCursor.UpdateCursorControl();
        }

        public static void Update()
        {
            // Temporary delay before UIManager.Init
            if (!m_doneUIInit)
            {
                m_timeSinceStartup += Time.deltaTime;

                if (m_timeSinceStartup > 0.1f)
                {
                    UIManager.Init();

                    Log("Initialized Explorer UI.");
                    m_doneUIInit = true;
                }
            }

            if (InputManager.GetKeyDown(ModConfig.Instance.Main_Menu_Toggle))
            {
                ShowMenu = !ShowMenu;
            }

            if (ShowMenu)
            {
                ForceUnlockCursor.Update();
                UIManager.Update();
            }
        }

        public static void OnSceneChange()
        {
            UIManager.OnSceneChange();
        }

        private void LogCallback(string message, string stackTrace, LogType type)
        {
            if (!DebugConsole.LogUnity)
                return;

            message = $"[UNITY] {message}";

            switch (type)
            {
                case LogType.Assert:
                case LogType.Log:
                    Log(message);
                    break;
                case LogType.Warning:
                    LogWarning(message);
                    break;
                case LogType.Exception:
                case LogType.Error:
                    LogError(message);
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
    }
}
