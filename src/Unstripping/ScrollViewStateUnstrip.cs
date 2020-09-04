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
    // This is a manual unstrip of UnityEngine.ScrollViewState.
    // This code is provided "as-is".
    // Taken from dnSpy output using Unity 2018.4.20.

    // "Unity", Unity logos, and other Unity trademarks are trademarks or 
    // registered trademarks of Unity Technologies or its affiliates in the 
    // U.S. and elsewhere. 
    // https://unity3d.com/legal/terms-of-service
    // https://unity3d.com/legal/terms-of-service/software

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
