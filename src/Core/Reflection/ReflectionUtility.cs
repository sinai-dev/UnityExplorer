using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Runtime;
using BF = System.Reflection.BindingFlags;

namespace UnityExplorer
{
    public class ReflectionUtility
    {
        public const BF FLAGS = BF.Public | BF.Instance | BF.NonPublic | BF.Static;

        internal static ReflectionUtility Instance;

        public static void Init()
        {
            Instance =
#if CPP
                new Il2CppReflection();
#else
                new ReflectionUtility();
#endif
            Instance.Initialize();
        }

        protected virtual void Initialize()
        {
            SetupTypeCache();

            LoadBlacklistString(ConfigManager.Reflection_Signature_Blacklist.Value);
            ConfigManager.Reflection_Signature_Blacklist.OnValueChanged += LoadBlacklistString;
        }

        #region Type cache

        public static Action<Type> OnTypeLoaded;

        /// <summary>Key: Type.FullName</summary>
        protected static readonly SortedDictionary<string, Type> AllTypes = new SortedDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        public static readonly List<string> AllNamespaces = new List<string>();
        private static readonly HashSet<string> uniqueNamespaces = new HashSet<string>();

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
            float start = Time.realtimeSinceStartup;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                CacheTypes(asm);

            AppDomain.CurrentDomain.AssemblyLoad += AssemblyLoaded;

            ExplorerCore.Log($"Cached AppDomain assemblies in {Time.realtimeSinceStartup - start} seconds");
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
                if (!string.IsNullOrEmpty(type.Namespace) && !uniqueNamespaces.Contains(type.Namespace))
                {
                    uniqueNamespaces.Add(type.Namespace);
                    int i = 0;
                    while (i < AllNamespaces.Count)
                    {
                        if (type.Namespace.CompareTo(AllNamespaces[i]) < 0)
                            break;
                        i++;
                    }
                    AllNamespaces.Insert(i, type.Namespace);
                }

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

        //// Force loading modules
        //public static bool LoadModule(string moduleName)
        //    => Instance.Internal_LoadModule(moduleName);
        //
        //internal virtual bool Internal_LoadModule(string moduleName) 
        //    => false;

        // Singleton finder

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

        public virtual string[] DefaultReflectionBlacklist => new string[0];

        public static void LoadBlacklistString(string blacklist)
        {
            try
            {
                if (string.IsNullOrEmpty(blacklist) && !Instance.DefaultReflectionBlacklist.Any())
                    return;

                try
                {
                    var sigs = blacklist.Split(';');
                    foreach (var sig in sigs)
                    {
                        var s = sig.Trim();
                        if (string.IsNullOrEmpty(s))
                            continue;
                        if (!currentBlacklist.Contains(s))
                            currentBlacklist.Add(s);
                    }
                }
                catch (Exception ex)
                {
                    ExplorerCore.LogWarning($"Exception parsing blacklist string: {ex.ReflectionExToString()}");
                }

                foreach (var sig in Instance.DefaultReflectionBlacklist)
                {
                    if (!currentBlacklist.Contains(sig))
                        currentBlacklist.Add(sig);
                }

                Mono.CSharp.IL2CPP.Blacklist.SignatureBlacklist = currentBlacklist;
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception setting up reflection blacklist: {ex.ReflectionExToString()}");
            }
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


        // Temp fix for IL2CPP until interface support improves

        // IsEnumerable 

        public static bool IsEnumerable(Type type) => Instance.Internal_IsEnumerable(type);

        protected virtual bool Internal_IsEnumerable(Type type)
        {
            return typeof(IEnumerable).IsAssignableFrom(type);
        }

        // TryGetEnumerator (list)

        public static bool TryGetEnumerator(object list, out IEnumerator enumerator)
            => Instance.Internal_TryGetEnumerator(list, out enumerator);

        protected virtual bool Internal_TryGetEnumerator(object list, out IEnumerator enumerator)
        {
            enumerator = (list as IEnumerable).GetEnumerator();
            return true;
        }

        // TryGetEntryType

        public static bool TryGetEntryType(Type enumerableType, out Type type)
            => Instance.Internal_TryGetEntryType(enumerableType, out type);

        protected virtual bool Internal_TryGetEntryType(Type enumerableType, out Type type)
        {
            // Check for arrays
            if (enumerableType.IsArray)
            {
                type = enumerableType.GetElementType();
                return true;
            }

            // Check for implementation of IEnumerable<T>, IList<T> or ICollection<T>
            foreach (var t in enumerableType.GetInterfaces())
            {
                if (t.IsGenericType)
                {
                    var typeDef = t.GetGenericTypeDefinition();
                    if (typeDef == typeof(IEnumerable<>) || typeDef == typeof(IList<>) || typeDef == typeof(ICollection<>))
                    {
                        type = t.GetGenericArguments()[0];
                        return true;
                    }
                }
            }

            // Unable to determine any generic element type, just use object.
            type = typeof(object);
            return false;
        }

        // IsDictionary

        public static bool IsDictionary(Type type) => Instance.Internal_IsDictionary(type);

        protected virtual bool Internal_IsDictionary(Type type)
        {
            return typeof(IDictionary).IsAssignableFrom(type);
        }

        // TryGetEnumerator (dictionary)

        public static bool TryGetDictEnumerator(object dictionary, out IEnumerator<DictionaryEntry> dictEnumerator)
            => Instance.Internal_TryGetDictEnumerator(dictionary, out dictEnumerator);

        protected virtual bool Internal_TryGetDictEnumerator(object dictionary, out IEnumerator<DictionaryEntry> dictEnumerator)
        {
            dictEnumerator = EnumerateDictionary((IDictionary)dictionary);
            return true;
        }

        private IEnumerator<DictionaryEntry> EnumerateDictionary(IDictionary dict)
        {
            var enumerator = dict.GetEnumerator();
            while (enumerator.MoveNext())
            {
                yield return new DictionaryEntry(enumerator.Key, enumerator.Value);
            }
        }

        // TryGetEntryTypes

        public static bool TryGetEntryTypes(Type dictionaryType, out Type keys, out Type values)
            => Instance.Internal_TryGetEntryTypes(dictionaryType, out keys, out values);

        protected virtual bool Internal_TryGetEntryTypes(Type dictionaryType, out Type keys, out Type values)
        {
            foreach (var t in dictionaryType.GetInterfaces())
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    var args = t.GetGenericArguments();
                    keys = args[0];
                    values = args[1];
                    return true;
                }
            }

            keys = typeof(object);
            values = typeof(object);
            return false;
        }
    }
}
