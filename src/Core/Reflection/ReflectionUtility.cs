using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BF = System.Reflection.BindingFlags;
using UnityExplorer.Core.Runtime;
using System.Text;
using UnityEngine;
using UnityExplorer.Core.Config;

namespace UnityExplorer
{

    public class ReflectionUtility
    {
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

        public static Action<Type> OnTypeLoaded;

        /// <summary>Key: Type.FullName</summary>
        public static readonly SortedDictionary<string, Type> AllTypes = new SortedDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        private static string[] allTypesArray;
        public static string[] GetTypeNameArray()
        {
            if (allTypesArray == null || allTypesArray.Length != AllTypes.Count)
            {
                allTypesArray = new string[AllTypes.Count];
                int i = 0;
                foreach (var name in AllTypes.Keys)
                {
                    allTypesArray[i] = name;
                    i++;
                }
            }
            return allTypesArray;
        }

        private static void SetupTypeCache()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                CacheTypes(asm);

            AppDomain.CurrentDomain.AssemblyLoad += AssemblyLoaded;
        }

        private static void AssemblyLoaded(object sender, AssemblyLoadEventArgs args)
        {
            if (args.LoadedAssembly == null || args.LoadedAssembly.GetName().Name == "completions")
                return;

            CacheTypes(args.LoadedAssembly);
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
                    //allTypeNames.Add(type.FullName);
                }

                OnTypeLoaded?.Invoke(type);

