#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnhollowerBaseLib;
using UnhollowerBaseLib.Attributes;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityExplorer.Core.Runtime.Il2Cpp;

namespace UnityExplorer
{
    public class AssetBundle : UnityEngine.Object
    {
        static AssetBundle()
        {
            ClassInjector.RegisterTypeInIl2Cpp<AssetBundle>();
        }

        // ~~~~~~~~~~~~ Static ~~~~~~~~~~~~

        internal delegate IntPtr d_LoadFromFile(IntPtr path, uint crc, ulong offset);

        [HideFromIl2Cpp]
        public static AssetBundle LoadFromFile(string path)
        {
            var iCall = ICallManager.GetICall<d_LoadFromFile>("UnityEngine.AssetBundle::LoadFromFile_Internal");
            var ptr = iCall(IL2CPP.ManagedStringToIl2Cpp(path), 0u, 0UL);
            return new AssetBundle(ptr);
        }

        private delegate IntPtr d_LoadFromMemory(IntPtr binary, uint crc);

        [HideFromIl2Cpp]
        public static AssetBundle LoadFromMemory(byte[] binary, uint crc = 0)
        {
            var iCall = ICallManager.GetICall<d_LoadFromMemory>("UnityEngine.AssetBundle::LoadFromMemory_Internal");
            var ptr = iCall(((Il2CppStructArray<byte>)binary).Pointer, crc);
            return new AssetBundle(ptr);
        }

        public delegate IntPtr d_GetAllLoadedAssetBundles_Native();

        [HideFromIl2Cpp]
        public static AssetBundle[] GetAllLoadedAssetBundles()
        {
            var iCall = ICallManager.GetICall<d_GetAllLoadedAssetBundles_Native>("UnityEngine.AssetBundle::GetAllLoadedAssetBundles_Native");
            var ptr = iCall();
            if (ptr == IntPtr.Zero)
                return null;
            return (AssetBundle[])new Il2CppReferenceArray<AssetBundle>(ptr);
        }

        // ~~~~~~~~~~~~ Instance ~~~~~~~~~~~~

        public readonly IntPtr m_bundlePtr = IntPtr.Zero;

        public AssetBundle(IntPtr ptr) : base(ptr) { m_bundlePtr = ptr; }

        // LoadAllAssets()

        internal delegate IntPtr d_LoadAssetWithSubAssets_Internal(IntPtr _this, IntPtr name, IntPtr type);

        [HideFromIl2Cpp]
        public UnityEngine.Object[] LoadAllAssets()
        {
            var iCall = ICallManager.GetICall<d_LoadAssetWithSubAssets_Internal>("UnityEngine.AssetBundle::LoadAssetWithSubAssets_Internal");
            var ptr = iCall.Invoke(m_bundlePtr, IL2CPP.ManagedStringToIl2Cpp(""), UnhollowerRuntimeLib.Il2CppType.Of<UnityEngine.Object>().Pointer);

            if (ptr == IntPtr.Zero)
                return new UnityEngine.Object[0];

            return new Il2CppReferenceArray<UnityEngine.Object>(ptr);
        }

        // LoadAsset<T>(string name, Type type)

        internal delegate IntPtr d_LoadAsset_Internal(IntPtr _this, IntPtr name, IntPtr type);

        [HideFromIl2Cpp]
        public T LoadAsset<T>(string name) where T : UnityEngine.Object
        {
            var iCall = ICallManager.GetICall<d_LoadAsset_Internal>("UnityEngine.AssetBundle::LoadAsset_Internal");
            var ptr = iCall.Invoke(m_bundlePtr, IL2CPP.ManagedStringToIl2Cpp(name), UnhollowerRuntimeLib.Il2CppType.Of<T>().Pointer);

            if (ptr == IntPtr.Zero)
                return null;

            return new UnityEngine.Object(ptr).TryCast<T>();
        }

        // Unload(bool unloadAllLoadedObjects);

        internal delegate void d_Unload(IntPtr _this, bool unloadAllLoadedObjects);

        [HideFromIl2Cpp]
        public void Unload(bool unloadAllLoadedObjects)
        {
            var iCall = ICallManager.GetICall<d_Unload>("UnityEngine.AssetBundle::Unload");
            iCall.Invoke(this.m_bundlePtr, unloadAllLoadedObjects);
        }
    }
}
#endif