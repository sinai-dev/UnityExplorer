using UnityExplorer.Config;

namespace UnityExplorer.Runtime
{
    // Not really that necessary anymore, can eventually just be refactored away into the few classes that use this class.

    public abstract class UERuntimeHelper
    {
        public static UERuntimeHelper Instance;

        public static void Init()
        {
#if CPP
            Instance = new Il2CppHelper();
#else
            Instance = new MonoHelper();
#endif
            Instance.SetupEvents();

            LoadBlacklistString(ConfigManager.Reflection_Signature_Blacklist.Value);
            ConfigManager.Reflection_Signature_Blacklist.OnValueChanged += (string val) =>
            {
                LoadBlacklistString(val);
            };
        }

        public abstract void SetupEvents();

        private static readonly HashSet<string> currentBlacklist = new();

        public virtual string[] DefaultReflectionBlacklist => new string[0];

        public static void LoadBlacklistString(string blacklist)
        {
            try
            {
                if (string.IsNullOrEmpty(blacklist) && !Instance.DefaultReflectionBlacklist.Any())
                    return;

                try
                {
                    string[] sigs = blacklist.Split(';');
                    foreach (string sig in sigs)
                    {
                        string s = sig.Trim();
                        if (string.IsNullOrEmpty(s))
                            continue;
                        if (!currentBlacklist.Contains(s))
                            currentBlacklist.Add(s);
                    }
                }
                catch (Exception ex)
                {
                    ExplorerCore.LogWarning($"Exception parsing blacklist string: {ex.ReflectionExToString()}");
                }

                foreach (string sig in Instance.DefaultReflectionBlacklist)
                {
                    if (!currentBlacklist.Contains(sig))
                        currentBlacklist.Add(sig);
                }

                Mono.CSharp.IL2CPP.Blacklist.SignatureBlacklist = currentBlacklist;
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception setting up reflection blacklist: {ex.ReflectionExToString()}");
            }
        }

        public static bool IsBlacklisted(MemberInfo member)
        {
            if (string.IsNullOrEmpty(member.DeclaringType?.Namespace))
                return false;

            string sig = $"{member.DeclaringType.FullName}.{member.Name}";

            return currentBlacklist.Contains(sig);
        }
    }
}
