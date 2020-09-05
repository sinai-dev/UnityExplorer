using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Explorer
{
    public class CacheRect : CacheObjectBase
    {
        private string x = "0";
        private string y = "0";
        private string w = "0";
        private string h = "0";

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
            GUILayout.Label($"<color=yellow>Rect</color>: {((Rect)Value).ToString()}", null);

            if (CanWrite)
            {
                GUILayout.EndHorizontal();
                var whitespace = window.width - width - 90;

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
