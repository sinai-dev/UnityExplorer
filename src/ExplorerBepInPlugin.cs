#if BIE
using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if CPP
using UnhollowerRuntimeLib;
using BepInEx.IL2CPP;
#endif

namespace UnityExplorer
{
#if MONO
    [BepInPlugin(ExplorerCore.GUID, "UnityExplorer", ExplorerCore.VERSION)]
    public class ExplorerBepInPlugin : BaseUnityPlugin
    {
        public static ExplorerBepInPlugin Instance;

        public static ManualLogSource Logging => Instance?.Logger;

        public static readonly Harmony HarmonyInstance = new Harmony(ExplorerCore.GUID);

        internal void Awake()
        {
            Instance = this;

            SceneManager.activeSceneChanged += DoSceneChange;

            new ExplorerCore();

            HarmonyInstance.PatchAll();
        }

        internal static void DoSceneChange(Scene arg0, Scene arg1)
        {
            ExplorerCore.OnSceneChange();
        }

        internal void Update()
        {
            ExplorerCore.Update();
        }

    }
#endif

#if CPP
    [BepInPlugin(ExplorerCore.GUID, "UnityExplorer", ExplorerCore.VERSION)]
    public class ExplorerBepInPlugin : BasePlugin
    {
        public static ExplorerBepInPlugin Instance;

        public static ManualLogSource Logging => Instance?.Log;

        public static readonly Harmony HarmonyInstance = new Harmony(ExplorerCore.GUID);

        // temporary for Il2Cpp until scene change delegate works
        private static string lastSceneName;

        // Init
        public override void Load()
        {
            Instance = this;

            ClassInjector.RegisterTypeInIl2Cpp<ExplorerBehaviour>();

            var obj = new GameObject(
                "ExplorerBehaviour",
                new Il2CppSystem.Type[]
                {
                        Il2CppType.Of<ExplorerBehaviour>()
                }
            );
            GameObject.DontDestroyOnLoad(obj);

            new ExplorerCore();

            HarmonyInstance.PatchAll();
        }

        internal static void DoSceneChange(Scene arg0, Scene arg1)
        {
            ExplorerCore.OnSceneChange();
        }

        // BepInEx Il2Cpp mod class doesn't have monobehaviour methods yet, so wrap them in a dummy.
        public class ExplorerBehaviour : MonoBehaviour
        {
            public ExplorerBehaviour(IntPtr ptr) : base(ptr) { }

            internal void Awake()
            {
                Logging.LogMessage("ExplorerBehaviour.Awake");
            }

            internal void Update()
            {
                ExplorerCore.Update();

                var scene = SceneManager.GetActiveScene();
                if (scene.name != lastSceneName)
                {
                    lastSceneName = scene.name;
                    DoSceneChange(scene, scene);
                }

            }
        }
    }
#endif
}
#endif
