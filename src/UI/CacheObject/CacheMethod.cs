using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityExplorer.UI.Inspectors;

namespace UnityExplorer.UI.CacheObject
{
    public class CacheMethod : CacheMember
    {
        public MethodInfo MethodInfo { get; internal set; }
        public override Type DeclaringType => MethodInfo.DeclaringType;
        public override bool CanWrite => false;
        public override bool IsStatic => MethodInfo.IsStatic;

        public override bool ShouldAutoEvaluate => false;

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

                if (Arguments.Length > 0)
                    return methodInfo.Invoke(DeclaringInstance, Evaluator.TryParseArguments());

                var ret = methodInfo.Invoke(DeclaringInstance, ArgumentUtility.EmptyArgs);

                HadException = false;
                LastException = null;
                return ret;
            }
            catch (Exception ex)
            {
                HadException = true;
                LastException = ex;
                return null;
            }
        }

        protected override void TrySetValue(object value) => throw new NotImplementedException("You can't set a method");
    }
}
