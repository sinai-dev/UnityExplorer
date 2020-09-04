using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using MelonLoader;
using UnityEngine;

namespace Explorer
{
    public class ScrollViewStateUnstrip
    {
        public Rect position;
        public Rect visibleRect;
        public Rect viewRect;
        public Vector2 scrollPosition;
        public bool apply;

        public static Dictionary<IntPtr, ScrollViewStateUnstrip> Dict = new Dictionary<IntPtr, ScrollViewStateUnstrip>();

        public static ScrollViewStateUnstrip FromPointer(IntPtr ptr)
        {
            if (!Dict.ContainsKey(ptr))
            {
                Dict.Add(ptr, new ScrollViewStateUnstrip());
            }

            return Dict[ptr];
        }
    }
}
