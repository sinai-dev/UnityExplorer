#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Reflection;

namespace Explorer.Helpers
{
    public static class ICallHelper
    {
        private static readonly Dictionary<string, Delegate> iCallCache = new Dictionary<string, Delegate>();

        public static T GetICall<T>(string iCallName) where T : Delegate
        {
            if (iCallCache.ContainsKey(iCallName))
            {
                return (T)iCallCache[iCallName];
            }

            var ptr = il2cpp_resolve_icall(iCallName);

            if (ptr == IntPtr.Zero)
            {
                throw new MissingMethodException($"Could not resolve internal call by name '{iCallName}'!");
            }

            var iCall = Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
            iCallCache.Add(iCallName, iCall);

            return (T)iCall;
        }

        #region External
        #pragma warning disable IDE1006 // Naming Styles

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr il2cpp_resolve_icall([MarshalAs(UnmanagedType.LPStr)] string name);

        #pragma warning restore IDE1006
        #endregion
    }
}
#endif