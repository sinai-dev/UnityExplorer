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
            var color32 = new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255), 
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255), 
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255), 
                1
            );

            return string.Format("{0:X2}{1:X2}{2:X2}", color32.r, color32.g, color32.b);
        }
    }
}
