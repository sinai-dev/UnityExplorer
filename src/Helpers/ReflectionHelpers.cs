using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;
using BF = System.Reflection.BindingFlags;
using ILType = Il2CppSystem.Type;

namespace Explorer
{
    public class ReflectionHelpers
    {
        public static BF CommonFlags = BF.Public | BF.Instance | BF.NonPublic | BF.Static;

        public static ILType GameObjectType => Il2CppType.Of<GameObject>();
        public static ILType TransformType => Il2CppType.Of<Transform>();
        public static ILType ObjectType => Il2CppType.Of<UnityEngine.Object>();
        public static ILType ComponentType => Il2CppType.Of<Component>();
        public static ILType BehaviourType => Il2CppType.Of<Behaviour>();

        private static readonly MethodInfo m_tryCastMethodInfo = typeof(Il2CppObjectBase).GetMethod("TryCast");

        public static object Il2CppCast(object obj, Type castTo)
        {
            if (!typeof(Il2CppSystem.Object).IsAssignableFrom(castTo)) return obj;

            return m_tryCastMethodInfo
                    .MakeGenericMethod(castTo)
                    .Invoke(obj, null);
        }

        public static bool IsEnumerable(Type t)
        {
            return typeof(IEnumerable).IsAssignableFrom(t);
        }

        // Only Il2Cpp List needs this check. C# List is IEnumerable.
        public static bool IsCppList(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() is Type g)
            {
                return typeof(Il2CppSystem.Collections.Generic.List<>).IsAssignableFrom(g)
                    || typeof(Il2CppSystem.Collections.Generic.IList<>).IsAssignableFrom(g);
            }
            else
            {
                return typeof(Il2CppSystem.Collections.IList).IsAssignableFrom(t);
            }
        }

        public static bool IsDictionary(Type t)
        {
            if (typeof(IDictionary).IsAssignableFrom(t))
            {
                return true;
            }

            if (t.IsGenericType && t.GetGenericTypeDefinition() is Type g)
            {
                return typeof(Il2CppSystem.Collections.Generic.Dictionary<,>).IsAssignableFrom(g)
                    || typeof(Il2CppSystem.Collections.Generic.IDictionary<,>).IsAssignableFrom(g);
            }
            else
            {
                return typeof(Il2CppSystem.Collections.IDictionary).IsAssignableFrom(t);
            }
        }

        public static Type GetTypeByName(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in asm.TryGetTypes())
                {
                    if (type.FullName == fullName)
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        public static Type GetActualType(object obj)
        {
            if (obj == null) return null;

            if (obj is Il2CppSystem.Object ilObject)
            {
                var ilTypeName = ilObject.GetIl2CppType().AssemblyQualifiedName;

                if (Type.GetType(ilTypeName) is Type t && !t.FullName.Contains("System.RuntimeType"))
                {
                    return t;
                }

                return ilObject.GetType();
            }

            return obj.GetType();
        }

        public static Type[] GetAllBaseTypes(object obj)
        {
            var list = new List<Type>();

            var type = GetActualType(obj);
            list.Add(type);

            while (type.BaseType != null)
            {
                type = type.BaseType;
                list.Add(type);
            }

            return list.ToArray();
        }

        public static string ExceptionToString(Exception e)
        {
            if (IsFailedGeneric(e))
            {
                return "Unable to initialize this type.";
            }
            else if (IsObjectCollected(e))
            {
                return "Garbage collected in Il2Cpp.";
            }

            return e.GetType() + ", " + e.Message;
        }

        public static bool IsFailedGeneric(Exception e)
        {
            return IsExceptionOfType(e, typeof(TargetInvocationException)) && IsExceptionOfType(e, typeof(TypeLoadException));
        }

        public static bool IsObjectCollected(Exception e)
        {
            return IsExceptionOfType(e, typeof(ObjectCollectedException));
        }

        public static bool IsExceptionOfType(Exception e, Type t, bool strict = true, bool checkInner = true)
        {
            bool isType;

            if (strict)
                isType = e.GetType() == t;
            else
                isType = t.IsAssignableFrom(e.GetType());

            if (isType) return true;

            if (e.InnerException != null && checkInner)
                return IsExceptionOfType(e.InnerException, t, strict);
            else
                return false;
        }
    }
}
