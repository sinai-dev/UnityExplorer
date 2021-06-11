using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace UnityExplorer
{
    public static class ReflectionExtensions
    {
        // ReflectionUtility extensions

        public static Type GetActualType(this object obj)
            => ReflectionUtility.Instance.Internal_GetActualType(obj);

        public static object TryCast(this object obj)
            => ReflectionUtility.Instance.Internal_TryCast(obj, ReflectionUtility.Instance.Internal_GetActualType(obj));

        public static object TryCast(this object obj, Type castTo)
            => ReflectionUtility.Instance.Internal_TryCast(obj, castTo);

        public static T TryCast<T>(this object obj)
        {
            try
            {
                return (T)ReflectionUtility.Instance.Internal_TryCast(obj, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        public static HashSet<Type> GetImplementationsOf(this Type baseType, bool allowAbstract, bool allowGeneric)
             => ReflectionUtility.GetImplementationsOf(baseType, allowAbstract, allowGeneric);

        // ------- Misc extensions --------

        /// <summary>
        /// Safely try to get all Types inside an Assembly.
        /// </summary>
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


        /// <summary>
        /// Check if the two objects are reference-equal, including checking for UnityEngine.Object-equality and Il2CppSystem.Object-equality.
        /// </summary>
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
        /// Helper to display a simple "{ExceptionType}: {Message}" of the exception, and optionally use the inner-most exception.
        /// </summary>
        public static string ReflectionExToString(this Exception e, bool innerMost = true)
        {
            if (innerMost)
                e = e.GetInnerMostException();

            return $"{e.GetType()}: {e.Message}";
        }

        public static Exception GetInnerMostException(this Exception e)
        {
            while (e != null)
            {
                if (e.InnerException == null)
                    break;
#if CPP
                if (e.InnerException is System.Runtime.CompilerServices.RuntimeWrappedException)
                    break;
#endif
                e = e.InnerException;
            }

            return e;
        }
    }
}
