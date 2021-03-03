using System;
using UnityExplorer.Helpers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityExplorer.Inspectors;
using System.Reflection;

namespace UnityExplorer.Unstrip
{
    public static class SceneUnstrip
    {
#if MONO
        private static readonly FieldInfo fi_Scene_handle = typeof(Scene).GetField("m_Handle", ReflectionHelpers.CommonFlags);
#endif

        public static int GetHandle(this Scene scene)
        {
#if CPP
            return scene.handle;
#else
            return (int)fi_Scene_handle.GetValue(scene);
#endif
        }

#if CPP
        //Scene.GetRootGameObjects();

        internal delegate void d_GetRootGameObjects(int handle, IntPtr list);

        public static GameObject[] GetRootGameObjects(Scene scene) => GetRootGameObjects(scene.handle);

        public static GameObject[] GetRootGameObjects(int handle)
        {
            if (handle == -1)
                return new GameObject[0];

            int count = GetRootCount(handle);

            if (count < 1)
                return new GameObject[0];

            var list = new Il2CppSystem.Collections.Generic.List<GameObject>(count);

            var iCall = ICallHelper.GetICall<d_GetRootGameObjects>("UnityEngine.SceneManagement.Scene::GetRootGameObjectsInternal");

            iCall.Invoke(handle, list.Pointer);

            return list.ToArray();
        }

        //Scene.rootCount;

        internal delegate int d_GetRootCountInternal(int handle);

        public static int GetRootCount(Scene scene) => GetRootCount(scene.handle);

        public static int GetRootCount(int handle)
        {
            return ICallHelper.GetICall<d_GetRootCountInternal>("UnityEngine.SceneManagement.Scene::GetRootCountInternal")
                   .Invoke(handle);
        }
#endif
    }
}