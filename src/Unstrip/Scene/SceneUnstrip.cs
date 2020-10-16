#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Explorer.Helpers;
using UnhollowerBaseLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Explorer.Unstrip.Scenes
{
    public class SceneUnstrip
    {
        //Scene.GetRootGameObjects();

        internal delegate void d_GetRootGameObjects(int handle, IntPtr list);

        public static GameObject[] GetRootGameObjects(Scene scene)
        {
            var list = new Il2CppSystem.Collections.Generic.List<GameObject>(GetRootCount(scene));

            var iCall = ICallHelper.GetICall<d_GetRootGameObjects>("UnityEngine.SceneManagement.Scene::GetRootGameObjectsInternal");

            iCall.Invoke(scene.handle, list.Pointer);

            return list.ToArray();
        }

        //Scene.rootCount;

        internal delegate int GetRootCountInternal_delegate(int handle);

        public static int GetRootCount(Scene scene)
        {
            var iCall = ICallHelper.GetICall<GetRootCountInternal_delegate>("UnityEngine.SceneManagement.Scene::GetRootCountInternal");
            return iCall.Invoke(scene.handle);
        }
    }
}
#endif