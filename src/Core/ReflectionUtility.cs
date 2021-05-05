using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BF = System.Reflection.BindingFlags;
using UnityExplorer.Core.Runtime;
using System.Text;

namespace UnityExplorer
{
    public static class ReflectionUtility
    {
        static ReflectionUtility()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                CacheTypes(asm);

            allTypeNames.Sort();

            AppDomain.CurrentDomain.AssemblyLoad += AssemblyLoaded;
        }

        /// <summary>Key: Type.FullName</summary>
        public static readonly SortedDictionary<string, Type> AllTypes = new SortedDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<string> allTypeNames = new List<string>();

        private static void AssemblyLoaded(object sender, AssemblyLoadEventArgs args)
        {
            if (args.LoadedAssembly == null)
                return;

            CacheTypes(args.LoadedAssembly);
            allTypeNames.Sort();
        }

        private static void CacheTypes(Assembly asm)
        {
            foreach (var type in asm.TryGetTypes())
            {
                if (AllTypes.ContainsKey(type.FullName))
                    AllTypes[type.FullName] = type;
                else
                { 
                    AllTypes.Add(type.FullName, type);
                    allTypeNames.Add(type.FullName);
                }

                foreach (var key in s_cachedTypeInheritance.Keys)
                {
                    try
                    {
                        var baseType = AllTypes[key];
                        if (baseType.IsAssignableFrom(type) && !s_cachedTypeInheritance[key].Contains(type))
                            s_cachedTypeInheritance[key].Add(type);
                    }
                    catch { }
                }
            }
        }

        public const BF AllFlags = BF.Public | BF.Instance | BF.NonPublic | BF.Static;

        public static bool ValueEqual<T>(this T objA, T objB)
        {
            return (objA == null && objB == null) || (objA != null && objA.Equals(objB));
        }

        public static bool ReferenceEqual(this object objA, object objB)
        {
            if (object.ReferenceEquals(objA, objB))
                return true;

            if (objA is UnityEngine.Object unityA && objB is UnityEngine.Object unityB)
            {
                if (unityA && unityB && unityA.m_CachedPtr == unityB.m_CachedPtr)
                    return true;
            }

#if CPP
            if (objA is Il2CppSystem.Object cppA && objB is Il2CppSystem.Object cppB
                && cppA.Pointer == cppB.Pointer)
                return true;
#endif

            return false;
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
        public static object TryCast(this object obj) => ReflectionProvider.Instance.Cast(obj, GetActualType(obj));

        /// <summary>
        /// Cast an object to a Type, if possible.
        /// </summary>
        /// <param name="obj">The object to cast</param>
        /// <param name="castTo">The Type to cast to </param>
        /// <returns>The object, cast to the Type provided if possible, otherwise the original object.</returns>
        public static object TryCast(this object obj, Type castTo) => ReflectionProvider.Instance.Cast(obj, castTo);

        /// <summary>Try to cast the object to the type.</summary>
        public static T TryCast<T>(this object obj) => ReflectionProvider.Instance.TryCast<T>(obj);

        /// <summary>
        /// Check if the provided Type is assignable to IEnumerable.
        /// </summary>
        /// <param name="t">The Type to check</param>
        /// <returns>True if the Type is assignable to IEnumerable, otherwise false.</returns>
        public static bool IsEnumerable(this Type t)
            => !typeof(UnityEngine.Transform).IsAssignableFrom(t)
            && ReflectionProvider.Instance.IsAssignableFrom(typeof(IEnumerable), t);

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
            

            AllTypes.TryGetValue(fullName, out Type type);
            return type;
        }

        // cache for GetBaseTypes
        internal static readonly Dictionary<string, Type[]> s_cachedBaseTypes = new Dictionary<string, Type[]>();

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

            if (s_cachedBaseTypes.TryGetValue(name, out Type[] ret))
                return ret;

            List<Type> list = new List<Type>();

            while (type != null)
            {
                list.Add(type);
                type = type.BaseType;
            }

            ret = list.ToArray();

            s_cachedBaseTypes.Add(name, ret);

