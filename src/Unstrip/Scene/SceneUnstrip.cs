#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnhollowerBaseLib;
using UnityEngine;

namespace Explorer.Unstrip.Scene
{
    public class SceneUnstrip
    {
        internal delegate void getRootSceneObjects(int handle, IntPtr list);
        internal static getRootSceneObjects getRootSceneObjects_iCall =
            IL2CPP.ResolveICall<getRootSceneObjects>("UnityEngine.SceneManagement.Scene::GetRootGameObjectsInternal");

        public static void GetRootGameObjects_Internal(UnityEngine.SceneManagement.Scene scene, IntPtr list)
        {
            getRootSceneObjects_iCall(scene.handle, list);
        }

        public static GameObject[] GetRootSceneObjects(UnityEngine.SceneManagement.Scene scene)
        {
            var list = new Il2CppSystem.Collections.Generic.List<GameObject>(GetRootCount_Internal(scene));

            GetRootGameObjects_Internal(scene, list.Pointer);

            return list.ToArray();
        }

        internal delegate int getRootCount(int handle);
        internal static getRootCount getRootCount_iCall =
            IL2CPP.ResolveICall<getRootCount>("UnityEngine.SceneManagement.Scene::GetRootCountInternal");

        public static int GetRootCount_Internal(UnityEngine.SceneManagement.Scene scene)
        {
            return getRootCount_iCall(scene.handle);
        }
    }
}
#endif