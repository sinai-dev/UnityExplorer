using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Explorer.CacheObject;
using Explorer.Config;
using UnityEngine;
using System.IO;
using Explorer.Helpers;
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
            }
        }

        public virtual void GetGUIContent()
        {
            texContent = new GUIContent
            {
                image = currentTex
            };
        }

        public override void DrawValue(Rect window, float width)
        {
            GUIHelper.BeginVertical();

            GUIHelper.BeginHorizontal();

            if (currentTex && !IsExpanded)
            {
                if (GUILayout.Button("v", new GUILayoutOption[] { GUILayout.Width(25) }))
                {
                    IsExpanded = true;
                    GetGUIContent();
                }
            }
            else if (currentTex)
            {
                if (GUILayout.Button("^", new GUILayoutOption[] { GUILayout.Width(25) }))
                {
                    IsExpanded = false;
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
            GUIHelper.BeginHorizontal();

            GUILayout.Label("Save folder:", new GUILayoutOption[] { GUILayout.Width(80f) });
            saveFolder = GUIHelper.TextField(saveFolder, new GUILayoutOption[0]);
            GUIHelper.Space(10f);

            GUILayout.EndHorizontal();

            if (GUILayout.Button("Save to PNG", new GUILayoutOption[] { GUILayout.Width(100f) }))
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

                Texture2DHelpers.SaveTextureAsPNG(currentTex, saveFolder, name, false);

                ExplorerCore.Log($@"Saved to {saveFolder}\{name}.png!");
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

        
    }
}
