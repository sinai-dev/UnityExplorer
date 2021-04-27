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

        public override void Initialize(ReflectionInspector inspector, Type declaringType, MemberInfo member, Type returnType)
        {
            base.Initialize(inspector, declaringType, member, returnType);

            CanWrite = true;
        }

        protected override void TryEvaluate()
        {
            try
            {
                Value = FieldInfo.GetValue(this.ParentInspector.Target.TryCast(this.DeclaringType));
            }
            catch (Exception ex)
            {
                HadException = true;
                LastException = ex;
            }
        }
    }
}
