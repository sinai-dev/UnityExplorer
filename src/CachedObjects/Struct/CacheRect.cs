using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Explorer
{
    public class CacheRect : CacheObjectBase, IExpandHeight
    {
        private string x = "0";
        private string y = "0";
        private string w = "0";
        private string h = "0";

        public bool IsExpanded { get; set; }
        public float WhiteSpace { get; set; } = 215f;

        public override void UpdateValue()
        {
            base.UpdateValue();

            var rect = (Rect)Value;

            x = rect.x.ToString();
            y = rect.y.ToString();
            w = rect.width.ToString();
            h = rect.height.ToString();
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

            GUIUnstrip.Label($"<color=#2df7b2>Rect</color>: {((Rect)Value).ToString()}");

            if (CanWrite && IsExpanded)
            {
                GUIUnstrip.EndHorizontal();

                var whitespace = CalcWhitespace(window);

                GUIUnstrip.BeginHorizontal();
                GUIUnstrip.Space(whitespace);
                GUIUnstrip.Label("X:", new GUILayoutOption[] { GUILayout.Width(30) });
                x = GUIUnstrip.TextField(x, new GUILayoutOption[] { GUILayout.Width(120) });
                GUIUnstrip.EndHorizontal();

                GUIUnstrip.BeginHorizontal();
                GUIUnstrip.Space(whitespace);
                GUIUnstrip.Label("Y:", new GUILayoutOption[] { GUILayout.Width(30) });
                y = GUIUnstrip.TextField(y, new GUILayoutOption[] { GUILayout.Width(120) });
                GUIUnstrip.EndHorizontal();

                GUIUnstrip.BeginHorizontal();
                GUIUnstrip.Space(whitespace);
                GUIUnstrip.Label("W:", new GUILayoutOption[] { GUILayout.Width(30) });
                w = GUIUnstrip.TextField(w, new GUILayoutOption[] { GUILayout.Width(120) });
                GUIUnstrip.EndHorizontal();

                GUIUnstrip.BeginHorizontal();
                GUIUnstrip.Space(whitespace);
                GUIUnstrip.Label("H:", new GUILayoutOption[] { GUILayout.Width(30) });
                h = GUIUnstrip.TextField(h, new GUILayoutOption[] { GUILayout.Width(120) });
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
            if (float.TryParse(x, out float fX)
                && float.TryParse(y, out float fY)
                && float.TryParse(w, out float fW)
                && float.TryParse(h, out float fH))
            {
                Value = new Rect(fX, fY, fW, fH);
                SetValue();
            }
        }
    }
}
