#if BIE6
using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExplorer.UI.Modules;
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

            new ExplorerCore();

            // HarmonyInstance.PatchAll();
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

        // Init
        public override void Load()
        {
            Instance = this;

            ClassInjector.RegisterTypeInIl2Cpp<ExplorerBehaviour>();

            var obj = new GameObject(
                "ExplorerBehaviour",
                new Il2CppSystem.Type[] { Il2CppType.Of<ExplorerBehaviour>() }
            );
            obj.hideFlags = HideFlags.HideAndDontSave;
            GameObject.DontDestroyOnLoad(obj);

            new ExplorerCore();

            // HarmonyInstance.PatchAll();
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
            }
        }
    }
#endif
}
#endif
