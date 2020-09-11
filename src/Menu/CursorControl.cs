using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Harmony;
using MelonLoader;
using System.Reflection;

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

        public static bool ShouldForceMouse => CppExplorer.ShowMenu && ForceUnlockMouse;

        public static void Init()
        {
            try
            {
                // Check if Cursor class is loaded
                if (ReflectionHelpers.GetTypeByName("UnityEngine.Cursor") == null)
                {
                    MelonLogger.Log("Trying to manually load Cursor module...");

                    if (ReflectionHelpers.LoadModule("UnityEngine.CoreModule"))
                    {
                        MelonLogger.Log("Ok!");
                    }
                    else
                    {
                        throw new Exception("Could not load UnityEngine.Cursor module!");
                    }
                }

                // Get current cursor state and enable cursor
                m_lastLockMode = Cursor.lockState;
                m_lastVisibleState = Cursor.visible;

                TryPatch("lockState", new HarmonyMethod(typeof(CursorControl).GetMethod(nameof(Prefix_set_lockState))), false, false);
                TryPatch("visible", new HarmonyMethod(typeof(CursorControl).GetMethod(nameof(Prefix_set_visible))), false, false);

                TryPatch("lockState", new HarmonyMethod(typeof(CursorControl).GetMethod(nameof(Postfix_get_lockState))), true, true);
                TryPatch("visible", new HarmonyMethod(typeof(CursorControl).GetMethod(nameof(Postfix_get_visible))), true, true);
            }
            catch (Exception e)
            {
                MelonLogger.Log($"Exception on CursorControl.Init! {e.GetType()}, {e.Message}");
            }

            // Enable ShowMenu and ForceUnlockMouse 
            // (set m_showMenu directly to not call UpdateCursorState twice)
            CppExplorer.m_showMenu = true;
            ForceUnlockMouse = true;
        }

        private static void TryPatch(string property, HarmonyMethod patch, bool getter = true, bool postfix = false)
        {
            // Setup Harmony Patches
            try
            {
                var harmony = CppExplorer.Instance.harmonyInstance;

                var prop = typeof(Cursor).GetProperty(property);

                harmony.Patch(getter  ? prop.GetGetMethod() : prop.GetSetMethod(), 
                              postfix ? null  : patch, 
                              postfix ? patch : null);
            }
            catch (Exception e)
            {
                MelonLogger.Log($"[NON-FATAL] Couldn't patch a method: {e.Message}");
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
                MelonLogger.Log($"Exception setting Cursor state: {e.GetType()}, {e.Message}");
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

        //[HarmonyPatch(typeof(Cursor), nameof(Cursor.lockState), MethodType.Setter)]
        //public class Cursor_set_lockState
        //{
        //    [HarmonyPrefix]
        //    public static void Prefix(ref CursorLockMode value)
        //    {
        //        if (!m_currentlySettingCursor)
        //        {
        //            m_lastLockMode = value;

        //            if (ShouldForceMouse)
        //            {
        //                value = CursorLockMode.None;
        //            }
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(Cursor), nameof(Cursor.visible), MethodType.Setter)]
        //public class Cursor_set_visible
        //{
        //    [HarmonyPrefix]
        //    public static void Prefix(ref bool value)
        //    {
        //        if (!m_currentlySettingCursor)
        //        {
        //            m_lastVisibleState = value;

        //            if (ShouldForceMouse)
        //            {
        //                value = true;
        //            }
        //        }
        //    }
        //}

        //// Make it appear as though UnlockMouse is disabled to the rest of the application.

        //[HarmonyPatch(typeof(Cursor), nameof(Cursor.lockState), MethodType.Getter)]
        //public class Cursor_get_lockState
        //{
        //    [HarmonyPostfix]
        //    public static void Postfix(ref CursorLockMode __result)
        //    {
        //        if (ShouldForceMouse)
        //        {
        //            __result = m_lastLockMode;
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(Cursor), nameof(Cursor.visible), MethodType.Getter)]
        //public class Cursor_get_visible
        //{
        //    [HarmonyPostfix]
        //    public static void Postfix(ref bool __result)
        //    {
        //        if (ShouldForceMouse)
        //        {
        //            __result = m_lastVisibleState;
        //        }
        //    }
        //}
    }
}
