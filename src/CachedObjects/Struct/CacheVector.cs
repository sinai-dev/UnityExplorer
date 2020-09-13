using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;

namespace Explorer
{
    public class CacheVector : CacheObjectBase, IExpandHeight
    {
        public int VectorSize = 2;

        private string x = "0";
        private string y = "0";
        private string z = "0";
        private string w = "0";

        private MethodInfo m_toStringMethod;

        public bool IsExpanded { get; set; }
        public float WhiteSpace { get; set; } = 215f;

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

            GUILayout.Label($"<color=#2df7b2>Vector{VectorSize}</color>: {(string)m_toStringMethod.Invoke(Value, new object[0])}", null);            

            if (CanWrite && IsExpanded)
            {
                GUIUnstrip.EndHorizontal();

                var whitespace = CalcWhitespace(window);

                // always draw x and y
                GUIUnstrip.BeginHorizontal();
                GUIUnstrip.Space(whitespace);
                GUILayout.Label("X:", new GUILayoutOption[] { GUILayout.Width(30) });
                x = GUILayout.TextField(x, new GUILayoutOption[] { GUILayout.Width(120) });
                GUIUnstrip.EndHorizontal();

                GUIUnstrip.BeginHorizontal();
                GUIUnstrip.Space(whitespace);
                GUILayout.Label("Y:", new GUILayoutOption[] { GUILayout.Width(30) });
                y = GUILayout.TextField(y, new GUILayoutOption[] { GUILayout.Width(120) });
                GUIUnstrip.EndHorizontal();

                if (VectorSize > 2)
                {
                    // draw z
                    GUIUnstrip.BeginHorizontal();
                    GUIUnstrip.Space(whitespace);
                    GUILayout.Label("Z:", new GUILayoutOption[] { GUILayout.Width(30) });
                    z = GUILayout.TextField(z, new GUILayoutOption[] { GUILayout.Width(120) });
                    GUIUnstrip.EndHorizontal();
                }
                if (VectorSize > 3)
                {
                    // draw w
                    GUIUnstrip.BeginHorizontal();
                    GUIUnstrip.Space(whitespace);
                    GUILayout.Label("W:", new GUILayoutOption[] { GUILayout.Width(30) });
                    w = GUILayout.TextField(w, new GUILayoutOption[] { GUILayout.Width(120) });
                    GUIUnstrip.EndHorizontal();
                }

                // draw set value button
                GUIUnstrip.BeginHorizontal();
                GUIUnstrip.Space(whitespace);
                if (GUILayout.Button("<color=lime>Apply</color>", new GUILayoutOption[] { GUILayout.Width(155) }))
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
