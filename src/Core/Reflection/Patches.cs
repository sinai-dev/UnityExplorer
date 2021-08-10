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
                processor.AddFinalizer(typeof(ReflectionPatches).GetMethod(nameof(ReflectionPatches.Assembly_GetTypes)));
                processor.Patch();
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception setting up Reflection patch: {ex}");
            }
        }

        private static readonly Type[] emptyTypes = new Type[0];

        public static Exception Assembly_GetTypes(Assembly __instance, Exception __exception, ref Type[] __result)
        {
            if (__exception != null)
            {
                try
                {
                    __result = __instance.GetExportedTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    try
                    {
                        __result = e.Types.Where(it => it != null).ToArray();
                    }
                    catch
                    {
                        __result = emptyTypes;
                    }
                }
                catch
                {
                    __result = emptyTypes;
                }
            }

            return null;
        }
    }
}
