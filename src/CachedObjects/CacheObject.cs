using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnhollowerBaseLib;
using UnityEngine;

namespace Explorer
{
    public abstract class CacheObject
    {
        public object Value;
        public string ValueType;

        // Reflection window only
        public MemberInfo MemberInfo { get; set; }
        public ReflectionWindow.MemberInfoType MemberInfoType { get; set; }
        public Type DeclaringType { get; set; }
        public object DeclaringInstance { get; set; }

        public abstract void DrawValue(Rect window, float width);
        public abstract void SetValue();

        public static CacheObject GetCacheObject(object obj)
        {
            return GetCacheObject(obj, null, null);
        }

        public static CacheObject GetCacheObject(object obj, MemberInfo memberInfo, object declaringInstance)
        {
            CacheObject holder;

            var type = ReflectionHelpers.GetActualType(obj) ?? (memberInfo as FieldInfo)?.FieldType ?? (memberInfo as PropertyInfo)?.PropertyType;

            if (obj is Il2CppSystem.Object || typeof(Il2CppSystem.Object).IsAssignableFrom(type))
            {
                var name = type.FullName;
                if (name == "UnityEngine.GameObject" || name == "UnityEngine.Transform")
                {
                    holder = new CacheGameObject(obj);
                }
                else
                {
                    holder = new CacheIl2CppObject();
                }
            }
            else
            {
                if (type.IsPrimitive || type == typeof(string))
                {
                    holder = new CachePrimitive(obj);
                }
                else if (type.IsEnum)
                {
                    holder = new CacheEnum(obj);
                }
                else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) || ReflectionHelpers.IsList(type))
                {
                    holder = new CacheList(obj);
                }
                else if (type.IsValueType)
                {
                    holder = new CacheStruct(obj);
                }
                else
                {
                    holder = new CacheOther();
                }
            }

            if (holder == null)
            {
                return null;
            }

            if (memberInfo != null)
            {
                holder.MemberInfo = memberInfo;                
                holder.DeclaringType = memberInfo.DeclaringType;
                
                if (declaringInstance is Il2CppSystem.Object ilInstance && ilInstance.GetType() != memberInfo.DeclaringType)
                {
                    try
                    {
                        holder.DeclaringInstance = ilInstance.Il2CppCast(holder.DeclaringType);
                    }
                    catch
                    {
                        holder.DeclaringInstance = declaringInstance;
                    }
                }
                else
                {
                    holder.DeclaringInstance = declaringInstance;
                }

                if (memberInfo.MemberType == MemberTypes.Field)
                {
                    holder.MemberInfoType = ReflectionWindow.MemberInfoType.Field;
                }
                else if (memberInfo.MemberType == MemberTypes.Property)
                {
                    holder.MemberInfoType = ReflectionWindow.MemberInfoType.Property;
                }
                else if (memberInfo.MemberType == MemberTypes.Method)
                {
                    holder.MemberInfoType = ReflectionWindow.MemberInfoType.Method;
                }
            }

            holder.Value = obj;
            holder.ValueType = type.FullName;

            return holder;
        }

        public void Draw(Rect window, float labelWidth = 180f)
        {
            if (MemberInfo != null)
            {
                GUILayout.Label("<color=cyan>" + MemberInfo.Name + ":</color>", new GUILayoutOption[] { GUILayout.Width(labelWidth) });
            }
            else
            {
                GUILayout.Space(labelWidth);
            }

            if (Value == null)
            {
                GUILayout.Label("<i>null (" + this.ValueType + ")</i>", null);
            }
            else
            {
                DrawValue(window, window.width - labelWidth - 90);
            }
        }

        public virtual void UpdateValue(object obj)
        {
            if (MemberInfo == null)
            {
                return;
            }

            try
            {
                if (MemberInfo.MemberType == MemberTypes.Field)
                {
                    var fi = MemberInfo as FieldInfo;
                    Value = fi.GetValue(fi.IsStatic ? null : DeclaringInstance);
                }
                else if (MemberInfo.MemberType == MemberTypes.Property)
                {
                    var pi = MemberInfo as PropertyInfo;
                    Value = pi.GetValue(pi.GetAccessors()[0].IsStatic ? null : DeclaringInstance, null);
                }
            }
            catch //(Exception e)
            {
                //MelonLogger.Log($"Error updating MemberInfo value | {e.GetType()}: {e.Message}\r\n{e.StackTrace}");
            }
        }

        public void SetValue(object value, MemberInfo memberInfo, object declaringInstance)
        {
            try
            {
                if (memberInfo.MemberType == MemberTypes.Field)
                {
                    var fi = memberInfo as FieldInfo;
                    if (!(fi.IsLiteral && !fi.IsInitOnly))
                    {
                        fi.SetValue(fi.IsStatic ? null : declaringInstance, value);
                    }
                }
                else if (memberInfo.MemberType == MemberTypes.Property)
                {
                    var pi = memberInfo as PropertyInfo;
                    if (pi.CanWrite)
                    {
                        pi.SetValue(pi.GetAccessors()[0].IsStatic ? null : declaringInstance, value);
                    }
                }
            }
            catch (Exception e)
            {
                MelonLogger.LogWarning($"Error setting value: {e.GetType()}, {e.Message}");
            }
        }
    }
}
