using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Explorer.Helpers;

namespace Explorer.Input
{
    public class InputSystem : IAbstractInput
    {
        public static Type TKeyboard => _keyboard ?? (_keyboard = ReflectionHelpers.GetTypeByName("UnityEngine.InputSystem.Keyboard"));
        private static Type _keyboard;

        public static Type TMouse => _mouse ?? (_mouse = ReflectionHelpers.GetTypeByName("UnityEngine.InputSystem.Mouse"));
        private static Type _mouse;

        public static Type TKey => _key ?? (_key = ReflectionHelpers.GetTypeByName("UnityEngine.InputSystem.Key"));
        private static Type _key;

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

        public Vector2 MousePosition => (Vector2)_readVector2InputMethod.Invoke(MousePositionInfo, new object[0]);

        public bool GetKeyDown(KeyCode key)
        {
            var parsedKey = Enum.Parse(TKey, key.ToString());
            var actualKey = _kbIndexer.GetValue(CurrentKeyboard, new object[] { parsedKey });

            return (bool)_btnWasPressedProp.GetValue(actualKey, null);
        }

        public bool GetKey(KeyCode key)
        {
            var parsed = Enum.Parse(TKey, key.ToString());
            var actualKey = _kbIndexer.GetValue(CurrentKeyboard, new object[] { parsed });

            return (bool)_btnIsPressedProp.GetValue(actualKey, null);
        }

        public bool GetMouseButtonDown(int btn)
        {
            switch (btn)
            {
                case 0: return (bool)_btnWasPressedProp.GetValue(LeftMouseButton, null);
                case 1: return (bool)_btnWasPressedProp.GetValue(RightMouseButton, null);
                // case 2: return (bool)_btnWasPressedProp.GetValue(MiddleMouseButton, null);
                default: throw new NotImplementedException();
            }
        }

        public bool GetMouseButton(int btn)
        {
            switch (btn)
            {
                case 0: return (bool)_btnIsPressedProp.GetValue(LeftMouseButton, null);
                case 1: return (bool)_btnIsPressedProp.GetValue(RightMouseButton, null);
                // case 2: return (bool)_btnIsPressedProp.GetValue(MiddleMouseButton, null);
                default: throw new NotImplementedException();
            }
        }

        public void Init()
        {
            ExplorerCore.Log("Initializing new InputSystem support...");

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
    }
}
