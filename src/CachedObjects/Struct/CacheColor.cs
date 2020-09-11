using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Explorer
{
    public class CacheColor : CacheObjectBase, IExpandHeight
    {
        private string r = "0";
        private string g = "0";
        private string b = "0";
        private string a = "0";

        public bool IsExpanded { get; set; }
        public float WhiteSpace { get; set; } = 215f;

        public override void UpdateValue()
        {
            base.UpdateValue();

            var color = (Color)Value;

            r = color.r.ToString();
            g = color.g.ToString();
            b = color.b.ToString();
            a = color.a.ToString();
        }

        public override void DrawValue(Rect window, float width)
        {
            if (CanWrite)
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

            //var c = (Color)Value;
            //GUI.color = c;
            GUILayout.Label($"<color=#2df7b2>Color:</color> {((Color)Value).ToString()}", null);
            //GUI.color = Color.white;

            if (CanWrite && IsExpanded)
            {
                GUILayout.EndHorizontal();

                var whitespace = CalcWhitespace(window);

                GUILayout.BeginHorizontal(null);
                GUIUnstrip.Space(whitespace);
                GUILayout.Label("R:", new GUILayoutOption[] { GUILayout.Width(30) });
                r = GUILayout.TextField(r, new GUILayoutOption[] { GUILayout.Width(120) });
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(null);
                GUIUnstrip.Space(whitespace);
                GUILayout.Label("G:", new GUILayoutOption[] { GUILayout.Width(30) });
                g = GUILayout.TextField(g, new GUILayoutOption[] { GUILayout.Width(120) });
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(null);
                GUIUnstrip.Space(whitespace);
                GUILayout.Label("B:", new GUILayoutOption[] { GUILayout.Width(30) });
                b = GUILayout.TextField(b, new GUILayoutOption[] { GUILayout.Width(120) });
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(null);
                GUIUnstrip.Space(whitespace);
                GUILayout.Label("A:", new GUILayoutOption[] { GUILayout.Width(30) });
                a = GUILayout.TextField(a, new GUILayoutOption[] { GUILayout.Width(120) });
                GUILayout.EndHorizontal();

                // draw set value button
                GUILayout.BeginHorizontal(null);
                GUIUnstrip.Space(whitespace);
                if (GUILayout.Button("<color=lime>Apply</color>", new GUILayoutOption[] { GUILayout.Width(155) }))
                {
                    SetValueFromInput();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(null);
            }
        }

        private void SetValueFromInput()
        {
            if (float.TryParse(r, out float fR)
                && float.TryParse(g, out float fG)
                && float.TryParse(b, out float fB)
                && float.TryParse(a, out float fA))
            {
                Value = new Color(fR, fB, fG, fA);
                SetValue();
            }
        }
    }
}
