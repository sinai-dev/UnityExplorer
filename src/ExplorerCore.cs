using UnityEngine;

namespace Explorer
{
    public class ExplorerCore
    {
        public const string NAME    = "Explorer (" + PLATFORM + ", " + MODLOADER + ")";
        public const string VERSION = "1.8.21";
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

            InputHelper.Init();
            CursorControl.Init();

            Log($"{NAME} {VERSION} initialized.");
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
            CursorControl.UpdateCursorControl();
        }

        public static void Update()
        {
            if (InputHelper.GetKeyDown(ModConfig.Instance.Main_Menu_Toggle))
            {
                ShowMenu = !ShowMenu;
            }

            if (ShowMenu)
            {
                CursorControl.Update();
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

        public static void Log(string message)
        {
#if ML
            MelonLoader.MelonLogger.Log(message);
#else
            ExplorerBepInPlugin.Logging?.LogMessage(message);
#endif
        }

        public static void LogWarning(string message)
        {
#if ML
            MelonLoader.MelonLogger.LogWarning(message);
#else
            ExplorerBepInPlugin.Logging?.LogWarning(message);
#endif
        }

        public static void LogError(string message)
        {
#if ML
            MelonLoader.MelonLogger.LogError(message);
#else
            ExplorerBepInPlugin.Logging?.LogError(message);
#endif
        }
    }
}
