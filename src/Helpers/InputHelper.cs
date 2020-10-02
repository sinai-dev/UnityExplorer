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
        // If no Input modules loaded at all
        public static bool NO_INPUT;

        // If using new InputSystem module
        public static bool USING_NEW_INPUT;

        // Cached Types
        private static Type TInput => _input ?? (_input = ReflectionHelpers.GetTypeByName("UnityEngine.Input"));
        private static Type _input;

        private static Type TKeyboard => _keyboard ?? (_keyboard = ReflectionHelpers.GetTypeByName("UnityEngine.InputSystem.Keyboard"));
        private static Type _keyboard;

        private static Type TMouse => _mouse ?? (_mouse = ReflectionHelpers.GetTypeByName("UnityEngine.InputSystem.Mouse"));
        private static Type _mouse;

        private static Type TKey => _key ?? (_key = ReflectionHelpers.GetTypeByName("UnityEngine.InputSystem.Key"));
        private static Type _key;

        // Cached member infos (new system)
        private static PropertyInfo _btnIsPressedProp;
        private static PropertyInfo _btnWasPressedProp;

        private static object CurrentKeyboard => _currentKeyboard ?? (_currentKeyboard = _kbCurrentProp.GetValue(null, null));
        private static object _currentKeyboard;
        private static PropertyInfo _kbCurrentProp;
        private static PropertyInfo _kbIndexer;

        private static object CurrentMouse => _currentMouse ?? (_currentMouse = _mouseCurrentProp.GetValue(null, null));
        private static object _currentMouse;
        private static PropertyInfo _mouseCurrentProp;

        private static object LeftMouseButton => _lmb ?? (_lmb = _leftButtonProp.GetValue(CurrentMouse, null));
        private static object _lmb;
        private static PropertyInfo _leftButtonProp;

        private static object RightMouseButton => _rmb ?? (_rmb = _rightButtonProp.GetValue(CurrentMouse, null));
        private static object _rmb;
        private static PropertyInfo _rightButtonProp;

        private static object MousePositionInfo => _pos ?? (_pos = _positionProp.GetValue(CurrentMouse, null));
        private static object _pos;
        private static PropertyInfo _positionProp;
        private static MethodInfo _readVector2InputMethod;

        // Cached member infos (legacy)
        private static PropertyInfo _mousePositionProp;
        private static MethodInfo _getKeyMethod;
        private static MethodInfo _getKeyDownMethod;
        private static MethodInfo _getMouseButtonMethod;
        private static MethodInfo _getMouseButtonDownMethod;

        public static void Init()
        {
            if (TKeyboard != null || TryLoadModule("Unity.InputSystem", TKeyboard))
            {
                InitNewInput();
            }
            else if (TInput != null || TryLoadModule("UnityEngine.Input", TInput))
            {
                InitLegacyInput();
            }
            else
            {
                ExplorerCore.LogWarning("Could not find any Input module!");
                NO_INPUT = true;
            }
        }

        private static bool TryLoadModule(string dll, Type check) => ReflectionHelpers.LoadModule(dll) && check != null;

        private static void InitNewInput()
        {
            ExplorerCore.Log("Initializing new InputSystem support...");

            USING_NEW_INPUT = true;

            _kbCurrentProp = TKeyboard.GetProperty("current");
            _kbIndexer = TKeyboard.GetProperty("Item", new Type[] { TKey });

            var btnControl = ReflectionHelpers.GetTypeByName("UnityEngine.InputSystem.Controls.ButtonControl");
            _btnIsPressedProp = btnControl.GetProperty("isPressed");
            _btnWasPressedProp = btnControl.GetProperty("wasPressedThisFrame");

            _mouseCurrentProp = TMouse.GetProperty("current");
            _leftButtonProp = TMouse.GetProperty("leftButton");
            _rightButtonProp = TMouse.GetProperty("rightButton");

            _positionProp = ReflectionHelpers.GetTypeByName("UnityEngine.InputSystem.Pointer")
                            .GetProperty("position");

            _readVector2InputMethod = ReflectionHelpers.GetTypeByName("UnityEngine.InputSystem.InputControl`1")
                                      .MakeGenericType(typeof(Vector2))
                                      .GetMethod("ReadValue");
        }

        private static void InitLegacyInput()
        {
            ExplorerCore.Log("Initializing Legacy Input support...");

            _mousePositionProp = TInput.GetProperty("mousePosition");
            _getKeyMethod = TInput.GetMethod("GetKey", new Type[] { typeof(KeyCode) });
            _getKeyDownMethod = TInput.GetMethod("GetKeyDown", new Type[] { typeof(KeyCode) });
            _getMouseButtonMethod = TInput.GetMethod("GetMouseButton", new Type[] { typeof(int) });
            _getMouseButtonDownMethod = TInput.GetMethod("GetMouseButtonDown", new Type[] { typeof(int) });
        }

        public static Vector3 MousePosition
        {
            get
            {
                if (NO_INPUT) 
                    return Vector3.zero;

                if (USING_NEW_INPUT)
                    return (Vector2)_readVector2InputMethod.Invoke(MousePositionInfo, new object[0]);

                return (Vector3)_mousePositionProp.GetValue(null, null);
            }
        }

        public static bool GetKeyDown(KeyCode key)
        {
            if (NO_INPUT) return false;

            if (USING_NEW_INPUT)
            {
                var parsedKey = Enum.Parse(TKey, key.ToString());
                var actualKey = _kbIndexer.GetValue(CurrentKeyboard, new object[] { parsedKey });

                return (bool)_btnWasPressedProp.GetValue(actualKey, null);
            }

            return (bool)_getKeyDownMethod.Invoke(null, new object[] { key });
        }

        public static bool GetKey(KeyCode key)
        {
            if (NO_INPUT) return false;

            if (USING_NEW_INPUT)
            {
                var parsed = Enum.Parse(TKey, key.ToString());
                var actualKey = _kbIndexer.GetValue(CurrentKeyboard, new object[] { parsed });

                return (bool)_btnIsPressedProp.GetValue(actualKey, null);
            }

            return (bool)_getKeyMethod.Invoke(null, new object[] { key });
        }

        /// <param name="btn">0 = left, 1 = right, 2 = middle.</param>
        public static bool GetMouseButtonDown(int btn)
        {
            if (NO_INPUT) return false;

            if (USING_NEW_INPUT)
            {
                object actualBtn;
                switch (btn)
                {
                    case 0: actualBtn = LeftMouseButton; break;
                    case 1: actualBtn = RightMouseButton; break;
                    default: throw new NotImplementedException();
                }

                return (bool)_btnWasPressedProp.GetValue(actualBtn, null);
            }

            return (bool)_getMouseButtonDownMethod.Invoke(null, new object[] { btn });
        }

        /// <param name="btn">0 = left, 1 = right, 2 = middle.</param>
        public static bool GetMouseButton(int btn)
        {
            if (NO_INPUT) return false;

            if (USING_NEW_INPUT)
            {
                object actualBtn;
                switch (btn)
                {
                    case 0: actualBtn = LeftMouseButton; break;
                    case 1: actualBtn = RightMouseButton; break;
                    default: throw new NotImplementedException();
                }

                return (bool)_btnIsPressedProp.GetValue(actualBtn, null);
            }

            return (bool)_getMouseButtonMethod.Invoke(null, new object[] { btn });
        }
    }
}
