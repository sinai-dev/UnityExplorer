using Explorer.Config;
using Explorer.UI;
using Explorer.UI.Inspectors;
using Explorer.UI.Main;
using Explorer.UI.Shared;
using UnityEngine;

namespace Explorer
{
    public class ExplorerCore
    {
        public const string NAME    = "Explorer " + VERSION + " (" + PLATFORM + ", " + MODLOADER + ")";
        public const string VERSION = "2.0.1";
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

        public ExplorerCore()
        {
            Instance = this;

            ModConfig.OnLoad();

            new MainMenu();
            new WindowManager();

            InputManager.Init();
            ForceUnlockCursor.Init();

            ShowMenu = true;

            Log($"{NAME} initialized.");
        }

        public static bool ShowMenu
        {
            get => m_showMenu;
            set => SetShowMenu(value);
        }
        public static bool m_showMenu;        

        private static void SetShowMenu(bool show)
        {
            m_showMenu = show;
            ForceUnlockCursor.UpdateCursorControl();
        }

        public static void Update()
        {
            if (InputManager.GetKeyDown(ModConfig.Instance.Main_Menu_Toggle))
            {
                ShowMenu = !ShowMenu;
            }

            if (ShowMenu)
            {
                ForceUnlockCursor.Update();
                InspectUnderMouse.Update();

                MainMenu.Instance.Update();
                WindowManager.Instance.Update();
            }
        }

        public static void OnGUI()
        {
            if (!ShowMenu) return;

            var origSkin = GUI.skin;
            GUI.skin = UIStyles.WindowSkin;

            MainMenu.Instance.OnGUI();
            WindowManager.Instance.OnGUI();
            InspectUnderMouse.OnGUI();

            GUI.skin = origSkin;
        }

        public static void OnSceneChange()
        {
            ScenePage.Instance?.OnSceneChange();
            SearchPage.Instance?.OnSceneChange();
        }

        public static void Log(object message)
        {
#if ML
            MelonLoader.MelonLogger.Log(message.ToString());
#else
            ExplorerBepInPlugin.Logging?.LogMessage(message.ToString());
#endif
        }

        public static void LogWarning(object message)
        {
#if ML
            MelonLoader.MelonLogger.LogWarning(message.ToString());
#else
            ExplorerBepInPlugin.Logging?.LogWarning(message.ToString());
#endif
        }

        public static void LogError(object message)
        {
#if ML
            MelonLoader.MelonLogger.LogError(message.ToString());
#else
            ExplorerBepInPlugin.Logging?.LogError(message.ToString());
#endif
        }
    }
}
