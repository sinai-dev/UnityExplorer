using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityExplorer.UI.Shared;
using UnityExplorer.Helpers;

namespace UnityExplorer.Inspectors.Reflection
{
    public class CacheMethod : CacheMember
    {
        private CacheObjectBase m_cachedReturnValue;

        public override bool HasParameters => base.HasParameters || GenericArgs.Length > 0;

        public override bool IsStatic => (MemInfo as MethodInfo).IsStatic;

        public Type[] GenericArgs { get; private set; }
        public Type[][] GenericConstraints { get; private set; }

        public string[] m_genericArgInput = new string[0];

        public CacheMethod(MethodInfo methodInfo, object declaringInstance) : base(methodInfo, declaringInstance)
        {
            GenericArgs = methodInfo.GetGenericArguments();

            GenericConstraints = GenericArgs.Select(x => x.GetGenericParameterConstraints())
                                            .ToArray();

            m_genericArgInput = new string[GenericArgs.Length];

            m_arguments = methodInfo.GetParameters();
            m_argumentInput = new string[m_arguments.Length];

            base.InitValue(null, methodInfo.ReturnType);
        }

        public override void UpdateValue()
        {
            // CacheMethod cannot UpdateValue directly. Need to Evaluate.
        }

        public override void UpdateReflection()
        {
            // CacheMethod cannot UpdateValue directly. Need to Evaluate.
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
                ReflectionException = null;
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"Exception evaluating: {e.GetType()}, {e.Message}");
                ReflectionException = ReflectionHelpers.ExceptionToString(e);
            }

            if (ret != null)
            {
                //m_cachedReturnValue = CacheFactory.GetTypeAndCacheObject(ret);

                //m_cachedReturnValue = CacheFactory.GetCacheObject(ret);
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
                var input = m_genericArgInput[i];
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
                                ExplorerCore.LogWarning($"Generic argument #{i}, '{input}' is not assignable from the constraint '{constraint}'!");
                                return null;
                            }
                        }

                        list.Add(t);
                    }
                }
                else
                {
                    ExplorerCore.LogWarning($"Generic argument #{i}, could not get any type by the name of '{input}'!" +
                        $" Make sure you use the full name including the namespace.");
                    return null;
                }
            }

            // make into a generic with type list
            mi = mi.MakeGenericMethod(list.ToArray());

            return mi;
        }
    }
}
