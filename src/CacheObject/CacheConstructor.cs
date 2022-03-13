using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityExplorer.Inspectors;
using UniverseLib.Utility;

namespace UnityExplorer.CacheObject
{
    public class CacheConstructor : CacheMember
    {
        public ConstructorInfo CtorInfo { get; }

        public override Type DeclaringType => CtorInfo.DeclaringType;
        public override bool IsStatic => true;
        public override bool ShouldAutoEvaluate => false;
        public override bool CanWrite => false;

        public CacheConstructor(ConstructorInfo ci)
        {
            this.CtorInfo = ci;
        }

        public override void SetInspectorOwner(ReflectionInspector inspector, MemberInfo member)
        {
            base.SetInspectorOwner(inspector, member);

            Arguments = CtorInfo.GetParameters();
            if (CtorInfo.DeclaringType.IsGenericTypeDefinition)
                GenericArguments = CtorInfo.DeclaringType.GetGenericArguments();
        }

        protected override object TryEvaluate()
        {
            try
            {
                Type returnType = DeclaringType;

                if (returnType.IsGenericTypeDefinition)
                    returnType = DeclaringType.MakeGenericType(Evaluator.TryParseGenericArguments());

                object ret;
                if (HasArguments)
                    ret = Activator.CreateInstance(returnType, Evaluator.TryParseArguments());
                else
                    ret = Activator.CreateInstance(returnType, ArgumentUtility.EmptyArgs);

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

        protected override void TrySetValue(object value) => throw new NotImplementedException("You can't set a constructor");
    }
}
