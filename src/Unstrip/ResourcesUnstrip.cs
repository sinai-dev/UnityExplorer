using System;
using Mono.CSharp;
using UnityExplorer.Helpers;
#if CPP
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
#endif

namespace UnityExplorer.Unstrip
{
    public class ResourcesUnstrip
    {
        public static UnityEngine.Object[] FindObjectsOfTypeAll(Type type)
        {
#if MONO
            return UnityEngine.Resources.FindObjectsOfTypeAll(type);
#else
            var iCall = ICallHelper.GetICall<d_FindObjectsOfTypeAll>("UnityEngine.Resources::FindObjectsOfTypeAll");
            var cppType = Il2CppType.From(type);

            return new Il2CppReferenceArray<UnityEngine.Object>(iCall.Invoke(cppType.Pointer));
#endif
        }

#if CPP
        internal delegate IntPtr d_FindObjectsOfTypeAll(IntPtr type);
#endif
    }
}
