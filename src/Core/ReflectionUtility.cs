using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BF = System.Reflection.BindingFlags;
using UnityExplorer.Core.Runtime;

namespace UnityExplorer
{
    public static class ReflectionUtility
    {
        public const BF AllFlags = BF.Public | BF.Instance | BF.NonPublic | BF.Static;

        public static void Test()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyResolver);
        }

        private static Assembly AssemblyResolver(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("UnityExplorer"))
                return typeof(ExplorerCore).Assembly;

            return null;
        }

        public static bool ValueEqual<T>(this T objA, T objB)
        {
            return (objA == null && objB == null) || (objA != null && objA.Equals(objB));
        }

        public static bool ReferenceEqual(this object objA, object objB)
        {
            if (objA.TryCast<UnityEngine.Object>() is UnityEngine.Object unityA)
            {
                var unityB = objB.TryCast<UnityEngine.Object>();
                if (unityB && unityA.m_CachedPtr == unityB.m_CachedPtr)
                    return true;
            }

            return object.ReferenceEquals(objA, objB);
        }

        /// <summary>
        /// Helper for IL2CPP to get the underlying true Type (Unhollowed) of the object.
        /// </summary>
        /// <param name="obj">The object to get the true Type for.</param>
        /// <returns>The most accurate Type of the object which could be identified.</returns>
        public static Type GetActualType(this object obj)
        {
            if (obj == null)
                return null;

            return ReflectionProvider.Instance.GetActualType(obj);
        }

        /// <summary>
        /// Cast an object to its underlying Type.
        /// </summary>
        /// <param name="obj">The object to cast</param>
        /// <returns>The object, cast to the underlying Type if possible, otherwise the original object.</returns>
        public static object TryCast(this object obj)
            => ReflectionProvider.Instance.Cast(obj, GetActualType(obj));

        /// <summary>
        /// Cast an object to a Type, if possible.
        /// </summary>
        /// <param name="obj">The object to cast</param>
        /// <param name="castTo">The Type to cast to </param>
        /// <returns>The object, cast to the Type provided if possible, otherwise the original object.</returns>
        public static object TryCast(this object obj, Type castTo)
            => ReflectionProvider.Instance.Cast(obj, castTo);

        public static T TryCast<T>(this object obj)
            => ReflectionProvider.Instance.TryCast<T>(obj);

        /// <summary>
        /// Check if the provided Type is assignable to IEnumerable.
        /// </summary>
        /// <param name="t">The Type to check</param>
        /// <returns>True if the Type is assignable to IEnumerable, otherwise false.</returns>
        public static bool IsEnumerable(this Type t)
            => ReflectionProvider.Instance.IsAssignableFrom(typeof(IEnumerable), t);

        /// <summary>
        /// Check if the provided Type is assignable to IDictionary.
        /// </summary>
        /// <param name="t">The Type to check</param>
        /// <returns>True if the Type is assignable to IDictionary, otherwise false.</returns>
        public static bool IsDictionary(this Type t)
            => ReflectionProvider.Instance.IsAssignableFrom(typeof(IDictionary), t);

        /// <summary>
        /// [INTERNAL] Used to load Unhollowed DLLs in IL2CPP.
        /// </summary>
        internal static bool LoadModule(string module)
            => ReflectionProvider.Instance.LoadModule(module);

        // cache for GetTypeByName
        internal static readonly Dictionary<string, Type> s_typesByName = new Dictionary<string, Type>();

        /// <summary>
        /// Find a <see cref="Type"/> in the current AppDomain whose <see cref="Type.FullName"/> matches the provided <paramref name="fullName"/>.
        /// </summary>
        /// <param name="fullName">The <see cref="Type.FullName"/> you want to search for - case sensitive and full matches only.</param>
        /// <returns>The Type if found, otherwise null.</returns>
        public static Type GetTypeByName(string fullName)
        {
            s_typesByName.TryGetValue(fullName, out Type ret);

            if (ret != null)
                return ret;

            foreach (var type in from asm in AppDomain.CurrentDomain.GetAssemblies() 
                                 from type in asm.TryGetTypes() 
                                 select type)
            {
                if (type.FullName == fullName)
                {
                    ret = type;
                    break;
                }
            }

            if (s_typesByName.ContainsKey(fullName))
                s_typesByName[fullName] = ret;
            else
                s_typesByName.Add(fullName, ret);

            return ret;
        }

        // cache for GetBaseTypes
        internal static readonly Dictionary<string, Type[]> s_cachedTypeInheritance = new Dictionary<string, Type[]>();

        /// <summary>
        /// Get all base types of the provided Type, including itself.
        /// </summary>
        public static Type[] GetAllBaseTypes(this object obj) => GetAllBaseTypes(GetActualType(obj));

        /// <summary>
        /// Get all base types of the provided Type, including itself.
        /// </summary>
        public static Type[] GetAllBaseTypes(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            var name = type.AssemblyQualifiedName;

            if (s_cachedTypeInheritance.TryGetValue(name, out Type[] ret))
                return ret;

            List<Type> list = new List<Type>();

            while (type != null)
            {
                list.Add(type);
                type = type.BaseType;
            }

            ret = list.ToArray();

            s_cachedTypeInheritance.Add(name, ret);

            return ret;
        }

        /// <summary>
        /// Safely get all valid Types inside an Assembly.
        /// </summary>
        /// <param name="asm">The Assembly to find Types in.</param>
        /// <returns>All possible Types which could be retrieved from the Assembly, or an empty array.</returns>
        public static IEnumerable<Type> TryGetTypes(this Assembly asm)
        {
            try
            {
                return asm.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                try
                {
                    return asm.GetExportedTypes();
                }
                catch
                {
                    return e.Types.Where(t => t != null);
                }
            }
            catch
            {
                return Enumerable.Empty<Type>();
            }
        }

        internal static Dictionary<Type, Dictionary<string, FieldInfo>> s_cachedFieldInfos = new Dictionary<Type, Dictionary<string, FieldInfo>>();

        public static FieldInfo GetFieldInfo(Type type, string fieldName)
        {
            if (!s_cachedFieldInfos.ContainsKey(type))
                s_cachedFieldInfos.Add(type, new Dictionary<string, FieldInfo>());

            if (!s_cachedFieldInfos[type].ContainsKey(fieldName))
                s_cachedFieldInfos[type].Add(fieldName, type.GetField(fieldName, AllFlags));

            return s_cachedFieldInfos[type][fieldName];
        }

        internal static Dictionary<Type, Dictionary<string, PropertyInfo>> s_cachedPropInfos = new Dictionary<Type, Dictionary<string, PropertyInfo>>();

        public static PropertyInfo GetPropertyInfo(Type type, string propertyName)
        {
            if (!s_cachedPropInfos.ContainsKey(type))
                s_cachedPropInfos.Add(type, new Dictionary<string, PropertyInfo>());

            if (!s_cachedPropInfos[type].ContainsKey(propertyName))
                s_cachedPropInfos[type].Add(propertyName, type.GetProperty(propertyName, AllFlags));

            return s_cachedPropInfos[type][propertyName];
        }

        internal static Dictionary<Type, Dictionary<string, MethodInfo>> s_cachedMethodInfos = new Dictionary<Type, Dictionary<string, MethodInfo>>();

        public static MethodInfo GetMethodInfo(Type type, string methodName, Type[] argumentTypes)
        {
            if (!s_cachedMethodInfos.ContainsKey(type))
                s_cachedMethodInfos.Add(type, new Dictionary<string, MethodInfo>());

            var sig = methodName;

            if (argumentTypes != null)
            {
                sig += "(";
                for (int i = 0; i < argumentTypes.Length; i++)
                {
                    if (i > 0)
                        sig += ",";
                    sig += argumentTypes[i].FullName;
                }
                sig += ")";
            }

            try
            {
                if (!s_cachedMethodInfos[type].ContainsKey(sig))
                {
                    if (argumentTypes != null)
                        s_cachedMethodInfos[type].Add(sig, type.GetMethod(methodName, AllFlags, null, argumentTypes, null));
                    else
                        s_cachedMethodInfos[type].Add(sig, type.GetMethod(methodName, AllFlags));
                }

                return s_cachedMethodInfos[type][sig];
            }
            catch (AmbiguousMatchException)
            {
                ExplorerCore.LogWarning($"AmbiguousMatchException trying to get method '{sig}'");
                return null;
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"{e.GetType()} trying to get method '{sig}': {e.Message}\r\n{e.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Helper to display a simple "{ExceptionType}: {Message}" of the exception, and optionally use the inner-most exception.
        /// </summary>
        /// <param name="e">The Exception to convert to string.</param>
        /// <param name="innerMost">Should the inner-most Exception of the stack be used? If false, the Exception you provided will be used directly.</param>
        /// <returns>The exception to string.</returns>
        public static string ReflectionExToString(this Exception e, bool innerMost = false)
        {
            if (innerMost)
            {
                while (e.InnerException != null)
                {
                    if (e.InnerException is System.Runtime.CompilerServices.RuntimeWrappedException)
                        break;

                    e = e.InnerException;
                }
            }

            return $"{e.GetType()}: {e.Message}";
        }
    }
}
