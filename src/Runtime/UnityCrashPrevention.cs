using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExplorer.CacheObject;

namespace UnityExplorer.Runtime
{
    internal static class UnityCrashPrevention
    {
        internal static void Init()
        {
            try
            {
                ExplorerCore.Harmony.PatchAll(typeof(UnityCrashPrevention));
                ExplorerCore.Log("Initialized UnityCrashPrevention.");
            }
            catch //(Exception ex)
            {
                //ExplorerCore.Log($"Exception setting up Canvas crash prevention patch: {ex}");
            }
        }

        // In Unity 2020 they introduced "Canvas.renderingDisplaySize".
        // If you try to get the value on a Canvas which has a renderMode value of WorldSpace and no worldCamera set,
        // the game will Crash when Unity tries to read from a null ptr (I think).
        [HarmonyPatch(typeof(Canvas), "renderingDisplaySize", MethodType.Getter)]
        [HarmonyPrefix]
        internal static void Prefix(Canvas __instance)
        {
            if (__instance.renderMode == RenderMode.WorldSpace && !__instance.worldCamera)
                throw new InvalidOperationException(
                    "Canvas is set to RenderMode.WorldSpace but not worldCamera is set, cannot get renderingDisplaySize.");
        }
    }
}
