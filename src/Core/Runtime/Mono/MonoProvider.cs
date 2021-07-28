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
using UnityExplorer;

namespace UnityExplorer.Core.Runtime.Mono
{
    public class MonoProvider : RuntimeProvider
    {
        public override void Initialize()
        {
            ExplorerCore.Context = RuntimeContext.Mono;
            TextureUtil = new MonoTextureUtil();
        }

        public override void SetupEvents()
        {
            Application.logMessageReceived += Application_logMessageReceived;
        }

        private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
             => ExplorerCore.LogUnity(condition, type);

        public override void StartCoroutine(IEnumerator routine) 
            => ExplorerBehaviour.Instance.StartCoroutine(routine);

        public override void Update()
        {
        }

        public override T AddComponent<T>(GameObject obj, Type type) 
            => (T)obj.AddComponent(type);

        public override ScriptableObject CreateScriptable(Type type) 
            => ScriptableObject.CreateInstance(type);

        public override void GraphicRaycast(GraphicRaycaster raycaster, PointerEventData data, List<RaycastResult> list)
            => raycaster.Raycast(data, list);

        public override string LayerToName(int layer) 
            => LayerMask.LayerToName(layer);

        public override UnityEngine.Object[] FindObjectsOfTypeAll(Type type) 
            => Resources.FindObjectsOfTypeAll(type);

        public override GameObject[] GetRootGameObjects(Scene scene) 
            => scene.isLoaded ? scene.GetRootGameObjects() : new GameObject[0];

        public override int GetRootCount(Scene scene) 
            => scene.rootCount;

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
            => selectable.colors = colors;
    }
}

public static class MonoExtensions
{
    // Helpers to use the same style of AddListener that IL2CPP uses.

    public static void AddListener(this UnityEvent _event, Action listener)
        => _event.AddListener(new UnityAction(listener));

    public static void AddListener<T>(this UnityEvent<T> _event, Action<T> listener)
        => _event.AddListener(new UnityAction<T>(listener));

    public static void RemoveListener(this UnityEvent _event, Action listener)
        => _event.RemoveListener(new UnityAction(listener));

    public static void RemoveListener<T>(this UnityEvent<T> _event, Action<T> listener)
        => _event.RemoveListener(new UnityAction<T>(listener));

    // Doesn't exist in NET 3.5

    public static void Clear(this StringBuilder sb) 
        => sb.Remove(0, sb.Length);

    // These properties don't exist in some earlier games, so null check before trying to set them.

    public static void SetChildControlHeight(this HorizontalOrVerticalLayoutGroup group, bool value)
        => ReflectionUtility.GetPropertyInfo(typeof(HorizontalOrVerticalLayoutGroup), "childControlHeight")
            ?.SetValue(group, value, null);


    public static void SetChildControlWidth(this HorizontalOrVerticalLayoutGroup group, bool value)
        => ReflectionUtility.GetPropertyInfo(typeof(HorizontalOrVerticalLayoutGroup), "childControlWidth")
            ?.SetValue(group, value, null);
}

#endif