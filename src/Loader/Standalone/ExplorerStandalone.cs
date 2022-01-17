#if STANDALONE
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityExplorer.Config;
using UnityExplorer.Loader.STANDALONE;
using UnityEngine.EventSystems;
using UniverseLib.Input;
using UnityExplorer.Core;
#if CPP
using UnhollowerRuntimeLib;
#endif

namespace UnityExplorer
{
	public class ExplorerStandalone : IExplorerLoader
    {
        /// <summary>
        /// Call this to initialize UnityExplorer without adding a log listener or Unhollowed modules path.
        /// The default Unhollowed path "UnityExplorer\Modules\" will be used.
        /// </summary>
        /// <returns>The new (or active, if one exists) instance of ExplorerStandalone.</returns>
        public static ExplorerStandalone CreateInstance() => CreateInstance(null, null);

        /// <summary>
        /// Call this to initialize UnityExplorer and add a listener for UnityExplorer's log messages, without specifying an Unhollowed modules path.
        /// The default Unhollowed path "UnityExplorer\Modules\" will be used.
        /// </summary>
        /// <param name="logListener">Your log listener to handle UnityExplorer logs.</param>
        /// <returns>The new (or active, if one exists) instance of ExplorerStandalone.</returns>
        public static ExplorerStandalone CreateInstance(Action<string, LogType> logListener) => CreateInstance(logListener, null);

        /// <summary>
        /// Call this to initialize UnityExplorer with the provided log listener and Unhollowed modules path.
        /// </summary>
        /// <param name="logListener">Your log listener to handle UnityExplorer logs.</param>
        /// <param name="unhollowedModulesPath">The path of the Unhollowed modules, either relative or absolute.</param>
        /// <returns>The new (or active, if one exists) instance of ExplorerStandalone.</returns>
        public static ExplorerStandalone CreateInstance(Action<string, LogType> logListener, string unhollowedModulesPath)
        {
            if (Instance != null)
                return Instance;

            var instance = new ExplorerStandalone();
            instance.Init();
            instance.CheckExplorerFolder();

            if (logListener != null)
                OnLog += logListener;

            if (string.IsNullOrEmpty(unhollowedModulesPath) || !Directory.Exists(unhollowedModulesPath))
                instance._unhollowedPath = Path.Combine(instance.ExplorerFolder, "Modules");
            else
                instance._unhollowedPath = unhollowedModulesPath;

            return instance;
        }

        public static ExplorerStandalone Instance { get; private set; }

        /// <summary>
        /// Invoked whenever Explorer logs something. Subscribe to this to handle logging.
        /// </summary>
        public static event Action<string, LogType> OnLog;

        public string UnhollowedModulesFolder => _unhollowedPath;
        private string _unhollowedPath;

        public ConfigHandler ConfigHandler => _configHandler;
        private StandaloneConfigHandler _configHandler;

        public string ExplorerFolder
        {
            get
            {
                CheckExplorerFolder();
                return s_explorerFolder;
            }
        }
        private static string s_explorerFolder;
        
        Action<object> IExplorerLoader.OnLogMessage => (object log) => { OnLog?.Invoke(log?.ToString() ?? "", LogType.Log); };
        Action<object> IExplorerLoader.OnLogWarning => (object log) => { OnLog?.Invoke(log?.ToString() ?? "", LogType.Warning); };
        Action<object> IExplorerLoader.OnLogError   => (object log) => { OnLog?.Invoke(log?.ToString() ?? "", LogType.Error); };

        private void Init()
        {
            Instance = this;
            _configHandler = new StandaloneConfigHandler();

            ExplorerCore.Init(this);
        }

        private void CheckExplorerFolder()
        {
            if (s_explorerFolder == null)
            {
                s_explorerFolder =
                    Path.Combine(
                        Path.GetDirectoryName(
                            Uri.UnescapeDataString(new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath)),
                        "UnityExplorer");

                if (!Directory.Exists(s_explorerFolder))
                    Directory.CreateDirectory(s_explorerFolder);
            }
        }
    }
}
#endif