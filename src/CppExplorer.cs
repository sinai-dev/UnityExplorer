using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using MelonLoader;
using UnhollowerBaseLib;
using UnityEngine;

namespace Explorer
{
    public class CppExplorer : MelonMod
    {
        public const string NAME    = "CppExplorer";
        public const string VERSION = "1.7.1";
        public const string AUTHOR  = "Sinai";
        public const string GUID    = "com.sinai.cppexplorer";

        public static CppExplorer Instance { get; private set; }

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

        public override void OnApplicationStart()
        {
            Instance = this;

            ModConfig.OnLoad();

            InputHelper.Init();

            new MainMenu();
            new WindowManager();

            CursorControl.Init();

            MelonLogger.Log($"CppExplorer {VERSION} initialized.");
        }

        public override void OnLevelWasLoaded(int level)
        {
            ScenePage.Instance?.OnSceneChange();
            SearchPage.Instance?.OnSceneChange();
        }

        public override void OnUpdate()
        {
            // Check main toggle key input
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

        public override void OnGUI()
        {
            if (!ShowMenu) return;

            var origSkin = GUI.skin;
            GUI.skin = UIStyles.WindowSkin;

            MainMenu.Instance.OnGUI();
            WindowManager.Instance.OnGUI();
            InspectUnderMouse.OnGUI();

            GUI.skin = origSkin;
        }
    }
}
