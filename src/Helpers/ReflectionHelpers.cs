using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BF = System.Reflection.BindingFlags;
using System.Diagnostics.CodeAnalysis;
#if CPP
using CppType = Il2CppSystem.Type;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using System.Runtime.InteropServices;
#endif

namespace UnityExplorer.Helpers
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "External methods")]
    public static class ReflectionHelpers
    {
        public static BF CommonFlags = BF.Public | BF.Instance | BF.NonPublic | BF.Static;

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
        public static Type[] GetAllBaseTypes(object obj) => GetAllBaseTypes(GetActualType(obj));

        /// <summary>
        /// Get all base types of the provided Type, including itself.
        /// </summary>
        public static Type[] GetAllBaseTypes(Type type)
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
        /// Helper for IL2CPP to get the underlying true Type (Unhollowed) of the object.
        /// </summary>
        /// <param name="obj">The object to get the true Type for.</param>
        /// <returns>The most accurate Type of the object which could be identified.</returns>
        public static Type GetActualType(this object obj)
        {
            if (obj == null)
                return null;

            var type = obj.GetType();
#if CPP
            if (obj is Il2CppSystem.Object cppObject)
            {
                if (cppObject is CppType)
                    return typeof(CppType);

                if (!string.IsNullOrEmpty(type.Namespace))
                {
                    // Il2CppSystem-namespace objects should just return GetType,
                    // because using GetIl2CppType returns the System namespace type instead.
                    if (type.Namespace.StartsWith("System.") || type.Namespace.StartsWith("Il2CppSystem."))
                        return cppObject.GetType();
                }

                var cppType = cppObject.GetIl2CppType();

                // check if type is injected
                IntPtr classPtr = il2cpp_object_get_class(cppObject.Pointer);
                if (RuntimeSpecificsStore.IsInjected(classPtr))
                {
                    var typeByName = GetTypeByName(cppType.FullName);
                    if (typeByName != null)
                        return typeByName;
                }

                // this should be fine for all other il2cpp objects
                var getType = GetMonoType(cppType);
                if (getType != null)
                    return getType;
            }
#endif
            return type;
        }

#if CPP
        // caching for GetMonoType
        private static readonly Dictionary<string, Type> Il2CppToMonoType = new Dictionary<string, Type>();

        // keep unobfuscated type name cache, used to display proper name.
        internal static Dictionary<string, string> UnobfuscatedTypeNames = new Dictionary<string, string>();

        /// <summary>
        /// Try to get the Mono (Unhollowed) Type representation of the provided <see cref="Il2CppSystem.Type"/>.
        /// </summary>
        /// <param name="cppType">The Cpp Type you want to convert to Mono.</param>
        /// <returns>The Mono Type if found, otherwise null.</returns>
        public static Type GetMonoType(CppType cppType)
        {
            string name = cppType.AssemblyQualifiedName;

            if (Il2CppToMonoType.ContainsKey(name))
                return Il2CppToMonoType[name];

            Type ret = Type.GetType(name);
            
            if (ret == null)
            {
                string baseName = cppType.FullName;
                string baseAssembly = cppType.Assembly.GetName().name;

                ret = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .FirstOrDefault(a 
                        => a.GetName().Name == baseAssembly)?
                    .TryGetTypes()
                    .FirstOrDefault(t 
                        => t.CustomAttributes.Any(ca 
                            => ca.AttributeType.Name == "ObfuscatedNameAttribute"
                               && (string)ca.ConstructorArguments[0].Value == baseName));

                if (ret != null)
                {
                    // unobfuscated type was found, add to cache.
                    UnobfuscatedTypeNames.Add(cppType.FullName, ret.FullName);
                }
            }

            Il2CppToMonoType.Add(name, ret);

            return ret;
        }

        // cached class pointers for Il2CppCast
        private static readonly Dictionary<string, IntPtr> CppClassPointers = new Dictionary<string, IntPtr>();

        /// <summary>
        /// Attempt to cast the object to its underlying type.
        /// </summary>
        /// <param name="obj">The object you want to cast.</param>
        /// <returns>The object, as the underlying type if successful or the input value if not.</returns>
        public static object Il2CppCast(this object obj) => Il2CppCast(obj, GetActualType(obj));

        /// <summary>
        /// Attempt to cast the object to the provided type.
        /// </summary>
        /// <param name="obj">The object you want to cast.</param>
        /// <param name="castTo">The Type you want to cast to.</param>
        /// <returns>The object, as the type (or a normal C# object) if successful or the input value if not.</returns>
        public static object Il2CppCast(this object obj, Type castTo)
        {
            if (!(obj is Il2CppSystem.Object ilObj))
                return obj;

            if (!Il2CppTypeNotNull(castTo, out IntPtr castToPtr))
                return obj;

            IntPtr castFromPtr = il2cpp_object_get_class(ilObj.Pointer);

            if (!il2cpp_class_is_assignable_from(castToPtr, castFromPtr))
                return obj;

            if (RuntimeSpecificsStore.IsInjected(castToPtr))
                return UnhollowerBaseLib.Runtime.ClassInjectorBase.GetMonoObjectFromIl2CppPointer(ilObj.Pointer);

            return Activator.CreateInstance(castTo, ilObj.Pointer);
        }

        /// <summary>
        /// Get the Il2Cpp Class Pointer for the provided Mono (Unhollowed) Type.
        /// </summary>
        /// <param name="type">The Mono/Unhollowed Type you want the Il2Cpp Class Pointer for.</param>
        /// <returns>True if successful, false if not.</returns>
        public static bool Il2CppTypeNotNull(Type type) => Il2CppTypeNotNull(type, out _);

        /// <summary>
        /// Get the Il2Cpp Class Pointer for the provided Mono (Unhollowed) Type.
        /// </summary>
        /// <param name="type">The Mono/Unhollowed Type you want the Il2Cpp Class Pointer for.</param>
        /// <param name="il2cppPtr">The IntPtr for the Il2Cpp class, or IntPtr.Zero if not found.</param>
        /// <returns>True if successful, false if not.</returns>
        public static bool Il2CppTypeNotNull(Type type, out IntPtr il2cppPtr)
        {
            if (CppClassPointers.TryGetValue(type.AssemblyQualifiedName, out il2cppPtr))
                return il2cppPtr != IntPtr.Zero;

            il2cppPtr = (IntPtr)typeof(Il2CppClassPointerStore<>)
                    .MakeGenericType(new Type[] { type })
                    .GetField("NativeClassPtr", BF.Public | BF.Static)
                    .GetValue(null);

            CppClassPointers.Add(type.AssemblyQualifiedName, il2cppPtr);

            return il2cppPtr != IntPtr.Zero;
        }

        // Extern C++ methods 
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool il2cpp_class_is_assignable_from(IntPtr klass, IntPtr oklass);

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr il2cpp_object_get_class(IntPtr obj);

        // cached il2cpp unbox methods
        internal static readonly Dictionary<string, MethodInfo> s_unboxMethods = new Dictionary<string, MethodInfo>();

        /// <summary>
        /// Attempt to unbox the object to the underlying struct type.
        /// </summary>
        /// <param name="obj">The object which is a struct underneath.</param>
        /// <returns>The struct if successful, otherwise null.</returns>
        public static object Unbox(this object obj) => Unbox(obj, GetActualType(obj));

        /// <summary>
        /// Attempt to unbox the object to the struct type.
        /// </summary>
        /// <param name="obj">The object which is a struct underneath.</param>
        /// <param name="type">The type of the struct you want to unbox to.</param>
        /// <returns>The struct if successful, otherwise null.</returns>
        public static object Unbox(this object obj, Type type)
        {
            if (!type.IsValueType)
                return null;

            if (!(obj is Il2CppSystem.Object))
                return obj;

            var name = type.AssemblyQualifiedName;

            if (!s_unboxMethods.ContainsKey(name))
            {
                s_unboxMethods.Add(name, typeof(Il2CppObjectBase)
                                            .GetMethod("Unbox")
                                            .MakeGenericMethod(type));
            }

            return s_unboxMethods[name].Invoke(obj, new object[0]);
        }

#endif

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

        // Helper for IL2CPP to check if a Type is assignable to IEnumerable

#if CPP
        internal static IntPtr s_cppEnumerableClassPtr;
#endif

        /// <summary>
        /// Check if the provided Type is assignable to IEnumerable.
        /// </summary>
        /// <param name="t">The Type to check</param>
        /// <returns>True if the Type is assignable to IEnumerable, otherwise false.</returns>
        public static bool IsEnumerable(Type t)
        {
            if (typeof(IEnumerable).IsAssignableFrom(t))
                return true;
#if CPP
            try
            {
                if (s_cppEnumerableClassPtr == IntPtr.Zero)
                    Il2CppTypeNotNull(typeof(Il2CppSystem.Collections.IEnumerable), out s_cppEnumerableClassPtr);

                if (s_cppEnumerableClassPtr != IntPtr.Zero
                    && Il2CppTypeNotNull(t, out IntPtr classPtr)
                    && il2cpp_class_is_assignable_from(s_cppEnumerableClassPtr, classPtr))
                {
                    return true;
                }
            }
            catch { }
#endif
            return false;
        }

        // Helper for IL2CPP to check if a Type is assignable to IDictionary

#if CPP
        internal static IntPtr s_cppDictionaryClassPtr;
#endif

        /// <summary>
        /// Check if the provided Type is assignable to IDictionary.
        /// </summary>
        /// <param name="t">The Type to check</param>
        /// <returns>True if the Type is assignable to IDictionary, otherwise false.</returns>
        public static bool IsDictionary(Type t)
        {
            if (typeof(IDictionary).IsAssignableFrom(t))
                return true;
#if CPP
            try
            {
                if (s_cppDictionaryClassPtr == IntPtr.Zero)
                    if (!Il2CppTypeNotNull(typeof(Il2CppSystem.Collections.IDictionary), out s_cppDictionaryClassPtr))
                        return false;

                if (Il2CppTypeNotNull(t, out IntPtr classPtr))
                {
                    if (il2cpp_class_is_assignable_from(s_cppDictionaryClassPtr, classPtr))
                        return true;
                }
            }
            catch { }
#endif
            return false;
        }

        // Helper for IL2CPP to try to make sure the Unhollowed game assemblies are actually loaded.

#if CPP
        internal static void TryLoadGameModules()
        {
            LoadModule("Assembly-CSharp");
            LoadModule("Assembly-CSharp-firstpass");
        }

        public static bool LoadModule(string module)
        {
#if ML
            var path = Path.Combine("MelonLoader", "Managed", $"{module}.dll");
#else
            var path = Path.Combine("BepInEx", "unhollowed", $"{module}.dll");
#endif
            return LoadModuleInternal(path);
        }

        internal static bool LoadModuleInternal(string fullPath)
        {
            if (!File.Exists(fullPath))
                return false;

            try
            {
                Assembly.Load(File.ReadAllBytes(fullPath));
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.GetType() + ", " + e.Message);
            }

            return false;
        }
#else
        // For Mono, just return true and do nothing, Mono will sort it out itself.
        public static bool LoadModule(string module) => true;
#endif

        /// <summary>
        /// Helper to display a simple "{ExceptionType}: {Message}" of the exception, and optionally use the inner-most exception.
        /// </summary>
        /// <param name="e">The Exception to convert to string.</param>
        /// <param name="innerMost">Should the inner-most Exception of the stack be used? If false, the Exception you provided will be used directly.</param>
        /// <returns>The exception to string.</returns>
        public static string ExceptionToString(Exception e, bool innerMost = false)
        {
            if (innerMost)
            {
                while (e.InnerException != null)
                {
#if CPP
                    if (e.InnerException is System.Runtime.CompilerServices.RuntimeWrappedException)
                        break;
#endif
                    e = e.InnerException;
                }
            }

            return $"{e.GetType()}: {e.Message}";
        }
    }
}
