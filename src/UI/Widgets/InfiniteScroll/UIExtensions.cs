using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityExplorer.UI.Widgets.InfiniteScroll
{
    public static class UIExtension
    {
        public static Vector3[] GetCorners(this RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return corners;
        }
        public static float MaxY(this RectTransform rectTransform)
        {
            return rectTransform.GetCorners()[1].y;
        }

        public static float MinY(this RectTransform rectTransform)
        {
            return rectTransform.GetCorners()[0].y;
        }

        public static float MaxX(this RectTransform rectTransform)
        {
            return rectTransform.GetCorners()[2].x;
        }

        public static float MinX(this RectTransform rectTransform)
        {
            return rectTransform.GetCorners()[0].x;
        }

    }
}
