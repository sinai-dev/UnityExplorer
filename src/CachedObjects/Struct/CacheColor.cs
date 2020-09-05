using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Explorer.CachedObjects
{
    public class CacheColor : CacheObjectBase
    {
        private string r = "0";
        private string g = "0";
        private string b = "0";
        private string a = "0";

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
            GUILayout.Label($"<color=yellow>Color</color>: {(Color)Value}", null);

            if (CanWrite)
            {
                GUILayout.EndHorizontal();
                var whitespace = window.width - width - 90;

                GUILayout.BeginHorizontal(null);
                GUILayout.Space(whitespace);
                GUILayout.Label("R:", new GUILayoutOption[] { GUILayout.Width(30) });
                r = GUILayout.TextField(r, new GUILayoutOption[] { GUILayout.Width(70) });
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(null);
                GUILayout.Space(whitespace);
                GUILayout.Label("G:", new GUILayoutOption[] { GUILayout.Width(30) });
                g = GUILayout.TextField(g, new GUILayoutOption[] { GUILayout.Width(70) });
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(null);
                GUILayout.Space(whitespace);
                GUILayout.Label("B:", new GUILayoutOption[] { GUILayout.Width(30) });
                b = GUILayout.TextField(b, new GUILayoutOption[] { GUILayout.Width(70) });
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(null);
                GUILayout.Space(whitespace);
                GUILayout.Label("A:", new GUILayoutOption[] { GUILayout.Width(30) });
                a = GUILayout.TextField(a, new GUILayoutOption[] { GUILayout.Width(70) });
                GUILayout.EndHorizontal();

                // draw set value button
                GUILayout.BeginHorizontal(null);
                GUILayout.Space(whitespace);
                if (GUILayout.Button("<color=lime>Apply</color>", new GUILayoutOption[] { GUILayout.Width(130) }))
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
            }
        }
    }
}
