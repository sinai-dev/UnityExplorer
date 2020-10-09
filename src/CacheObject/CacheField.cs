using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Explorer.CacheObject
{
    public class CacheField : CacheMember
    {
        public override bool IsStatic => (MemInfo as FieldInfo).IsStatic;

        public override void InitMember(MemberInfo member, object declaringInstance)
        {
            base.InitMember(member, declaringInstance);

            base.Init(null, (member as FieldInfo).FieldType);

            UpdateValue();
        }

        public override void UpdateValue()
        {
            try
            {
                var fi = MemInfo as FieldInfo;
                IValue.Value = fi.GetValue(fi.IsStatic ? null : DeclaringInstance);

                base.UpdateValue();
            }
            catch (Exception e)
            {
                ReflectionException = ReflectionHelpers.ExceptionToString(e);
            }
        }

        public override void SetValue()
        {
            var fi = MemInfo as FieldInfo;
            fi.SetValue(fi.IsStatic ? null : DeclaringInstance, IValue.Value);
        }
    }
}
