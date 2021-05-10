using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

// Project-wide namespace for accessibility
namespace UnityExplorer
{
    public static class UnityHelpers
    {
        // Time helpers, can't use Time.time since timeScale will affect it.

        // default 10ms (one frame at 100fps)
        public static bool OccuredEarlierThanDefault(this float time)
        {
            return Time.realtimeSinceStartup - 0.01f >= time;
        }

        public static bool OccuredEarlierThan(this float time, float secondsAgo)
        {
            return Time.realtimeSinceStartup - secondsAgo >= time;
        }

        /// <summary>
        /// Check if an object is null, and if it's a UnityEngine.Object then also check if it was destroyed.
        /// </summary>
        public static bool IsNullOrDestroyed(this object obj, bool suppressWarning = true)
        {
            var unityObj = obj as Object;
            if (obj == null)
            {
                if (!suppressWarning)
                    ExplorerCore.LogWarning("The target instance is null!");

                return true;
            }
            else if (obj is Object)
            {
                if (!unityObj)
                {
                    if (!suppressWarning)
                        ExplorerCore.LogWarning("The target UnityEngine.Object was destroyed!");

                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Print a nice {X, Y, Z} output of the Vector3, formatted to 3 decimal places.
        /// </summary>
        public static string ToStringPretty(this Vector3 vec)
        {
            return $"{vec.x:F3}, {vec.y:F3}, {vec.z:F3}";
        }
        
        /// <summary>
        /// Get the full Transform heirarchy path for this provided Transform.
        /// </summary>
        public static string GetTransformPath(this Transform transform, bool includeSelf = false)
        {
            string path = includeSelf 
                ? transform.transform.name 
                : "";

            while (transform.parent)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }

            return path;
        }

        /// <summary>
        /// Converts Color to 6-digit RGB hex code (without # symbol). Eg, RGBA(1,0,0,1) -> FF0000
        /// </summary>
        public static string ToHex(this Color color)
        {
            byte r = (byte)Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
            byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
            byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);

            return $"{r:X2}{g:X2}{b:X2}";
        }

        /// <summary>
        /// Assumes the string is a 6-digit RGB Hex color code (with optional leading #) which it will parse into a UnityEngine.Color.
        /// Eg, FF0000 -> RGBA(1,0,0,1)
        /// </summary>
        public static Color ToColor(this string _string)
        {
            _string = _string.Replace("#", "");

            if (_string.Length != 6)
                return Color.magenta;

            var r = byte.Parse(_string.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(_string.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(_string.Substring(4, 2), NumberStyles.HexNumber);

            var color = new Color
            {
                r = (float)(r / (decimal)255),
                g = (float)(g / (decimal)255),
                b = (float)(b / (decimal)255),
                a = 1
            };

            return color;
        }
    }
}
