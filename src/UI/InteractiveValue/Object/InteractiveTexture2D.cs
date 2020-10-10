using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Explorer.CacheObject;
using Explorer.Config;
using UnityEngine;
using System.IO;
#if CPP
using Explorer.Unstrip.ImageConversion;
#endif

namespace Explorer.UI
{ 
    public class InteractiveTexture2D : InteractiveValue, IExpandHeight
    {
        public bool IsExpanded { get; set; }
        public float WhiteSpace { get; set; } = 215f;

        public Texture2D currentTex;
        public GUIContent texContent;

        private string saveFolder = ModConfig.Instance.Default_Output_Path;

        public override void Init()
        {
            base.Init();
        }

        public override void UpdateValue()
        {
            base.UpdateValue();

            GetTexture2D();
        }

        public virtual void GetTexture2D()
        {
#if CPP
            if (Value != null && Value.Il2CppCast(typeof(Texture2D)) is Texture2D tex)
#else
            if (Value is Texture2D tex)
#endif
            {
                currentTex = tex;
                texContent = new GUIContent
                {
                    image = currentTex
                };
            }
        }

        public override void DrawValue(Rect window, float width)
        {
            GUIUnstrip.BeginVertical();

            GUIUnstrip.BeginHorizontal();

            if (currentTex)
            {
                if (!IsExpanded)
                {
                    if (GUILayout.Button("v", new GUILayoutOption[] { GUILayout.Width(25) }))
                    {
                        IsExpanded = true;
                    }
                }
                else
                {
                    if (GUILayout.Button("^", new GUILayoutOption[] { GUILayout.Width(25) }))
                    {
                        IsExpanded = false;
                    }
                }
            }

            base.DrawValue(window, width);

            GUILayout.EndHorizontal();

            if (currentTex && IsExpanded)
            {
                DrawTextureControls();
                DrawTexture();
            }

            GUILayout.EndVertical();
        }

        // Temporarily disabled in BepInEx IL2CPP.
        private void DrawTexture()
        {
#if CPP
#if BIE
#else
            GUILayout.Label(texContent, new GUILayoutOption[0]);
#endif
#else
            GUILayout.Label(texContent, new GUILayoutOption[0]);
#endif
        }

        private void DrawTextureControls()
        {
            GUIUnstrip.BeginHorizontal();

            GUILayout.Label("Save folder:", new GUILayoutOption[] { GUILayout.Width(80f) });
            saveFolder = GUIUnstrip.TextField(saveFolder, new GUILayoutOption[0]);
            GUIUnstrip.Space(10f);

            GUILayout.EndHorizontal();

            if (GUILayout.Button("Save to PNG", new GUILayoutOption[] { GUILayout.Width(100f) }))
            {
                if (currentTex)
                {
                    var name = RemoveInvalidFilenameChars(currentTex.name ?? "");
                    if (string.IsNullOrEmpty(name))
                    {
                        if (OwnerCacheObject is CacheMember cacheMember)
                        {
                            name = cacheMember.MemInfo.Name;
                        }
                        else
                        {
                            name = "UNTITLED";
                        }
                    }

                    SaveTextureAsPNG(currentTex, saveFolder, name, false);

                    ExplorerCore.Log($@"Saved to {saveFolder}\{name}.png!");
                }
                else
                {
                    ExplorerCore.Log("Cannot save a null texture!");
                }
            }
        }

        private string RemoveInvalidFilenameChars(string s)
        {
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            foreach (var c in invalid)
            {
                s = s.Replace(c.ToString(), "");
            }
            return s;
        }

        public static void SaveTextureAsPNG(Texture2D tex, string dir, string name, bool isDTXnmNormal = false)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            byte[] data;
            var savepath = dir + @"\" + name + ".png";

            try
            {
                if (isDTXnmNormal)
                {
                    tex = DTXnmToRGBA(tex);
                    tex.Apply(false, false);
                }

                data = tex.EncodeToPNG();

                if (data == null)
                {
                    ExplorerCore.Log("Couldn't get data with EncodeToPNG (probably ReadOnly?), trying manually...");
                    throw new Exception();
                }
            }
            catch
            {
                var origFilter = tex.filterMode;
                tex.filterMode = FilterMode.Point;

                RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height);
                rt.filterMode = FilterMode.Point;
                RenderTexture.active = rt;
                Graphics.Blit(tex, rt);

                Texture2D _newTex = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
                _newTex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);

                if (isDTXnmNormal)
                {
                    _newTex = DTXnmToRGBA(_newTex);
                }

                _newTex.Apply(false, false);

                RenderTexture.active = null;
                tex.filterMode = origFilter;

                data = _newTex.EncodeToPNG();
                //data = _newTex.GetRawTextureData();
            }

            if (data == null || data.Length < 1)
            {
                ExplorerCore.LogWarning("Couldn't get any data for the texture!");
            }
            else
            {
#if CPP
                // The IL2CPP method will return invalid byte data.
                // However, we can just iterate into safe C# byte[] array.
                byte[] safeData = new byte[data.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    safeData[i] = (byte)data[i]; // not sure if cast is needed
                }

                File.WriteAllBytes(savepath, safeData);
#else
                File.WriteAllBytes(savepath, data);
#endif
            }
        }

        // Converts DTXnm-format Normal Map to RGBA-format Normal Map.
        public static Texture2D DTXnmToRGBA(Texture2D tex)
        {
            Color[] colors = tex.GetPixels();

            for (int i = 0; i < colors.Length; i++)
            {
                Color c = colors[i];

                c.r = c.a * 2 - 1;  // red <- alpha
                c.g = c.g * 2 - 1;  // green is always the same

                Vector2 rg = new Vector2(c.r, c.g); //this is the red-green vector
                c.b = Mathf.Sqrt(1 - Mathf.Clamp01(Vector2.Dot(rg, rg))); //recalculate the blue channel

                colors[i] = new Color(
                    (c.r * 0.5f) + 0.5f,
                    (c.g * 0.5f) + 0.25f,
                    (c.b * 0.5f) + 0.5f
                );
            }

            var newtex = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            newtex.SetPixels(colors);

            return newtex;
        }
    }
}
