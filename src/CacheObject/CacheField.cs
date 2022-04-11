using System;
using System.Reflection;
using UnityExplorer.Inspectors;

namespace UnityExplorer.CacheObject
{
    public class CacheField : CacheMember
    {
        public FieldInfo FieldInfo { get; internal set; }
        public override Type DeclaringType => FieldInfo.DeclaringType;
        public override bool IsStatic => FieldInfo.IsStatic;
        public override bool CanWrite => m_canWrite ?? (bool)(m_canWrite = !(FieldInfo.IsLiteral && !FieldInfo.IsInitOnly));
        private bool? m_canWrite;

        public override bool ShouldAutoEvaluate => true;

        public CacheField(FieldInfo fi)
        {
            this.FieldInfo = fi;
        }

        public override void SetInspectorOwner(ReflectionInspector inspector, MemberInfo member)
        {
            base.SetInspectorOwner(inspector, member);
        }

        protected override object TryEvaluate()
        {
            try
            {
                object ret = FieldInfo.GetValue(DeclaringInstance);
                LastException = null;
                return ret;
            }
            catch (Exception ex)
            {
                LastException = ex;
                return null;
            }
        }

        protected override void TrySetValue(object value)
        {
            try
            {
                FieldInfo.SetValue(DeclaringInstance, value);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning(ex);
            }
        }
    }
}
