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

    public class ReflectionUtility
    {
        // The Instance and instance methods are not for public use, they're only so IL2CPP can override.
        // This class and the Extensions class expose static methods to use instead.

        public const BF FLAGS = BF.Public | BF.Instance | BF.NonPublic | BF.Static;

        internal static readonly ReflectionUtility Instance =
#if CPP
                new Il2CppReflection();
#else
                new ReflectionUtility();
#endif

        static ReflectionUtility()
        {
            SetupTypeCache();
        }

        #region Type cache

        /// <summary>Key: Type.FullName</summary>
        public static readonly SortedDictionary<string, Type> AllTypes = new SortedDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<string> allTypeNames = new List<string>();

        private static void SetupTypeCache()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                CacheTypes(asm);

            allTypeNames.Sort();

            AppDomain.CurrentDomain.AssemblyLoad += AssemblyLoaded;
        }

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

        #endregion

        /// <summary>
        /// Find a <see cref="Type"/> in the current AppDomain whose <see cref="Type.FullName"/> matches the provided <paramref name="fullName"/>.
        /// </summary>
        /// <param name="fullName">The <see cref="Type.FullName"/> you want to search for - case sensitive and full matches only.</param>
        /// <returns>The Type if found, otherwise null.</returns>
        public static Type GetTypeByName(string fullName)
            => Instance.Internal_GetTypeByName(fullName);

        internal virtual Type Internal_GetTypeByName(string fullName)
        {
            AllTypes.TryGetValue(fullName, out Type type);
            return type;
        }

        // Getting the actual type of an object
        internal virtual Type Internal_GetActualType(object obj)
            => obj.GetType();

        // Force-casting an object to a type
        internal virtual object Internal_TryCast(object obj, Type castTo)
            => obj;

        // Processing deobfuscated type names in strings
        public static string ProcessTypeInString(Type type, string theString, ref string typeName)
            => Instance.Internal_ProcessTypeInString(theString, type, ref typeName);

        internal virtual string Internal_ProcessTypeInString(string theString, Type type, ref string typeName)
            => theString;

        // Force loading modules
        public static bool LoadModule(string moduleName)
            => Instance.Internal_LoadModule(moduleName);

        internal virtual bool Internal_LoadModule(string moduleName) 
            => false;

        public static void FindSingleton(string[] possibleNames, Type type, BindingFlags flags, List<object> instances)
            => Instance.Internal_FindSingleton(possibleNames, type, flags, instances);

        internal virtual void Internal_FindSingleton(string[] possibleNames, Type type, BindingFlags flags, List<object> instances)
        {
            // Look for a typical Instance backing field.
            FieldInfo fi;
            foreach (var name in possibleNames)
            {
                fi = type.GetField(name, flags);
                if (fi != null)
                {
                    var instance = fi.GetValue(null);
                    if (instance != null)
                    {
                        instances.Add(instance);
                        return;
                    }
                }
            }
        }

        // Universal helpers

        #region Type inheritance cache

        // cache for GetBaseTypes
        internal static readonly Dictionary<string, Type[]> s_cachedBaseTypes = new Dictionary<string, Type[]>();

        /// <summary>
        /// Get all base types of the provided Type, including itself.
        /// </summary>
        public static Type[] GetAllBaseTypes(object obj) => GetAllBaseTypes(obj?.GetActualType());

        /// <summary>
        /// Get all base types of the provided Type, including itself.
        /// </summary>
        public static Type[] GetAllBaseTypes(Type type)
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

#endregion


        #region Type and Generic Parameter implementation cache

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
        public static HashSet<Type> GetImplementationsOf(Type baseType, bool allowAbstract, bool allowGeneric)
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

#endregion


        #region Internal MemberInfo Cache

        internal static Dictionary<Type, Dictionary<string, FieldInfo>> s_cachedFieldInfos = new Dictionary<Type, Dictionary<string, FieldInfo>>();

        public static FieldInfo GetFieldInfo(Type type, string fieldName)
        {
            if (!s_cachedFieldInfos.ContainsKey(type))
                s_cachedFieldInfos.Add(type, new Dictionary<string, FieldInfo>());

            if (!s_cachedFieldInfos[type].ContainsKey(fieldName))
                s_cachedFieldInfos[type].Add(fieldName, type.GetField(fieldName, FLAGS));

            return s_cachedFieldInfos[type][fieldName];
        }

        internal static Dictionary<Type, Dictionary<string, PropertyInfo>> s_cachedPropInfos = new Dictionary<Type, Dictionary<string, PropertyInfo>>();

        public static PropertyInfo GetPropertyInfo(Type type, string propertyName)
        {
            if (!s_cachedPropInfos.ContainsKey(type))
                s_cachedPropInfos.Add(type, new Dictionary<string, PropertyInfo>());

            if (!s_cachedPropInfos[type].ContainsKey(propertyName))
                s_cachedPropInfos[type].Add(propertyName, type.GetProperty(propertyName, FLAGS));

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
                        s_cachedMethodInfos[type].Add(sig, type.GetMethod(methodName, FLAGS, null, argumentTypes, null));
                    else
                        s_cachedMethodInfos[type].Add(sig, type.GetMethod(methodName, FLAGS));
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

#endregion

    }
}
