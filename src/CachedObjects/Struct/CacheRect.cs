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
        public float ButtonWidthOffset { get; set; } = 290f;

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

            GUILayout.Label($"<color=#2df7b2>Rect</color>: {((Rect)Value).ToString()}", null);

            if (CanWrite && IsExpanded)
            {
                GUILayout.EndHorizontal();

                float whitespace = WhiteSpace;
                if (whitespace > 0)
                {
                    ClampLabelWidth(window, ref whitespace);
                }

                GUILayout.BeginHorizontal(null);
                GUILayout.Space(whitespace);
                GUILayout.Label("X:", new GUILayoutOption[] { GUILayout.Width(30) });
                x = GUILayout.TextField(x, new GUILayoutOption[] { GUILayout.Width(120) });
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(null);
                GUILayout.Space(whitespace);
                GUILayout.Label("Y:", new GUILayoutOption[] { GUILayout.Width(30) });
                y = GUILayout.TextField(y, new GUILayoutOption[] { GUILayout.Width(120) });
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(null);
                GUILayout.Space(whitespace);
                GUILayout.Label("W:", new GUILayoutOption[] { GUILayout.Width(30) });
                w = GUILayout.TextField(w, new GUILayoutOption[] { GUILayout.Width(120) });
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(null);
                GUILayout.Space(whitespace);
                GUILayout.Label("H:", new GUILayoutOption[] { GUILayout.Width(30) });
                h = GUILayout.TextField(h, new GUILayoutOption[] { GUILayout.Width(120) });
                GUILayout.EndHorizontal();

                // draw set value button
                GUILayout.BeginHorizontal(null);
                GUILayout.Space(whitespace);
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
