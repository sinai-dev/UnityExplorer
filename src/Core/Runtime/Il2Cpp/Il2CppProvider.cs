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
            ExplorerCore.Context = RuntimeContext.IL2CPP;
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
            ExplorerCore.LogUnity(condition, type);
        }

        public override void StartCoroutine(IEnumerator routine)
        {
            Il2CppCoroutine.Start(routine);
        }

        internal override void ProcessOnPostRender()
        {
            Il2CppCoroutine.ProcessWaitForEndOfFrame();
        }

        public override void Update()
        {
            Il2CppCoroutine.Process();
        }

        public override T AddComponent<T>(GameObject obj, Type type)
        {
            return obj.AddComponent(Il2CppType.From(type)).TryCast<T>();
        }

        public override ScriptableObject CreateScriptable(Type type)
        {
            return ScriptableObject.CreateInstance(Il2CppType.From(type));
        }

        public override void GraphicRaycast(GraphicRaycaster raycaster, PointerEventData data, List<RaycastResult> list)
        {
            var il2cppList = new Il2CppSystem.Collections.Generic.List<RaycastResult>();

            raycaster.Raycast(data, il2cppList);

            if (il2cppList.Count > 0)
                list.AddRange(il2cppList.ToArray());
        }

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

        // Scene.GetRootGameObjects();

        internal delegate void d_GetRootGameObjects(int handle, IntPtr list);

        public override GameObject[] GetRootGameObjects(Scene scene)
        {
            if (!scene.isLoaded)
                return new GameObject[0];

            int handle = scene.handle;

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

        internal static bool triedToGetColorBlockProps;
        internal static PropertyInfo _normalColorProp;
        internal static PropertyInfo _highlightColorProp;
        internal static PropertyInfo _pressedColorProp;
        internal static PropertyInfo _disabledColorProp;

        public override void SetColorBlock(Selectable selectable, Color? normal = null, Color? highlighted = null, Color? pressed = null, 
            Color? disabled = null)
        {
            var colors = selectable.colors;

            colors.colorMultiplier = 1;

            object boxed = (object)colors;

            if (!triedToGetColorBlockProps)
            {
                triedToGetColorBlockProps = true;

                if (ReflectionUtility.GetPropertyInfo(typeof(ColorBlock), "normalColor") is PropertyInfo norm && norm.CanWrite)
                    _normalColorProp = norm;
                if (ReflectionUtility.GetPropertyInfo(typeof(ColorBlock), "highlightedColor") is PropertyInfo high && high.CanWrite)
                    _highlightColorProp = high;
                if (ReflectionUtility.GetPropertyInfo(typeof(ColorBlock), "pressedColor") is PropertyInfo pres && pres.CanWrite)
                    _pressedColorProp = pres;
                if (ReflectionUtility.GetPropertyInfo(typeof(ColorBlock), "disabledColor") is PropertyInfo disa && disa.CanWrite)
                    _disabledColorProp = disa;
            }

            try
            {
                if (normal != null)
                {
                    if (_normalColorProp != null)
                        _normalColorProp.SetValue(boxed, (Color)normal);
                    else if (ReflectionUtility.GetFieldInfo(typeof(ColorBlock), "m_NormalColor") is FieldInfo fi)
                        fi.SetValue(boxed, (Color)normal);
                }

                if (highlighted != null)
                {
                    if (_highlightColorProp != null)
                        _highlightColorProp.SetValue(boxed, (Color)highlighted);
                    else if (ReflectionUtility.GetFieldInfo(typeof(ColorBlock), "m_HighlightedColor") is FieldInfo fi)
                        fi.SetValue(boxed, (Color)highlighted);
                }

                if (pressed != null)
                {
                    if (_pressedColorProp != null)
                        _pressedColorProp.SetValue(boxed, (Color)pressed);
                    else if (ReflectionUtility.GetFieldInfo(typeof(ColorBlock), "m_PressedColor") is FieldInfo fi)
                        fi.SetValue(boxed, (Color)pressed);
                }

                if (disabled != null)
                {
                    if (_disabledColorProp != null)
                        _disabledColorProp.SetValue(boxed, (Color)disabled);
                    else if (ReflectionUtility.GetFieldInfo(typeof(ColorBlock), "m_DisabledColor") is FieldInfo fi)
                        fi.SetValue(boxed, (Color)disabled);
                }
            }
            catch (Exception ex)
            {
                ExplorerCore.Log(ex);
            }

            colors = (ColorBlock)boxed;

            SetColorBlock(selectable, colors);
        }

        public override void SetColorBlock(Selectable selectable, ColorBlock _colorBlock)
        {
            try
            {
                selectable = selectable.TryCast<Selectable>();

                ReflectionUtility.GetPropertyInfo(typeof(Selectable), "m_Colors")
                    .SetValue(selectable, _colorBlock, null);

                ReflectionUtility.GetMethodInfo(typeof(Selectable), "OnSetProperty", new Type[0])
                    .Invoke(selectable, new object[0]);
            }
            catch (Exception ex)
            {
                ExplorerCore.Log(ex);
            }
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

    public static void RemoveListener(this UnityEvent action, Action listener)
    {
        action.RemoveListener(listener);
    }

    public static void RemoveListener<T>(this UnityEvent<T> action, Action<T> listener)
    {
        action.RemoveListener(listener);
    }

    public static void SetChildControlHeight(this HorizontalOrVerticalLayoutGroup group, bool value) => group.childControlHeight = value;
    public static void SetChildControlWidth(this HorizontalOrVerticalLayoutGroup group, bool value) => group.childControlWidth = value;
}

#endif