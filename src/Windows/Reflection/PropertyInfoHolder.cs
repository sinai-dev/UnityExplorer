using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnhollowerBaseLib;

namespace Explorer
{
    public class PropertyInfoHolder : MemberInfoHolder
    {
        public PropertyInfo propInfo;
        public object m_value;

        public PropertyInfoHolder(Type _type, PropertyInfo _propInfo)
        {
            classType = _type;
            propInfo = _propInfo;
        }

        public override void Draw(ReflectionWindow window)
        {
            UIHelpers.DrawMember(ref m_value, ref this.IsExpanded, ref this.arrayOffset, this.propInfo, window.m_rect, window.Target, SetValue);
        }

        public override void UpdateValue(object obj)
        {
            try
            {
                if (obj is Il2CppSystem.Object ilObject)
                {
                    var declaringType = this.propInfo.DeclaringType;

                    if (declaringType == typeof(Il2CppObjectBase))
                    {
                        m_value = ilObject.Pointer;
                    }
                    else
                    {
                        //var cast = ReflectionHelpers.Il2CppCast(obj, declaringType);
                        var cast = obj.Il2CppCast(declaringType);
                        m_value = this.propInfo.GetValue(this.propInfo.GetAccessors()[0].IsStatic ? null : cast, null);
                    }
                }
                else
                {
                    m_value = this.propInfo.GetValue(obj, null);
                }
            }
            catch //(Exception e)
            {
                //MelonLogger.Log("Exception on PropertyInfoHolder.UpdateValue, Name: " + this.propInfo.Name);
                //MelonLogger.Log(e.GetType() + ", " + e.Message);

                //var inner = e.InnerException;
                //while (inner != null)
                //{
                //    MelonLogger.Log("inner: " + inner.GetType() + ", " + inner.Message);
                //    inner = inner.InnerException;
                //}

                //m_value = null;
            }
        }

        public override void SetValue(object obj)
        {
            try
            {
                if (propInfo.PropertyType.IsEnum)
                {
                    if (Enum.Parse(propInfo.PropertyType, m_value.ToString()) is object enumValue && enumValue != null)
                    {
                        m_value = enumValue;
                    }
                }
                else if (propInfo.PropertyType.IsPrimitive)
                {
                    if (propInfo.PropertyType == typeof(float))
                    {
                        if (float.TryParse(m_value.ToString(), out float f))
                        {
                            m_value = f;
                        }
                        else
                        {
                            MelonLogger.LogWarning("Cannot parse " + m_value.ToString() + " to a float!");
                        }
                    }
                    else if (propInfo.PropertyType == typeof(double))
                    {
                        if (double.TryParse(m_value.ToString(), out double d))
                        {
                            m_value = d;
                        }
                        else
                        {
                            MelonLogger.LogWarning("Cannot parse " + m_value.ToString() + " to a double!");
                        }
                    }
                    else if (propInfo.PropertyType != typeof(bool))
                    {
                        if (int.TryParse(m_value.ToString(), out int i))
                        {
                            m_value = i;
                        }
                        else
                        {
                            MelonLogger.LogWarning("Cannot parse " + m_value.ToString() + " to an integer! type: " + propInfo.PropertyType);
                        }
                    }
                }

                var cast = obj.Il2CppCast(propInfo.DeclaringType);
                propInfo.SetValue(propInfo.GetAccessors()[0].IsStatic ? null : cast, m_value, null);
            }
            catch
            {
                //MelonLogger.Log("Exception trying to set property " + this.propInfo.Name);
            }
        }
    }
}
