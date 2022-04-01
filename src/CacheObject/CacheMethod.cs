using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityExplorer.Inspectors;
using UniverseLib;
using UniverseLib.Utility;

namespace UnityExplorer.CacheObject
{
    public class CacheMethod : CacheMember
    {
        public MethodInfo MethodInfo { get; }
        public override Type DeclaringType => MethodInfo.DeclaringType;
        public override bool CanWrite => false;
        public override bool IsStatic => MethodInfo.IsStatic;

        public override bool ShouldAutoEvaluate => false;

        public CacheMethod (MethodInfo mi)
        {
            this.MethodInfo = mi;
        }

        public override void SetInspectorOwner(ReflectionInspector inspector, MemberInfo member)
        {
            base.SetInspectorOwner(inspector, member);

            Arguments = MethodInfo.GetParameters();
            if (MethodInfo.IsGenericMethod)
                GenericArguments = MethodInfo.GetGenericArguments();
        }

        protected override object TryEvaluate()
        {
            try
            {
                var methodInfo = MethodInfo;
                if (methodInfo.IsGenericMethod)
                    methodInfo = MethodInfo.MakeGenericMethod(Evaluator.TryParseGenericArguments());

                object ret;
                if (HasArguments)
                    ret = methodInfo.Invoke(DeclaringInstance, Evaluator.TryParseArguments());
                else 
                    ret = methodInfo.Invoke(DeclaringInstance, ArgumentUtility.EmptyArgs);
                LastException = null;
                return ret;
            }
            catch (Exception ex)
            {
                LastException = ex;
                return null;
            }
        }

        protected override void TrySetValue(object value) => throw new NotImplementedException("You can't set a method");
    }
}
