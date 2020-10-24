using System;
using UnityEngine;
using ExplorerBeta.Helpers;
using UnityEngine.EventSystems;
using ExplorerBeta.UI;
using ExplorerBeta.Input;
using BF = System.Reflection.BindingFlags;
#if ML
using Harmony;
#else
using HarmonyLib;
#endif

namespace ExplorerBeta.UI
{
    public class ForceUnlockCursor
    {
        public static bool Unlock
        {
            get => m_forceUnlock;
            set => SetForceUnlock(value);
        }
        private static bool m_forceUnlock;

        private static void SetForceUnlock(bool unlock)
        {
            m_forceUnlock = unlock;
            UpdateCursorControl();
        }

        public static bool ShouldForceMouse => ExplorerCore.ShowMenu && Unlock;

        private static CursorLockMode m_lastLockMode;
        private static bool m_lastVisibleState;

        private static bool m_currentlySettingCursor = false;

        private static Type CursorType
            => m_cursorType
            ?? (m_cursorType = ReflectionHelpers.GetTypeByName("UnityEngine.Cursor"));
        private static Type m_cursorType;

        public static void Init()
        {
            try
            {
                if (CursorType == null)
                {
                    throw new Exception("Could not find Type 'UnityEngine.Cursor'!");
                }

                // Get current cursor state and enable cursor
                try
                {
                    //m_lastLockMode = Cursor.lockState;
                    m_lastLockMode = (CursorLockMode?)typeof(Cursor).GetProperty("lockState", BF.Public | BF.Static)?.GetValue(null, null)
                                     ?? CursorLockMode.None;

                    //m_lastVisibleState = Cursor.visible;
                    m_lastVisibleState = (bool?)typeof(Cursor).GetProperty("visible", BF.Public | BF.Static)?.GetValue(null, null)
                                         ?? false;
                }
                catch { }

                // Setup Harmony Patches
                TryPatch(typeof(EventSystem), "current", new HarmonyMethod(typeof(ForceUnlockCursor).GetMethod(nameof(Prefix_EventSystem_set_current))), true);

                TryPatch(typeof(Cursor), "lockState", new HarmonyMethod(typeof(ForceUnlockCursor).GetMethod(nameof(Prefix_set_lockState))), true);
                //TryPatch(typeof(Cursor), "lockState", new HarmonyMethod(typeof(ForceUnlockCursor).GetMethod(nameof(Postfix_get_lockState))), false);

                TryPatch(typeof(Cursor), "visible", new HarmonyMethod(typeof(ForceUnlockCursor).GetMethod(nameof(Prefix_set_visible))), true);
                //TryPatch(typeof(Cursor), "visible", new HarmonyMethod(typeof(ForceUnlockCursor).GetMethod(nameof(Postfix_get_visible))), false);
            }
            catch (Exception e)
            {
                ExplorerCore.Log($"Exception on CursorControl.Init! {e.GetType()}, {e.Message}");
            }

            Unlock = true;
        }

        private static void TryPatch(Type type, string property, HarmonyMethod patch, bool setter)
        {
            try
            {
                var harmony =
#if ML
                    ExplorerMelonMod.Instance.harmonyInstance;
#else
                    ExplorerBepInPlugin.HarmonyInstance;
#endif
                ;

                var prop = type.GetProperty(property);

                if (setter)
                {
                    // setter is prefix
                    harmony.Patch(prop.GetSetMethod(), prefix: patch);
                }
                else
                {
                    // getter is postfix
                    harmony.Patch(prop.GetGetMethod(), postfix: patch);
                }
            }
            catch (Exception e)
            {
                string suf = setter ? "set_" : "get_";
                ExplorerCore.Log($"Unable to patch {type.Name}.{suf}{property}: {e.Message}");
            }
        }

        public static void Update()
        {
            // Check Force-Unlock input
            if (InputManager.GetKeyDown(KeyCode.LeftAlt))
            {
                Unlock = !Unlock;
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

        // Event system

        private static bool m_settingEventSystem;
        private static EventSystem m_lastEventSystem;
        private static BaseInputModule m_lastInputModule;

        public static void SetEventSystem()
        {
            m_settingEventSystem = true;
            UIManager.SetEventSystem();
            m_settingEventSystem = false;
        }

        public static void ReleaseEventSystem()
        {
            if (m_lastEventSystem)
            {
                m_settingEventSystem = true;
                EventSystem.current = m_lastEventSystem;
                m_lastInputModule?.ActivateModule();
                m_settingEventSystem = false;
            }
        }

        [HarmonyPrefix]
        public static void Prefix_EventSystem_set_current(ref EventSystem value)
        {
            if (!m_settingEventSystem)
            {
                m_lastEventSystem = value;
                m_lastInputModule = value?.currentInputModule;

                if (ExplorerCore.ShowMenu)
                {
                    value = UIManager.EventSys;
                }
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

        //[HarmonyPrefix]
        //public static void Postfix_get_lockState(ref CursorLockMode __result)
        //{
        //    if (ShouldForceMouse)
        //    {
        //        __result = m_lastLockMode;
        //    }
        //}

        //[HarmonyPrefix]
        //public static void Postfix_get_visible(ref bool __result)
        //{
        //    if (ShouldForceMouse)
        //    {
        //        __result = m_lastVisibleState;
        //    }
        //}
    }
}
