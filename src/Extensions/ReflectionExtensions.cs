using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Explorer
{
    public static class ReflectionExtensions
    {
        public static object Il2CppCast(this object obj, Type castTo)
        {
            return ReflectionHelpers.Il2CppCast(obj, castTo);
        }
    }
}
