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
        public const string VERSION = "1.6.3";
        public const string AUTHOR  = "Sinai";
        public const string GUID    = "com.sinai.cppexplorer";

        public static CppExplorer Instance { get; private set; }

        public static bool ShowMenu
        {
            get => m_showMenu;
            set => SetShowMenu(value);
        }
        private static bool m_showMenu;

        public static bool ForceUnlockMouse
        {
            get => m_forceUnlock;
            set => SetForceUnlock(value);
        }
        private static bool m_forceUnlock;
        private static CursorLockMode m_lastLockMode;
        private static bool m_lastVisibleState;
        private static bool m_currentlySettingCursor = false;

        public static bool ShouldForceMouse => ShowMenu && ForceUnlockMouse;

        private static void SetShowMenu(bool show)
        {
            m_showMenu = show;
            UpdateCursorControl();
        }

        // ========== MonoBehaviour methods ==========

        public override void OnApplicationStart()
        {
            Instance = this;

            new MainMenu();
            new WindowManager();

            // Get current cursor state and enable cursor
            m_lastLockMode = Cursor.lockState;
            m_lastVisibleState = Cursor.visible;

            // Enable ShowMenu and ForceUnlockMouse 
            // (set m_showMenu to not call UpdateCursorState twice)
            m_showMenu = true;
            SetForceUnlock(true);

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
            if (InputHelper.GetKeyDown(KeyCode.F7))
            {
                ShowMenu = !ShowMenu;
            }

            if (ShowMenu)
            {
                // Check Force-Unlock input
                if (InputHelper.GetKeyDown(KeyCode.LeftAlt))
                {
                    ForceUnlockMouse = !ForceUnlockMouse;
                }

                MainMenu.Instance.Update();
                WindowManager.Instance.Update();
                InspectUnderMouse.Update();
            }
        }

        public override void OnGUI()
        {
            if (!ShowMenu) return;

            MainMenu.Instance.OnGUI();
            WindowManager.Instance.OnGUI();
            InspectUnderMouse.OnGUI();
        }

        // =========== Cursor control ===========

        private static void SetForceUnlock(bool unlock)
        {
            m_forceUnlock = unlock;
            UpdateCursorControl();
        }

        private static void UpdateCursorControl()
        {
            m_currentlySettingCursor = true;
            if (ShouldForceMouse)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = m_lastLockMode;
                Cursor.visible = m_lastVisibleState;
            }
            m_currentlySettingCursor = false;
        }

        // Force mouse to stay unlocked and visible while UnlockMouse and ShowMenu are true.
        // Also keep track of when anything else tries to set Cursor state, this will be the
        // value that we set back to when we close the menu or disable force-unlock.

        [HarmonyPatch(typeof(Cursor), nameof(Cursor.lockState), MethodType.Setter)]
        public class Cursor_set_lockState
        {
            [HarmonyPrefix]
            public static void Prefix(ref CursorLockMode value)
            {
                if (!m_currentlySettingCursor)
                {
                    m_lastLockMode = value;

                    if (ShouldForceMouse)
                    {
                        value = CursorLockMode.None;
                    }
                }                
            }
        }

        [HarmonyPatch(typeof(Cursor), nameof(Cursor.visible), MethodType.Setter)]
        public class Cursor_set_visible
        {
            [HarmonyPrefix]
            public static void Prefix(ref bool value)
            {
                if (!m_currentlySettingCursor)
                {
                    m_lastVisibleState = value;

                    if (ShouldForceMouse)
                    {
                        value = true;
                    }
                }
            }
        }

        // Make it appear as though UnlockMouse is disabled to the rest of the application.

        [HarmonyPatch(typeof(Cursor), nameof(Cursor.lockState), MethodType.Getter)]
        public class Cursor_get_lockState
        {
            [HarmonyPostfix]
            public static void Postfix(ref CursorLockMode __result)
            {
                if (ShouldForceMouse)
                {
                    __result = m_lastLockMode;
                }
            }
        }

        [HarmonyPatch(typeof(Cursor), nameof(Cursor.visible), MethodType.Getter)]
        public class Cursor_get_visible
        {
            [HarmonyPostfix]
            public static void Postfix(ref bool __result)
            {
                if (ShouldForceMouse)
                {
                    __result = m_lastVisibleState;
                }
            }
        }
    }
}
