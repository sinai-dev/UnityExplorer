#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityExplorer.Core.Runtime.Il2Cpp;

namespace UnityExplorer
{
    public class AssetBundle
    {
        // ~~~~~~~~~~~~ Static ~~~~~~~~~~~~

        internal delegate IntPtr d_LoadFromFile(IntPtr path, uint crc, ulong offset);

        public static AssetBundle LoadFromFile(string path)
        {
            var iCall = ICallManager.GetICall<d_LoadFromFile>("UnityEngine.AssetBundle::LoadFromFile_Internal");
            var ptr = iCall.Invoke(IL2CPP.ManagedStringToIl2Cpp(path), 0u, 0UL);
            return new AssetBundle(ptr);
        }
        
        private delegate IntPtr d_LoadFromMemory(IntPtr binary, uint crc);

        public static AssetBundle LoadFromMemory(byte[] binary, uint crc = 0)
        {
            var iCall = ICallManager.GetICall<d_LoadFromMemory>("UnityEngine.AssetBundle::LoadFromMemory_Internal");
            var ptr = iCall(((Il2CppStructArray<byte>) binary).Pointer, crc);
            return new AssetBundle(ptr);
        }

        // static void UnloadAllAssetBundles(bool unloadAllObjects);

        internal delegate void d_UnloadAllAssetBundles(bool unloadAllObjects);

        public static void UnloadAllAssetBundles(bool unloadAllObjects)
        {
            var iCall = ICallManager.GetICall<d_UnloadAllAssetBundles>("UnityEngine.AssetBundle::UnloadAllAssetBundles");
            iCall.Invoke(unloadAllObjects);
        }

        // ~~~~~~~~~~~~ Instance ~~~~~~~~~~~~

        private readonly IntPtr m_bundlePtr = IntPtr.Zero;

        public AssetBundle(IntPtr ptr) { m_bundlePtr = ptr; }

        // LoadAllAssets()

        internal delegate IntPtr d_LoadAssetWithSubAssets_Internal(IntPtr _this, IntPtr name, IntPtr type);

        public UnityEngine.Object[] LoadAllAssets()
        {
            var iCall = ICallManager.GetICall<d_LoadAssetWithSubAssets_Internal>("UnityEngine.AssetBundle::LoadAssetWithSubAssets_Internal");
            var ptr = iCall.Invoke(m_bundlePtr, IL2CPP.ManagedStringToIl2Cpp(""), Il2CppType.Of<UnityEngine.Object>().Pointer);

            if (ptr == IntPtr.Zero)
                return new UnityEngine.Object[0];

            return new Il2CppReferenceArray<UnityEngine.Object>(ptr);
        }

        // LoadAsset<T>(string name, Type type)

        internal delegate IntPtr d_LoadAsset_Internal(IntPtr _this, IntPtr name, IntPtr type);

        public T LoadAsset<T>(string name) where T : UnityEngine.Object
        {
            var iCall = ICallManager.GetICall<d_LoadAsset_Internal>("UnityEngine.AssetBundle::LoadAsset_Internal");
            var ptr = iCall.Invoke(m_bundlePtr, IL2CPP.ManagedStringToIl2Cpp(name), Il2CppType.Of<T>().Pointer);

            if (ptr == IntPtr.Zero)
                return null;

            return new UnityEngine.Object(ptr).TryCast<T>();
        }

        // public extern void Unload(bool unloadAllLoadedObjects);
        internal delegate void d_Unload(IntPtr _this, bool unloadAllLoadedObjects);

        public void Unload(bool unloadAssets = true)
        {
            var iCall = ICallManager.GetICall<d_Unload>("UnityEngine.AssetBundle::Unload");
            iCall.Invoke(this.m_bundlePtr, unloadAssets);
        }
    }
}
#endif