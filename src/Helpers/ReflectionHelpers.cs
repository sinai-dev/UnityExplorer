using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using BF = System.Reflection.BindingFlags;

#if CPP
using ILType = Il2CppSystem.Type;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
#endif

namespace Explorer
{
    public class ReflectionHelpers
    {
        public static BF CommonFlags = BF.Public | BF.Instance | BF.NonPublic | BF.Static;

#if CPP
        public static ILType GameObjectType => Il2CppType.Of<GameObject>();
        public static ILType TransformType => Il2CppType.Of<Transform>();
        public static ILType ObjectType => Il2CppType.Of<UnityEngine.Object>();
        public static ILType ComponentType => Il2CppType.Of<Component>();
        public static ILType BehaviourType => Il2CppType.Of<Behaviour>();

        private static readonly MethodInfo tryCastMethodInfo = typeof(Il2CppObjectBase).GetMethod("TryCast");
        private static readonly Dictionary<Type, MethodInfo> cachedTryCastMethods = new Dictionary<Type, MethodInfo>();

        public static object Il2CppCast(object obj, Type castTo)
        {
            if (!typeof(Il2CppSystem.Object).IsAssignableFrom(castTo)) return obj;

            if (!cachedTryCastMethods.ContainsKey(castTo))
            {
                cachedTryCastMethods.Add(castTo, tryCastMethodInfo.MakeGenericMethod(castTo));
            }

            return cachedTryCastMethods[castTo].Invoke(obj, null);
        }
#else
        public static Type GameObjectType =>    typeof(GameObject);
        public static Type TransformType  =>    typeof(Transform);
        public static Type ObjectType     =>    typeof(UnityEngine.Object);
        public static Type ComponentType  =>    typeof(Component);
        public static Type BehaviourType  =>    typeof(Behaviour);
#endif

        public static bool IsEnumerable(Type t)
        {
            if (typeof(IEnumerable).IsAssignableFrom(t))
            {
                return true;
            }

#if CPP
            if (t.IsGenericType && t.GetGenericTypeDefinition() is Type g)
            {
                return typeof(Il2CppSystem.Collections.Generic.List<>).IsAssignableFrom(g)
                    || typeof(Il2CppSystem.Collections.Generic.IList<>).IsAssignableFrom(g)
                    || typeof(Il2CppSystem.Collections.Generic.HashSet<>).IsAssignableFrom(g);
            }
            else
            {
                return typeof(Il2CppSystem.Collections.IList).IsAssignableFrom(t);
            }
#else
            return false;
#endif
        }

        public static bool IsDictionary(Type t)
        {
            if (typeof(IDictionary).IsAssignableFrom(t))
            {
                return true;
            }

#if CPP
            if (t.IsGenericType && t.GetGenericTypeDefinition() is Type g)
            {
                return typeof(Il2CppSystem.Collections.Generic.Dictionary<,>).IsAssignableFrom(g)
                    || typeof(Il2CppSystem.Collections.Generic.IDictionary<,>).IsAssignableFrom(g);
            }
            else
            {
                return typeof(Il2CppSystem.Collections.IDictionary).IsAssignableFrom(t)
                    || typeof(Il2CppSystem.Collections.Hashtable).IsAssignableFrom(t);
            }
#else
            return false;
#endif
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

#if CPP
            // Need to use GetIl2CppType for Il2CppSystem Objects
            if (obj is Il2CppSystem.Object ilObject)
            {
                // Prevent weird behaviour when inspecting an Il2CppSystem.Type object.
                if (ilObject is ILType)
                {
                    return typeof(ILType);
                }

                return Type.GetType(ilObject.GetIl2CppType().AssemblyQualifiedName) ?? obj.GetType();
            }
#endif

            // It's a normal object, this is fine
            return obj.GetType();
        }

        public static Type[] GetAllBaseTypes(object obj) => GetAllBaseTypes(GetActualType(obj));

        public static Type[] GetAllBaseTypes(Type type)
        {
            var list = new List<Type>();

            while (type != null)
            {
                list.Add(type);
                type = type.BaseType;
            }

            return list.ToArray();
        }

        public static bool LoadModule(string module)
        {
#if CPP
#if ML
            var path = $@"MelonLoader\Managed\{module}.dll";
#else
            var path = $@"BepInEx\unhollowed\{module}.dll";
#endif
            if (!File.Exists(path)) return false;

            try
            {
                Assembly.Load(File.ReadAllBytes(path));
                return true;
            }
            catch (Exception e)
            {
                ExplorerCore.Log(e.GetType() + ", " + e.Message);
            }
#endif
            return false;
        }

        public static string ExceptionToString(Exception e)
        {
#if CPP
            if (IsFailedGeneric(e))
            {
                return "Unable to initialize this type.";
            }
            else if (IsObjectCollected(e))
            {
                return "Garbage collected in Il2Cpp.";
            }
#endif
            return e.GetType() + ", " + e.Message;
        }

#if CPP
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
#endif
    }
}
