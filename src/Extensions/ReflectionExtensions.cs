using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Explorer
{
    public static class ReflectionExtensions
    {
#if CPP
        /// <summary>
        /// Extension to allow for easy, non-generic Il2Cpp casting.
        /// The extension is on System.Object, but only Il2Cpp objects would be a valid target.
        /// </summary>
        public static object Il2CppCast(this object obj, Type castTo)
        {
            return ReflectionHelpers.Il2CppCast(obj, castTo);
        }
#endif

        /// <summary>
        /// Extension to safely try to get all Types from an Assembly, with a fallback for ReflectionTypeLoadException.
        /// </summary>
        public static IEnumerable<Type> TryGetTypes(this Assembly asm)
        {
            try
            {
                return asm.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
            catch
            {
                return Enumerable.Empty<Type>();
            }
        }
    }
}
