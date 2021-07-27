#if CPP
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;

namespace UnityExplorer.Core.Runtime.Il2Cpp
{
    public static class ICallManager
    {
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr il2cpp_resolve_icall([MarshalAs(UnmanagedType.LPStr)] string name);

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
                throw new MissingMethodException($"Could not find any iCall with the signature '{signature}'!");

            Delegate iCall = Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
            iCallCache.Add(signature, iCall);

            return (T)iCall;
        }

        private static readonly Dictionary<string, Delegate> s_unreliableCache = new Dictionary<string, Delegate>();

        /// <summary>
        /// Get an iCall which may be one of multiple different signatures (ie, it changed in different Unity versions).
        /// Each possible signature must have the same Type pattern, it can only vary by name.
        /// </summary>
        public static T GetICallUnreliable<T>(IEnumerable<string> possibleSignatures) where T : Delegate
        {
            // use the first possible signature as the 'key'.
            string key = possibleSignatures.First();

            if (s_unreliableCache.ContainsKey(key))
                return (T)s_unreliableCache[key];

            T iCall;
            IntPtr ptr;
            foreach (var sig in possibleSignatures)
            {
                ptr = il2cpp_resolve_icall(sig);
                if (ptr != IntPtr.Zero)
                {
                    iCall = (T)Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
                    s_unreliableCache.Add(key, iCall);
                    return iCall;
                }
            }

            throw new MissingMethodException($"Could not find any iCall from list of provided signatures starting with '{key}'!");
        }
    }
}
#endif