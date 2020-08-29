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

        private bool m_isEvaluating;
        private ParameterInfo[] m_arguments;
        private string[] m_argumentInput;

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

        public static bool CanEvaluate(MethodInfo mi)
        {
            // generic type args not supported yet
            if (mi.GetGenericArguments().Length > 0)
            {
                return false;
            }

            // only primitive and string args supported
            foreach (var param in mi.GetParameters())
            {
                if (!param.ParameterType.IsPrimitive && param.ParameterType != typeof(string))
                {
                    return false;
                }
            }

            return true;
        }

        public override void Init()
        {
            base.Init();

            var mi = MemberInfo as MethodInfo;

            m_arguments = mi.GetParameters();
            m_argumentInput = new string[m_arguments.Length];
        }

        public override void UpdateValue()
        {
            base.UpdateValue();
        }        

        public override void DrawValue(Rect window, float width)
        {
            GUILayout.BeginVertical(null);

            string evaluateLabel = "<color=lime>Evaluate</color>";
            if (HasParameters)
            {
                if (m_isEvaluating)
                {
                    for (int i = 0; i < m_arguments.Length; i++)
                    {
                        var name = m_arguments[i].Name;
                        var input = m_argumentInput[i];
                        var type = m_arguments[i].ParameterType.Name;

                        GUILayout.BeginHorizontal(null);
                        m_argumentInput[i] = GUILayout.TextField(input, new GUILayoutOption[] { GUILayout.Width(150) });
                        GUILayout.Label(i + ": <color=cyan>" + name + "</color> <color=yellow>(" + type + ")</color>", null);

                        GUILayout.EndHorizontal();
                    }

                    GUILayout.BeginHorizontal(null);
                    if (GUILayout.Button(evaluateLabel, new GUILayoutOption[] { GUILayout.Width(70) }))
                    {
                        Evaluate();
                        m_isEvaluating = false;
                    }
                    if (GUILayout.Button("Cancel", new GUILayoutOption[] { GUILayout.Width(70) }))
                    {
                        m_isEvaluating = false;
                    }
                }
                else
                {
                    GUILayout.BeginHorizontal(null);
                    if (GUILayout.Button($"Evaluate ({m_arguments.Length} params)", new GUILayoutOption[] { GUILayout.Width(150) }))
                    {
                        m_isEvaluating = true;
                    }
                }
            }
            else
            {
                GUILayout.BeginHorizontal(null);
                if (GUILayout.Button(evaluateLabel, new GUILayoutOption[] { GUILayout.Width(70) }))
                {
                    Evaluate();
                }
            }
            
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
                    GUILayout.Label($"null (<color=yellow>{ValueType}</color>)", null);
                }
            }
            else
            {
                GUILayout.Label($"<color=grey><i>Not yet evaluated</i></color> (<color=yellow>{ValueType}</color>)", null);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void Evaluate()
        {
            m_evaluated = true;

            var mi = MemberInfo as MethodInfo;

            object ret = null;

            if (!HasParameters)
            {
                ret = mi.Invoke(mi.IsStatic ? null : DeclaringInstance, new object[0]);
            }
            else
            {
                var arguments = new List<object>();
                for (int i = 0; i < m_arguments.Length; i++)
                {
                    var input = m_argumentInput[i];
                    var type = m_arguments[i].ParameterType;

                    if (type == typeof(string))
                    {
                        arguments.Add(input);
                    }
                    else
                    {
                        try
                        {
                            if (type.GetMethod("Parse", new Type[] { typeof(string) }).Invoke(null, new object[] { input }) is object parsed)
                            {
                                arguments.Add(parsed);
                            }
                            else
                            {
                                throw new Exception();
                            }

                        }
                        catch
                        {
                            MelonLogger.Log($"Unable to parse '{input}' to type '{type.Name}'");
                            break;
                        }
                    }
                }

                if (arguments.Count == m_arguments.Length)
                {
                    ret = mi.Invoke(mi.IsStatic ? null : DeclaringInstance, arguments.ToArray());
                }
                else
                {
                    MelonLogger.Log($"Did not invoke because {m_arguments.Length - arguments.Count} arguments could not be parsed!");
                }
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
    }
}
