//using System;
//using System.Reflection;
//using ExplorerBeta.CacheObject;
//using UnityEngine;
//using ExplorerBeta.Helpers;

//namespace Explorer
//{
//    public static class CacheFactory
//    {
//        public static CacheObjectBase GetCacheObject(object obj)
//        {
//            if (obj == null) return null;

//            return GetCacheObject(obj, ReflectionHelpers.GetActualType(obj));
//        }

//        public static CacheObjectBase GetCacheObject(object obj, Type type)
//        {
//            var ret = new CacheObjectBase();
//            ret.Init(obj, type);
//            return ret;
//        }

//        public static CacheMember GetCacheObject(MemberInfo member, object declaringInstance)
//        {
//            CacheMember ret;

//            if (member is MethodInfo mi && CanProcessArgs(mi.GetParameters()))
//            {
//                ret = new CacheMethod();
//                ret.InitMember(mi, declaringInstance);
//            }
//            else if (member is PropertyInfo pi && CanProcessArgs(pi.GetIndexParameters()))
//            {
//                ret = new CacheProperty();
//                ret.InitMember(pi, declaringInstance);
//            }
//            else if (member is FieldInfo fi)
//            {
//                ret = new CacheField();
//                ret.InitMember(fi, declaringInstance);
//            }
//            else
//            {
//                return null;
//            }

//            return ret;
//        }

//        public static bool CanProcessArgs(ParameterInfo[] parameters)
//        {
//            foreach (var param in parameters)
//            {
//                var pType = param.ParameterType;

//                if (pType.IsByRef && pType.HasElementType)
//                {
//                    pType = pType.GetElementType();
//                }

//                if (pType.IsPrimitive || pType == typeof(string))
//                {
//                    continue;
//                }
//                else
//                {
//                    return false;
//                }
//            }

//            return true;
//        }
//    }
//}
