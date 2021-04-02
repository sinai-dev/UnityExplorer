#if STANDALONE
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityExplorer.Core.Config;
using UnityExplorer.Loader.STANDALONE;
using UnityEngine.EventSystems;
using UnityExplorer.Core.Input;
using UnityExplorer.Core;
#if CPP
using UnhollowerRuntimeLib;
#endif

namespace UnityExplorer
{
	public class ExplorerStandalone : IExplorerLoader
    {
        /// <summary>
        /// Call this to initialize UnityExplorer without adding a log listener.
        /// </summary>
        /// <returns>The new (or active, if one exists) instance of ExplorerStandalone.</returns>
        public static ExplorerStandalone CreateInstance()
            => CreateInstance(null);

        /// <summary>
        /// Call this to initialize UnityExplorer and add a listener for UnityExplorer's log messages.
        /// </summary>
        /// <param name="logListener">Your log listener to handle UnityExplorer logs.</param>
        /// <returns>The new (or active, if one exists) instance of ExplorerStandalone.</returns>
        public static ExplorerStandalone CreateInstance(Action<string, LogType> logListener)
        {
            if (Instance != null)
                return Instance;

            OnLog += logListener;

            var instance = new ExplorerStandalone();
            instance.Init();
            return instance;
        }

        public static ExplorerStandalone Instance { get; private set; }

        /// <summary>
        /// Invoked whenever Explorer logs something. Subscribe to this to handle logging.
        /// </summary>
        public static event Action<string, LogType> OnLog;

        public Harmony HarmonyInstance => s_harmony;
        public static readonly Harmony s_harmony = new Harmony(ExplorerCore.GUID);

        public ConfigHandler ConfigHandler => _configHandler;
        private StandaloneConfigHandler _configHandler;

        public string ExplorerFolder
        {
            get
            {
                if (s_explorerFolder == null)
                {
                    s_explorerFolder = 
                        Path.Combine(
                            Path.GetDirectoryName(
                                Uri.UnescapeDataString(new Uri(Assembly.GetExecutingAssembly().CodeBase)
                                .AbsolutePath)), 
                        "UnityExplorer");

                    if (!Directory.Exists(s_explorerFolder))
                        Directory.CreateDirectory(s_explorerFolder);
                }

                return s_explorerFolder;
            }
        }
        private static string s_explorerFolder;
        
        public string ConfigFolder => ExplorerFolder;

        Action<object> IExplorerLoader.OnLogMessage => (object log) => { OnLog?.Invoke(log?.ToString() ?? "", LogType.Log); };
        Action<object> IExplorerLoader.OnLogWarning => (object log) => { OnLog?.Invoke(log?.ToString() ?? "", LogType.Warning); };
        Action<object> IExplorerLoader.OnLogError   => (object log) => { OnLog?.Invoke(log?.ToString() ?? "", LogType.Error); };

        private void Init()
        {
            Instance = this;
            _configHandler = new StandaloneConfigHandler();

#if CPP
            ClassInjector.RegisterTypeInIl2Cpp<ExplorerBehaviour>();
#endif
            var obj = new GameObject("ExplorerBehaviour");
            obj.AddComponent<ExplorerBehaviour>();

            GameObject.DontDestroyOnLoad(obj);
            obj.hideFlags = HideFlags.HideAndDontSave;

            ExplorerCore.Init(this);
        }

        public class ExplorerBehaviour : MonoBehaviour
        {
#if CPP
            public ExplorerBehaviour(IntPtr ptr) : base(ptr) { }
#endif
            internal void Update()
            {
                ExplorerCore.Update();
            }
        }

        public void SetupPatches()
        {
            try
            {
                this.HarmonyInstance.PatchAll();
            }
            catch (Exception ex)
            {
                ExplorerCore.Log($"Exception setting up Harmony patches:\r\n{ex.ReflectionExToString()}");
            }
        }

        [HarmonyPatch(typeof(EventSystem), "current", MethodType.Setter)]
        public class PATCH_EventSystem_current
        {
            [HarmonyPrefix]
            public static void Prefix_EventSystem_set_current(ref EventSystem value)
            {
                CursorUnlocker.Prefix_EventSystem_set_current(ref value);
            }
        }

        [HarmonyPatch(typeof(Cursor), "lockState", MethodType.Setter)]
        public class PATCH_Cursor_lockState
        {
            [HarmonyPrefix]
            public static void Prefix_set_lockState(ref CursorLockMode value)
            {
                CursorUnlocker.Prefix_set_lockState(ref value);
            }
        }

        [HarmonyPatch(typeof(Cursor), "visible", MethodType.Setter)]
        public class PATCH_Cursor_visible
        {
            [HarmonyPrefix]
            public static void Prefix_set_visible(ref bool value)
            {
                CursorUnlocker.Prefix_set_visible(ref value);
            }
        }
    }
}
#endif