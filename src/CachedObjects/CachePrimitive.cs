using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;

namespace Explorer
{
    public class CachePrimitive : CacheObject
    {
        public enum PrimitiveType
        {
            Bool,
            Double,
            Float,
            Int,
            String
        }

        private readonly PrimitiveType m_primitiveType;

        public CachePrimitive(object obj)
        {
            if (obj is bool)
            {
                m_primitiveType = PrimitiveType.Bool;
            }
            else if (obj is double)
            {
                m_primitiveType = PrimitiveType.Double;
            }
            else if (obj is float)
            {
                m_primitiveType = PrimitiveType.Float;
            }
            else if (obj is int)
            {
                m_primitiveType = PrimitiveType.Int;
            }
            else if (obj is string)
            {
                m_primitiveType = PrimitiveType.String;
            }
        }

        public override void DrawValue(Rect window, float width)
        {
            if (m_primitiveType == PrimitiveType.Bool && Value is bool b)
            {
                var color = "<color=" + (b ? "lime>" : "red>");
                Value = GUILayout.Toggle((bool)Value, color + Value.ToString() + "</color>", null);

                if (b != (bool)Value)
                {
                    SetValue();
                }
            }
            else
            {
                var toString = Value.ToString();
                if (toString.Length > 37)
                {
                    Value = GUILayout.TextArea(toString, new GUILayoutOption[] { GUILayout.MaxWidth(window.width - 260) });
                }
                else
                {
                    Value = GUILayout.TextField(toString, new GUILayoutOption[] { GUILayout.MaxWidth(window.width - 260) });
                }
                
                if (MemberInfo != null)
                {
                    if (GUILayout.Button("<color=#00FF00>Apply</color>", new GUILayoutOption[] { GUILayout.Width(60) }))
                    {
                        SetValue();
                    }
                }
            }
        }

        public override void SetValue()
        {
            if (MemberInfo == null)
            {
                MelonLogger.Log("Trying to SetValue but the MemberInfo is null!");
                return;
            }

            switch (m_primitiveType)
            {
                case PrimitiveType.Bool:
                    SetValue(bool.Parse(Value.ToString()), MemberInfo, DeclaringInstance); return;
                case PrimitiveType.Double:
                    SetValue(double.Parse(Value.ToString()), MemberInfo, DeclaringInstance); return;
                case PrimitiveType.Float:
                    SetValue(float.Parse(Value.ToString()), MemberInfo, DeclaringInstance); return;
                case PrimitiveType.Int:
                    SetValue(int.Parse(Value.ToString()), MemberInfo, DeclaringInstance); return;
                case PrimitiveType.String:
                    SetValue(Value.ToString(), MemberInfo, DeclaringInstance); return;
            }
        }
    }
}
