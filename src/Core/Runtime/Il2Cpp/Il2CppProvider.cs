#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BF = System.Reflection.BindingFlags;
using System.Text;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

namespace UnityExplorer.Core.Runtime.Il2Cpp
{
    public class Il2CppProvider : RuntimeProvider
    {
        public override void Initialize()
        {
            Reflection = new Il2CppReflection();
            TextureUtil = new Il2CppTextureUtil();
        }

        public override void SetupEvents()
        {
            try
            {
                //Application.add_logMessageReceived(new Action<string, string, LogType>(ExplorerCore.Instance.OnUnityLog));

                var logType = ReflectionUtility.GetTypeByName("UnityEngine.Application+LogCallback");
                var castMethod = logType.GetMethod("op_Implicit", new[] { typeof(Action<string, string, LogType>) });
                var addMethod = typeof(Application).GetMethod("add_logMessageReceived", BF.Static | BF.Public, null, new[] { logType }, null);
                addMethod.Invoke(null, new[]
                {
                    castMethod.Invoke(null, new[] { new Action<string, string, LogType>(ExplorerCore.Instance.OnUnityLog) })
                });
            }
            catch 
            {
                ExplorerCore.LogWarning("Exception setting up Unity log listener, make sure Unity libraries have been unstripped!");
            }
        }

        public override void StartConsoleCoroutine(IEnumerator routine)
        {
            Il2CppCoroutine.Start(routine);
        }

        internal delegate IntPtr d_LayerToName(int layer);

        public override string LayerToName(int layer)
        {
            var iCall = ICallManager.GetICall<d_LayerToName>("UnityEngine.LayerMask::LayerToName");
            return IL2CPP.Il2CppStringToManaged(iCall.Invoke(layer));
        }

        internal delegate IntPtr d_FindObjectsOfTypeAll(IntPtr type);

        public override UnityEngine.Object[] FindObjectsOfTypeAll(Type type)
        {
            var iCall = ICallManager.GetICall<d_FindObjectsOfTypeAll>("UnityEngine.Resources::FindObjectsOfTypeAll");
            var cppType = Il2CppType.From(type);

            return new Il2CppReferenceArray<UnityEngine.Object>(iCall.Invoke(cppType.Pointer));
        }

        public override int GetSceneHandle(Scene scene)
            => scene.handle;

        //Scene.GetRootGameObjects();

        internal delegate void d_GetRootGameObjects(int handle, IntPtr list);

        public override GameObject[] GetRootGameObjects(Scene scene) => GetRootGameObjects(scene.handle);

        public static GameObject[] GetRootGameObjects(int handle)
        {
            if (handle == -1)
                return new GameObject[0];

            int count = GetRootCount(handle);

            if (count < 1)
                return new GameObject[0];

            var list = new Il2CppSystem.Collections.Generic.List<GameObject>(count);

            var iCall = ICallManager.GetICall<d_GetRootGameObjects>("UnityEngine.SceneManagement.Scene::GetRootGameObjectsInternal");

            iCall.Invoke(handle, list.Pointer);

            return list.ToArray();
        }

        //Scene.rootCount;

        internal delegate int d_GetRootCountInternal(int handle);

        public override int GetRootCount(Scene scene) => GetRootCount(scene.handle);

        public static int GetRootCount(int handle)
        {
            return ICallManager.GetICall<d_GetRootCountInternal>("UnityEngine.SceneManagement.Scene::GetRootCountInternal")
                   .Invoke(handle);
        }
    }
}

public static class Il2CppExtensions
{
    public static void AddListener(this UnityEvent action, Action listener)
    {
        action.AddListener(listener);
    }

    public static void AddListener<T>(this UnityEvent<T> action, Action<T> listener)
    {
        action.AddListener(listener);
    }

    public static void SetChildControlHeight(this HorizontalOrVerticalLayoutGroup group, bool value) => group.childControlHeight = value;
    public static void SetChildControlWidth(this HorizontalOrVerticalLayoutGroup group, bool value) => group.childControlWidth = value;
}

#endif