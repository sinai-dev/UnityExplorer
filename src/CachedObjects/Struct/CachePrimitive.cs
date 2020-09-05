using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace Explorer
{
    public class CachePrimitive : CacheObjectBase
    {
        public enum Types
        {
            Bool,
            Double,
            Float,
            Int,
            String,
            Char
        }

        private string m_valueToString;

        public Types PrimitiveType;

        public MethodInfo ParseMethod => m_parseMethod ?? (m_parseMethod = Value.GetType().GetMethod("Parse", new Type[] { typeof(string) }));
        private MethodInfo m_parseMethod;

        public override void Init()
        {
            if (Value == null)
            {
                // this must mean it is a string. No other primitive type should be nullable.
                PrimitiveType = Types.String;
                return;
            }

            m_valueToString = Value.ToString();

            var type = Value.GetType();
            if (type == typeof(bool))
            {
                PrimitiveType = Types.Bool;
            }
            else if (type == typeof(double))
            {
                PrimitiveType = Types.Double;
            }
            else if (type == typeof(float))
            {
                PrimitiveType = Types.Float;
            }
            else if (type == typeof(char))
            {
                PrimitiveType = Types.Char;
            }
            else if (typeof(int).IsAssignableFrom(type))
            {
                PrimitiveType = Types.Int;
            }
            else
            {
                PrimitiveType = Types.String;
            }
        }

        public override void UpdateValue()
        {
            base.UpdateValue();

            m_valueToString = Value?.ToString();
        }

        public override void DrawValue(Rect window, float width)
        {
            if (PrimitiveType == Types.Bool)
            {
                var b = (bool)Value;
                var label = $"<color={(b ? "lime" : "red")}>{b}</color>";

                if (CanWrite)
                {
                    b = GUILayout.Toggle(b, label, null);
                    if (b != (bool)Value)
                    {
                        SetValueFromInput(b.ToString());
                    }
                }
                else
                {
                    GUILayout.Label(label, null);
                }
            }
            else
            {
                GUILayout.Label("<color=yellow><i>" + PrimitiveType + "</i></color>", new GUILayoutOption[] { GUILayout.Width(50) });

                int dynSize = 25 + (m_valueToString.Length * 15);
                var maxwidth = window.width - 300f;
                if (CanWrite) maxwidth -= 60;

                if (dynSize > maxwidth)
                {
                    m_valueToString = GUILayout.TextArea(m_valueToString, new GUILayoutOption[] { GUILayout.MaxWidth(maxwidth) });
                }
                else
                {
                    m_valueToString = GUILayout.TextField(m_valueToString, new GUILayoutOption[] { GUILayout.MaxWidth(dynSize) });
                }

                if (CanWrite)
                {
                    if (GUILayout.Button("<color=#00FF00>Apply</color>", new GUILayoutOption[] { GUILayout.Width(60) }))
                    {
                        SetValueFromInput(m_valueToString);
                    }
                }

                GUILayout.Space(5);
            }
        }

        public void SetValueFromInput(string valueString)
        {
            if (MemInfo == null)
            {
                MelonLogger.Log("Trying to SetValue but the MemberInfo is null!");
                return;
            }

            if (PrimitiveType == Types.String)
            {
                Value = valueString;
            }
            else
            {
                try
                {
                    Value = ParseMethod.Invoke(null, new object[] { valueString });
                }
                catch (Exception e)
                {
                    MelonLogger.Log("Exception parsing value: " + e.GetType() + ", " + e.Message);
                }
            }

            SetValue();
        }
    }
}
