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
                    if (GUIUnstrip.Button("v", new GUILayoutOption[] { GUILayout.Width(25) }))
                    {
                        IsExpanded = true;
                    }
                }
                else
                {
                    if (GUIUnstrip.Button("^", new GUILayoutOption[] { GUILayout.Width(25) }))
                    {
                        IsExpanded = false;
                    }
                }
            }

            //var c = (Color)Value;
            //GUI.color = c;
            GUIUnstrip.Label($"<color=#2df7b2>Color:</color> {((Color)Value).ToString()}");
            //GUI.color = Color.white;

            if (CanWrite && IsExpanded)
            {
                GUIUnstrip.EndHorizontal();

                var whitespace = CalcWhitespace(window);

                GUIUnstrip.BeginHorizontal();
                GUIUnstrip.Space(whitespace);
                GUIUnstrip.Label("R:", new GUILayoutOption[] { GUILayout.Width(30) });
                r = GUIUnstrip.TextField(r, new GUILayoutOption[] { GUILayout.Width(120) });
                GUIUnstrip.EndHorizontal();

                GUIUnstrip.BeginHorizontal();
                GUIUnstrip.Space(whitespace);
                GUIUnstrip.Label("G:", new GUILayoutOption[] { GUILayout.Width(30) });
                g = GUIUnstrip.TextField(g, new GUILayoutOption[] { GUILayout.Width(120) });
                GUIUnstrip.EndHorizontal();

                GUIUnstrip.BeginHorizontal();
                GUIUnstrip.Space(whitespace);
                GUIUnstrip.Label("B:", new GUILayoutOption[] { GUILayout.Width(30) });
                b = GUIUnstrip.TextField(b, new GUILayoutOption[] { GUILayout.Width(120) });
                GUIUnstrip.EndHorizontal();

                GUIUnstrip.BeginHorizontal();
                GUIUnstrip.Space(whitespace);
                GUIUnstrip.Label("A:", new GUILayoutOption[] { GUILayout.Width(30) });
                a = GUIUnstrip.TextField(a, new GUILayoutOption[] { GUILayout.Width(120) });
                GUIUnstrip.EndHorizontal();

                // draw set value button
                GUIUnstrip.BeginHorizontal();
                GUIUnstrip.Space(whitespace);
                if (GUIUnstrip.Button("<color=lime>Apply</color>", new GUILayoutOption[] { GUILayout.Width(155) }))
                {
                    SetValueFromInput();
                }
                GUIUnstrip.EndHorizontal();

                GUIUnstrip.BeginHorizontal();
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
