#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnhollowerBaseLib;
using UnityEngine;
using System.IO;
using Explorer.Helpers;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Explorer.Unstrip.ImageConversion
{
    public static class ImageConversionUnstrip
    {
        // byte[] ImageConversion.EncodeToPNG(this Texture2D image);

        internal delegate byte[] d_EncodeToPNG(IntPtr tex);

        public static byte[] EncodeToPNG(this Texture2D tex)
        {
            var data = ICallHelper.GetICall<d_EncodeToPNG>("UnityEngine.ImageConversion::EncodeToPNG")
                .Invoke(tex.Pointer);

            // The Il2Cpp EncodeToPNG() method does return System.Byte[],
            // but for some reason it is not recognized or valid.
            // Simple fix is iterating into a new array manually.

            byte[] safeData = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                safeData[i] = (byte)data[i]; // not sure if cast is needed
            }

            return safeData;
        }

        // ******** LoadImage not yet working. ********

        // bool ImageConversion.LoadImage(this Texture2D tex, byte[] data, bool markNonReadable);

        internal delegate bool d_LoadImage(IntPtr tex, IntPtr data, bool markNonReadable);

        public static bool LoadImage(this Texture2D tex, byte[] data, bool markNonReadable)
        {
            IntPtr unmanagedArray = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, unmanagedArray, data.Length);

            var ret = ICallHelper.GetICall<d_LoadImage>("UnityEngine.ImageConversion::LoadImage")
                .Invoke(tex.Pointer, unmanagedArray, markNonReadable);

            // var ret = tex.LoadRawTextureDataImpl(unmanagedArray, data.Length);            

            Marshal.FreeHGlobal(unmanagedArray);

            return ret;
        }

        // Helper for LoadImage from filepath

        public static bool LoadImage(Texture2D tex, string filePath, bool markNonReadable)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            var data = File.ReadAllBytes(filePath);
            return tex.LoadImage(data, markNonReadable);
        }
    }
}

#endif