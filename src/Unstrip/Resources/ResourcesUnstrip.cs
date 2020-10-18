using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Explorer.Helpers;
using UnityEngine;
#if CPP
using UnhollowerBaseLib;
#endif

namespace Explorer.Unstrip.Resources
{
    public class ResourcesUnstrip
    {
#if CPP
        internal delegate IntPtr d_FindObjectsOfTypeAll(IntPtr type);

        public static UnityEngine.Object[] FindObjectsOfTypeAll(Il2CppSystem.Type type)
        {
            var arrayPtr = ICallHelper.GetICall<d_FindObjectsOfTypeAll>("UnityEngine.Resources::FindObjectsOfTypeAll")
                .Invoke(type.Pointer);

            var array = new Il2CppReferenceArray<UnityEngine.Object>(arrayPtr);

            var ret = new UnityEngine.Object[array.Length];

            for (int i = 0; i < array.Length; i++)
            {
                ret[i] = array[i];
            }

            return ret;
        }
#else
        public static UnityEngine.Object[] FindObjectsOfTypeAll(Type type) => UnityEngine.Resources.FindObjectsOfTypeAll(type);
#endif

    }
}
