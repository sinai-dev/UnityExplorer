using System;
using UnityExplorer.Helpers;
using UnityEngine;
#if CPP
using UnhollowerBaseLib;
#endif

namespace UnityExplorer.Unstrip.LayerMasks
{
    public static class LayerMaskUnstrip
    {
#if CPP
        internal delegate IntPtr d_LayerToName(int layer);

        public static string LayerToName(int layer)
        {
            d_LayerToName iCall = ICallHelper.GetICall<d_LayerToName>("UnityEngine.LayerMask::LayerToName");
            return IL2CPP.Il2CppStringToManaged(iCall.Invoke(layer));
        }
#else
        public static string LayerToName(int layer) => LayerMask.LayerToName(layer);
#endif
    }
}
