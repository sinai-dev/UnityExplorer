using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityExplorer.UI.Inspectors;

namespace UnityExplorer.UI.CacheObject
{
    public class CacheProperty : CacheMember
    {
        public PropertyInfo PropertyInfo { get; internal set; }
        public override Type DeclaringType => PropertyInfo.DeclaringType;
        public override bool CanWrite => PropertyInfo.CanWrite;
        public override bool IsStatic => m_isStatic ?? (bool)(m_isStatic = PropertyInfo.GetAccessors(true)[0].IsStatic);
        private bool? m_isStatic;

        public override bool ShouldAutoEvaluate => !HasArguments;

        public override void SetInspectorOwner(ReflectionInspector inspector, MemberInfo member)
        {
            base.SetInspectorOwner(inspector, member);

            Arguments = PropertyInfo.GetIndexParameters();
        }

        protected override object TryEvaluate()
        {
            try
            {
                if (HasArguments)
                    return PropertyInfo.GetValue(DeclaringInstance, this.Evaluator.TryParseArguments());

                var ret = PropertyInfo.GetValue(DeclaringInstance, null);
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
            if (!CanWrite)
                return;

            try
            {
                bool _static = PropertyInfo.GetAccessors(true)[0].IsStatic;
                var target = _static ? null : Owner.Target.TryCast(DeclaringType);

                if (HasArguments)
                    PropertyInfo.SetValue(target, value, Evaluator.TryParseArguments());
                else
                    PropertyInfo.SetValue(target, value, null);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning(ex);
            }
        }
    }
}