            return ret;
        }

        // cache for GetImplementationsOf
        internal static readonly Dictionary<string, HashSet<Type>> s_cachedTypeInheritance = new Dictionary<string, HashSet<Type>>();
        internal static readonly Dictionary<string, HashSet<Type>> s_cachedGenericParameterInheritance = new Dictionary<string, HashSet<Type>>();

        public static string GetImplementationKey(Type type)
        {
            if (!type.IsGenericParameter)
                return type.FullName;
            else
            {
                var sb = new StringBuilder();
                sb.Append(type.GenericParameterAttributes)
                    .Append('|');
                foreach (var c in type.GetGenericParameterConstraints())
                    sb.Append(c.FullName).Append(',');
                return sb.ToString();
            }
        }

        /// <summary>
        /// Get all non-abstract implementations of the provided type (include itself, if not abstract) in the current AppDomain.
        /// Also works for generic parameters by analyzing the constraints.
        /// </summary>
        /// <param name="baseType">The base type, which can optionally be abstract / interface.</param>
        /// <returns>All implementations of the type in the current AppDomain.</returns>
        public static HashSet<Type> GetImplementationsOf(this Type baseType, bool allowAbstract, bool allowGeneric)
        {
            var key = GetImplementationKey(baseType); //baseType.FullName;

            if (!baseType.IsGenericParameter)
                return GetImplementations(key, baseType, allowAbstract, allowGeneric);
            else
                return GetGenericParameterImplementations(key, baseType, allowAbstract, allowGeneric);
        }

        private static HashSet<Type> GetImplementations(string key, Type baseType, bool allowAbstract, bool allowGeneric)
        {
            if (!s_cachedTypeInheritance.ContainsKey(key))
            {
                var set = new HashSet<Type>();
                for (int i = 0; i < allTypeNames.Count; i++)
                {
                    var type = AllTypes[allTypeNames[i]];
                    //type = ReflectionProvider.Instance.GetDeobfuscatedType(type);
                    try
                    {
                        if (set.Contains(type)
                        || (type.IsAbstract && type.IsSealed) // ignore static classes
                        || (!allowAbstract && type.IsAbstract)
                        || (!allowGeneric && (type.IsGenericType || type.IsGenericTypeDefinition)))
                            continue;

                        if (type.FullName.Contains("PrivateImplementationDetails")
                            || type.FullName.Contains("DisplayClass")
                            || type.FullName.Contains('<'))
                            continue;

                        if (baseType.IsAssignableFrom(type) && !set.Contains(type))
                            set.Add(type);
                    }
                    catch { }
                }

                //set.

                s_cachedTypeInheritance.Add(key, set);
            }

            return s_cachedTypeInheritance[key];
        }

        private static HashSet<Type> GetGenericParameterImplementations(string key, Type baseType, bool allowAbstract, bool allowGeneric)
        {
            if (!s_cachedGenericParameterInheritance.ContainsKey(key))
            {
                var set = new HashSet<Type>();

                for (int i = 0; i < allTypeNames.Count; i++)
                {
                    var type = AllTypes[allTypeNames[i]];
                    try
                    {
                        if (set.Contains(type)
                        || (type.IsAbstract && type.IsSealed) // ignore static classes
                        || (!allowAbstract && type.IsAbstract)
                        || (!allowGeneric && (type.IsGenericType || type.IsGenericTypeDefinition)))
                            continue;

                        if (type.FullName.Contains("PrivateImplementationDetails")
                            || type.FullName.Contains("DisplayClass")
                            || type.FullName.Contains('<'))
                            continue;

                        if (baseType.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint)
                            && type.IsClass)
                            continue;

                        if (baseType.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint)
                            && type.IsValueType)
                            continue;

                        if (baseType.GetGenericParameterConstraints().Any(it => !it.IsAssignableFrom(type)))
                            continue;

                        set.Add(type);
                    }
                    catch { }
                }

                s_cachedGenericParameterInheritance.Add(key, set);
            }

            return s_cachedGenericParameterInheritance[key];
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
        public static string ReflectionExToString(this Exception e, bool innerMost = true)
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
