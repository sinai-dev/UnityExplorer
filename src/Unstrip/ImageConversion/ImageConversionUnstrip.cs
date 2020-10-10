#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnhollowerBaseLib;
using UnityEngine;
using System.IO;

namespace Explorer.Unstrip.ImageConversion
{
    public static class ImageConversionUnstrip
    {
        // byte[] ImageConversion.EncodeToPNG(this Texture2D image);

        public static byte[] EncodeToPNG(this Texture2D tex)
        {
            return EncodeToPNG_iCall(tex.Pointer);
        }

        internal delegate byte[] EncodeToPNG_delegate(IntPtr tex);
        internal static EncodeToPNG_delegate EncodeToPNG_iCall =
            IL2CPP.ResolveICall<EncodeToPNG_delegate>("UnityEngine.ImageConversion::EncodeToPNG");

        // bool ImageConversion.LoadImage(this Texture2D tex, byte[] data, bool markNonReadable);

        public static bool LoadImage(this Texture2D tex, byte[] data, bool markNonReadable)
        {
            return LoadImage_iCall(tex.Pointer, data, markNonReadable);
        }

        internal delegate bool LoadImage_delegate(IntPtr tex, byte[] data, bool markNonReadable);
        internal static LoadImage_delegate LoadImage_iCall =
            IL2CPP.ResolveICall<LoadImage_delegate>("UnityEngine.ImageConversion::LoadImage");

        // Helper for LoadImage

        public static bool LoadImage(this Texture2D tex, string filePath, bool markNonReadable)
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