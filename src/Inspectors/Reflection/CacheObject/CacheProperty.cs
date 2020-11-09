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
        public override bool IsStatic => (MemInfo as PropertyInfo).GetAccessors()[0].IsStatic;

        public CacheProperty(PropertyInfo propertyInfo, object declaringInstance) : base(propertyInfo, declaringInstance) 
        {
            this.m_arguments = propertyInfo.GetIndexParameters();
            this.m_argumentInput = new string[m_arguments.Length];

            base.InitValue(null, propertyInfo.PropertyType);

            UpdateValue();
        }

        public override void UpdateValue()
        {
            if (HasParameters && !m_isEvaluating)
            {
                // Need to enter parameters first.
                return;
            }

            //if (IValue is InteractiveDictionary iDict)
            //{
            //    if (!iDict.EnsureDictionaryIsSupported())
            //    {
            //        ReflectionException = "Not supported due to TypeInitializationException";
            //        return;
            //    }
            //}

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
