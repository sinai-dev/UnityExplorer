#if ML
using System;
using System.IO;
using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityExplorer;
using UnityExplorer.Core;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Input;
using UnityExplorer.Loader.ML;
#if ML_LEGACY
using Harmony;
#else
using HarmonyLib;
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.UNIVERSAL)]
#endif

[assembly: MelonInfo(typeof(ExplorerMelonMod), ExplorerCore.NAME, ExplorerCore.VERSION, ExplorerCore.AUTHOR)]
[assembly: MelonGame(null, null)]
[assembly: MelonColor(ConsoleColor.DarkCyan)]

namespace UnityExplorer
{
    public class ExplorerMelonMod : MelonMod, IExplorerLoader
    {
        public static ExplorerMelonMod Instance;

        public string ExplorerFolder => Path.Combine("Mods", ExplorerCore.NAME);

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

        public void SetupCursorPatches()
        {
            try
            {
                PrefixProperty(typeof(Cursor),
                "lockState",
                new HarmonyMethod(typeof(CursorUnlocker).GetMethod(nameof(CursorUnlocker.Prefix_set_lockState))));

                PrefixProperty(typeof(Cursor),
                    "visible",
                    new HarmonyMethod(typeof(CursorUnlocker).GetMethod(nameof(CursorUnlocker.Prefix_set_visible))));

                PrefixProperty(typeof(EventSystem),
                    "current",
                    new HarmonyMethod(typeof(CursorUnlocker).GetMethod(nameof(CursorUnlocker.Prefix_EventSystem_set_current))));
            }
            catch (Exception ex)
            {
                ExplorerCore.Log($"Exception setting up Harmony patches:\r\n{ex.ReflectionExToString()}");
            }
        }

        private void PrefixProperty(Type type, string property, HarmonyMethod prefix)
        {
            try
            {
                var prop = type.GetProperty(property);
#if ML_LEGACY
                this.Harmony.Patch(prop.GetSetMethod(), prefix: prefix);
#else
                HarmonyInstance.Patch(prop.GetSetMethod(), prefix: prefix);
#endif
            }
            catch (Exception e)
            {
                ExplorerCore.Log($"Unable to patch {type.Name}.set_{property}: {e.Message}");
            }
        }
    }
}
#endif