using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityExplorer
{
    public static class ArgumentUtility
    {
        public static readonly Type[] EmptyTypes = new Type[0];
        public static readonly object[] EmptyArgs = new object[0];

        public static readonly Type[] ParseArgs = new Type[] { typeof(string) };
    }
}
