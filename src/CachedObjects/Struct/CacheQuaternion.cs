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
        private Vector3 EulerAngle = Vector3.zero;

        private string x = "0";
        private string y = "0";
        private string z = "0";

        public override void UpdateValue()
        {
            base.UpdateValue();

            EulerAngle = ((Quaternion)Value).eulerAngles;

            x = EulerAngle.x.ToString();
            y = EulerAngle.y.ToString();
            z = EulerAngle.z.ToString();
        }

        public override void DrawValue(Rect window, float width)
        {
            GUILayout.Label($"<color=yellow>Quaternion</color>: {((Quaternion)Value).eulerAngles}", null);

            if (CanWrite)
            {
                GUILayout.EndHorizontal();
                var whitespace = window.width - width - 90;

                GUILayout.BeginHorizontal(null);
                GUILayout.Space(whitespace);
                GUILayout.Label("X:", new GUILayoutOption[] { GUILayout.Width(30) });
                x = GUILayout.TextField(x, new GUILayoutOption[] { GUILayout.Width(70) });
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(null);
                GUILayout.Space(whitespace);
                GUILayout.Label("Y:", new GUILayoutOption[] { GUILayout.Width(30) });
                y = GUILayout.TextField(y, new GUILayoutOption[] { GUILayout.Width(70) });
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(null);
                GUILayout.Space(whitespace);
                GUILayout.Label("Z:", new GUILayoutOption[] { GUILayout.Width(30) });
                z = GUILayout.TextField(z, new GUILayoutOption[] { GUILayout.Width(70) });
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
