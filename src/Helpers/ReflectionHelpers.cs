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
using ILType = Il2CppSystem.Type;
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
                if (obj is ILType)
                    return typeof(ILType);

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
                    return GetTypeByName(il2cppType.FullName);

                var getType = Type.GetType(il2cppType.AssemblyQualifiedName);

                if (getType != null)
                    return getType;
            }
#endif
            return type;
        }

#if CPP
        private static readonly Dictionary<Type, IntPtr> ClassPointers = new Dictionary<Type, IntPtr>();

        public static object Il2CppCast(this object obj, Type castTo)
        {
            if (!(obj is Il2CppSystem.Object ilObj))
            {
                return obj;
            }

            if (!Il2CppTypeNotNull(castTo, out IntPtr castToPtr))
                return obj;

            IntPtr classPtr = il2cpp_object_get_class(ilObj.Pointer);

            if (!il2cpp_class_is_assignable_from(castToPtr, classPtr))
                return obj;

            if (RuntimeSpecificsStore.IsInjected(castToPtr))
                return UnhollowerBaseLib.Runtime.ClassInjectorBase.GetMonoObjectFromIl2CppPointer(ilObj.Pointer);

            return Activator.CreateInstance(castTo, ilObj.Pointer);
        }

        public static bool Il2CppTypeNotNull(Type type)
        {
            return Il2CppTypeNotNull(type, out _);
        }

        public static bool Il2CppTypeNotNull(Type type, out IntPtr il2cppPtr)
        {
            if (!ClassPointers.ContainsKey(type))
            {
                il2cppPtr = (IntPtr)typeof(Il2CppClassPointerStore<>)
                    .MakeGenericType(new Type[] { type })
                    .GetField("NativeClassPtr", BF.Public | BF.Static)
                    .GetValue(null);

                ClassPointers.Add(type, il2cppPtr);
            }
            else
                il2cppPtr = ClassPointers[type];

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

        public static bool LoadModule(string module)
        {
#if CPP
#if ML
            string path = $@"MelonLoader\Managed\{module}.dll";
#else
            var path = $@"BepInEx\unhollowed\{module}.dll";
#endif
            if (!File.Exists(path))
            {
                return false;
            }

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
                e = e.InnerException;

            return e.GetType() + ", " + e.Message;
        }
    }
}
