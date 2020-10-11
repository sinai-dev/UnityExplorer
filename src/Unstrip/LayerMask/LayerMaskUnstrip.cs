using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
#if CPP
using UnhollowerBaseLib;
#endif

namespace Explorer.Unstrip.LayerMasks
{
    public static class LayerMaskUnstrip
    {
#if CPP
        internal delegate IntPtr d_LayerToName(int layer);
        internal static d_LayerToName LayerToName_iCall =
            IL2CPP.ResolveICall<d_LayerToName>("UnityEngine.LayerMask::LayerToName");

        public static string LayerToName(int layer)
        {
            var ptr = LayerToName_iCall(layer);

            return IL2CPP.Il2CppStringToManaged(ptr);
        }
#else
        public static string LayerToName(int layer)
        {
            return LayerMask.LayerToName(layer);
        }
#endif
    }
}
