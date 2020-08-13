using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnhollowerBaseLib;

namespace Explorer
{
    public class FieldInfoHolder : MemberInfoHolder
    {
        public FieldInfo fieldInfo;
        public object m_value;

        public FieldInfoHolder(Type _type, FieldInfo _fieldInfo)
        {
            classType = _type;
            fieldInfo = _fieldInfo;
        }

        public override void UpdateValue(object obj)
        {
            try
            {
                if (obj is Il2CppSystem.Object ilObject)
                {
                    var declaringType = this.fieldInfo.DeclaringType;

                    var cast = CppExplorer.Il2CppCast(obj, declaringType);
                    m_value = this.fieldInfo.GetValue(fieldInfo.IsStatic ? null : cast);
                }
                else
                {
                    m_value = this.fieldInfo.GetValue(fieldInfo.IsStatic ? null : obj);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Log($"Error updating FieldInfoHolder | {e.GetType()}: {e.Message}\r\n{e.StackTrace}");
            }
        }

        public override void Draw(ReflectionWindow window)
        {
            UIStyles.DrawMember(ref m_value, ref this.IsExpanded, ref this.arrayOffset, this.fieldInfo, window.m_rect, window.m_object, SetValue);
        }

        public override void SetValue(object obj)
        {
            try
            {
                if (fieldInfo.FieldType.IsEnum)
                {
                    if (Enum.Parse(fieldInfo.FieldType, m_value.ToString()) is object enumValue && enumValue != null)
                    {
                        m_value = enumValue;
                    }
                }
                else if (fieldInfo.FieldType.IsPrimitive)
                {
                    if (fieldInfo.FieldType == typeof(float))
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
                    else if (fieldInfo.FieldType == typeof(double))
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
                    else if (fieldInfo.FieldType != typeof(bool))
                    {
                        if (int.TryParse(m_value.ToString(), out int i))
                        {
                            m_value = i;
                        }
                        else
                        {
                            MelonLogger.LogWarning("Cannot parse " + m_value.ToString() + " to an integer! type: " + fieldInfo.FieldType);
                        }
                    }
                    else
                    {
                        MelonLogger.Log("Unsupported primitive field type: " + fieldInfo.FieldType.FullName);
                    }
                }

                if (obj is Il2CppSystem.Object ilObject)
                {
                    var declaringType = this.fieldInfo.DeclaringType;

                    var cast = CppExplorer.Il2CppCast(obj, declaringType);
                    fieldInfo.SetValue(fieldInfo.IsStatic ? null : cast, m_value);
                }
                else
                {
                    fieldInfo.SetValue(fieldInfo.IsStatic ? null : obj, m_value);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Log($"Error setting FieldInfoHolder | {e.GetType()}: {e.Message}\r\n{e.StackTrace}");
            }
        }
    }
}
