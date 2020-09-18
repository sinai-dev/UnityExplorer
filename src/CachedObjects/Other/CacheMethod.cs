using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace Explorer
{
    public class CacheMethod : CacheObjectBase
    {
        private CacheObjectBase m_cachedReturnValue;

        public override bool HasParameters => base.HasParameters || GenericArgs.Length > 0;

        public Type[] GenericArgs { get; private set; }
        public Type[] GenericConstraints { get; private set; }

        public string[] GenericArgInput = new string[0];

        public static bool CanEvaluate(MethodInfo mi)
        {
            // primitive and string args supported
            return CanProcessArgs(mi.GetParameters());
        }

        public override void Init()
        {
            var mi = (MemInfo as MethodInfo);
            GenericArgs = mi.GetGenericArguments();

            GenericConstraints = GenericArgs.Select(x => x.GetGenericParameterConstraints()
                                                          .FirstOrDefault())
                                            .ToArray();

            GenericArgInput = new string[GenericArgs.Length];

            ValueType = mi.ReturnType;
            ValueTypeName = ValueType.FullName;
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

            // Parse generic arguments
            if (GenericArgs.Length > 0)
            {
                var list = new List<Type>();
                for (int i = 0; i < GenericArgs.Length; i++)
                {
                    var input = GenericArgInput[i];
                    if (ReflectionHelpers.GetTypeByName(input) is Type t)
                    {
                        if (GenericConstraints[i] == null)
                        {
                            list.Add(t);
                        }
                        else
                        {
                            if (GenericConstraints[i].IsAssignableFrom(t))
                            {
                                list.Add(t);
                            }
                            else
                            {
                                MelonLogger.Log($"Generic argument #{i} '{input}', is not assignable from the generic constraint!");
                                return;
                            }
                        }
                    }
                    else
                    {
                        MelonLogger.Log($"Generic argument #{i}, could not get any type by the name of '{input}'!" +
                            $" Make sure you use the full name, including the NameSpace.");
                        return;
                    }
                }

                // make into a generic with type list
                mi = mi.MakeGenericMethod(list.ToArray());
            }

            // Parse arguments
            if (!HasParameters)
            {
                ret = mi.Invoke(mi.IsStatic ? null : DeclaringInstance, new object[0]);
                m_evaluated = true;
            }
            else
            {
                try
                {
                    ret = mi.Invoke(mi.IsStatic ? null : DeclaringInstance, ParseArguments());
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
            string typeLabel = $"<color={UIStyles.Syntax.Class_Instance}>{ValueTypeName}</color>";

            if (m_evaluated)
            {
                if (m_cachedReturnValue != null)
                {
                    m_cachedReturnValue.DrawValue(window, width);
                }
                else
                {
                    GUILayout.Label($"null ({typeLabel})", null);
                }
            }
            else
            {
                GUILayout.Label($"<color=grey><i>Not yet evaluated</i></color> ({typeLabel})", null);
            }
        }
    }
}
