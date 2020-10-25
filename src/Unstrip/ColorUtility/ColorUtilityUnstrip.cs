using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Explorer.Unstrip.ColorUtility
{
    public static class ColorUtilityUnstrip
    {
        public static string ToHex(this Color color)
        {
            var r = (byte)Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
            var g = (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
            var b = (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);

            return $"{r:X2}{g:X2}{b:X2}";
        }
    }
}
