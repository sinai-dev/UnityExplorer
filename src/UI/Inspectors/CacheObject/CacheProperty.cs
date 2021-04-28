using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace UnityExplorer.UI.Inspectors.CacheObject
{
    public class CacheProperty : CacheMember
    {
        public PropertyInfo PropertyInfo { get; internal set; }

        public override bool ShouldAutoEvaluate => !HasArguments;

        public override void Initialize(ReflectionInspector inspector, Type declaringType, MemberInfo member, Type returnType)
        {
            base.Initialize(inspector, declaringType, member, returnType);

            this.CanWrite = PropertyInfo.CanWrite;
            Arguments = PropertyInfo.GetIndexParameters();
        }

        protected override void TryEvaluate()
        {
            try
            {
                Value = PropertyInfo.GetValue(ParentInspector.Target.TryCast(DeclaringType), null);
            }
            catch (Exception ex)
            {
                HadException = true;
                LastException = ex;
            }
        }
    }
}
