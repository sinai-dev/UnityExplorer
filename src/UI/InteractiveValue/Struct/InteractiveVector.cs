using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace Explorer.UI
{
    public class InteractiveVector : InteractiveValue, IExpandHeight
    {
        public int VectorSize = 2;

        private string x = "0";
        private string y = "0";
        private string z = "0";
        private string w = "0";

        //private MethodInfo m_toStringMethod;

        public bool IsExpanded { get; set; }
        public float WhiteSpace { get; set; } = 215f;

        public override void Init()
        {
            if (ValueType == null && Value != null)
            {
                ValueType = Value.GetType();
            }

            if (ValueType == typeof(Vector2))
            {
                VectorSize = 2;
                //m_toStringMethod = typeof(Vector2).GetMethod("ToString", new Type[0]);
            }
            else if (ValueType == typeof(Vector3))
            {
                VectorSize = 3;
                //m_toStringMethod = typeof(Vector3).GetMethod("ToString", new Type[0]);
            }
            else
            {
                VectorSize = 4;
                //m_toStringMethod = typeof(Vector4).GetMethod("ToString", new Type[0]);
            }

            base.Init();
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

            GUILayout.Label($"<color=#2df7b2>Vector{VectorSize}</color>: {(string)ToStringMethod.Invoke(Value, new object[0])}", new GUILayoutOption[0]);

            if (OwnerCacheObject.CanWrite && IsExpanded)
            {
                GUILayout.EndHorizontal();

                var whitespace = CalcWhitespace(window);

                // always draw x and y
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

                if (VectorSize > 2)
                {
                    // draw z
                    GUIHelper.BeginHorizontal(new GUILayoutOption[0]);
                    GUIHelper.Space(whitespace);
                    GUILayout.Label("Z:", new GUILayoutOption[] { GUILayout.Width(30) });
                    z = GUIHelper.TextField(z, new GUILayoutOption[] { GUILayout.Width(120) });
                    GUILayout.EndHorizontal();
                }
                if (VectorSize > 3)
                {
                    // draw w
                    GUIHelper.BeginHorizontal(new GUILayoutOption[0]);
                    GUIHelper.Space(whitespace);
                    GUILayout.Label("W:", new GUILayoutOption[] { GUILayout.Width(30) });
                    w = GUIHelper.TextField(w, new GUILayoutOption[] { GUILayout.Width(120) });
                    GUILayout.EndHorizontal();
                }

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
                    OwnerCacheObject.SetValue();
                }
            }
        }
    }
}
