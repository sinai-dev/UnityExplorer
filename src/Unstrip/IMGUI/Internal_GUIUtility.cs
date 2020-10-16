#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Explorer.Helpers;

namespace Explorer.Unstrip.IMGUI
{
    public class Internal_GUIUtility
    {
        public static Dictionary<int, object> MonoStateCache = new Dictionary<int, object>();

        public static object GetMonoStateObject(Type type, int controlID)
        {
            if (!MonoStateCache.ContainsKey(controlID))
            {
                MonoStateCache.Add(controlID, Activator.CreateInstance(type));
            }

            return MonoStateCache[controlID];
        }

        public static Dictionary<int, Il2CppSystem.Object> StateCache => m_stateCacheDict ?? GetStateCacheDict();
        public static Dictionary<int, Il2CppSystem.Object> m_stateCacheDict;

        public static Il2CppSystem.Object GetStateObject(Il2CppSystem.Type type, int controlID)
        {
            Il2CppSystem.Object obj;
            if (StateCache.ContainsKey(controlID))
            {
                obj = StateCache[controlID];
            }
            else
            {
                obj = Il2CppSystem.Activator.CreateInstance(type);
                StateCache.Add(controlID, obj);
            }

            return obj;
        }

        private static Dictionary<int, Il2CppSystem.Object> GetStateCacheDict()
        {
            if (m_stateCacheDict == null)
            {
                try
                {
                    m_stateCacheDict = ReflectionHelpers.GetTypeByName("UnityEngine.GUIStateObjects")
                                            .GetProperty("s_StateCache")
                                            .GetValue(null, null)
                                            as Dictionary<int, Il2CppSystem.Object>;

                    if (m_stateCacheDict == null) throw new Exception();
                }
                catch
                {
                    m_stateCacheDict = new Dictionary<int, Il2CppSystem.Object>();
                }
            }
            return m_stateCacheDict;
        }
    }
}

#endif