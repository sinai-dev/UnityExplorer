#if BIE
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityExplorer.Core;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Input;
using UnityExplorer.Loader.BIE;
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

        public string UnhollowedModulesFolder => Path.Combine(Paths.BepInExRootPath, "unhollowed");

        public ConfigHandler ConfigHandler => _configHandler;
        private BepInExConfigHandler _configHandler;

        public Harmony HarmonyInstance => s_harmony;
        private static readonly Harmony s_harmony = new Harmony(ExplorerCore.GUID);

        public string ExplorerFolder => Path.Combine(Paths.PluginPath, ExplorerCore.NAME);

        public Action<object> OnLogMessage => LogSource.LogMessage;
        public Action<object> OnLogWarning => LogSource.LogWarning;
        public Action<object> OnLogError => LogSource.LogError;

        private void Init()
        {
            Instance = this;
            _configHandler = new BepInExConfigHandler();
            ExplorerCore.Init(this);
        }

#if MONO // Mono
        internal void Awake()
        {
            Init();
        }

#else   // Il2Cpp
        public override void Load()
        {
            Init();
        }
#endif
    }
}
#endif