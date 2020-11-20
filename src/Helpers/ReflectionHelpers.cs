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

        public static Type[] GetAllBaseTypes(object obj) => GetAllBaseTypes(GetActualType(obj));

        public static Type[] GetAllBaseTypes(Type type)
        {
            List<Type> list = new List<Type>();

            while (type != null)
            {
                list.Add(type);
                type = type.BaseType;
            }

            return list.ToArray();
        }

        public static Type GetActualType(object obj)
        {
            if (obj == null)
                return null;

            var type = obj.GetType();
#if CPP
            if (obj is Il2CppSystem.Object ilObject)
            {
                if (ilObject is CppType)
                    return typeof(CppType);

                if (!string.IsNullOrEmpty(type.Namespace))
                {
                    // Il2CppSystem-namespace objects should just return GetType,
                    // because using GetIl2CppType returns the System namespace type instead.
                    if (type.Namespace.StartsWith("System.") || type.Namespace.StartsWith("Il2CppSystem."))
                        return ilObject.GetType();
                }

                var il2cppType = ilObject.GetIl2CppType();

                // check if type is injected
                IntPtr classPtr = il2cpp_object_get_class(ilObject.Pointer);
                if (RuntimeSpecificsStore.IsInjected(classPtr))
                {
                    var typeByName = GetTypeByName(il2cppType.FullName);
                    if (typeByName != null)
                        return typeByName;
                }

                // this should be fine for all other il2cpp objects
                var getType = GetMonoType(il2cppType);
                if (getType != null)
                    return getType;
            }
#endif
            return type;
        }

#if CPP
        private static readonly Dictionary<CppType, Type> Il2CppToMonoType = new Dictionary<CppType, Type>();

        public static Type GetMonoType(CppType cppType)
        {
            if (Il2CppToMonoType.ContainsKey(cppType))
                return Il2CppToMonoType[cppType];

            var getType = Type.GetType(cppType.AssemblyQualifiedName);
            Il2CppToMonoType.Add(cppType, getType);
            return getType;
        }

        private static readonly Dictionary<Type, IntPtr> CppClassPointers = new Dictionary<Type, IntPtr>();

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

        public static bool Il2CppTypeNotNull(Type type) => Il2CppTypeNotNull(type, out _);

        public static bool Il2CppTypeNotNull(Type type, out IntPtr il2cppPtr)
        {
            if (!CppClassPointers.ContainsKey(type))
            {
                il2cppPtr = (IntPtr)typeof(Il2CppClassPointerStore<>)
                    .MakeGenericType(new Type[] { type })
                    .GetField("NativeClassPtr", BF.Public | BF.Static)
                    .GetValue(null);

                CppClassPointers.Add(type, il2cppPtr);
            }
            else
                il2cppPtr = CppClassPointers[type];

            return il2cppPtr != IntPtr.Zero;
        }

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool il2cpp_class_is_assignable_from(IntPtr klass, IntPtr oklass);

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr il2cpp_object_get_class(IntPtr obj);

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

#if CPP
        internal static void TryLoadGameModules()
        {
            LoadModule("Assembly-CSharp");
            LoadModule("Assembly-CSharp-firstpass");
        }

        public static bool LoadModule(string module)
        {
#if ML
            var path = $@"MelonLoader\Managed\{module}.dll";
#else
            var path = $@"BepInEx\unhollowed\{module}.dll";
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
        public static bool LoadModule(string module) => true;
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

        public static string ExceptionToString(Exception e, bool innerMost = false)
        {
            while (innerMost && e.InnerException != null)
            {
#if CPP
                if (e.InnerException is System.Runtime.CompilerServices.RuntimeWrappedException runtimeEx)
                    break;
#endif
                e = e.InnerException;
            }

            return e.GetType() + ", " + e.Message;
        }
    }
}
