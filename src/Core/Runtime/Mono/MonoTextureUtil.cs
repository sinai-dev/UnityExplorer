#if MONO
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityExplorer.Core;

namespace UnityExplorer.Core.Runtime.Mono
{
    public class MonoTextureUtil : TextureUtilProvider
    {
        public override void Blit(Texture2D tex, RenderTexture rt)
        {
            Graphics.Blit(tex, rt);
        }

        public override Sprite CreateSprite(Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }

        public override bool LoadImage(Texture2D tex, byte[] data, bool markNonReadable)
        {
            return tex.LoadImage(data, markNonReadable);
        }

        public override Texture2D NewTexture2D(int width, int height)
        {
            return new Texture2D(width, height);
        }

        public override byte[] EncodeToPNG(Texture2D tex)
        {
            return EncodeToPNGSafe(tex);
        }

        private static MethodInfo EncodeToPNGMethod => m_encodeToPNGMethod ?? GetEncodeToPNGMethod();
        private static MethodInfo m_encodeToPNGMethod;

        public static byte[] EncodeToPNGSafe(Texture2D tex)
        {
            var method = EncodeToPNGMethod;

            if (method.IsStatic)
                return (byte[])method.Invoke(null, new object[] { tex });
            else
                return (byte[])method.Invoke(tex, ArgumentUtility.EmptyArgs);
        }

        private static MethodInfo GetEncodeToPNGMethod()
        {
            if (ReflectionUtility.GetTypeByName("UnityEngine.ImageConversion") is Type imageConversion)
                return m_encodeToPNGMethod = imageConversion.GetMethod("EncodeToPNG", ReflectionUtility.FLAGS);

            var method = typeof(Texture2D).GetMethod("EncodeToPNG", ReflectionUtility.FLAGS);
            if (method != null)
                return m_encodeToPNGMethod = method;

            ExplorerCore.Log("ERROR: Cannot get any EncodeToPNG method!");
            return null;
        }
    }
}
#endif