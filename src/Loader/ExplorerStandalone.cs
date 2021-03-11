#if STANDALONE
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;

namespace UnityExplorer
{
	public class ExplorerStandalone : IExplorerLoader
    {
        public static ExplorerStandalone CreateInstance()
        {
            if (Instance != null)
                return Instance;

            return new ExplorerStandalone();
        }

        private ExplorerStandalone()
        {
            Instance = this;
            new ExplorerCore();
        }

        public static ExplorerStandalone Instance { get; private set; }

        /// <summary>
        /// Invoked whenever Explorer logs something. Subscribe to this to handle logging.
        /// </summary>
        public static event Action<string, UnityEngine.LogType> OnLog;

        public Harmony HarmonyInstance => s_harmony;
        public static readonly Harmony s_harmony = new Harmony(ExplorerCore.GUID);

        public string ExplorerFolder
        {
            get
            {
                if (s_explorerFolder == null)
                {
                    s_explorerFolder = new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
                    s_explorerFolder = Uri.UnescapeDataString(s_explorerFolder);
                    s_explorerFolder = Path.GetDirectoryName(s_explorerFolder);
                }

                return s_explorerFolder;
            }
        }
        private static string s_explorerFolder;
        
        public string ConfigFolder => ExplorerFolder;

        Action<object> IExplorerLoader.OnLogMessage => (object log) => { OnLog?.Invoke(log?.ToString() ?? "", UnityEngine.LogType.Log); };
        Action<object> IExplorerLoader.OnLogWarning => (object log) => { OnLog?.Invoke(log?.ToString() ?? "", UnityEngine.LogType.Warning); };
        Action<object> IExplorerLoader.OnLogError   => (object log) => { OnLog?.Invoke(log?.ToString() ?? "", UnityEngine.LogType.Error); };

        /// <summary>
        /// Call this once per frame for Explorer to update.
        /// </summary>
        public static void Update()
        {
            ExplorerCore.Update();
        }
    }
}
#endif