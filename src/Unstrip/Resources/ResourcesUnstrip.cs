using System;
using ExplorerBeta.Helpers;
#if CPP
using UnhollowerBaseLib;
#endif

namespace ExplorerBeta.Unstrip.Resources
{
    public class ResourcesUnstrip
    {
#if CPP
        internal delegate IntPtr d_FindObjectsOfTypeAll(IntPtr type);

        public static UnityEngine.Object[] FindObjectsOfTypeAll(Il2CppSystem.Type type)
        {
            IntPtr arrayPtr = ICallHelper.GetICall<d_FindObjectsOfTypeAll>("UnityEngine.Resources::FindObjectsOfTypeAll")
                .Invoke(type.Pointer);

            Il2CppReferenceArray<UnityEngine.Object> array = new Il2CppReferenceArray<UnityEngine.Object>(arrayPtr);

            UnityEngine.Object[] ret = new UnityEngine.Object[array.Length];

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
