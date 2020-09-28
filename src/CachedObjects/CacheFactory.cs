using System;
using System.Reflection;
using UnityEngine;

namespace Explorer
{
    public static class CacheFactory
    {
        public static CacheObjectBase GetTypeAndCacheObject(object obj) 
            => GetTypeAndCacheObject(obj, null, null);

        public static CacheObjectBase GetTypeAndCacheObject(MemberInfo memberInfo, object declarer) 
            => GetTypeAndCacheObject(null, memberInfo, declarer);

        public static CacheObjectBase GetTypeAndCacheObject(object obj, MemberInfo memberInfo, object declarer)
        {
            Type type = null;

            if (memberInfo != null)
            {
                if (memberInfo is FieldInfo fi)
                {
                    type = fi.FieldType;
                }
                else if (memberInfo is PropertyInfo pi)
                {
                    type = pi.PropertyType;
                }
                else if (memberInfo is MethodInfo mi)
                {
                    type = mi.ReturnType;
                }
            }
            else if (obj != null)
            {
                type = ReflectionHelpers.GetActualType(obj);
            }

            if (type == null)
            {
                return null;
            }

            return GetCacheObject(obj, memberInfo, declarer, type);
        }

        public static CacheObjectBase GetCacheObject(object obj, Type valueType)
            => GetCacheObject(obj, null, null, valueType);

        private static CacheObjectBase GetCacheObject(object obj, MemberInfo memberInfo, object declaringInstance, Type valueType)
        {
            CacheObjectBase cached;

            var pi = memberInfo as PropertyInfo;
            var mi = memberInfo as MethodInfo;

            // Check if can process args
            if ((pi != null && !CanProcessArgs(pi.GetIndexParameters()))
                || (mi != null && !CanProcessArgs(mi.GetParameters())))
            {
                return null;
            }

            if (mi != null)
            {
                cached = new CacheMethod();
            }
            else if (valueType == typeof(GameObject) || valueType == typeof(Transform))
            {
                cached = new CacheGameObject();
            }
            else if (valueType.IsPrimitive || valueType == typeof(string))
            {
                cached = new CachePrimitive();
            }
            else if (valueType.IsEnum)
            {
                if (valueType.GetCustomAttributes(typeof(FlagsAttribute), true) is object[] attributes && attributes.Length > 0)
                {
                    cached = new CacheEnumFlags();
                }
                else
                {
                    cached = new CacheEnum();
                }
            }
            else if (valueType == typeof(Vector2) || valueType == typeof(Vector3) || valueType == typeof(Vector4))
            {
                cached = new CacheVector();
            }
            else if (valueType == typeof(Quaternion))
            {
                cached = new CacheQuaternion();
            }
            else if (valueType == typeof(Color))
            {
                cached = new CacheColor();
            }
            else if (valueType == typeof(Rect))
            {
                cached = new CacheRect();
            }
            // must check this before IsEnumerable
            else if (ReflectionHelpers.IsDictionary(valueType))
            {
                cached = new CacheDictionary();
            }
            else if (ReflectionHelpers.IsEnumerable(valueType))
            {
                cached = new CacheList();
            }
            else
            {
                cached = new CacheOther();
            }

            cached.Value = obj;
            cached.ValueType = valueType;

            if (memberInfo != null)
            {
                cached.MemInfo = memberInfo;
                cached.DeclaringType = memberInfo.DeclaringType;
                cached.DeclaringInstance = declaringInstance;
            }

            if (pi != null)
            {
                cached.m_arguments = pi.GetIndexParameters();
            }
            else if (mi != null)
            {
                cached.m_arguments = mi.GetParameters();
            }

            cached.m_argumentInput = new string[cached.m_arguments.Length];

            cached.UpdateValue();

            cached.Init();

            return cached;
        }

        public static bool CanProcessArgs(ParameterInfo[] parameters)
        {
            foreach (var param in parameters)
            {
                var pType = param.ParameterType;

                if (pType.IsByRef && pType.HasElementType)
                {
                    pType = pType.GetElementType();
                }

                if (pType.IsPrimitive || pType == typeof(string))
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}
