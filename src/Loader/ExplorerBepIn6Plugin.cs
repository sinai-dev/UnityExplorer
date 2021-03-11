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
    public class ExplorerBepInPlugin : BaseUnityPlugin, IExplorerLoader
    {
        public static ExplorerBepInPlugin Instance;

        public static ManualLogSource Logging => Instance?.Logger;

        public Harmony HarmonyInstance => s_harmony;
        private static readonly Harmony s_harmony = new Harmony(ExplorerCore.GUID);
        
        public string ExplorerFolder => Path.Combine(Paths.PluginPath, ExplorerCore.NAME);
        public string ConfigFolder => Path.Combine(Paths.ConfigPath, ExplorerCore.NAME);

        public Action<object> OnLogMessage => (object log) => { Logging?.LogMessage(log?.ToString() ?? ""); };
        public Action<object> OnLogWarning => (object log) => { Logging?.LogWarning(log?.ToString() ?? ""); };
        public Action<object> OnLogError   => (object log) => { Logging?.LogError(log?.ToString() ?? ""); };

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
    public class ExplorerBepInPlugin : BasePlugin, IExplorerLoader
    {
        public static ExplorerBepInPlugin Instance;

        public static ManualLogSource Logging => Instance?.Log;

        public Harmony HarmonyInstance => s_harmony;
        private static readonly Harmony s_harmony = new Harmony(ExplorerCore.GUID);

        public string ExplorerFolder => Path.Combine(Paths.PluginPath, ExplorerCore.NAME);
        public string ConfigFolder => Path.Combine(Paths.ConfigPath, ExplorerCore.NAME);

        public Action<object> OnLogMessage => (object log) => { Logging?.LogMessage(log?.ToString() ?? ""); };
        public Action<object> OnLogWarning => (object log) => { Logging?.LogWarning(log?.ToString() ?? ""); };
        public Action<object> OnLogError   => (object log) => { Logging?.LogError(log?.ToString() ?? ""); };

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
