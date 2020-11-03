using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    public class ReflectionHelpers
    {
        public static BF CommonFlags = BF.Public | BF.Instance | BF.NonPublic | BF.Static;

#if CPP
        public static ILType GameObjectType => Il2CppType.Of<GameObject>();
        public static ILType TransformType => Il2CppType.Of<Transform>();
        public static ILType ObjectType => Il2CppType.Of<UnityEngine.Object>();
        public static ILType ComponentType => Il2CppType.Of<Component>();
        public static ILType BehaviourType => Il2CppType.Of<Behaviour>();
#else
        public static Type GameObjectType   => typeof(GameObject);
        public static Type TransformType    => typeof(Transform);
        public static Type ObjectType       => typeof(UnityEngine.Object);
        public static Type ComponentType    => typeof(Component);
        public static Type BehaviourType    => typeof(Behaviour);
#endif

#if CPP
        private static readonly Dictionary<Type, IntPtr> ClassPointers = new Dictionary<Type, IntPtr>();

        public static object Il2CppCast(object obj, Type castTo)
        {
            if (!(obj is Il2CppSystem.Object ilObj))
            {
                return obj;
            }

            if (!typeof(Il2CppSystem.Object).IsAssignableFrom(castTo))
            {
                return obj;
            }

            IntPtr castToPtr;
            if (!ClassPointers.ContainsKey(castTo))
            {
                castToPtr = (IntPtr)typeof(Il2CppClassPointerStore<>)
                    .MakeGenericType(new Type[] { castTo })
                    .GetField("NativeClassPtr", BF.Public | BF.Static)
                    .GetValue(null);

                ClassPointers.Add(castTo, castToPtr);
            }
            else
            {
                castToPtr = ClassPointers[castTo];
            }

            if (castToPtr == IntPtr.Zero)
            {
                return obj;
            }

            IntPtr classPtr = il2cpp_object_get_class(ilObj.Pointer);

            if (!il2cpp_class_is_assignable_from(castToPtr, classPtr))
                return obj;

            if (RuntimeSpecificsStore.IsInjected(castToPtr))
                return UnhollowerBaseLib.Runtime.ClassInjectorBase.GetMonoObjectFromIl2CppPointer(ilObj.Pointer);

            return Activator.CreateInstance(castTo, ilObj.Pointer);
        }

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool il2cpp_class_is_assignable_from(IntPtr klass, IntPtr oklass);

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr il2cpp_object_get_class(IntPtr obj);

#endif

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
            if (obj == null)
                return null;

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
            List<Type> list = new List<Type>();

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

        public static string ExceptionToString(Exception e)
        {
            return e.GetType() + ", " + e.Message;
        }
    }
}
