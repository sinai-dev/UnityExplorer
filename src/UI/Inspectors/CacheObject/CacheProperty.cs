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
        public override Type DeclaringType => PropertyInfo.DeclaringType;
        public override bool CanWrite => PropertyInfo.CanWrite;

        public override bool ShouldAutoEvaluate => !HasArguments;

        public override void SetInspectorOwner(ReflectionInspector inspector, MemberInfo member)
        {
            base.SetInspectorOwner(inspector, member);

            Arguments = PropertyInfo.GetIndexParameters();
        }

        protected override void TryEvaluate()
        {
            try
            {
                Value = PropertyInfo.GetValue(Owner.Target.TryCast(DeclaringType), null);
            }
            catch (Exception ex)
            {
                HadException = true;
                LastException = ex;
            }
        }

        protected override void TrySetValue(object value)
        {
            if (!CanWrite)
                return;

            try
            {
                // TODO property indexers

                PropertyInfo.SetValue(PropertyInfo.GetSetMethod().IsStatic ? null : Owner.Target, value, null);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning(ex);
            }
        }
    }
}
