using System;
using UnityExplorer.Helpers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityExplorer.Inspectors;

namespace UnityExplorer.Unstrip
{
    public class SceneUnstrip
    {
#if MONO
        public static GameObject[] GetRootGameObjects(Scene scene) => scene.GetRootGameObjects();

        public static GameObject[] GetRootGameObjects(int handle)
        {
            Scene scene = default;
            if (handle == SceneExplorer.DontDestroyHandle)
                scene = SceneExplorer.DontDestroyObject.scene;
            else
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var iscene = SceneManager.GetSceneAt(i);
                    if (iscene.handle == handle)
                        scene = iscene;
                }
            }

            if (scene != default && scene.handle != -1)
                return scene.GetRootGameObjects();

            return new GameObject[0];
        }
#endif

#if CPP
        //Scene.GetRootGameObjects();

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