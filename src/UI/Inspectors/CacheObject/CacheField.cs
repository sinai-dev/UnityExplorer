using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace UnityExplorer.UI.Inspectors.CacheObject
{
    public class CacheField : CacheMember
    {
        public FieldInfo FieldInfo { get; internal set; }
        public override Type DeclaringType => FieldInfo.DeclaringType;
        public override bool CanWrite => m_canWrite ?? (bool)(m_canWrite = !(FieldInfo.IsLiteral && !FieldInfo.IsInitOnly));
        private bool? m_canWrite;

        public override bool ShouldAutoEvaluate => true;

        public override void SetInspectorOwner(ReflectionInspector inspector, MemberInfo member)
        {
            base.SetInspectorOwner(inspector, member);
        }

        protected override void TryEvaluate()
        {
            try
            {
                Value = FieldInfo.GetValue(this.Owner.Target.TryCast(this.DeclaringType));
            }
            catch (Exception ex)
            {
                HadException = true;
                LastException = ex;
            }
        }

        protected override void TrySetValue(object value)
        {
            try
            {
                FieldInfo.SetValue(FieldInfo.IsStatic ? null : Owner.Target, value);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning(ex);
            }
        }
    }
}
