#if STANDALONE
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
#if CPP
using UnhollowerRuntimeLib;
#endif

namespace UnityExplorer
{
	public class ExplorerStandalone : IExplorerLoader
    {
        /// <summary>
        /// Call this to initialize UnityExplorer. Optionally, also subscribe to the 'OnLog' event to handle logging.
        /// </summary>
        /// <returns>The new (or active, if one exists) instance of ExplorerStandalone.</returns>
        public static ExplorerStandalone CreateInstance()
        {
            if (Instance != null)
                return Instance;

            return new ExplorerStandalone();
        }

        private ExplorerStandalone()
        {
            Init();
        }

        public static ExplorerStandalone Instance { get; private set; }

        /// <summary>
        /// Invoked whenever Explorer logs something. Subscribe to this to handle logging.
        /// </summary>
        public static event Action<string, LogType> OnLog;

        public Harmony HarmonyInstance => s_harmony;
        public static readonly Harmony s_harmony = new Harmony(ExplorerCore.GUID);

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
#if CPP
            ClassInjector.RegisterTypeInIl2Cpp<ExplorerBehaviour>();

            var obj = new GameObject(
                "ExplorerBehaviour",
                new Il2CppSystem.Type[] { Il2CppType.Of<ExplorerBehaviour>() }
            );
#else
            var obj = new GameObject(
                "ExplorerBehaviour",
                new Type[] { typeof(ExplorerBehaviour) }
            );           
#endif

            obj.hideFlags = HideFlags.HideAndDontSave;
            GameObject.DontDestroyOnLoad(obj);

            new ExplorerCore();
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
    }
}
#endif