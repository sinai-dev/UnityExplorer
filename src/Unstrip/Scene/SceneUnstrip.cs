using System;
using ExplorerBeta.Helpers;
using ExplorerBeta.UI.Main;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ExplorerBeta.Unstrip.Scenes
{
    public class SceneUnstrip
    {
        public static GameObject[] GetRootGameObjects(Scene scene) => scene.GetRootGameObjects();

        public static GameObject[] GetRootGameObjects(int handle)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.handle == handle)
                    return scene.GetRootGameObjects();
            }
            return new GameObject[0];
        }

        //Scene.GetRootGameObjects();

#if CPP
        internal delegate void d_GetRootGameObjects(int handle, IntPtr list);

        public static GameObject[] GetRootGameObjects(Scene scene) => GetRootGameObjects(scene.handle);

        public static GameObject[] GetRootGameObjects(int handle)
        {
            if (handle == -1)
            {
                return new GameObject[0];
            }

            Il2CppSystem.Collections.Generic.List<GameObject> list = new Il2CppSystem.Collections.Generic.List<GameObject>(GetRootCount(handle));

            d_GetRootGameObjects iCall = ICallHelper.GetICall<d_GetRootGameObjects>("UnityEngine.SceneManagement.Scene::GetRootGameObjectsInternal");

            iCall.Invoke(handle, list.Pointer);

            return list.ToArray();
        }

        //Scene.rootCount;

        internal delegate int GetRootCountInternal_delegate(int handle);

        public static int GetRootCount(Scene scene) => GetRootCount(scene.handle);

        public static int GetRootCount(int handle)
        {
            GetRootCountInternal_delegate iCall = ICallHelper.GetICall<GetRootCountInternal_delegate>("UnityEngine.SceneManagement.Scene::GetRootCountInternal");
            return iCall.Invoke(handle);
        }
#endif
    }
}