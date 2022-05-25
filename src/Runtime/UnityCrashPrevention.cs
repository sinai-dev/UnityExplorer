namespace UnityExplorer.Runtime
{
    internal static class UnityCrashPrevention
    {
        static readonly HarmonyLib.Harmony harmony = new($"{ExplorerCore.GUID}.crashprevention");

        internal static void Init()
        {
            TryPatch<Canvas>("get_renderingDisplaySize", nameof(Canvas_renderingDisplaySize_Prefix));

            IEnumerable<MethodBase> patched = harmony.GetPatchedMethods();
            if (patched.Any())
                ExplorerCore.Log(
                    $"Initialized UnityCrashPrevention for: {string.Join(", ", patched.Select(it => $"{it.DeclaringType.Name}.{it.Name}").ToArray())}");
        }

        internal static void TryPatch<T>(string orig, string prefix, Type[] argTypes = null)
        {
            try
            {
                harmony.Patch(
                    HarmonyLib.AccessTools.Method(typeof(T), orig, argTypes),
                    new HarmonyLib.HarmonyMethod(HarmonyLib.AccessTools.Method(typeof(UnityCrashPrevention), prefix)));
            }
            catch //(Exception ex)
            {
                //ExplorerCore.Log($"Exception patching {typeof(T).Name}.{orig}: {ex}");
            }
        }

        // In Unity 2020 they introduced "Canvas.renderingDisplaySize".
        // If you try to get the value on a Canvas which has a renderMode value of WorldSpace and no worldCamera set,
        // the game will Crash (I think from Unity trying to read from null ptr).
        internal static void Canvas_renderingDisplaySize_Prefix(Canvas __instance)
        {
            if (__instance.renderMode == RenderMode.WorldSpace && !__instance.worldCamera)
                throw new InvalidOperationException("Canvas is set to RenderMode.WorldSpace but not worldCamera is set.");
        }
    }
}
