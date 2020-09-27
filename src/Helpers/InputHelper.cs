using System;
using System.Reflection;
using UnityEngine;

namespace Explorer
{
    /// <summary>
    /// Version-agnostic UnityEngine Input module using Reflection.
    /// </summary>
    public static class InputHelper
    {
        // If Input module failed to load at all
        public static bool NO_INPUT;

        // Base UnityEngine.Input class
        private static Type Input => _input ?? (_input = ReflectionHelpers.GetTypeByName("UnityEngine.Input"));
        private static Type _input;

        // Cached member infos
        private static PropertyInfo _mousePosition;
        private static MethodInfo _getKey;
        private static MethodInfo _getKeyDown;
        private static MethodInfo _getMouseButton;
        private static MethodInfo _getMouseButtonDown;

        public static void Init()
        {
            if (Input == null && !TryManuallyLoadInput())
            {
                NO_INPUT = true;
                return;
            }

            // Cache reflection now that we know Input is loaded

            _mousePosition = Input.GetProperty("mousePosition");

            _getKey = Input.GetMethod("GetKey", new Type[] { typeof(KeyCode) });
            _getKeyDown = Input.GetMethod("GetKeyDown", new Type[] { typeof(KeyCode) });
            _getMouseButton = Input.GetMethod("GetMouseButton", new Type[] { typeof(int) });
            _getMouseButtonDown = Input.GetMethod("GetMouseButtonDown", new Type[] { typeof(int) });
        }

#pragma warning disable IDE1006 // Camel-case property (Unity style)
        public static Vector3 mousePosition
        {
            get
            {
                if (NO_INPUT) return Vector3.zero;
                return (Vector3)_mousePosition.GetValue(null, null);
            }
        }
#pragma warning restore IDE1006

        public static bool GetKeyDown(KeyCode key)
        {
            if (NO_INPUT) return false;
            return (bool)_getKeyDown.Invoke(null, new object[] { key });
        }

        public static bool GetKey(KeyCode key)
        {
            if (NO_INPUT) return false;
            return (bool)_getKey.Invoke(null, new object[] { key });
        }

        /// <param name="btn">1 = left, 2 = middle, 3 = right, etc</param>
        public static bool GetMouseButtonDown(int btn)
        {
            if (NO_INPUT) return false;
            return (bool)_getMouseButtonDown.Invoke(null, new object[] { btn });
        }

        /// <param name="btn">1 = left, 2 = middle, 3 = right, etc</param>
        public static bool GetMouseButton(int btn)
        {
            if (NO_INPUT) return false;
            return (bool)_getMouseButton.Invoke(null, new object[] { btn });
        }

        private static bool TryManuallyLoadInput()
        {
            ExplorerCore.Log("UnityEngine.Input is null, trying to load manually....");

            if ((ReflectionHelpers.LoadModule("UnityEngine.InputLegacyModule.dll") || ReflectionHelpers.LoadModule("UnityEngine.CoreModule.dll")) 
                && Input != null)
            {
                ExplorerCore.Log("Ok!");
                return true;
            }
            else
            {
                ExplorerCore.Log("Could not load Input module!");
                return false;
            }
        }
    }
}
