#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnhollowerBaseLib;
using UnityEngine;

namespace UnityExplorer.Core.Runtime.Il2Cpp
{
    public class Il2CppTextureUtil : TextureUtilProvider
    {
        public override Texture2D NewTexture2D(int width, int height)
            => new Texture2D((int)width, (int)height, TextureFormat.RGBA32, Texture.GenerateAllMips, false, IntPtr.Zero);

        internal delegate void d_Blit2(IntPtr source, IntPtr dest);

        public override void Blit(Texture2D tex, RenderTexture rt)
        {
            var iCall = ICallManager.GetICall<d_Blit2>("UnityEngine.Graphics::Blit2");
            iCall.Invoke(tex.Pointer, rt.Pointer);
        }

        // byte[] ImageConversion.EncodeToPNG(this Texture2D image);

        internal delegate IntPtr d_EncodeToPNG(IntPtr tex);

        public override byte[] EncodeToPNG(Texture2D tex)
        {
            var iCall = ICallManager.GetICall<d_EncodeToPNG>("UnityEngine.ImageConversion::EncodeToPNG");

            IntPtr ptr = iCall.Invoke(tex.Pointer);

            if (ptr == IntPtr.Zero)
                return null;

            return new Il2CppStructArray<byte>(ptr);
        }

        // bool ImageConversion.LoadImage(this Texture2D tex, byte[] data, bool markNonReadable);

        internal delegate bool d_LoadImage(IntPtr tex, IntPtr data, bool markNonReadable);

        public override bool LoadImage(Texture2D tex, byte[] data, bool markNonReadable)
        {
            var il2cppArray = (Il2CppStructArray<byte>)data;

            var iCall = ICallManager.GetICall<d_LoadImage>("UnityEngine.ImageConversion::LoadImage");

            return iCall.Invoke(tex.Pointer, il2cppArray.Pointer, markNonReadable);
        }

        // Sprite Sprite.Create

        public override Sprite CreateSprite(Texture2D texture)
        {
            return CreateSpriteImpl(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero, 100f, 0u, Vector4.zero);
        }

        internal delegate IntPtr d_CreateSprite(IntPtr texture, ref Rect rect, ref Vector2 pivot, float pixelsPerUnit,
            uint extrude, int meshType, ref Vector4 border, bool generateFallbackPhysicsShape);

        public static Sprite CreateSpriteImpl(Texture texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, Vector4 border)
        {
            var iCall = ICallManager.GetICall<d_CreateSprite>("UnityEngine.Sprite::CreateSprite_Injected");

            var ptr = iCall.Invoke(texture.Pointer, ref rect, ref pivot, pixelsPerUnit, extrude, 1, ref border, false);

            if (ptr == IntPtr.Zero)
                return null;
            else
                return new Sprite(ptr);
        }
    }
}
#endif