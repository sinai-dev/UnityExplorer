#if ML
using System;
using System.IO;
using MelonLoader;
using UnityExplorer;
using UnityExplorer.Core.Config;
using UnityExplorer.Loader.ML;

[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.UNIVERSAL)]
[assembly: MelonInfo(typeof(ExplorerMelonMod), ExplorerCore.NAME, ExplorerCore.VERSION, ExplorerCore.AUTHOR)]
[assembly: MelonGame(null, null)]
[assembly: MelonColor(ConsoleColor.DarkCyan)]

namespace UnityExplorer
{
    public class ExplorerMelonMod : MelonMod, IExplorerLoader
    {
        public static ExplorerMelonMod Instance;

        public string ExplorerFolder => Path.Combine(MelonHandler.ModsDirectory, ExplorerCore.NAME);

        public string UnhollowedModulesFolder => Path.Combine(
            Path.GetDirectoryName(MelonHandler.ModsDirectory),
            Path.Combine("MelonLoader", "Managed"));

        public ConfigHandler ConfigHandler => _configHandler;
        public MelonLoaderConfigHandler _configHandler;

        public Action<object> OnLogMessage => MelonLogger.Msg;
        public Action<object> OnLogWarning => MelonLogger.Warning;
        public Action<object> OnLogError   => MelonLogger.Error;

        public override void OnApplicationStart()
        {
            Instance = this;
            _configHandler = new MelonLoaderConfigHandler();

            ExplorerCore.Init(this);
        }
    }
}
#endif