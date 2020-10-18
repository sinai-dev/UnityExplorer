using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Explorer.UI.Shared;
using Explorer.CacheObject;

namespace Explorer.UI
{
    public class InteractiveQuaternion : InteractiveValue, IExpandHeight
    {
        private string x = "0";
        private string y = "0";
        private string z = "0";

        public bool IsExpanded { get; set; }
        public float WhiteSpace { get; set; } = 215f;

        public override void UpdateValue()
        {
            base.UpdateValue();

            if (Value == null) return;

            var euler = ((Quaternion)Value).eulerAngles;

            x = euler.x.ToString();
            y = euler.y.ToString();
            z = euler.z.ToString();
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

            GUILayout.Label($"<color=#2df7b2>Quaternion</color>: {((Quaternion)Value).eulerAngles.ToString()}", new GUILayoutOption[0]);

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
                GUILayout.Label("Z:", new GUILayoutOption[] { GUILayout.Width(30) });
                z = GUIHelper.TextField(z, new GUILayoutOption[] { GUILayout.Width(120) });
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
                && float.TryParse(z, out float fZ))
            {
                Value = Quaternion.Euler(new Vector3(fX, fY, fZ));
                OwnerCacheObject.SetValue();
            }
        }
    }
}
