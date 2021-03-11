#if BIE5
using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace UnityExplorer
{
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
        }

        internal void Update()
        {
            ExplorerCore.Update();
        }
    }
}
#endif
