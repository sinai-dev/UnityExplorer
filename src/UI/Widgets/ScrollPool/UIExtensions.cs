using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityExplorer.UI
{
    public static class UIExtension
    {
        public static void GetCorners(this RectTransform rect, Vector3[] corners)
        {
            Vector3 bottomLeft = new Vector3(rect.position.x, rect.position.y - rect.rect.height, 0);

            corners[0] = bottomLeft;
            corners[1] = bottomLeft + new Vector3(0, rect.rect.height, 0);
            corners[2] = bottomLeft + new Vector3(rect.rect.width, rect.rect.height, 0);
            corners[3] = bottomLeft + new Vector3(rect.rect.width, 0, 0);
        }

        // again, using position and rect instead of 

        public static float MaxY(this RectTransform rect) => rect.position.y - rect.rect.height;

        public static float MinY(this RectTransform rect) => rect.position.y;

        public static float MaxX(this RectTransform rect) => rect.position.x - rect.rect.width;

        public static float MinX(this RectTransform rect) => rect.position.x;
    }
}
