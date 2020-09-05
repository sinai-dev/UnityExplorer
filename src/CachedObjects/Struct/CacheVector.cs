using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;

namespace Explorer
{
    public class CacheVector : CacheObjectBase
    {
        public int VectorSize = 2;

        private string x = "0";
        private string y = "0";
        private string z = "0";
        private string w = "0";

        private MethodInfo m_toStringMethod;

        public override void Init()
        {
            if (Value is Vector2)
            {
                VectorSize = 2;
            }
            else if (Value is Vector3)
            {
                VectorSize = 3;
            }
            else
            {
                VectorSize = 4;
            }

            m_toStringMethod = Value.GetType().GetMethod("ToString", new Type[0]);
        }

        public override void UpdateValue()
        {
            base.UpdateValue();

            if (Value is Vector2 vec2)
            {
                x = vec2.x.ToString();
                y = vec2.y.ToString();
            }
            else if (Value is Vector3 vec3)
            {
                x = vec3.x.ToString();
                y = vec3.y.ToString();
                z = vec3.z.ToString();
            }
            else if (Value is Vector4 vec4)
            {
                x = vec4.x.ToString();
                y = vec4.y.ToString();
                z = vec4.z.ToString();
                w = vec4.w.ToString();
            }
        }

        public override void DrawValue(Rect window, float width)
        {
            GUILayout.Label($"<color=yellow>Vector{VectorSize}</color>: {(string)m_toStringMethod.Invoke(Value, new object[0])}", null);            

            if (CanWrite)
            {
                GUILayout.EndHorizontal();
                var whitespace = window.width - width - 90;

                // always draw x and y
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

                if (VectorSize > 2)
                {
                    // draw z
                    GUILayout.BeginHorizontal(null);
                    GUILayout.Space(whitespace);
                    GUILayout.Label("Z:", new GUILayoutOption[] { GUILayout.Width(30) });
                    z = GUILayout.TextField(z, new GUILayoutOption[] { GUILayout.Width(70) });
                    GUILayout.EndHorizontal();
                }
                if (VectorSize > 3)
                {
                    // draw w
                    GUILayout.BeginHorizontal(null);
                    GUILayout.Space(whitespace);
                    GUILayout.Label("W:", new GUILayoutOption[] { GUILayout.Width(30) });
                    w = GUILayout.TextField(w, new GUILayoutOption[] { GUILayout.Width(70) });
                    GUILayout.EndHorizontal();
                }

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
                && float.TryParse(z, out float fZ)
                && float.TryParse(w, out float fW))
            {
                object vector = null;

                switch (VectorSize)
                {
                    case 2: vector = new Vector2(fX, fY); break;
                    case 3: vector = new Vector3(fX, fY, fZ); break;
                    case 4: vector = new Vector4(fX, fY, fZ, fW); break;
                }

                if (vector != null)
                {
                    Value = vector;
                    SetValue();
                }
            }
        }
    }
}
