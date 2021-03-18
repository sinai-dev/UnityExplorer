using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityExplorer.Core.Runtime
{
    // Work in progress, this will be used to replace all the "if CPP / if MONO" 
    // pre-processor directives all over the codebase.

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
            Instance = new Il2Cpp.Il2CppProvider();
#else
            Instance = new Mono.MonoProvider();
#endif


        public abstract void Initialize();

        public abstract void SetupEvents();

        // Unity API handlers

        public abstract string LayerToName(int layer);

        public abstract UnityEngine.Object[] FindObjectsOfTypeAll(Type type);

        public abstract int GetSceneHandle(Scene scene);

        public abstract GameObject[] GetRootGameObjects(Scene scene);

        public abstract int GetRootCount(Scene scene);
    }
}
