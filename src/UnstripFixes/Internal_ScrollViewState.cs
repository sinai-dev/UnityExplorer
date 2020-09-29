#if CPP
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Explorer
{
    public class Internal_ScrollViewState
    {
        public Rect position;
        public Rect visibleRect;
        public Rect viewRect;
        public Vector2 scrollPosition;
        public bool apply;

        public static Dictionary<IntPtr, Internal_ScrollViewState> Dict = new Dictionary<IntPtr, Internal_ScrollViewState>();

        public static Internal_ScrollViewState FromPointer(IntPtr ptr)
        {
            if (!Dict.ContainsKey(ptr))
            {
                Dict.Add(ptr, new Internal_ScrollViewState());
            }

            return Dict[ptr];
        }
    }
}
#endif