using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityExplorer.Helpers;

namespace UnityExplorer
{
    public static class ReflectionExtensions
    {
#if CPP
        public static object Il2CppCast(this object obj, Type castTo)
        {
            return ReflectionHelpers.Il2CppCast(obj, castTo);
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
    }
}
