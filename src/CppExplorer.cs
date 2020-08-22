using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MelonLoader;
using UnhollowerBaseLib;

namespace Explorer
{
    public class CppExplorer : MelonMod
    {
        // consts

        public const string ID = "com.sinai.cppexplorer";
        public const string VERSION = "1.4.1";
        public const string AUTHOR = "Sinai";

        public const string NAME = "CppExplorer"
#if Release_Unity2018
        + " (Unity 2018)"
#endif
        ;

        // fields

        public static CppExplorer Instance;

        // props

        public static bool ShowMenu { get; set; } = false;
        public static int ArrayLimit { get; set; } = 20;

        // methods

        public override void OnApplicationStart()
        {
            base.OnApplicationStart();

            Instance = this;

            new MainMenu();
            new WindowManager();

            ShowMenu = true;
        }

        public override void OnLevelWasLoaded(int level)
        {
            ScenePage.Instance?.OnSceneChange();
            SearchPage.Instance?.OnSceneChange();
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F7))
            {
                ShowMenu = !ShowMenu;
            }

            if (ShowMenu)
            {
                if (!Cursor.visible)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }

                MainMenu.Instance.Update();
                WindowManager.Instance.Update();

                InspectUnderMouse.Update();
            }
        }

        public override void OnGUI()
        {
            base.OnGUI();

            MainMenu.Instance.OnGUI();
            WindowManager.Instance.OnGUI();

            InspectUnderMouse.OnGUI();
        }
    }
}
