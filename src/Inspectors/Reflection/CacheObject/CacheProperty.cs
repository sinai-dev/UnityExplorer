using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityExplorer.UI;
using UnityExplorer.Helpers;

namespace UnityExplorer.Inspectors.Reflection
{
    public class CacheProperty : CacheMember
    {
        public override Type FallbackType => (MemInfo as PropertyInfo).PropertyType;

        public override bool IsStatic => (MemInfo as PropertyInfo).GetAccessors(true)[0].IsStatic;

        public CacheProperty(PropertyInfo propertyInfo, object declaringInstance) : base(propertyInfo, declaringInstance) 
        {
            this.m_arguments = propertyInfo.GetIndexParameters();
            this.m_argumentInput = new string[m_arguments.Length];

            CreateIValue(null, propertyInfo.PropertyType);
        }

        public override void UpdateReflection()
        {
            if (HasParameters && !m_isEvaluating)
            {
                // Need to enter parameters first.
                return;
            }

            var pi = MemInfo as PropertyInfo;

            if (pi.CanRead)
            {
                var target = pi.GetAccessors(true)[0].IsStatic ? null : DeclaringInstance;

                IValue.Value = pi.GetValue(target, ParseArguments());

                m_evaluated = true;
                ReflectionException = null;
            }
            else 
            {
                if (FallbackType == typeof(string))
                {
                    IValue.Value = "";
                }
                else if (FallbackType.IsPrimitive)
                {
                    IValue.Value = Activator.CreateInstance(FallbackType);
                }
                m_evaluated = true;
                ReflectionException = null;
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
