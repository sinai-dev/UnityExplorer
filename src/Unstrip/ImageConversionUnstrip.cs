using System;
using System.IO;
using UnityExplorer.Helpers;
using UnityEngine;
#if CPP
using UnhollowerBaseLib;
#endif

namespace UnityExplorer.Unstrip
{
    public static class ImageConversionUnstrip
    {
        // LoadImage helper from a filepath

        public static bool LoadImage(Texture2D tex, string filePath, bool markNonReadable)
        {
            if (!File.Exists(filePath))
                return false;

            return tex.LoadImage(File.ReadAllBytes(filePath), markNonReadable);
        }

#if CPP
        // byte[] ImageConversion.EncodeToPNG(this Texture2D image);

        internal delegate IntPtr d_EncodeToPNG(IntPtr tex);

        public static byte[] EncodeToPNG(this Texture2D tex)
        {
            var iCall = ICallHelper.GetICall<d_EncodeToPNG>("UnityEngine.ImageConversion::EncodeToPNG");

            IntPtr ptr = iCall.Invoke(tex.Pointer);

            if (ptr == IntPtr.Zero)
                return null;

            return new Il2CppStructArray<byte>(ptr);
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

        // Sprite Sprite.Create

        internal delegate IntPtr d_CreateSprite(IntPtr texture, ref Rect rect, ref Vector2 pivot, float pixelsPerUnit, 
            uint extrude, int meshType, ref Vector4 border, bool generateFallbackPhysicsShape);

        public static Sprite CreateSprite(Texture texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, Vector4 border)
        {
            var iCall = ICallHelper.GetICall<d_CreateSprite>("UnityEngine.Sprite::CreateSprite_Injected");

            var ptr = iCall.Invoke(texture.Pointer, ref rect, ref pivot, pixelsPerUnit, extrude, 1, ref border, false);

            if (ptr == IntPtr.Zero)
                return null;
            else
                return new Sprite(ptr);
        }
#endif
    }
}