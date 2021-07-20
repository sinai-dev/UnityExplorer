using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityExplorer.Core;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Input;
using UnityExplorer.UI;

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

        public static void SetupPatches()
        {
            try
            {
                PrefixPropertySetter(typeof(Cursor),
                    "lockState",
                    new HarmonyMethod(typeof(CursorUnlocker).GetMethod(nameof(CursorUnlocker.Prefix_set_lockState))));

                PrefixPropertySetter(typeof(Cursor),
                    "visible",
                    new HarmonyMethod(typeof(CursorUnlocker).GetMethod(nameof(CursorUnlocker.Prefix_set_visible))));

                PrefixPropertySetter(typeof(EventSystem),
                    "current",
                    new HarmonyMethod(typeof(CursorUnlocker).GetMethod(nameof(CursorUnlocker.Prefix_EventSystem_set_current))));

                PrefixMethod(typeof(EventSystem),
                    "SetSelectedGameObject",
                    new Type[] { typeof(GameObject), typeof(BaseEventData) },
                    new HarmonyMethod(typeof(CursorUnlocker).GetMethod(nameof(CursorUnlocker.Prefix_EventSystem_SetSelectedGameObject))),
                    new Type[] { typeof(GameObject), typeof(BaseEventData), typeof(int) });
                    // some games use a modified version of uGUI that includes this extra int argument on this method.

                PrefixMethod(typeof(PointerInputModule),
                    "ClearSelection",
                    new Type[] { },
                    new HarmonyMethod(typeof(CursorUnlocker).GetMethod(nameof(CursorUnlocker.Prefix_PointerInputModule_ClearSelection))));
            }
            catch (Exception ex)
            {
                ExplorerCore.Log($"Exception setting up Harmony patches:\r\n{ex.ReflectionExToString()}");
            }
        }

        private static void PrefixMethod(Type type, string method, Type[] arguments, HarmonyMethod prefix, Type[] backupArgs = null)
        {
            try
            {
                var methodInfo = type.GetMethod(method, ReflectionUtility.FLAGS, null, arguments, null);
                if (methodInfo == null)
                {
                    if (backupArgs != null)
                        methodInfo = type.GetMethod(method, ReflectionUtility.FLAGS, null, backupArgs, null);
                    
                    if (methodInfo == null)
                        throw new MissingMethodException($"Could not find method for patching - '{type.FullName}.{method}'!");
                }

                var processor = ExplorerCore.Harmony.CreateProcessor(methodInfo);
                processor.AddPrefix(prefix);
                processor.Patch();
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"Unable to patch {type.Name}.{method}: {e.Message}");
            }
        }

        private static void PrefixPropertySetter(Type type, string property, HarmonyMethod prefix)
        {
            try
            {
                var processor = ExplorerCore.Harmony.CreateProcessor(type.GetProperty(property, ReflectionUtility.FLAGS).GetSetMethod());
                processor.AddPrefix(prefix);
                processor.Patch();
            }
            catch (Exception e)
            {
                ExplorerCore.Log($"Unable to patch {type.Name}.set_{property}: {e.Message}");
            }
        }

        // Prevent setting non-UnityExplorer objects as selected when menu is open

        public static bool Prefix_PointerInputModule_ClearSelection()
        {
            return !(UIManager.ShowMenu && UIManager.CanvasRoot);
        }

        public static bool Prefix_EventSystem_SetSelectedGameObject(GameObject __0)
        {
            if (!UIManager.ShowMenu || !UIManager.CanvasRoot)
                return true;

            return __0 && __0.transform.root.gameObject.GetInstanceID() == UIManager.CanvasRoot.GetInstanceID();
        }

        // Force EventSystem.current to be UnityExplorer's when menu is open

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