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
        // public ReflectionWindow.MemberInfoType MemberInfoType { get; set; }
        public Type DeclaringType { get; set; }
        public object DeclaringInstance { get; set; }
        public string FullName => $"{MemberInfo.DeclaringType.Name}.{MemberInfo.Name}"; 
        public string ReflectionException;

        public bool CanWrite
        {
            get
            {
                if (MemberInfo is FieldInfo fi)
                {
                    return !(fi.IsLiteral && !fi.IsInitOnly);
                }
                else if (MemberInfo is PropertyInfo pi)
                {
                    return pi.CanWrite;
                }
                else
                {
                    return false;
                }
            }
        }

        public ReflectionWindow.MemberInfoType MemberInfoType
        {
            get
            {
                if (MemberInfo is FieldInfo) return ReflectionWindow.MemberInfoType.Field;
                if (MemberInfo is PropertyInfo) return ReflectionWindow.MemberInfoType.Property;
                if (MemberInfo is MethodInfo) return ReflectionWindow.MemberInfoType.Method;
                return ReflectionWindow.MemberInfoType.All;
            }
        }

        // methods
        public virtual void Init() { }
        public abstract void DrawValue(Rect window, float width);

        public static CacheObject GetCacheObject(object obj)
        {
            return GetCacheObject(obj, null, null);
        }

        /// <summary>
        /// Gets the CacheObject subclass for an object or MemberInfo
        /// </summary>
        /// <param name="obj">The current value (can be null if memberInfo is not null)</param>
        /// <param name="memberInfo">The MemberInfo (can be null if obj is not null)</param>
        /// <param name="declaringInstance">If MemberInfo is not null, the declaring class instance. Can be null if static.</param>
        /// <returns></returns>
        public static CacheObject GetCacheObject(object obj, MemberInfo memberInfo, object declaringInstance)
        {
            var type = ReflectionHelpers.GetActualType(obj) ?? (memberInfo as FieldInfo)?.FieldType ?? (memberInfo as PropertyInfo)?.PropertyType;

            if (type == null)
            {
                MelonLogger.Log("Could not get type for object or memberinfo!");
                return null;
            }

            return GetCacheObject(obj, memberInfo, declaringInstance, type);
        }

        /// <summary>
        /// Gets the CacheObject subclass for an object or MemberInfo
        /// </summary>
        /// <param name="obj">The current value (can be null if memberInfo is not null)</param>
        /// <param name="memberInfo">The MemberInfo (can be null if obj is not null)</param>
        /// <param name="declaringInstance">If MemberInfo is not null, the declaring class instance. Can be null if static.</param>
        /// <param name="type">The type of the object or MemberInfo value.</param>
        /// <returns></returns>
        public static CacheObject GetCacheObject(object obj, MemberInfo memberInfo, object declaringInstance, Type type)
        {
            CacheObject holder;

            if ((obj is Il2CppSystem.Object || typeof(Il2CppSystem.Object).IsAssignableFrom(type))
                && (type.FullName.Contains("UnityEngine.GameObject") || type.FullName.Contains("UnityEngine.Transform")))
            {
                holder = new CacheGameObject();
            }
            else if (type.IsPrimitive || type == typeof(string))
            {
                holder = new CachePrimitive();
            }
            else if (type.IsEnum)
            {
                holder = new CacheEnum();
            }
            else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) || ReflectionHelpers.IsList(type))
            {
                holder = new CacheList();
            }
            else
            {
                holder = new CacheOther();
            }

            holder.Value = obj;
            holder.ValueType = type.FullName;

            if (memberInfo != null)
            {
                holder.MemberInfo = memberInfo;
                holder.DeclaringType = memberInfo.DeclaringType;
                holder.DeclaringInstance = declaringInstance;
            }

            holder.UpdateValue();
            holder.Init();

            return holder;
        }

        public void Draw(Rect window, float labelWidth = 215f)
        {
            if (MemberInfo != null)
            {
                GUILayout.Label("<color=cyan>" + FullName + "</color>", new GUILayoutOption[] { GUILayout.Width(labelWidth) });
            }
            else
            {
                GUILayout.Space(labelWidth);
            }

            if (!string.IsNullOrEmpty(ReflectionException))
            {
                GUILayout.Label("<color=red>Reflection failed!</color> (" + ReflectionException + ")", null);
            }
            else if (Value == null)
            {
                GUILayout.Label("<i>null (" + ValueType + ")</i>", null);
            }
            else
            {
                DrawValue(window, window.width - labelWidth - 90);
            }
        }

        public virtual void UpdateValue()
        {
            if (MemberInfo == null || !string.IsNullOrEmpty(ReflectionException))
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
                    bool isStatic = pi.GetAccessors()[0].IsStatic;
                    var target = isStatic ? null : DeclaringInstance;
                    Value = pi.GetValue(target, null);
                }
                //ReflectionException = null;
            }
            catch (Exception e)
            {
                ReflectionException = ReflectionHelpers.ExceptionToString(e);
            }
        }

        public void SetValue()
        {
            try
            {
                if (MemberInfo.MemberType == MemberTypes.Field)
                {
                    var fi = MemberInfo as FieldInfo;
                    fi.SetValue(fi.IsStatic ? null : DeclaringInstance, Value);
                }
                else if (MemberInfo.MemberType == MemberTypes.Property)
                {
                    var pi = MemberInfo as PropertyInfo;
                    pi.SetValue(pi.GetAccessors()[0].IsStatic ? null : DeclaringInstance, Value);
                }
            }
            catch (Exception e)
            {
                MelonLogger.LogWarning($"Error setting value: {e.GetType()}, {e.Message}");
            }
        }
    }
}
