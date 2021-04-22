#if MONO
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExplorer.Core;
using UnityExplorer.Core.CSharp;
using UnityExplorer.Core.Input;

namespace UnityExplorer.Core.Runtime.Mono
{
    public class MonoProvider : RuntimeProvider
    {
        public override void Initialize()
        {
            ExplorerCore.Context = RuntimeContext.Mono;
            Reflection = new MonoReflection();
            TextureUtil = new MonoTextureUtil();

            DummyBehaviour.Setup();
        }

        public override void SetupCameraDelegate()
        {
            Camera.onPostRender += CursorUnlocker.OnCameraPostRender;
        }

        public override void SetupEvents()
        {
            Application.logMessageReceived += Application_logMessageReceived;
        }

        private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
        {
            ExplorerCore.LogUnity(condition, type);
        }

        public override void StartCoroutine(IEnumerator routine)
        {
            DummyBehaviour.Instance.StartCoroutine(routine);
        }

        public override void Update()
        {

        }

        public override T AddComponent<T>(GameObject obj, Type type)
        {
            return (T)obj.AddComponent(type);
        }

        public override ScriptableObject CreateScriptable(Type type)
        {
            return ScriptableObject.CreateInstance(type);
        }

        public override void GraphicRaycast(GraphicRaycaster raycaster, PointerEventData data, List<RaycastResult> list)
        {
            raycaster.Raycast(data, list);
        }

        public override string LayerToName(int layer)
            => LayerMask.LayerToName(layer);

        public override UnityEngine.Object[] FindObjectsOfTypeAll(Type type)
            => Resources.FindObjectsOfTypeAll(type);

        //private static readonly FieldInfo fi_Scene_handle = typeof(Scene).GetField("m_Handle", ReflectionUtility.AllFlags);

        //public override int GetSceneHandle(Scene scene)
        //{
        //    return (int)fi_Scene_handle.GetValue(scene);
        //}

        public override GameObject[] GetRootGameObjects(Scene scene)
        {
            if (!scene.isLoaded)
                return new GameObject[0];

            return scene.GetRootGameObjects();
        }

        public override int GetRootCount(Scene scene)
        {
            return scene.rootCount;
        }

        public override void SetColorBlock(Selectable selectable, Color? normal = null, Color? highlighted = null, Color? pressed = null,
            Color? disabled = null)
        {
            var colors = selectable.colors;

            if (normal != null)
                colors.normalColor = (Color)normal;

            if (highlighted != null)
                colors.highlightedColor = (Color)highlighted;

            if (pressed != null)
                colors.pressedColor = (Color)pressed;

            if (disabled != null)
                colors.disabledColor = (Color)disabled;

            SetColorBlock(selectable, colors);
        }

        public override void SetColorBlock(Selectable selectable, ColorBlock colors)
        {
            selectable.colors = colors;
        }
    }
}

public static class MonoExtensions
{
    public static void AddListener(this UnityEvent _event, Action listener)
    {
        _event.AddListener(new UnityAction(listener));
    }

    public static void AddListener<T>(this UnityEvent<T> _event, Action<T> listener)
    {
        _event.AddListener(new UnityAction<T>(listener));
    }

    public static void RemoveListener(this UnityEvent _event, Action listener)
    {
        _event.RemoveListener(new UnityAction(listener));
    }

    public static void RemoveListener<T>(this UnityEvent<T> _event, Action<T> listener)
    {
        _event.RemoveListener(new UnityAction<T>(listener));
    }

    public static void Clear(this StringBuilder sb)
    {
        sb.Remove(0, sb.Length);
    }

    private static PropertyInfo pi_childControlHeight;

    public static void SetChildControlHeight(this HorizontalOrVerticalLayoutGroup group, bool value)
    {
        if (pi_childControlHeight == null)
            pi_childControlHeight = group.GetType().GetProperty("childControlHeight");
        
        pi_childControlHeight?.SetValue(group, value, null);
    }

    private static PropertyInfo pi_childControlWidth;

    public static void SetChildControlWidth(this HorizontalOrVerticalLayoutGroup group, bool value)
    {
        if (pi_childControlWidth == null)
            pi_childControlWidth = group.GetType().GetProperty("childControlWidth");

        pi_childControlWidth?.SetValue(group, value, null);
    }
}

#endif