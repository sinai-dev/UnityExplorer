using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace UnityExplorer
{
    public static class ReflectionPatches
    {
        public static void Init()
        {
            try
            {
                var method = typeof(Assembly).GetMethod(nameof(Assembly.GetTypes), new Type[0]);
                var processor = ExplorerCore.Harmony.CreateProcessor(method);
                processor.AddPrefix(typeof(ReflectionPatches).GetMethod(nameof(ReflectionPatches.Assembly_GetTypes)));
                processor.Patch();
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception setting up Reflection patch: {ex}");
            }
        }

        public static bool Assembly_GetTypes(Assembly __instance, ref Type[] __result)
        {
            __result = __instance.TryGetTypes().ToArray();
            return false;
        }
    }
}
