#if ML
using System;
using System.IO;
using MelonLoader;
using UnityExplorer;
using UnityExplorer.Core.Config;
using UnityExplorer.Loader.ML;

[assembly: MelonInfo(typeof(ExplorerMelonMod), ExplorerCore.NAME, ExplorerCore.VERSION, ExplorerCore.AUTHOR)]
[assembly: MelonGame(null, null)]

namespace UnityExplorer
{
    public class ExplorerMelonMod : MelonMod, IExplorerLoader
    {
        public static ExplorerMelonMod Instance;

        public string ExplorerFolder => Path.Combine("Mods", ExplorerCore.NAME);
        public string ConfigFolder => ExplorerFolder;

        public IConfigHandler ConfigHandler => _configHandler;
        public MelonLoaderConfigHandler _configHandler;

        public Action<object> OnLogMessage => MelonLogger.Msg;
        public Action<object> OnLogWarning => MelonLogger.Warning;
        public Action<object> OnLogError   => MelonLogger.Error;

        public Harmony.HarmonyInstance HarmonyInstance => Instance.Harmony;

        public override void OnApplicationStart()
        {
            Instance = this;
            _configHandler = new MelonLoaderConfigHandler();

            ExplorerCore.Init(this);
        }

        public override void OnUpdate()
        {
            ExplorerCore.Update();
        }
    }
}
#endif