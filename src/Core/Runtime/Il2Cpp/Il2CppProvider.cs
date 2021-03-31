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
using UnityExplorer.Core.Input;
using UnityEngine.EventSystems;

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
                    castMethod.Invoke(null, new[] { new Action<string, string, LogType>(Application_logMessageReceived) })
                });
            }
            catch 
            {
                ExplorerCore.LogWarning("Exception setting up Unity log listener, make sure Unity libraries have been unstripped!");
            }
        }

        private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
        {
            ExplorerCore.Log(condition, type, true);
        }

        public override void StartConsoleCoroutine(IEnumerator routine)
        {
            Il2CppCoroutine.Start(routine);
        }

        // Unity API Handlers

        // LayerMask.LayerToName

        internal delegate IntPtr d_LayerToName(int layer);

        public override string LayerToName(int layer)
        {
            var iCall = ICallManager.GetICall<d_LayerToName>("UnityEngine.LayerMask::LayerToName");
            return IL2CPP.Il2CppStringToManaged(iCall.Invoke(layer));
        }

        // Resources.FindObjectsOfTypeAll

        internal delegate IntPtr d_FindObjectsOfTypeAll(IntPtr type);

        public override UnityEngine.Object[] FindObjectsOfTypeAll(Type type)
        {
            var iCall = ICallManager.GetICallUnreliable<d_FindObjectsOfTypeAll>(new[]
            {
                "UnityEngine.Resources::FindObjectsOfTypeAll",
                "UnityEngine.ResourcesAPIInternal::FindObjectsOfTypeAll" // Unity 2020+ updated to this
            });

            return new Il2CppReferenceArray<UnityEngine.Object>(iCall.Invoke(Il2CppType.From(type).Pointer));
        }

        public override int GetSceneHandle(Scene scene)
            => scene.handle;

        // Scene.GetRootGameObjects();

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

        // Scene.rootCount

        internal delegate int d_GetRootCountInternal(int handle);

        public override int GetRootCount(Scene scene) => GetRootCount(scene.handle);

        public static int GetRootCount(int handle)
        {
            return ICallManager.GetICall<d_GetRootCountInternal>("UnityEngine.SceneManagement.Scene::GetRootCountInternal")
                   .Invoke(handle);
        }

        internal static bool? s_doPropertiesExist;

        public override ColorBlock SetColorBlock(ColorBlock colors, Color? normal = null, Color? highlighted = null, Color? pressed = null)
        {
            if (s_doPropertiesExist == null)
            {
                var prop = ReflectionUtility.GetPropertyInfo(typeof(ColorBlock), "normalColor") as PropertyInfo;
                s_doPropertiesExist = prop != null && prop.CanWrite;
            }

            colors.colorMultiplier = 1;

            object boxed = (object)colors;

            if (s_doPropertiesExist == true)
            {
                if (normal != null)
                    ReflectionUtility.GetPropertyInfo(typeof(ColorBlock), "normalColor").SetValue(boxed, (Color)normal);
                if (pressed != null)
                    ReflectionUtility.GetPropertyInfo(typeof(ColorBlock), "pressedColor").SetValue(boxed, (Color)pressed);
                if (highlighted != null)
                    ReflectionUtility.GetPropertyInfo(typeof(ColorBlock), "highlightedColor").SetValue(boxed, (Color)highlighted);
            }
            else if (s_doPropertiesExist == false)
            {
                if (normal != null)
                    ReflectionUtility.GetFieldInfo(typeof(ColorBlock), "m_NormalColor").SetValue(boxed, (Color)normal);
                if (pressed != null)
                    ReflectionUtility.GetFieldInfo(typeof(ColorBlock), "m_PressedColor").SetValue(boxed, (Color)pressed);
                if (highlighted != null)
                    ReflectionUtility.GetFieldInfo(typeof(ColorBlock), "m_HighlightedColor").SetValue(boxed, (Color)highlighted);
            }

            colors = (ColorBlock)boxed;

            return colors;
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