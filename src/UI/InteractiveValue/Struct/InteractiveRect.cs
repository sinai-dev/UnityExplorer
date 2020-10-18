using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Explorer.UI.Shared;
using Explorer.CacheObject;

namespace Explorer.UI
{
    public class InteractiveRect : InteractiveValue, IExpandHeight
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

            if (Value == null) return;

            var rect = (Rect)Value;

            x = rect.x.ToString();
            y = rect.y.ToString();
            w = rect.width.ToString();
            h = rect.height.ToString();
        }

        public override void DrawValue(Rect window, float width)
        {
            if (OwnerCacheObject.CanWrite)
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

            GUILayout.Label($"<color=#2df7b2>Rect</color>: {((Rect)Value).ToString()}", new GUILayoutOption[0]);

            if (OwnerCacheObject.CanWrite && IsExpanded)
            {
                GUILayout.EndHorizontal();

                var whitespace = CalcWhitespace(window);

                GUIHelper.BeginHorizontal(new GUILayoutOption[0]);
                GUIHelper.Space(whitespace);
                GUILayout.Label("X:", new GUILayoutOption[] { GUILayout.Width(30) });
                x = GUIHelper.TextField(x, new GUILayoutOption[] { GUILayout.Width(120) });
                GUILayout.EndHorizontal();

                GUIHelper.BeginHorizontal(new GUILayoutOption[0]);
                GUIHelper.Space(whitespace);
                GUILayout.Label("Y:", new GUILayoutOption[] { GUILayout.Width(30) });
                y = GUIHelper.TextField(y, new GUILayoutOption[] { GUILayout.Width(120) });
                GUILayout.EndHorizontal();

                GUIHelper.BeginHorizontal(new GUILayoutOption[0]);
                GUIHelper.Space(whitespace);
                GUILayout.Label("W:", new GUILayoutOption[] { GUILayout.Width(30) });
                w = GUIHelper.TextField(w, new GUILayoutOption[] { GUILayout.Width(120) });
                GUILayout.EndHorizontal();

                GUIHelper.BeginHorizontal(new GUILayoutOption[0]);
                GUIHelper.Space(whitespace);
                GUILayout.Label("H:", new GUILayoutOption[] { GUILayout.Width(30) });
                h = GUIHelper.TextField(h, new GUILayoutOption[] { GUILayout.Width(120) });
                GUILayout.EndHorizontal();

                // draw set value button
                GUIHelper.BeginHorizontal(new GUILayoutOption[0]);
                GUIHelper.Space(whitespace);
                if (GUILayout.Button("<color=lime>Apply</color>", new GUILayoutOption[] { GUILayout.Width(155) }))
                {
                    SetValueFromInput();
                }
                GUILayout.EndHorizontal();

                GUIHelper.BeginHorizontal(new GUILayoutOption[0]);
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
                OwnerCacheObject.SetValue();
            }
        }
    }
}
