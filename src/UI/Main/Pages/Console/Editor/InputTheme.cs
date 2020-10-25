using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Explorer.UI.Main.Pages.Console
{
    public static class InputTheme
    {
        public static bool allowSyntaxHighlighting = true;

        public static Color caretColor = new Color32(255, 255, 255, 255);
        public static Color textColor = new Color32(255, 255, 255, 255);
        public static Color backgroundColor = new Color32(37, 37, 37, 255);
        public static Color lineHighlightColor = new Color32(50, 50, 50, 255);
        public static Color lineNumberBackgroundColor = new Color32(25, 25, 25, 255);
        public static Color lineNumberTextColor = new Color32(180, 180, 180, 255);
        public static Color scrollbarColor = new Color32(45, 50, 50, 255);
    }
}
