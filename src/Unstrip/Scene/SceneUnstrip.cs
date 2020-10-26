#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExplorerBeta.Helpers;
using UnhollowerBaseLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ExplorerBeta.Unstrip.Scenes
{
    public class SceneUnstrip
    {
        //Scene.GetRootGameObjects();

        internal delegate void d_GetRootGameObjects(int handle, IntPtr list);

        public static GameObject[] GetRootGameObjects(Scene scene) => GetRootGameObjects(scene.handle);

        public static GameObject[] GetRootGameObjects(int handle)
        {
            if (handle == -1)
                return new GameObject[0];

            var list = new Il2CppSystem.Collections.Generic.List<GameObject>(GetRootCount(handle));

            var iCall = ICallHelper.GetICall<d_GetRootGameObjects>("UnityEngine.SceneManagement.Scene::GetRootGameObjectsInternal");

            iCall.Invoke(handle, list.Pointer);

            return list.ToArray();
        }

        //Scene.rootCount;

        internal delegate int GetRootCountInternal_delegate(int handle);

        public static int GetRootCount(Scene scene) => GetRootCount(scene.handle);

        public static int GetRootCount(int handle)
        {
            var iCall = ICallHelper.GetICall<GetRootCountInternal_delegate>("UnityEngine.SceneManagement.Scene::GetRootCountInternal");
            return iCall.Invoke(handle);
        }
    }
}
#endif