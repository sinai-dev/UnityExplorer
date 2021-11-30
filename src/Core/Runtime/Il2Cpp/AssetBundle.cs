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

        // AssetBundle.LoadFromFile(string path)

        internal delegate IntPtr d_LoadFromFile(IntPtr path, uint crc, ulong offset);

        [HideFromIl2Cpp]
        public static AssetBundle LoadFromFile(string path)
        {
            IntPtr ptr = ICallManager.GetICall<d_LoadFromFile>("UnityEngine.AssetBundle::LoadFromFile_Internal")
                .Invoke(IL2CPP.ManagedStringToIl2Cpp(path), 0u, 0UL);

            return ptr != IntPtr.Zero ? new AssetBundle(ptr) : null;
        }

        // AssetBundle.LoadFromMemory(byte[] binary)

        private delegate IntPtr d_LoadFromMemory(IntPtr binary, uint crc);

        [HideFromIl2Cpp]
        public static AssetBundle LoadFromMemory(byte[] binary, uint crc = 0)
        {
            IntPtr ptr = ICallManager.GetICall<d_LoadFromMemory>("UnityEngine.AssetBundle::LoadFromMemory_Internal")
                .Invoke(((Il2CppStructArray<byte>)binary).Pointer, crc);

            return ptr != IntPtr.Zero ? new AssetBundle(ptr) : null;
        }

        // AssetBundle.GetAllLoadedAssetBundles()

        public delegate IntPtr d_GetAllLoadedAssetBundles_Native();

        [HideFromIl2Cpp]
        public static AssetBundle[] GetAllLoadedAssetBundles()
        {
            IntPtr ptr = ICallManager.GetICall<d_GetAllLoadedAssetBundles_Native>("UnityEngine.AssetBundle::GetAllLoadedAssetBundles_Native")
                .Invoke();

            return ptr != IntPtr.Zero ? (AssetBundle[])new Il2CppReferenceArray<AssetBundle>(ptr) : null;
        }

        // ~~~~~~~~~~~~ Instance ~~~~~~~~~~~~

        public readonly IntPtr m_bundlePtr = IntPtr.Zero;

        public AssetBundle(IntPtr ptr) : base(ptr) { m_bundlePtr = ptr; }

        // LoadAllAssets()

        internal delegate IntPtr d_LoadAssetWithSubAssets_Internal(IntPtr _this, IntPtr name, IntPtr type);

        [HideFromIl2Cpp]
        public UnityEngine.Object[] LoadAllAssets()
        {
            IntPtr ptr = ICallManager.GetICall<d_LoadAssetWithSubAssets_Internal>("UnityEngine.AssetBundle::LoadAssetWithSubAssets_Internal")
                .Invoke(m_bundlePtr, IL2CPP.ManagedStringToIl2Cpp(""), UnhollowerRuntimeLib.Il2CppType.Of<UnityEngine.Object>().Pointer);

            return ptr != IntPtr.Zero ? (UnityEngine.Object[])new Il2CppReferenceArray<UnityEngine.Object>(ptr) : new UnityEngine.Object[0];
        }

        // LoadAsset<T>(string name, Type type)

        internal delegate IntPtr d_LoadAsset_Internal(IntPtr _this, IntPtr name, IntPtr type);

        [HideFromIl2Cpp]
        public T LoadAsset<T>(string name) where T : UnityEngine.Object
        {
            IntPtr ptr = ICallManager.GetICall<d_LoadAsset_Internal>("UnityEngine.AssetBundle::LoadAsset_Internal")
                .Invoke(m_bundlePtr, IL2CPP.ManagedStringToIl2Cpp(name), UnhollowerRuntimeLib.Il2CppType.Of<T>().Pointer);

            return ptr != IntPtr.Zero ? new UnityEngine.Object(ptr).TryCast<T>() : null;
        }

        // Unload(bool unloadAllLoadedObjects);

        internal delegate void d_Unload(IntPtr _this, bool unloadAllLoadedObjects);

        [HideFromIl2Cpp]
        public void Unload(bool unloadAllLoadedObjects)
        {
            ICallManager.GetICall<d_Unload>("UnityEngine.AssetBundle::Unload")
                .Invoke(this.m_bundlePtr, unloadAllLoadedObjects);
        }
    }
}
#endif