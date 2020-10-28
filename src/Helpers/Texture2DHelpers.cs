using System;
using UnityEngine;
using System.IO;
using System.Reflection;
#if CPP
using ExplorerBeta.Unstrip.ImageConversion;
#endif

namespace ExplorerBeta.Helpers
{
    public static class Texture2DHelpers
    {
#if CPP // If Mono
#else
        private static bool isNewEncodeMethod = false;
        private static MethodInfo EncodeToPNGMethod => m_encodeToPNGMethod ?? GetEncodeToPNGMethod();
        private static MethodInfo m_encodeToPNGMethod;

        private static MethodInfo GetEncodeToPNGMethod()
        {
            if (ReflectionHelpers.GetTypeByName("UnityEngine.ImageConversion") is Type imageConversion)
            {
                isNewEncodeMethod = true;
                return m_encodeToPNGMethod = imageConversion.GetMethod("EncodeToPNG", ReflectionHelpers.CommonFlags);
            }

            var method = typeof(Texture2D).GetMethod("EncodeToPNG", ReflectionHelpers.CommonFlags);
            if (method != null)
            {
                return m_encodeToPNGMethod = method;
            }

            ExplorerCore.Log("ERROR: Cannot get any EncodeToPNG method!");
            return null;
        }
#endif


        public static bool IsReadable(this Texture2D tex)
        {
            try
            {
                // This will cause an exception if it's not readable.
                // Reason for doing it this way is not all Unity versions
                // ship with the 'Texture.isReadable' property.

                tex.GetPixel(0, 0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static Texture2D Copy(Texture2D orig, Rect rect, bool isDTXnmNormal = false)
        {
            Color[] pixels;

            if (!orig.IsReadable())
                orig = ForceReadTexture(orig);

            pixels = orig.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);

            var _newTex = new Texture2D((int)rect.width, (int)rect.height);
            _newTex.SetPixels(pixels);

            return _newTex;
        }

        public static Texture2D ForceReadTexture(Texture2D tex)
        {
            try
            {
                FilterMode origFilter = tex.filterMode;
                tex.filterMode = FilterMode.Point;

                var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
                rt.filterMode = FilterMode.Point;
                RenderTexture.active = rt;
                Graphics.Blit(tex, rt);

                var _newTex = new Texture2D(tex.width, tex.height, TextureFormat.ARGB32, false);

                _newTex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                _newTex.Apply(false, false);

                RenderTexture.active = null;
                tex.filterMode = origFilter;

                return _newTex;
            }
            catch (Exception e)
            {
                ExplorerCore.Log("Exception on ForceReadTexture: " + e.ToString());
                return default;
            }
        }

        public static void SaveTextureAsPNG(Texture2D tex, string dir, string name, bool isDTXnmNormal = false)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            byte[] data;
            string savepath = dir + @"\" + name + ".png";

            // Make sure we can EncodeToPNG it.
            if (tex.format != TextureFormat.ARGB32 || !tex.IsReadable())
            {
                tex = ForceReadTexture(tex);
            }

            if (isDTXnmNormal)
            {
                tex = DTXnmToRGBA(tex);
                tex.Apply(false, false);
            }

#if CPP
            data = tex.EncodeToPNG();
#else
            if (isNewEncodeMethod)
            {
                data = (byte[])EncodeToPNGMethod.Invoke(null, new object[] { tex });
            }
            else
            {
                data = (byte[])EncodeToPNGMethod.Invoke(tex, new object[0]);
            }
#endif

            if (data == null || data.Length < 1)
            {
                ExplorerCore.LogWarning("Couldn't get any data for the texture!");
            }
            else
            {
                File.WriteAllBytes(savepath, data);
            }
        }

        // Converts DTXnm-format Normal Map to RGBA-format Normal Map.
        public static Texture2D DTXnmToRGBA(Texture2D tex)
        {
            Color[] colors = tex.GetPixels();

            for (int i = 0; i < colors.Length; i++)
            {
                var c = colors[i];

                c.r = c.a * 2 - 1;  // red <- alpha
                c.g = c.g * 2 - 1;  // green is always the same

                var rg = new Vector2(c.r, c.g); //this is the red-green vector
                c.b = Mathf.Sqrt(1 - Mathf.Clamp01(Vector2.Dot(rg, rg))); //recalculate the blue channel

                colors[i] = new Color(
                    (c.r * 0.5f) + 0.5f,
                    (c.g * 0.5f) + 0.25f,
                    (c.b * 0.5f) + 0.5f
                );
            }

            var newtex = new Texture2D(tex.width, tex.height, TextureFormat.ARGB32, false);
            newtex.SetPixels(colors);

            return newtex;
        }
    }
}
