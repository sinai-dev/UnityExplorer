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

        private bool m_isEvaluating;
        private ParameterInfo[] m_arguments;
        private string[] m_argumentInput;

        public bool HasParameters => m_arguments != null && m_arguments.Length > 0;

        public static bool CanEvaluate(MethodInfo mi)
        {
            // TODO generic args
            if (mi.GetGenericArguments().Length > 0)
            {
                return false;
            }

            // primitive and string args supported
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

            var mi = MemInfo as MethodInfo;

            m_arguments = mi.GetParameters();
            m_argumentInput = new string[m_arguments.Length];
        }

        public override void UpdateValue()
        {
            //base.UpdateValue();
        }

        private void Evaluate()
        {
            var mi = MemInfo as MethodInfo;

            object ret = null;

            if (!HasParameters)
            {
                ret = mi.Invoke(mi.IsStatic ? null : DeclaringInstance, new object[0]);
                m_evaluated = true;
            }
            else
            {
                var parsedArgs = new List<object>();
                for (int i = 0; i < m_arguments.Length; i++)
                {
                    var input = m_argumentInput[i];
                    var type = m_arguments[i].ParameterType;

                    if (type == typeof(string))
                    {
                        parsedArgs.Add(input);
                    }
                    else
                    {
                        try
                        {
                            if (type.GetMethod("Parse", new Type[] { typeof(string) }).Invoke(null, new object[] { input }) is object parsed)
                            {
                                parsedArgs.Add(parsed);
                            }
                            else
                            {
                                // try add a null arg i guess
                                parsedArgs.Add(null);
                            }

                        }
                        catch
                        {
                            MelonLogger.Log($"Unable to parse '{input}' to type '{type.Name}'");
                            break;
                        }
                    }
                }

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

                if (m_cachedReturnValue is IExpandHeight expander)
                {
                    expander.WhiteSpace = 0f;
                    expander.ButtonWidthOffset += 70f;
                }

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
                    m_cachedReturnValue.DrawValue(window, width);
                }
                else
                {
                    GUILayout.Label($"null (<color=yellow>{ValueTypeName}</color>)", null);
                }
            }
            else
            {
                GUILayout.Label($"<color=grey><i>Not yet evaluated</i></color> (<color=yellow>{ValueTypeName}</color>)", null);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }
}
