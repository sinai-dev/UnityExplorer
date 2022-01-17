using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExplorer.Config;
using UniverseLib;

namespace UnityExplorer.Runtime
{
    public abstract class RuntimeHelper
    {
        public static RuntimeHelper Instance;

        public static void Init()
        { 
#if CPP
            Instance = new Il2CppProvider();
#else
            Instance = new MonoProvider();
#endif
            Instance.SetupEvents();

            LoadBlacklistString(ConfigManager.Reflection_Signature_Blacklist.Value);
            ConfigManager.Reflection_Signature_Blacklist.OnValueChanged += (string val) =>
            {
                LoadBlacklistString(val);
            };
        }

        public abstract void SetupEvents();

        #region Reflection Blacklist

        private static readonly HashSet<string> currentBlacklist = new HashSet<string>();

        public virtual string[] DefaultReflectionBlacklist => new string[0];

        public static void LoadBlacklistString(string blacklist)
        {
            try
            {
                if (string.IsNullOrEmpty(blacklist) && !Instance.DefaultReflectionBlacklist.Any())
                    return;

                try
                {
                    var sigs = blacklist.Split(';');
                    foreach (var sig in sigs)
                    {
                        var s = sig.Trim();
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

                foreach (var sig in Instance.DefaultReflectionBlacklist)
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

            var sig = $"{member.DeclaringType.FullName}.{member.Name}";

            return currentBlacklist.Contains(sig);
        }

        #endregion
    }
}