                foreach (var key in typeInheritance.Keys)
                {
                    try
                    {
                        var baseType = AllTypes[key];
                        if (baseType.IsAssignableFrom(type) && !typeInheritance[key].Contains(type))
                            typeInheritance[key].Add(type);
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
            => obj?.GetType();

        // Force-casting an object to a type
        internal virtual object Internal_TryCast(object obj, Type castTo)
            => obj;

        // Processing deobfuscated type names in strings
        public static string ProcessTypeInString(Type type, string theString)
            => Instance.Internal_ProcessTypeInString(theString, type);

        internal virtual string Internal_ProcessTypeInString(string theString, Type type)
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
        internal static readonly Dictionary<string, Type[]> baseTypes = new Dictionary<string, Type[]>();

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

            if (baseTypes.TryGetValue(name, out Type[] ret))
                return ret;

            List<Type> list = new List<Type>();

            while (type != null)
            {
                list.Add(type);
                type = type.BaseType;
            }

            ret = list.ToArray();

            baseTypes.Add(name, ret);

            return ret;
        }

#endregion

        #region Type and Generic Parameter implementation cache

        // cache for GetImplementationsOf
        internal static readonly Dictionary<string, HashSet<Type>> typeInheritance = new Dictionary<string, HashSet<Type>>();
        internal static readonly Dictionary<string, HashSet<Type>> genericParameterInheritance = new Dictionary<string, HashSet<Type>>();

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
        public static HashSet<Type> GetImplementationsOf(Type baseType, bool allowAbstract, bool allowGeneric, bool allowRecursive = true)
        {
            var key = GetImplementationKey(baseType);

            int count = AllTypes.Count;
            HashSet<Type> ret;
            if (!baseType.IsGenericParameter)
                ret = GetImplementations(key, baseType, allowAbstract, allowGeneric);
            else
                ret = GetGenericParameterImplementations(key, baseType, allowAbstract, allowGeneric);

            // types were resolved during the parse, do it again if we're not already rebuilding.
            if (allowRecursive && AllTypes.Count != count)
            {
                ret = GetImplementationsOf(baseType, allowAbstract, allowGeneric, false);
            }

            return ret;
        }

        private static HashSet<Type> GetImplementations(string key, Type baseType, bool allowAbstract, bool allowGeneric)
        {
            if (!typeInheritance.ContainsKey(key))
            {
                var set = new HashSet<Type>();
                var names = GetTypeNameArray();
                for (int i = 0; i < names.Length; i++)
                {
                    var name = names[i];
                    try
                    {
                        var type = AllTypes[name];

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

                typeInheritance.Add(key, set);
            }

            return typeInheritance[key];
        }

        private static HashSet<Type> GetGenericParameterImplementations(string key, Type baseType, bool allowAbstract, bool allowGeneric)
        {
            if (!genericParameterInheritance.ContainsKey(key))
            {
                var set = new HashSet<Type>();

                var names = GetTypeNameArray();
                for (int i = 0; i < names.Length; i++)
                {
                    var name = names[i];
                    try
                    {
                        var type = AllTypes[name];

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

                genericParameterInheritance.Add(key, set);
            }

            return genericParameterInheritance[key];
        }

#endregion

        #region Internal MemberInfo Cache

        internal static Dictionary<Type, Dictionary<string, FieldInfo>> fieldInfos = new Dictionary<Type, Dictionary<string, FieldInfo>>();

        public static FieldInfo GetFieldInfo(Type type, string fieldName)
        {
            if (!fieldInfos.ContainsKey(type))
                fieldInfos.Add(type, new Dictionary<string, FieldInfo>());

            if (!fieldInfos[type].ContainsKey(fieldName))
                fieldInfos[type].Add(fieldName, type.GetField(fieldName, FLAGS));

            return fieldInfos[type][fieldName];
        }

        internal static Dictionary<Type, Dictionary<string, PropertyInfo>> propertyInfos = new Dictionary<Type, Dictionary<string, PropertyInfo>>();

        public static PropertyInfo GetPropertyInfo(Type type, string propertyName)
        {
            if (!propertyInfos.ContainsKey(type))
                propertyInfos.Add(type, new Dictionary<string, PropertyInfo>());

            if (!propertyInfos[type].ContainsKey(propertyName))
                propertyInfos[type].Add(propertyName, type.GetProperty(propertyName, FLAGS));

            return propertyInfos[type][propertyName];
        }

        internal static Dictionary<Type, Dictionary<string, MethodInfo>> methodInfos = new Dictionary<Type, Dictionary<string, MethodInfo>>();

        public static MethodInfo GetMethodInfo(Type type, string methodName)
            => GetMethodInfo(type, methodName, ArgumentUtility.EmptyTypes, false);

        public static MethodInfo GetMethodInfo(Type type, string methodName, Type[] argumentTypes, bool cacheAmbiguous = false)
        {
            if (!methodInfos.ContainsKey(type))
                methodInfos.Add(type, new Dictionary<string, MethodInfo>());

            if (cacheAmbiguous)
            {
                methodName += "|";
                foreach (var arg in argumentTypes)
                    methodName += arg.FullName + ",";
            }

            try
            {
                if (!methodInfos[type].ContainsKey(methodName))
                {
                    if (argumentTypes != null)
                        methodInfos[type].Add(methodName, type.GetMethod(methodName, FLAGS, null, argumentTypes, null));
                    else
                        methodInfos[type].Add(methodName, type.GetMethod(methodName, FLAGS));
                }

                return methodInfos[type][methodName];
            }
            catch (AmbiguousMatchException)
            {
                ExplorerCore.LogWarning($"AmbiguousMatchException trying to get method '{methodName}'");
                return null;
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"{e.GetType()} trying to get method '{methodName}': {e.Message}\r\n{e.StackTrace}");
                return null;
            }
        }

        #endregion

        #region Reflection Blacklist

        public virtual string DefaultReflectionBlacklist => string.Empty;

        public static void LoadBlacklistString(string blacklist)
        {
            if (string.Equals(blacklist, "DEFAULT", StringComparison.InvariantCultureIgnoreCase))
                blacklist = Instance.DefaultReflectionBlacklist;

            if (string.IsNullOrEmpty(blacklist))
                return;

            var sigs = blacklist.Split(';');
            foreach (var sig in sigs)
            {
                var s = sig.Trim();
                if (string.IsNullOrEmpty(s))
                    continue;
                if (!currentBlacklist.Contains(s))
                    currentBlacklist.Add(s);
            }

            Mono.CSharp.IL2CPP.Blacklist.SignatureBlacklist = currentBlacklist;
        }

        public static bool IsBlacklisted(MemberInfo member)
        {
            if (string.IsNullOrEmpty(member.DeclaringType?.Namespace))
                return false;

            var sig = $"{member.DeclaringType.FullName}.{member.Name}";
            return currentBlacklist.Contains(sig);
        }

        private static readonly HashSet<string> currentBlacklist = new HashSet<string>();

        #endregion

    }
}
