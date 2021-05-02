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
        public override Type DeclaringType => MethodInfo.DeclaringType;
        public override bool CanWrite => false;

        public override bool ShouldAutoEvaluate => false;

        public override void SetInspectorOwner(ReflectionInspector inspector, MemberInfo member)
        {
            base.SetInspectorOwner(inspector, member);

            Arguments = MethodInfo.GetParameters();
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

        protected override void TrySetValue(object value) => throw new NotImplementedException("You can't set a method");
    }
}
