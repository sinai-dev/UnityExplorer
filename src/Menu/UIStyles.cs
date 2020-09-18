using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Explorer
{
    public class UIStyles
    {
        public class Syntax
        {
            public const string Field_Static = "#8d8dc6";
            public const string Field_Instance = "#c266ff";

            public const string Method_Static = "#b55b02";
            public const string Method_Instance = "#ff8000";

            public const string Prop_Static = "#588075";
            public const string Prop_Instance = "#55a38e";

            public const string Class_Static = "#3a8d71";
            public const string Class_Instance = "#2df7b2";

            public const string Local = "#a6e9e9";

            public const string StructGreen = "#b8d7a3";
        }

        public static Color LightGreen = new Color(Color.green.r - 0.3f, Color.green.g - 0.3f, Color.green.b - 0.3f);

        public static GUISkin WindowSkin
        {
            get
            {
                if (_customSkin == null)
                {
                    try
                    {
                        _customSkin = CreateWindowSkin();
                    }
                    catch
                    {
                        _customSkin = GUI.skin;
                    }
                }

                return _customSkin;
            }
        }

        public static void HorizontalLine(Color _color, bool small = false)
        {
            var orig = GUI.color;

            GUI.color = _color;
            GUILayout.Box(GUIContent.none, !small ? HorizontalBar : HorizontalBarSmall, null);

            GUI.color = orig;
        }

        private static GUISkin _customSkin;

        public static Texture2D m_nofocusTex;
        public static Texture2D m_focusTex;

        private static GUIStyle HorizontalBar
        {
            get
            {
                if (_horizBarStyle == null)
                {
                    _horizBarStyle = new GUIStyle();
                    _horizBarStyle.normal.background = Texture2D.whiteTexture;
                    var rectOffset = new RectOffset();
                    rectOffset.top = 4;
                    rectOffset.bottom = 4;
                    _horizBarStyle.margin = rectOffset;
                    _horizBarStyle.fixedHeight = 2;
                }

                return _horizBarStyle;
            }
        }
        private static GUIStyle _horizBarStyle;

        private static GUIStyle HorizontalBarSmall
        {
            get
            {
                if (_horizBarSmallStyle == null)
                {
                    _horizBarSmallStyle = new GUIStyle();
                    _horizBarSmallStyle.normal.background = Texture2D.whiteTexture;
                    var rectOffset = new RectOffset();
                    rectOffset.top = 2;
                    rectOffset.bottom = 2;
                    _horizBarSmallStyle.margin = rectOffset;
                    _horizBarSmallStyle.fixedHeight = 1;
                }

                return _horizBarSmallStyle;
            }
        }
        private static GUIStyle _horizBarSmallStyle;

        private static GUISkin CreateWindowSkin()
        {
            var newSkin = Object.Instantiate(GUI.skin);
            Object.DontDestroyOnLoad(newSkin);

            m_nofocusTex = MakeTex(1, 1, new Color(0.1f, 0.1f, 0.1f, 0.7f));
            m_focusTex = MakeTex(1, 1, new Color(0.3f, 0.3f, 0.3f, 1f));

            newSkin.window.normal.background = m_nofocusTex;
            newSkin.window.onNormal.background = m_focusTex;

            newSkin.box.normal.textColor = Color.white;
            newSkin.window.normal.textColor = Color.white;
            newSkin.button.normal.textColor = Color.white;
            newSkin.textField.normal.textColor = Color.white;
            newSkin.label.normal.textColor = Color.white;

            return newSkin;
        }

        public static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
