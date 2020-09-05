using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Explorer
{
    public class CacheQuaternion : CacheObjectBase
    {
        private string x = "0";
        private string y = "0";
        private string z = "0";

        public override void UpdateValue()
        {
            base.UpdateValue();

            var euler = ((Quaternion)Value).eulerAngles;

            x = euler.x.ToString();
            y = euler.y.ToString();
            z = euler.z.ToString();
        }

        public override void DrawValue(Rect window, float width)
        {
            GUILayout.Label($"<color=yellow>Quaternion</color>: {((Quaternion)Value).eulerAngles.ToString()}", null);

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
                GUILayout.Label("Z:", new GUILayoutOption[] { GUILayout.Width(30) });
                z = GUILayout.TextField(z, new GUILayoutOption[] { GUILayout.Width(120) });
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
                && float.TryParse(z, out float fZ))
            {
                Value = Quaternion.Euler(new Vector3(fX, fY, fZ));
                SetValue();
            }
        }
    }
}
