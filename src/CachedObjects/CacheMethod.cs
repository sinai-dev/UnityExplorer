using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using MelonLoader;

namespace Explorer
{
    public class CacheMethod : CacheObjectBase
    {
        private bool m_evaluated = false;
        private CacheObjectBase m_cachedReturnValue;

        public static bool CanEvaluate(MethodInfo mi)
        {
            if (mi.GetParameters().Length > 0 || mi.GetGenericArguments().Length > 0)
            {
                // Currently methods with arguments are not supported (no good way to input them).
                return false;
            }

            return true;
        }

        public override void Init()
        {
            base.Init();
        }

        public override void UpdateValue()
        {
            base.UpdateValue();
        }

        private void Evaluate()
        {
            m_evaluated = true;

            var mi = MemberInfo as MethodInfo;
            var ret = mi.Invoke(mi.IsStatic ? null : DeclaringInstance, new object[0]);

            if (ret != null)
            {
                m_cachedReturnValue = GetCacheObject(ret);
                if (m_cachedReturnValue is CacheList cacheList)
                {
                    cacheList.WhiteSpace = 0f;
                    cacheList.ButtonWidthOffset += 70f;
                }
                m_cachedReturnValue.UpdateValue();
            }
            else
            {
                m_cachedReturnValue = null;
            }
        }

        public override void DrawValue(Rect window, float width)
        {
            GUILayout.BeginVertical(null);

            GUILayout.BeginHorizontal(null);
            if (GUILayout.Button("Evaluate", new GUILayoutOption[] { GUILayout.Width(70) }))
            {
                Evaluate();
            }
            GUI.skin.label.wordWrap = false;
            GUILayout.Label($"<color=yellow>{ValueType}</color>", null);
            GUI.skin.label.wordWrap = true;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(null);
            if (m_evaluated)
            {
                if (m_cachedReturnValue != null)
                {
                    try
                    {
                        m_cachedReturnValue.DrawValue(window, width);
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Log("Exception drawing m_cachedReturnValue!");
                        MelonLogger.Log(e.ToString());
                    }
                }
                else
                {
                    GUILayout.Label($"null", null);
                }
            }
            else
            {
                GUILayout.Label($"<color=grey><i>Not yet evaluated</i></color>", null);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }
}
