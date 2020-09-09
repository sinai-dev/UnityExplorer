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
        private CacheObjectBase m_cachedReturnValue;

        public static bool CanEvaluate(MethodInfo mi)
        {
            // TODO generic args
            if (mi.GetGenericArguments().Length > 0)
            {
                return false;
            }

            // primitive and string args supported
            return CanProcessArgs(mi.GetParameters());
        }

        public override void UpdateValue()
        {
            //base.UpdateValue();
        }

        public void Evaluate()
        {
            m_isEvaluating = false;

            var mi = MemInfo as MethodInfo;
            object ret = null;

            if (!HasParameters)
            {
                ret = mi.Invoke(mi.IsStatic ? null : DeclaringInstance, new object[0]);
                m_evaluated = true;
            }
            else
            {
                var parsedArgs = ParseArguments();

                try
                {
                    ret = mi.Invoke(mi.IsStatic ? null : DeclaringInstance, parsedArgs.ToArray());
                    m_evaluated = true;
                }
                catch (Exception e)
                {
                    MelonLogger.Log($"Exception evaluating: {e.GetType()}, {e.Message}");
                }
            }

            if (ret != null)
            {
                m_cachedReturnValue = GetCacheObject(ret);
                m_cachedReturnValue.UpdateValue();
            }
            else
            {
                m_cachedReturnValue = null;
            }
        }

        // ==== GUI DRAW ====

        public override void DrawValue(Rect window, float width)
        {
            if (m_evaluated)
            {
                if (m_cachedReturnValue != null)
                {
                    m_cachedReturnValue.DrawValue(window, width);
                }
                else
                {
                    GUILayout.Label($"null (<color=#2df7b2>{ValueTypeName}</color>)", null);
                }
            }
            else
            {
                GUILayout.Label($"<color=grey><i>Not yet evaluated</i></color> (<color=#2df7b2>{ValueTypeName}</color>)", null);
            }
        }
    }
}
