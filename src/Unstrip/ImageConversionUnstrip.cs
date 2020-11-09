#if CPP
using System;
using System.IO;
using UnityExplorer.Helpers;
using UnhollowerBaseLib;
using UnityEngine;

namespace UnityExplorer.Unstrip
{
    public static class ImageConversionUnstrip
    {
        // byte[] ImageConversion.EncodeToPNG(this Texture2D image);

        internal delegate IntPtr d_EncodeToPNG(IntPtr tex);

        public static byte[] EncodeToPNG(this Texture2D tex)
        {
            IntPtr ptr = ICallHelper.GetICall<d_EncodeToPNG>("UnityEngine.ImageConversion::EncodeToPNG")
                .Invoke(tex.Pointer);

            return new Il2CppStructArray<byte>(ptr);

            //// This is a bit of a hack. The iCall actually returns an Il2CppStructArray<byte>...

            // byte[] data = ICallHelper.GetICall<d_EncodeToPNG>("UnityEngine.ImageConversion::EncodeToPNG")
            //                  .Invoke(tex.Pointer);

            //// However, if you try to use that result with for example File.WriteAllBytes, it won't work.
            //// Simple fix: iterate into a new managed array.

            //byte[] safeData = new byte[data.Length];
            //for (int i = 0; i < data.Length; i++)
            //    safeData[i] = (byte)data[i];

            //return safeData;
        }

        // bool ImageConversion.LoadImage(this Texture2D tex, byte[] data, bool markNonReadable);

        internal delegate bool d_LoadImage(IntPtr tex, IntPtr data, bool markNonReadable);

        public static bool LoadImage(this Texture2D tex, byte[] data, bool markNonReadable)
        {
            Il2CppStructArray<byte> il2cppArray = new Il2CppStructArray<byte>(data.Length);
            for (int i = 0; i < data.Length; i++)
                il2cppArray[i] = data[i];

            bool ret = ICallHelper.GetICall<d_LoadImage>("UnityEngine.ImageConversion::LoadImage")
                .Invoke(tex.Pointer, il2cppArray.Pointer, markNonReadable);

            return ret;
        }

        // Helper for LoadImage from filepath

        public static bool LoadImage(Texture2D tex, string filePath, bool markNonReadable)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            byte[] data = File.ReadAllBytes(filePath);
            return tex.LoadImage(data, markNonReadable);
        }
    }
}

#endif