using System;
using Mono.CSharp;
using UnityExplorer.Helpers;
#if CPP
using UnhollowerBaseLib;
#endif

namespace UnityExplorer.Unstrip
{
    public class ResourcesUnstrip
    {
#if CPP
        internal delegate IntPtr d_FindObjectsOfTypeAll(IntPtr type);

        public static UnityEngine.Object[] FindObjectsOfTypeAll(Il2CppSystem.Type type)
        {
            var iCall = ICallHelper.GetICall<d_FindObjectsOfTypeAll>("UnityEngine.Resources::FindObjectsOfTypeAll");

            return new Il2CppReferenceArray<UnityEngine.Object>(iCall.Invoke(type.Pointer));
        }
#else
        public static UnityEngine.Object[] FindObjectsOfTypeAll(Type type) => UnityEngine.Resources.FindObjectsOfTypeAll(type);
#endif

    }
}
