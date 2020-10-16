#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnhollowerBaseLib;
using UnityEngine;
using System.IO;
using Explorer.Helpers;

namespace Explorer.Unstrip.ImageConversion
{
    public static class ImageConversionUnstrip
    {
        // byte[] ImageConversion.EncodeToPNG(this Texture2D image);

        internal delegate byte[] d_EncodeToPNG(IntPtr tex);

        public static byte[] EncodeToPNG(this Texture2D tex)
        {
            return ICallHelper.GetICall<d_EncodeToPNG>("UnityEngine.ImageConversion::EncodeToPNG")
                .Invoke(tex.Pointer);
        }

        // bool ImageConversion.LoadImage(this Texture2D tex, byte[] data, bool markNonReadable);

        internal delegate bool d_LoadImage(IntPtr tex, byte[] data, bool markNonReadable);

        public static bool LoadImage(this Texture2D tex, byte[] data, bool markNonReadable)
        {
            return ICallHelper.GetICall<d_LoadImage>("UnityEngine.ImageConversion::LoadImage")
                .Invoke(tex.Pointer, data, markNonReadable);
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