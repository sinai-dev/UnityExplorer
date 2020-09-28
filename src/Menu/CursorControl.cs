using System;
using UnityEngine;
#if ML
using Harmony;
#else
using HarmonyLib;
#endif

namespace Explorer
{
    public class CursorControl
    {
        public static bool ForceUnlockMouse
        {
            get => m_forceUnlock;
            set => SetForceUnlock(value);
        }
        private static bool m_forceUnlock;
        private static CursorLockMode m_lastLockMode;
        private static bool m_lastVisibleState;
        private static bool m_currentlySettingCursor = false;

        public static bool ShouldForceMouse => ExplorerCore.ShowMenu && ForceUnlockMouse;

        private static Type CursorType => m_cursorType ?? (m_cursorType = ReflectionHelpers.GetTypeByName("UnityEngine.Cursor"));
        private static Type m_cursorType;

        public static void Init()
        {
            try
            {
                // Check if Cursor class is loaded
                if (CursorType == null)
                {
                    ExplorerCore.Log("Trying to manually load Cursor module...");

                    if (ReflectionHelpers.LoadModule("UnityEngine.CoreModule") && CursorType != null)
                    {
                        ExplorerCore.Log("Ok!");
                    }
                    else
                    {
                        throw new Exception("Could not load UnityEngine.Cursor module!");
                    }
                }

                // Get current cursor state and enable cursor
                m_lastLockMode = Cursor.lockState;
                m_lastVisibleState = Cursor.visible;

                // Setup Harmony Patches
                TryPatch("lockState", new HarmonyMethod(typeof(CursorControl).GetMethod(nameof(Prefix_set_lockState))), true);
                TryPatch("lockState", new HarmonyMethod(typeof(CursorControl).GetMethod(nameof(Postfix_get_lockState))), false);

                TryPatch("visible", new HarmonyMethod(typeof(CursorControl).GetMethod(nameof(Prefix_set_visible))), true);
                TryPatch("visible", new HarmonyMethod(typeof(CursorControl).GetMethod(nameof(Postfix_get_visible))), false);
            }
            catch (Exception e)
            {
                ExplorerCore.Log($"Exception on CursorControl.Init! {e.GetType()}, {e.Message}");
            }

            // Enable ShowMenu and ForceUnlockMouse 
            // (set m_showMenu directly to not call UpdateCursorState twice)
            ExplorerCore.m_showMenu = true;
            ForceUnlockMouse = true;
        }

        private static void TryPatch(string property, HarmonyMethod patch, bool setter)
        {
            try
            {
                // var harmony = ExplorerCore.Instance.harmonyInstance;

                var harmony =
#if ML
                    ExplorerMelonMod.Instance.harmonyInstance;
#else
                    ExplorerBepInPlugin.HarmonyInstance;
#endif
                ;

                var prop = typeof(Cursor).GetProperty(property);

                if (setter)
                {
                    // setter is prefix
                    harmony.Patch(prop.GetSetMethod(), patch);
                }
                else
                {
                    // getter is postfix
                    harmony.Patch(prop.GetGetMethod(), null, patch);
                }
            }
            catch (Exception e)
            {
                ExplorerCore.Log($"[NON-FATAL] Couldn't patch a method: {e.Message}");
            }
        }

        private static void SetForceUnlock(bool unlock)
        {
            m_forceUnlock = unlock;
            UpdateCursorControl();
        }

        public static void Update()
        {
            // Check Force-Unlock input
            if (InputHelper.GetKeyDown(KeyCode.LeftAlt))
            {
                ForceUnlockMouse = !ForceUnlockMouse;
            }
        }

        public static void UpdateCursorControl()
        {
            try
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
            catch (Exception e)
            {
                ExplorerCore.Log($"Exception setting Cursor state: {e.GetType()}, {e.Message}");
            }
        }

        // Force mouse to stay unlocked and visible while UnlockMouse and ShowMenu are true.
        // Also keep track of when anything else tries to set Cursor state, this will be the
        // value that we set back to when we close the menu or disable force-unlock.

        [HarmonyPrefix]
        public static void Prefix_set_lockState(ref CursorLockMode value)
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

        [HarmonyPrefix]
        public static void Prefix_set_visible(ref bool value)
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

        [HarmonyPrefix]
        public static void Postfix_get_lockState(ref CursorLockMode __result)
        {
            if (ShouldForceMouse)
            {
                __result = m_lastLockMode;
            }
        }

        [HarmonyPrefix]
        public static void Postfix_get_visible(ref bool __result)
        {
            if (ShouldForceMouse)
            {
                __result = m_lastVisibleState;
            }
        }
    }
}
