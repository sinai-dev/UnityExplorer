#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnhollowerBaseLib;
using UnityExplorer.Core.Unity;
using UnhollowerRuntimeLib;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using UnityExplorer.Core;
using CppType = Il2CppSystem.Type;
using BF = System.Reflection.BindingFlags;

namespace UnityExplorer.Core.Runtime.Il2Cpp
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "External methods")]
    public class Il2CppReflection : ReflectionProvider
    {
        public Il2CppReflection() : base()
        {
            Instance = this;

            TryLoadGameModules();
        }

        public override object Cast(object obj, Type castTo)
        {
            return Il2CppCast(obj, castTo);
        }

        public override string ProcessTypeNameInString(Type type, string theString, ref string typeName)
        {
            if (!Il2CppTypeNotNull(type))
                return theString;

            var cppType = Il2CppType.From(type);
            if (cppType != null && s_deobfuscatedTypeNames.ContainsKey(cppType.FullName))
            {
                typeName = s_deobfuscatedTypeNames[cppType.FullName];
                theString = theString.Replace(cppType.FullName, typeName);
            }

            return theString;
        }

        public override Type GetActualType(object obj)
        {
            if (obj == null)
                return null;

            var type = obj.GetType();

            if (obj is Il2CppSystem.Object cppObject)
            {
                // weird specific case - if the object is an Il2CppSystem.Type, then return so manually.
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
                    var typeByName = ReflectionUtility.GetTypeByName(cppType.FullName);
                    if (typeByName != null)
                        return typeByName;
                }

                // this should be fine for all other il2cpp objects
                var getType = GetMonoType(cppType);
                if (getType != null)
                    return getType;
            }

            return type;
        }

        // caching for GetMonoType
        private static readonly Dictionary<string, Type> Il2CppToMonoType = new Dictionary<string, Type>();

        // keep deobfuscated type name cache, used to display proper name.
        internal static Dictionary<string, string> s_deobfuscatedTypeNames = new Dictionary<string, string>();

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
                    // deobfuscated type was found, add to cache.
                    s_deobfuscatedTypeNames.Add(cppType.FullName, ret.FullName);
                }
            }

            Il2CppToMonoType.Add(name, ret);

            return ret;
        }

        // cached class pointers for Il2CppCast
        private static readonly Dictionary<string, IntPtr> s_cppClassPointers = new Dictionary<string, IntPtr>();

        /// <summary>
        /// Attempt to cast the object to its underlying type.
        /// </summary>
        /// <param name="obj">The object you want to cast.</param>
        /// <returns>The object, as the underlying type if successful or the input value if not.</returns>
        public static object Il2CppCast(object obj) => Il2CppCast(obj, Instance.GetActualType(obj));

        /// <summary>
        /// Attempt to cast the object to the provided type.
        /// </summary>
        /// <param name="obj">The object you want to cast.</param>
        /// <param name="castTo">The Type you want to cast to.</param>
        /// <returns>The object, as the type (or a normal C# object) if successful or the input value if not.</returns>
        public static object Il2CppCast(object obj, Type castTo)
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
            if (s_cppClassPointers.TryGetValue(type.AssemblyQualifiedName, out il2cppPtr))
                return il2cppPtr != IntPtr.Zero;

            il2cppPtr = (IntPtr)typeof(Il2CppClassPointerStore<>)
                    .MakeGenericType(new Type[] { type })
                    .GetField("NativeClassPtr", BF.Public | BF.Static)
                    .GetValue(null);

            s_cppClassPointers.Add(type.AssemblyQualifiedName, il2cppPtr);

            return il2cppPtr != IntPtr.Zero;
        }

        // Extern C++ methods 
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool il2cpp_class_is_assignable_from(IntPtr klass, IntPtr oklass);

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr il2cpp_object_get_class(IntPtr obj);

        internal static IntPtr s_cppEnumerableClassPtr;
        internal static IntPtr s_cppDictionaryClassPtr;

        public override bool IsAssignableFrom(Type toAssignTo, Type toAssignFrom)
        {
            if (toAssignTo.IsAssignableFrom(toAssignFrom))
                return true;

            if (toAssignTo == typeof(IEnumerable))
            {
                try
                {
                    if (s_cppEnumerableClassPtr == IntPtr.Zero)
                        Il2CppTypeNotNull(typeof(Il2CppSystem.Collections.IEnumerable), out s_cppEnumerableClassPtr);

                    if (s_cppEnumerableClassPtr != IntPtr.Zero
                        && Il2CppTypeNotNull(toAssignFrom, out IntPtr assignFromPtr)
                        && il2cpp_class_is_assignable_from(s_cppEnumerableClassPtr, assignFromPtr))
                    {
                        return true;
                    }
                }
                catch { }
            }
            else if (toAssignTo == typeof(IDictionary))
            {
                try
                {
                    if (s_cppDictionaryClassPtr == IntPtr.Zero)
                        if (!Il2CppTypeNotNull(typeof(Il2CppSystem.Collections.IDictionary), out s_cppDictionaryClassPtr))
                            return false;

                    if (Il2CppTypeNotNull(toAssignFrom, out IntPtr classPtr))
                    {
                        if (il2cpp_class_is_assignable_from(s_cppDictionaryClassPtr, classPtr))
                            return true;
                    }
                }
                catch { }
            }

            return false;
        }

        public override bool IsReflectionSupported(Type type)
        {
            try
            {
                var gArgs = type.GetGenericArguments();
                if (!gArgs.Any())
                    return true;

                foreach (var gType in gArgs)
                {
                    if (!Supported(gType))
                        return false;
                }

                return true;

                bool Supported(Type t)
                {
                    if (!typeof(Il2CppSystem.Object).IsAssignableFrom(t))
                        return true;

                    if (!Il2CppTypeNotNull(t, out IntPtr ptr))
                        return false;

                    return CppType.internal_from_handle(IL2CPP.il2cpp_class_get_type(ptr)) is CppType;
                }
            }
            catch
            {
                return false;
            }
        }

        // Helper for IL2CPP to try to make sure the Unhollowed game assemblies are actually loaded.

        internal static void TryLoadGameModules()
        {
            Instance.LoadModule("Assembly-CSharp");
            Instance.LoadModule("Assembly-CSharp-firstpass");
        }

        public override bool LoadModule(string module)
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

        // ~~~~~~~~~~ not used ~~~~~~~~~~~~

        // cached il2cpp unbox methods
        internal static readonly Dictionary<string, MethodInfo> s_unboxMethods = new Dictionary<string, MethodInfo>();

        /// <summary>
        /// Attempt to unbox the object to the underlying struct type.
        /// </summary>
        /// <param name="obj">The object which is a struct underneath.</param>
        /// <returns>The struct if successful, otherwise null.</returns>
        public static object Unbox(object obj) => Unbox(obj, Instance.GetActualType(obj));

        /// <summary>
        /// Attempt to unbox the object to the struct type.
        /// </summary>
        /// <param name="obj">The object which is a struct underneath.</param>
        /// <param name="type">The type of the struct you want to unbox to.</param>
        /// <returns>The struct if successful, otherwise null.</returns>
        public static object Unbox(object obj, Type type)
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
    }
}

#endif