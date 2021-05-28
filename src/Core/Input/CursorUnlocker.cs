using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityExplorer.Core.Input;
using BF = System.Reflection.BindingFlags;
using UnityExplorer.Core.Config;
using UnityExplorer.Core;
using UnityExplorer.UI;
using System.Collections;


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

        private static CursorLockMode lastLockMode;
        private static bool lastVisibleState;

        private static bool currentlySettingCursor = false;

        public static void Init()
        {
            lastLockMode = Cursor.lockState;
            lastVisibleState = Cursor.visible;

            SetupPatches();
            UpdateCursorControl();

            // Hook up config values

            // Force Unlock Mouse
            Unlock = ConfigManager.Force_Unlock_Mouse.Value;
            ConfigManager.Force_Unlock_Mouse.OnValueChanged += (bool val) => { Unlock = val; };

            // Aggressive Mouse Unlock
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

        private static WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();

        private static IEnumerator AggressiveUnlockCoroutine()
        {
            while (true)
            {
                yield return _waitForEndOfFrame ?? (_waitForEndOfFrame = new WaitForEndOfFrame());

                if (UIManager.ShowMenu)
                    UpdateCursorControl();
            }
        }

        public static void UpdateCursorControl()
        {
            try
            {
                currentlySettingCursor = true;

                if (ShouldActuallyUnlock)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;

                    if (!ConfigManager.Disable_EventSystem_Override.Value && UIManager.EventSys)
                        SetEventSystem();
                }
                else
                {
                    Cursor.lockState = lastLockMode;
                    Cursor.visible = lastVisibleState;

                    if (!ConfigManager.Disable_EventSystem_Override.Value && UIManager.EventSys)
                        ReleaseEventSystem();
                }

                currentlySettingCursor = false;
            }
            catch (Exception e)
            {
                ExplorerCore.Log($"Exception setting Cursor state: {e.GetType()}, {e.Message}");
            }
        }

        // Event system overrides

        private static bool settingEventSystem;
        private static EventSystem lastEventSystem;
        private static BaseInputModule lastInputModule;

        public static void SetEventSystem()
        {
            if (InputManager.CurrentType == InputType.InputSystem)
                return;

            if (EventSystem.current && EventSystem.current != UIManager.EventSys)
            {
                lastEventSystem = EventSystem.current;
                lastEventSystem.enabled = false;
            }

            // Set to our current system
            settingEventSystem = true;
            UIManager.EventSys.enabled = true;
            EventSystem.current = UIManager.EventSys;
            InputManager.ActivateUIModule();
            settingEventSystem = false;
        }

        public static void ReleaseEventSystem()
        {
            if (InputManager.CurrentType == InputType.InputSystem)
                return;

            if (lastEventSystem && lastEventSystem.gameObject.activeSelf)
            {
                lastEventSystem.enabled = true;

                settingEventSystem = true;
                EventSystem.current = lastEventSystem;
                lastInputModule?.ActivateModule();
                settingEventSystem = false;
            }
        }

        // Patches

        private static void SetupPatches()
        {
            try
            {
                ExplorerCore.Loader.SetupCursorPatches();
            }
            catch (Exception e)
            {
                ExplorerCore.Log($"Exception setting up Cursor patches: {e.GetType()}, {e.Message}");
            }
        }

        public static void Prefix_EventSystem_set_current(ref EventSystem value)
        {
            if (!settingEventSystem && value)
            {
                lastEventSystem = value;
                lastInputModule = value.currentInputModule;
            }

            if (!UIManager.EventSys)
                return;

            if (!settingEventSystem && ShouldActuallyUnlock && !ConfigManager.Disable_EventSystem_Override.Value)
            {
                value = UIManager.EventSys;
                value.enabled = true;
            }
        }

        // Force mouse to stay unlocked and visible while UnlockMouse and ShowMenu are true.
        // Also keep track of when anything else tries to set Cursor state, this will be the
        // value that we set back to when we close the menu or disable force-unlock.

        public static void Prefix_set_lockState(ref CursorLockMode value)
        {
            if (!currentlySettingCursor)
            {
                lastLockMode = value;

                if (ShouldActuallyUnlock)
                    value = CursorLockMode.None;
            }
        }

        public static void Prefix_set_visible(ref bool value)
        {
            if (!currentlySettingCursor)
            {
                lastVisibleState = value;

                if (ShouldActuallyUnlock)
                    value = true;
            }
        }
    }
}