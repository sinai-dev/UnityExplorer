#if CPP
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace UnityExplorer.Helpers
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "External methods")]
    public static class ICallHelper
    {
        private static readonly Dictionary<string, Delegate> iCallCache = new Dictionary<string, Delegate>();

        /// <summary>
        /// Helper to get and cache an iCall by providing the signature (eg. "UnityEngine.Resources::FindObjectsOfTypeAll").
        /// </summary>
        /// <typeparam name="T">The Type of Delegate to provide for the iCall.</typeparam>
        /// <param name="signature">The signature of the iCall you want to get.</param>
        /// <returns>The <typeparamref name="T"/> delegate if successful.</returns>
        /// <exception cref="MissingMethodException">If the iCall could not be found.</exception>
        public static T GetICall<T>(string signature) where T : Delegate
        {
            if (iCallCache.ContainsKey(signature))
                return (T)iCallCache[signature];

            IntPtr ptr = il2cpp_resolve_icall(signature);

            if (ptr == IntPtr.Zero)
            {
                throw new MissingMethodException($"Could not resolve internal call by name '{signature}'!");
            }

            Delegate iCall = Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
            iCallCache.Add(signature, iCall);

            return (T)iCall;
        }

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr il2cpp_resolve_icall([MarshalAs(UnmanagedType.LPStr)] string name);
    }
}
#endif