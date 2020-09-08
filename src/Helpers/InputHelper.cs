using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using MelonLoader;

namespace Explorer
{
    /// <summary>
    /// Version-agnostic UnityEngine Input module using Reflection.
    /// </summary>
    public static class InputHelper
    {
        public static void CheckInput()
        {
            if (Input == null)
            {
                MelonLogger.Log("UnityEngine.Input is null, trying to load manually....");

                if ((TryLoad("UnityEngine.InputLegacyModule.dll") || TryLoad("UnityEngine.CoreModule.dll")) && Input != null)
                {
                    MelonLogger.Log("Ok!");
                }
                else
                {
                    MelonLogger.Log("Could not load Input module!");
                }

                bool TryLoad(string module)
                {
                    var path = $@"MelonLoader\Managed\{module}";
                    if (!File.Exists(path)) return false;

                    try
                    {
                        Assembly.Load(File.ReadAllBytes(path));
                        return true;
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Log(e.GetType() + ", " + e.Message);
                        return false;
                    }
                }
            }
        }

        public static Type Input => _input ?? (_input = ReflectionHelpers.GetTypeByName("UnityEngine.Input"));
        private static Type _input;

        private static PropertyInfo MousePosInfo => _mousePosition ?? (_mousePosition = Input?.GetProperty("mousePosition"));
        private static PropertyInfo _mousePosition;

        private static MethodInfo GetKeyInfo => _getKey ?? (_getKey = Input?.GetMethod("GetKey", new Type[] { typeof(KeyCode) }));
        private static MethodInfo _getKey;

        private static MethodInfo GetKeyDownInfo => _getKeyDown ?? (_getKeyDown = Input?.GetMethod("GetKeyDown", new Type[] { typeof(KeyCode) }));
        private static MethodInfo _getKeyDown;

        private static MethodInfo GetMouseButtonInfo => _getMouseButton ?? (_getMouseButton = Input?.GetMethod("GetMouseButton", new Type[] { typeof(int) }));
        private static MethodInfo _getMouseButton;

        private static MethodInfo GetMouseButtonDownInfo => _getMouseButtonDown ?? (_getMouseButtonDown = Input?.GetMethod("GetMouseButtonDown", new Type[] { typeof(int) }));
        private static MethodInfo _getMouseButtonDown;

#pragma warning disable IDE1006 // Camel-case property (Unity style)
        public static Vector3 mousePosition
        {
            get
            {
                if (Input == null) return Vector3.zero;
                return (Vector3)MousePosInfo.GetValue(null);
            }
        }
#pragma warning restore IDE1006

        public static bool GetKeyDown(KeyCode key)
        {
            if (Input == null) return false;
            return (bool)GetKeyDownInfo.Invoke(null, new object[] { key });
        }

        public static bool GetKey(KeyCode key)
        {
            if (Input == null) return false;
            return (bool)GetKeyInfo.Invoke(null, new object[] { key });
        }

        /// <param name="btn">1 = left, 2 = middle, 3 = right, etc</param>
        public static bool GetMouseButtonDown(int btn)
        {
            if (Input == null) return false;
            return (bool)GetMouseButtonDownInfo.Invoke(null, new object[] { btn });
        }

        /// <param name="btn">1 = left, 2 = middle, 3 = right, etc</param>
        public static bool GetMouseButton(int btn)
        {
            if (Input == null) return false;
            return (bool)GetMouseButtonInfo.Invoke(null, new object[] { btn });
        }
    }
}
