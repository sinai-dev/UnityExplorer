#if MONO
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityExplorer.Core;

namespace UnityExplorer.Core.Runtime.Mono
{
    public class MonoProvider : RuntimeProvider
    {
        public override void Initialize()
        {
            Reflection = new MonoReflection();
            TextureUtil = new MonoTextureUtil();
        }

        public override void SetupEvents()
        {
            Application.logMessageReceived += ExplorerCore.Instance.OnUnityLog;
            //SceneManager.sceneLoaded += ExplorerCore.Instance.OnSceneLoaded1;
            //SceneManager.activeSceneChanged += ExplorerCore.Instance.OnSceneLoaded2;
        }

        public override string LayerToName(int layer)
            => LayerMask.LayerToName(layer);

        public override UnityEngine.Object[] FindObjectsOfTypeAll(Type type)
            => Resources.FindObjectsOfTypeAll(type);

        private static readonly FieldInfo fi_Scene_handle = typeof(Scene).GetField("m_Handle", ReflectionUtility.CommonFlags);

        public override int GetSceneHandle(Scene scene)
        {
            return (int)fi_Scene_handle.GetValue(scene);
        }

        public override GameObject[] GetRootGameObjects(Scene scene)
        {
            return scene.GetRootGameObjects();
        }

        public override int GetRootCount(Scene scene)
        {
            return scene.rootCount;
        }
    }
}

#endif