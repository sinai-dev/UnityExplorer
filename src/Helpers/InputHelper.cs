using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;

namespace Explorer
{
    /// <summary>
    /// Version-agnostic UnityEngine.Input module using Reflection
    /// </summary>
    public class InputHelper
    {
        private static readonly Type input = ReflectionHelpers.GetTypeByName("UnityEngine.Input");

        private static readonly PropertyInfo mousePositionInfo = input.GetProperty("mousePosition");

        private static readonly MethodInfo getKey = input.GetMethod("GetKey", new Type[] { typeof(KeyCode) });
        private static readonly MethodInfo getKeyDown = input.GetMethod("GetKeyDown", new Type[] { typeof(KeyCode) });
        private static readonly MethodInfo getMouseButtonDown = input.GetMethod("GetMouseButtonDown", new Type[] { typeof(int) });
        private static readonly MethodInfo getMouseButton = input.GetMethod("GetMouseButton", new Type[] { typeof(int) });

#pragma warning disable IDE1006 // Camel-case property (Unity style)
        public static Vector3 mousePosition => (Vector3)mousePositionInfo.GetValue(null);
#pragma warning restore IDE1006

        public static bool GetKeyDown(KeyCode key)
        {
            return (bool)getKeyDown.Invoke(null, new object[] { key });
        }

        public static bool GetKey(KeyCode key)
        {
            return (bool)getKey.Invoke(null, new object[] { key });
        }

        /// <param name="btn">1 = left, 2 = middle, 3 = right, etc</param>
        public static bool GetMouseButtonDown(int btn)
        {
            return (bool)getMouseButtonDown.Invoke(null, new object[] { btn });
        }

        /// <param name="btn">1 = left, 2 = middle, 3 = right, etc</param>
        public static bool GetMouseButton(int btn)
        {
            return (bool)getMouseButton.Invoke(null, new object[] { btn });
        }
    }
}
