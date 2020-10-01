using System;
using System.Reflection;
using UnityEngine;

namespace Explorer
{
    /// <summary>
    /// Version-agnostic Input module using Reflection.
    /// </summary>
    public static class InputHelper
    {
        // If Input module failed to load at all
        public static bool NO_INPUT;

        // If using new InputSystem module
        public static bool USING_NEW_INPUT;

        // Cached Types
        private static Type TInput => _input ?? (_input = ReflectionHelpers.GetTypeByName("UnityEngine.Input"));
        private static Type _input;

        private static Type TKeyboard => _keyboardSys ?? (_keyboardSys = ReflectionHelpers.GetTypeByName("UnityEngine.InputSystem.Keyboard"));
        private static Type _keyboardSys;

        private static Type TMouse => _mouseSys ?? (_mouseSys = ReflectionHelpers.GetTypeByName("UnityEngine.InputSystem.Mouse"));
        private static Type _mouseSys;

        private static Type TKey => _key ?? (_key = ReflectionHelpers.GetTypeByName("UnityEngine.InputSystem.Key"));
        private static Type _key;

        // Cached member infos (new system)
        private static PropertyInfo _keyboardCurrent;
        private static PropertyInfo _kbItemProp;
        private static PropertyInfo _isPressed;
        private static PropertyInfo _wasPressedThisFrame;
        private static PropertyInfo _mouseCurrent;
        private static PropertyInfo _leftButton;
        private static PropertyInfo _rightButton;
        private static PropertyInfo _position;
        private static MethodInfo _readValueMethod;

        // Cached member infos (legacy)
        private static PropertyInfo _mousePosition;
        private static MethodInfo _getKey;
        private static MethodInfo _getKeyDown;
        private static MethodInfo _getMouseButton;
        private static MethodInfo _getMouseButtonDown;

        public static void Init()
        {
            if (TKeyboard != null || TryManuallyLoadNewInput())
            {
                InitNewInput();
                return;
            }

            if (TInput != null || TryManuallyLoadLegacyInput())
            {
                InitLegacyInput();
                return;
            }

            ExplorerCore.LogWarning("Could not find any Input module!");
            NO_INPUT = true;
        }

        private static void InitNewInput()
        {
            ExplorerCore.Log("Initializing new InputSystem support...");

            USING_NEW_INPUT = true;

            _keyboardCurrent = TKeyboard.GetProperty("current");
            _kbItemProp = TKeyboard.GetProperty("Item", new Type[] { TKey });

            var btnControl = ReflectionHelpers.GetTypeByName("UnityEngine.InputSystem.Controls.ButtonControl");
            _isPressed = btnControl.GetProperty("isPressed");
            _wasPressedThisFrame = btnControl.GetProperty("wasPressedThisFrame");

            _mouseCurrent = TMouse.GetProperty("current");
            _leftButton = TMouse.GetProperty("leftButton");
            _rightButton = TMouse.GetProperty("rightButton");

            _position = ReflectionHelpers.GetTypeByName("UnityEngine.InputSystem.Pointer")
                                         .GetProperty("position");

            _readValueMethod = ReflectionHelpers.GetTypeByName("UnityEngine.InputSystem.InputControl`1")
                               .MakeGenericType(typeof(Vector2))
                               .GetMethod("ReadValue");
        }

        private static void InitLegacyInput()
        {
            ExplorerCore.Log("Initializing Legacy Input support...");

            _mousePosition = TInput.GetProperty("mousePosition");
            _getKey = TInput.GetMethod("GetKey", new Type[] { typeof(KeyCode) });
            _getKeyDown = TInput.GetMethod("GetKeyDown", new Type[] { typeof(KeyCode) });
            _getMouseButton = TInput.GetMethod("GetMouseButton", new Type[] { typeof(int) });
            _getMouseButtonDown = TInput.GetMethod("GetMouseButtonDown", new Type[] { typeof(int) });
        }

        private static bool TryManuallyLoadNewInput()
        {
            if (ReflectionHelpers.LoadModule("Unity.InputSystem") && TKeyboard != null)
            {
                ExplorerCore.Log("Loaded new InputSystem module!");
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool TryManuallyLoadLegacyInput()
        {
            if ((ReflectionHelpers.LoadModule("UnityEngine.InputLegacyModule") || ReflectionHelpers.LoadModule("UnityEngine.CoreModule"))
                && TInput != null)
            {
                ExplorerCore.Log("Loaded legacy InputModule!");
                return true;
            }
            else
            {
                return false;
            }
        }

        public static Vector3 MousePosition
        {
            get
            {
                if (NO_INPUT) return Vector3.zero;

                if (USING_NEW_INPUT)
                {
                    var mouse = _mouseCurrent.GetValue(null, null);
                    var pos = _position.GetValue(mouse, null);

                    return (Vector2)_readValueMethod.Invoke(pos, new object[0]);
                }

                return (Vector3)_mousePosition.GetValue(null, null);
            }
        }

        public static bool GetKeyDown(KeyCode key)
        {
            if (NO_INPUT) return false;

            if (USING_NEW_INPUT)
            {
                var parsed = Enum.Parse(TKey, key.ToString());
                var currentKB = _keyboardCurrent.GetValue(null, null);
                var actualKey = _kbItemProp.GetValue(currentKB, new object[] { parsed });

                return (bool)_wasPressedThisFrame.GetValue(actualKey, null);
            }

            return (bool)_getKeyDown.Invoke(null, new object[] { key });
        }

        public static bool GetKey(KeyCode key)
        {
            if (NO_INPUT) return false;

            if (USING_NEW_INPUT)
            {
                var parsed = Enum.Parse(TKey, key.ToString());
                var currentKB = _keyboardCurrent.GetValue(null, null);
                var actualKey = _kbItemProp.GetValue(currentKB, new object[] { parsed });

                return (bool)_isPressed.GetValue(actualKey, null);
            }

            return (bool)_getKey.Invoke(null, new object[] { key });
        }

        /// <param name="btn">0/1 = left, 2 = middle, 3 = right, etc</param>
        public static bool GetMouseButtonDown(int btn)
        {
            if (NO_INPUT) return false;

            if (USING_NEW_INPUT)
            {
                var mouse = _mouseCurrent.GetValue(null, null);

                PropertyInfo btnProp;
                if (btn < 2) btnProp = _leftButton;
                else         btnProp = _rightButton;

                var actualBtn = btnProp.GetValue(mouse, null);

                return (bool)_wasPressedThisFrame.GetValue(actualBtn, null);
            }

            return (bool)_getMouseButtonDown.Invoke(null, new object[] { btn });
        }

        /// <param name="btn">1 = left, 2 = middle, 3 = right, etc</param>
        public static bool GetMouseButton(int btn)
        {
            if (NO_INPUT) return false;

            if (USING_NEW_INPUT)
            {
                var mouse = _mouseCurrent.GetValue(null, null);

                PropertyInfo btnProp;
                if (btn < 2) btnProp = _leftButton;
                else btnProp = _rightButton;

                var actualBtn = btnProp.GetValue(mouse, null);

                return (bool)_isPressed.GetValue(actualBtn, null);
            }

            return (bool)_getMouseButton.Invoke(null, new object[] { btn });
        }
    }
}
