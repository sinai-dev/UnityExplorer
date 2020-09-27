#if BIE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Mono.CSharp;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine;
#if CPP
using UnhollowerRuntimeLib;
using BepInEx.IL2CPP;
#endif

namespace Explorer
{
    [BepInPlugin(ExplorerCore.GUID, ExplorerCore.NAME, ExplorerCore.VERSION)]
#if CPP
    public class Explorer_BepInPlugin : BasePlugin
#else
    public class Explorer_BepInPlugin : BaseUnityPlugin
#endif
    {
        public static Explorer_BepInPlugin Instance;
        public static ManualLogSource Logging =>
#if CPP
                                        Instance.Log;
#else
                                        Instance?.Logger;
#endif

        public static readonly Harmony HarmonyInstance = new Harmony(ExplorerCore.GUID);

#if CPP
        // temporary for BIE Il2Cpp
        private static bool tempSceneChangeCheck;
        private static string lastSceneName;
#endif

        // Init
#if CPP
        public override void Load()
        {
#else
        internal void Awake()
        {
#endif
            Instance = this;

#if CPP
            tempSceneChangeCheck = true;
            ClassInjector.RegisterTypeInIl2Cpp<DummyMB>();

            GameObject.DontDestroyOnLoad(
                new GameObject(
                    "Explorer_Dummy", 
                    new Il2CppSystem.Type[]
                    {
                        Il2CppType.Of<DummyMB>()
                    })
            );
#else
            SceneManager.activeSceneChanged += DoSceneChange;
#endif

            LoadMCS();

            new ExplorerCore();

            HarmonyInstance.PatchAll();
        }

        void LoadMCS()
        {
#if NET35
            var path = @"BepInEx\plugins\mcs.NET35.dll";
#else
            var path = @"BepInEx\plugins\mcs.dll";
#endif
            Assembly.Load(File.ReadAllBytes(path));
            ExplorerCore.Log("Loaded mcs!");
        }

        internal static void DoSceneChange(Scene arg0, Scene arg1)
        {
            ExplorerCore.OnSceneChange();
        }

        internal static void DoUpdate()
        {
            ExplorerCore.Update();

#if CPP
            if (tempSceneChangeCheck)
            {
                var scene = SceneManager.GetActiveScene();
                if (scene.name != lastSceneName)
                {
                    lastSceneName = scene.name;
                    DoSceneChange(scene, scene);
                }
            }
#endif
        }

        internal static void DoOnGUI()
        {
            ExplorerCore.OnGUI();
        }

#if CPP
        public class DummyMB : MonoBehaviour
        {
            public DummyMB(IntPtr ptr) : base(ptr) { }

            internal void Awake()
            {
                Logging.LogMessage("DummyMB Awake");
            }

#endif
            internal void Update()
            {
                DoUpdate();
            }

            internal void OnGUI()
            {
                DoOnGUI();
            }
#if CPP
        }
#endif
    }
}
#endif
