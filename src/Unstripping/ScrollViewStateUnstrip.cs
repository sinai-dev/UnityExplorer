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
    // This is a copy+paste of UnityEngine source code, fixed for Il2Cpp.
    // Taken from dnSpy output using Unity 2018.4.20.

    // Subject to Unity's License and ToS.
    // https://unity3d.com/legal/terms-of-service
    // https://unity3d.com/legal/terms-of-service/software

    public class ScrollViewStateUnstrip
    {
        public Rect position;
        public Rect visibleRect;
        public Rect viewRect;
        public Vector2 scrollPosition;
        public bool apply;

        // The code below is not unstripped. 
        // This is a custom dictionary to allow for the manual implementation.

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
