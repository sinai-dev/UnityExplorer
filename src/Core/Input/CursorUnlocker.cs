using System;
using UnityEngine;
using UnityExplorer.Core.Unity;
using UnityEngine.EventSystems;
using UnityExplorer.Core.Input;
using BF = System.Reflection.BindingFlags;
using UnityExplorer.Core.Config;
using UnityExplorer.Core;
using UnityExplorer.UI;
#if ML
using Harmony;
#else
using HarmonyLib;
#endif

namespace UnityExplorer.Core.Input
{
    public class CursorUnlocker
    {
        public static bool Unlock
        {
            get => m_forceUnlock;
            set
            {
                m_forceUnlock = value;
                UpdateCursorControl();
            }
        }
        private static bool m_forceUnlock;

        public static bool ShouldActuallyUnlock => UIManager.ShowMenu && Unlock;

        private static CursorLockMode m_lastLockMode;
        private static bool m_lastVisibleState;

        private static bool m_currentlySettingCursor = false;

        private static Type CursorType
            => m_cursorType
            ?? (m_cursorType = ReflectionUtility.GetTypeByName("UnityEngine.Cursor"));
        private static Type m_cursorType;

        public static void Init()
        {
            SetupPatches();

            UpdateCursorControl();

            Unlock = true;
            ConfigManager.Force_Unlock_Mouse.OnValueChanged += (bool val) => { Unlock = val; };
        }

        private static void SetupPatches()
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
                    m_lastLockMode = (CursorLockMode?)typeof(Cursor).GetProperty("lockState", BF.Public | BF.Static)?.GetValue(null, null)
                                     ?? CursorLockMode.None;

                    m_lastVisibleState = (bool?)typeof(Cursor).GetProperty("visible", BF.Public | BF.Static)?.GetValue(null, null)
                                         ?? false;
                }
                catch { }

                // Setup Harmony Patches
                TryPatch(typeof(Cursor),
                    "lockState",
                    new HarmonyMethod(typeof(CursorUnlocker).GetMethod(nameof(Prefix_set_lockState))),
                    true);

                TryPatch(typeof(Cursor),
                    "visible",
                    new HarmonyMethod(typeof(CursorUnlocker).GetMethod(nameof(Prefix_set_visible))),
                    true);

                TryPatch(typeof(EventSystem),
                    "current",
                    new HarmonyMethod(typeof(CursorUnlocker).GetMethod(nameof(Prefix_EventSystem_set_current))),
                    true);
            }
            catch (Exception e)
            {
                ExplorerCore.Log($"Exception on ForceUnlockCursor.Init! {e.GetType()}, {e.Message}");
            }
        }

        private static void TryPatch(Type type, string property, HarmonyMethod patch, bool setter)
        {
            try
            {
                var harmony = ExplorerCore.Loader.HarmonyInstance;

                var prop = type.GetProperty(property);

                if (setter) // setter is prefix
                {
                    harmony.Patch(prop.GetSetMethod(), prefix: patch);
                }
                else // getter is postfix
                {
                    harmony.Patch(prop.GetGetMethod(), postfix: patch);
                }
            }
            catch (Exception e)
            {
                string suf = setter ? "set_" : "get_";
                ExplorerCore.Log($"Unable to patch {type.Name}.{suf}{property}: {e.Message}");
            }
        }

        public static void UpdateCursorControl()
        {
            try
            {
                m_currentlySettingCursor = true;
                if (ShouldActuallyUnlock)
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

        // Event system overrides

        private static bool m_settingEventSystem;
        private static EventSystem m_lastEventSystem;
        private static BaseInputModule m_lastInputModule;

        public static void SetEventSystem()
        {
            // temp disabled for new InputSystem
            if (InputManager.CurrentType == InputType.InputSystem)
                return;

            // Disable current event system object
            if (m_lastEventSystem || EventSystem.current)
            {
                if (!m_lastEventSystem)
                    m_lastEventSystem = EventSystem.current;

                //ExplorerCore.Log("Disabling current event system...");
                m_lastEventSystem.enabled = false;
                //m_lastEventSystem.gameObject.SetActive(false);
            }

            // Set to our current system
            m_settingEventSystem = true;
            EventSystem.current = UIManager.EventSys;
            UIManager.EventSys.enabled = true;
            InputManager.ActivateUIModule();
            m_settingEventSystem = false;
        }

        public static void ReleaseEventSystem()
        {
            if (InputManager.CurrentType == InputType.InputSystem)
                return;

            if (m_lastEventSystem)
            {
                m_lastEventSystem.enabled = true;
                //m_lastEventSystem.gameObject.SetActive(true);

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

                if (ShouldActuallyUnlock)
                    value = UIManager.EventSys;
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

                if (ShouldActuallyUnlock)
                    value = CursorLockMode.None;
            }
        }

        [HarmonyPrefix]
        public static void Prefix_set_visible(ref bool value)
        {
            if (!m_currentlySettingCursor)
            {
                m_lastVisibleState = value;

                if (ShouldActuallyUnlock)
                    value = true;
            }
        }
    }
}
