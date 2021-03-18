using System.Globalization;
using UnityEngine;

namespace UnityExplorer.Core.Unity
{
    public static class ColorHelper
    {
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
        /// Assumes the string is a 6-digit RGB Hex color code, which it will parse into a UnityEngine.Color.
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
