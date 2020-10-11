#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnhollowerBaseLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Explorer.Unstrip.Scenes
{
    public class SceneUnstrip
    {
        //Scene.GetRootGameObjects();
        public static GameObject[] GetRootGameObjects(Scene scene)
        {
            var list = new Il2CppSystem.Collections.Generic.List<GameObject>(GetRootCount_Internal(scene));

            GetRootGameObjectsInternal_iCall(scene.handle, list.Pointer);

            return list.ToArray();
        }

        internal delegate void GetRootGameObjectsInternal_delegate(int handle, IntPtr list);
        internal static GetRootGameObjectsInternal_delegate GetRootGameObjectsInternal_iCall =
            IL2CPP.ResolveICall<GetRootGameObjectsInternal_delegate>("UnityEngine.SceneManagement.Scene::GetRootGameObjectsInternal");

        //Scene.rootCount;
        public static int GetRootCount_Internal(Scene scene)
        {
            return GetRootCountInternal_iCall(scene.handle);
        }

        internal delegate int GetRootCountInternal_delegate(int handle);
        internal static GetRootCountInternal_delegate GetRootCountInternal_iCall =
            IL2CPP.ResolveICall<GetRootCountInternal_delegate>("UnityEngine.SceneManagement.Scene::GetRootCountInternal");
    }
}
#endif