using System;
using System.Reflection;
using UnityEngine;
using UnityExplorer.Helpers;

namespace UnityExplorer.Inspectors.Reflection
{
    public static class CacheFactory
    {
        // Don't think I need these with new structure.
        // Will possibly need something for CacheEnumerated / InteractiveEnumeration though.

        //public static CacheObjectBase GetCacheObject(object obj)
        //{
        //    if (obj == null) return null;

        //    return GetCacheObject(obj, ReflectionHelpers.GetActualType(obj));
        //}

        //public static CacheObjectBase GetCacheObject(object obj, Type type)
        //{
        //    var ret = new CacheObjectBase();
        //    ret.InitValue(obj, type);
        //    return ret;
        //}

        public static CacheMember GetCacheObject(MemberInfo member, object declaringInstance, GameObject parentUIContent)
        {
            CacheMember ret;

            if (member is MethodInfo mi && CanProcessArgs(mi.GetParameters()))
            {
                ret = new CacheMethod(mi, declaringInstance);
            }
            else if (member is PropertyInfo pi && CanProcessArgs(pi.GetIndexParameters()))
            {
                ret = new CacheProperty(pi, declaringInstance);
            }
            else if (member is FieldInfo fi)
            {
                ret = new CacheField(fi, declaringInstance);
            }
            else
            {
                return null;
            }

            ret.m_parentContent = parentUIContent;

            return ret;
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

                if (pType != null && (pType.IsPrimitive || pType == typeof(string)))
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
