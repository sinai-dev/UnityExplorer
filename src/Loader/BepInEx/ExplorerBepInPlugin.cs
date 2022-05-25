#if BIE
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityExplorer.Config;
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
        private static readonly Harmony s_harmony = new(ExplorerCore.GUID);

        public string ExplorerFolderName => ExplorerCore.DEFAULT_EXPLORER_FOLDER_NAME;
        public string ExplorerFolderDestination => Paths.PluginPath;

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