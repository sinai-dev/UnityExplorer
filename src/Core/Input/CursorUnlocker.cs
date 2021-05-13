using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityExplorer.Core.Input;
using BF = System.Reflection.BindingFlags;
using UnityExplorer.Core.Config;
using UnityExplorer.Core;
using UnityExplorer.UI;
using System.Collections;
using HarmonyLib;

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

            Unlock = ConfigManager.Force_Unlock_Mouse.Value;
            ConfigManager.Force_Unlock_Mouse.OnValueChanged += (bool val) => { Unlock = val; };

            if (ConfigManager.Aggressive_Mouse_Unlock.Value)
                SetupAggressiveUnlock();
        }

        public static void SetupAggressiveUnlock()
        {
            try
            {
                RuntimeProvider.Instance.StartCoroutine(AggressiveUnlockCoroutine());
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception setting up Aggressive Mouse Unlock: {ex}");
            }
        }

        private static readonly WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();

        private static IEnumerator AggressiveUnlockCoroutine()
        {
            while (true)
            {
                yield return _waitForEndOfFrame;

                if (UIManager.ShowMenu)
                    UpdateCursorControl();
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

                    if (UIManager.EventSys)
                        SetEventSystem();
                }
                else
                {
                    Cursor.lockState = m_lastLockMode;
                    Cursor.visible = m_lastVisibleState;

                    if (UIManager.EventSys)
                        ReleaseEventSystem();
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
            if (InputManager.CurrentType == InputType.InputSystem)
                return;

            if (EventSystem.current && EventSystem.current != UIManager.EventSys)
            {
                m_lastEventSystem = EventSystem.current;
                m_lastEventSystem.enabled = false;
            }

            // Set to our current system
            m_settingEventSystem = true;
            UIManager.EventSys.enabled = true;
            EventSystem.current = UIManager.EventSys;
            InputManager.ActivateUIModule();
            m_settingEventSystem = false;
        }

        public static void ReleaseEventSystem()
        {
            if (InputManager.CurrentType == InputType.InputSystem)
                return;

            if (m_lastEventSystem && m_lastEventSystem.gameObject.activeSelf)
            {
                m_lastEventSystem.enabled = true;

                m_settingEventSystem = true;
                EventSystem.current = m_lastEventSystem;
                m_lastInputModule?.ActivateModule();
                m_settingEventSystem = false;
            }
        }

        // Patches

        private static void SetupPatches()
        {
            try
            {
                if (CursorType == null)
                    throw new Exception("Could not load Type 'UnityEngine.Cursor'!");

                // Get current cursor state and enable cursor
                m_lastLockMode = (CursorLockMode?)CursorType.GetProperty("lockState", BF.Public | BF.Static)?.GetValue(null, null)
                                 ?? CursorLockMode.None;

                m_lastVisibleState = (bool?)CursorType.GetProperty("visible", BF.Public | BF.Static)?.GetValue(null, null)
                                     ?? false;

                ExplorerCore.Loader.SetupCursorPatches();
            }
            catch (Exception e)
            {
                ExplorerCore.Log($"Error on CursorUnlocker.Init! {e.GetType()}, {e.Message}");
            }
        }

        public static void Prefix_EventSystem_set_current(ref EventSystem value)
        {
            if (!m_settingEventSystem && value != UIManager.EventSys)
            {
                m_lastEventSystem = value;
                m_lastInputModule = value?.currentInputModule;

                if (ShouldActuallyUnlock)
                {
                    value = UIManager.EventSys;
                    value.enabled = true;
                }
            }
        }

        // Force mouse to stay unlocked and visible while UnlockMouse and ShowMenu are true.
        // Also keep track of when anything else tries to set Cursor state, this will be the
        // value that we set back to when we close the menu or disable force-unlock.

        public static void Prefix_set_lockState(ref CursorLockMode value)
        {
            if (!m_currentlySettingCursor)
            {
                m_lastLockMode = value;

                if (ShouldActuallyUnlock)
                    value = CursorLockMode.None;
            }
        }

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