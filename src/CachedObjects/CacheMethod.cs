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
    // TODO implement methods with primitive arguments

    public class CacheMethod : CacheObjectBase
    {
        private bool m_evaluated = false;
        private CacheObjectBase m_cachedReturnValue;

        public bool HasParameters
        {
            get
            {
                if (m_hasParams == null)
                {
                    m_hasParams = (MemberInfo as MethodInfo).GetParameters().Length > 0;
                }
                return (bool)m_hasParams;
            }
        }
        private bool? m_hasParams;

        // ======= TODO =======
        private bool m_isEvaluating;
        private string[] m_argumentNames;
        private Type[] m_argumentTypes;
        private string[] m_argumentInput;
        // =====================

        public static bool CanEvaluate(MethodInfo mi)
        {
            // generic type args not supported yet
            if (mi.GetGenericArguments().Length > 0)
            {
                return false;
            }

            // TODO primitive params (commented out impl below)
            if (mi.GetParameters().Length > 0)
            {
                return false;
            }

            //// only primitive and string args supported
            //foreach (var param in mi.GetParameters())
            //{
            //    if (!param.ParameterType.IsPrimitive && param.ParameterType != typeof(string))
            //    {
            //        return false;
            //    }
            //}

            return true;
        }

        public override void Init()
        {
            base.Init();

            // TODO cache params
        }

        public override void UpdateValue()
        {
            base.UpdateValue();

            // TODO update params (?)
        }

        private void Evaluate()
        {
            m_evaluated = true;

            var mi = MemberInfo as MethodInfo;

            object ret;

            if (!HasParameters)
            {
                ret = mi.Invoke(mi.IsStatic ? null : DeclaringInstance, new object[0]);
            }
            else
            {
                // TODO parse params, invoke if valid
                throw new NotImplementedException("TODO");
            }

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
                if (HasParameters)
                {
                    throw new NotImplementedException("TODO");
                }
                else
                {
                    Evaluate();
                }
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
