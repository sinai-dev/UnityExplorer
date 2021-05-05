using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityExplorer.UI.Inspectors;

namespace UnityExplorer.UI.CacheObject
{
    public class CacheField : CacheMember
    {
        public FieldInfo FieldInfo { get; internal set; }
        public override Type DeclaringType => FieldInfo.DeclaringType;
        public override bool IsStatic => FieldInfo.IsStatic;
        public override bool CanWrite => m_canWrite ?? (bool)(m_canWrite = !(FieldInfo.IsLiteral && !FieldInfo.IsInitOnly));
        private bool? m_canWrite;

        public override bool ShouldAutoEvaluate => true;

        public override void SetInspectorOwner(ReflectionInspector inspector, MemberInfo member)
        {
            base.SetInspectorOwner(inspector, member);
        }

        protected override object TryEvaluate()
        {
            try
            {
                var ret = FieldInfo.GetValue(this.Owner.Target.TryCast(this.DeclaringType));
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

        protected override void TrySetValue(object value)
        {
            try
            {
                FieldInfo.SetValue(FieldInfo.IsStatic ? null : Owner.Target.TryCast(this.DeclaringType), value);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning(ex);
            }
        }
    }
}
