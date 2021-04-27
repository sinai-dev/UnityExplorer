using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace UnityExplorer.UI.Inspectors.CacheObject
{
    public class CacheMethod : CacheMember
    {
        public MethodInfo MethodInfo { get; internal set; }

        public override void Initialize(ReflectionInspector inspector, Type declaringType, MemberInfo member, Type returnType)
        {
            base.Initialize(inspector, declaringType, member, returnType);


        }

        protected override void TryEvaluate()
        {
            try
            {
                throw new NotImplementedException("TODO");
            }
            catch (Exception ex)
            {
                HadException = true;
                LastException = ex;
            }
        }
    }
}
