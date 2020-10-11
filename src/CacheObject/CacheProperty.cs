using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Explorer.CacheObject
{
    public class CacheProperty : CacheMember
    {
        public override bool IsStatic => (MemInfo as PropertyInfo).GetAccessors()[0].IsStatic;

        public override void InitMember(MemberInfo member, object declaringInstance)
        {
            base.InitMember(member, declaringInstance);

            var pi = member as PropertyInfo;

            this.m_arguments = pi.GetIndexParameters();
            this.m_argumentInput = new string[m_arguments.Length];

            base.Init(null, pi.PropertyType);

            UpdateValue();
        }

        public override void UpdateValue()
        {
            if (HasParameters && !m_isEvaluating)
            {
                // Need to enter parameters first.
                return;
            }

            try
            {
                var pi = MemInfo as PropertyInfo;

                if (pi.CanRead)
                {
                    var target = pi.GetAccessors()[0].IsStatic ? null : DeclaringInstance;

                    IValue.Value = pi.GetValue(target, ParseArguments());

                    base.UpdateValue();
                }
                else // create a dummy value for Write-Only properties.
                {
                    if (IValue.ValueType == typeof(string))
                    {
                        IValue.Value = "";
                    }
                    else
                    {
                        IValue.Value = Activator.CreateInstance(IValue.ValueType);
                    }
                }
            }
            catch (Exception e)
            {
                ReflectionException = ReflectionHelpers.ExceptionToString(e);
            }
        }

        public override void SetValue()
        {
            var pi = MemInfo as PropertyInfo;
            var target = pi.GetAccessors()[0].IsStatic ? null : DeclaringInstance;

            pi.SetValue(target, IValue.Value, ParseArguments());
        }
    }
}
