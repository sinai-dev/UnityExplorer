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
        public Type[][] GenericConstraints { get; private set; }

        public string[] GenericArgInput = new string[0];

        public override void Init()
        {
            var mi = (MemInfo as MethodInfo);
            GenericArgs = mi.GetGenericArguments();

            GenericConstraints = GenericArgs.Select(x => x.GetGenericParameterConstraints())
                                            .ToArray();

            GenericArgInput = new string[GenericArgs.Length];

            ValueType = mi.ReturnType;
        }

        public override void UpdateValue()
        {
            //base.UpdateValue();
        }

        public void Evaluate()
        {
            MethodInfo mi;
            if (GenericArgs.Length > 0)
            {
                mi = MakeGenericMethodFromInput();
                if (mi == null) return;
            }
            else
            {
                mi = MemInfo as MethodInfo;
            }

            object ret = null;

            try
            {
                ret = mi.Invoke(mi.IsStatic ? null : DeclaringInstance, ParseArguments());
                m_evaluated = true;
                m_isEvaluating = false;
            }
            catch (Exception e)
            {
                MelonLogger.LogWarning($"Exception evaluating: {e.GetType()}, {e.Message}");
                ReflectionException = ReflectionHelpers.ExceptionToString(e);
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

        private MethodInfo MakeGenericMethodFromInput()
        {
            var mi = MemInfo as MethodInfo;

            var list = new List<Type>();
            for (int i = 0; i < GenericArgs.Length; i++)
            {
                var input = GenericArgInput[i];
                if (ReflectionHelpers.GetTypeByName(input) is Type t)
                {
                    if (GenericConstraints[i].Length == 0)
                    {
                        list.Add(t);
                    }
                    else
                    {
                        foreach (var constraint in GenericConstraints[i].Where(x => x != null))
                        {
                           if (!constraint.IsAssignableFrom(t))
                           {
                                MelonLogger.LogWarning($"Generic argument #{i}, '{input}' is not assignable from the constraint '{constraint}'!");
                                return null;
                           }
                        }

                        list.Add(t);
                    }
                }
                else
                {
                    MelonLogger.LogWarning($"Generic argument #{i}, could not get any type by the name of '{input}'!" +
                        $" Make sure you use the full name, including the NameSpace.");
                    return null;
                }
            }

            // make into a generic with type list
            mi = mi.MakeGenericMethod(list.ToArray());

            return mi;
        }

        // ==== GUI DRAW ====

        public override void DrawValue(Rect window, float width)
        {
            string typeLabel = $"<color={UIStyles.Syntax.Class_Instance}>{ValueType.FullName}</color>";

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
