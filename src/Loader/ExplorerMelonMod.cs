#if ML
using System;
using System.IO;
using MelonLoader;

namespace UnityExplorer
{
    public class ExplorerMelonMod : MelonMod, IExplorerLoader
    {
        public static ExplorerMelonMod Instance;

        public string ExplorerFolder => Path.Combine("Mods", ExplorerCore.NAME);
        public string ConfigFolder => ExplorerFolder;

        public Action<object> OnLogMessage => (object log) => { MelonLogger.Msg(log?.ToString() ?? ""); };
        public Action<object> OnLogWarning => (object log) => { MelonLogger.Warning(log?.ToString() ?? ""); };
        public Action<object> OnLogError   => (object log) => { MelonLogger.Error(log?.ToString() ?? ""); };

        public Harmony.HarmonyInstance HarmonyInstance => Instance.Harmony;

        public override void OnApplicationStart()
        {
            Instance = this;

            new ExplorerCore();
        }

        public override void OnUpdate()
        {
            ExplorerCore.Update();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            ExplorerCore.Instance.OnSceneLoaded();
        }
    }
}
#endif