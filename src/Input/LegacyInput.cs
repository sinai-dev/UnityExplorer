using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Explorer.Helpers;

namespace Explorer.Input
{
    public class LegacyInput : IAbstractInput
    {
        public static Type TInput => _input ?? (_input = ReflectionHelpers.GetTypeByName("UnityEngine.Input"));
        private static Type _input;

        private static PropertyInfo _mousePositionProp;
        private static MethodInfo _getKeyMethod;
        private static MethodInfo _getKeyDownMethod;
        private static MethodInfo _getMouseButtonMethod;
        private static MethodInfo _getMouseButtonDownMethod;

        public Vector2 MousePosition => (Vector3)_mousePositionProp.GetValue(null, null);

        public bool GetKey(KeyCode key) => (bool)_getKeyMethod.Invoke(null, new object[] { key });

        public bool GetKeyDown(KeyCode key) => (bool)_getKeyDownMethod.Invoke(null, new object[] { key });

        public bool GetMouseButton(int btn) => (bool)_getMouseButtonMethod.Invoke(null, new object[] { btn });

        public bool GetMouseButtonDown(int btn) => (bool)_getMouseButtonDownMethod.Invoke(null, new object[] { btn });

        public void Init()
        {
            ExplorerCore.Log("Initializing Legacy Input support...");

            _mousePositionProp = TInput.GetProperty("mousePosition");
            _getKeyMethod = TInput.GetMethod("GetKey", new Type[] { typeof(KeyCode) });
            _getKeyDownMethod = TInput.GetMethod("GetKeyDown", new Type[] { typeof(KeyCode) });
            _getMouseButtonMethod = TInput.GetMethod("GetMouseButton", new Type[] { typeof(int) });
            _getMouseButtonDownMethod = TInput.GetMethod("GetMouseButtonDown", new Type[] { typeof(int) });
        }
    }
}
