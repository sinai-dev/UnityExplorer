using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExplorer.Core.Runtime;

// Intentionally project-wide namespace so that its always easily accessible.
namespace UnityExplorer
{
    public abstract class RuntimeProvider
    {
        public static RuntimeProvider Instance;

        public ReflectionProvider Reflection;
        public TextureUtilProvider TextureUtil;

        public RuntimeProvider()
        {
            Initialize();

            SetupEvents();
        }

        public static void Init() =>
#if CPP
            Instance = new Core.Runtime.Il2Cpp.Il2CppProvider();
#else
            Instance = new Core.Runtime.Mono.MonoProvider();
#endif

        public abstract void Initialize();

        public abstract void SetupEvents();

        public abstract void StartCoroutine(IEnumerator routine);

        public abstract void Update();

        //public virtual bool IsReferenceEqual(object a, object b) => ReferenceEquals(a, b);

        // Unity API handlers

        public abstract T AddComponent<T>(GameObject obj, Type type) where T : Component;

        public abstract ScriptableObject CreateScriptable(Type type);

        public abstract string LayerToName(int layer);

        public abstract UnityEngine.Object[] FindObjectsOfTypeAll(Type type);

        public abstract void GraphicRaycast(GraphicRaycaster raycaster, PointerEventData data, List<RaycastResult> list);

        //public abstract int GetSceneHandle(Scene scene);

        public abstract GameObject[] GetRootGameObjects(Scene scene);

        public abstract int GetRootCount(Scene scene);

        public abstract void SetColorBlock(Selectable selectable, ColorBlock colors);

        public abstract void SetColorBlock(Selectable selectable, Color? normal = null, Color? highlighted = null, Color? pressed = null,
            Color? disabled = null);
    }
}
