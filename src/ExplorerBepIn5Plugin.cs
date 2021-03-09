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
    public class ExplorerBepInPlugin : BaseUnityPlugin
    {
        public static ExplorerBepInPlugin Instance;

        public static ManualLogSource Logging => Instance?.Logger;

        public static readonly Harmony HarmonyInstance = new Harmony(ExplorerCore.GUID);

        internal void Awake()
        {
            Instance = this;

            new ExplorerCore();

            // HarmonyInstance.PatchAll();
        }

        internal void Update()
        {
            ExplorerCore.Update();
        }
    }
}
#endif
