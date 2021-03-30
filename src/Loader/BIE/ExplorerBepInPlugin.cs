#if BIE
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityExplorer.Core.Config;
using UnityExplorer.Loader.BIE;
using UnityEngine;
#if CPP
using BepInEx.IL2CPP;
using UnhollowerRuntimeLib;
#endif

namespace UnityExplorer
{
    [BepInPlugin(ExplorerCore.GUID, "UnityExplorer", ExplorerCore.VERSION)]

    public class ExplorerBepInPlugin :
#if MONO
        BaseUnityPlugin
#else
        BasePlugin
#endif
        , IExplorerLoader
    {
        public static ExplorerBepInPlugin Instance;

        public ManualLogSource LogSource
#if MONO
            => Logger;
#else
            => Log;
#endif

        public IConfigHandler ConfigHandler => _configHandler;
        private BepInExConfigHandler _configHandler;

        public Harmony HarmonyInstance => s_harmony;
        private static readonly Harmony s_harmony = new Harmony(ExplorerCore.GUID);

        public string ExplorerFolder => Path.Combine(Paths.PluginPath, ExplorerCore.NAME);
        public string ConfigFolder => Path.Combine(Paths.ConfigPath, ExplorerCore.NAME);

        public Action<object> OnLogMessage => LogSource.LogMessage;
        public Action<object> OnLogWarning => LogSource.LogWarning;
        public Action<object> OnLogError   => LogSource.LogError;

        // Init common to Mono and Il2Cpp
        internal void UniversalInit()
        {
            Instance = this;
            _configHandler = new BepInExConfigHandler();
        }

#if MONO // Mono-specific
        internal void Awake()
        {
            UniversalInit();
            ExplorerCore.Init(this);
        }

        internal void Update()
        {
            ExplorerCore.Update();
        }

#else   // Il2Cpp-specific
        public override void Load()
        {
            UniversalInit();

            ClassInjector.RegisterTypeInIl2Cpp<ExplorerBehaviour>();

            var obj = new GameObject(
                "ExplorerBehaviour",
                new Il2CppSystem.Type[] { Il2CppType.Of<ExplorerBehaviour>() }
            );
            obj.hideFlags = HideFlags.HideAndDontSave;
            GameObject.DontDestroyOnLoad(obj);

            ExplorerCore.Init(this);
        }

        // BepInEx Il2Cpp mod class doesn't have monobehaviour methods yet, so wrap them in a dummy.
        public class ExplorerBehaviour : MonoBehaviour
        {
            public ExplorerBehaviour(IntPtr ptr) : base(ptr) { }

            internal void Awake()
            {
                Instance.LogSource.LogMessage("ExplorerBehaviour.Awake");
            }

            internal void Update()
            {
                ExplorerCore.Update();
            }
        }
#endif
    }
}
#endif