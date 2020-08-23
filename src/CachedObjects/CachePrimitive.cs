using System;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace Explorer
{
    public class CachePrimitive : CacheObject
    {
        public enum PrimitiveTypes
        {
            Bool,
            Double,
            Float,
            Int,
            String
        }

        private string m_valueToString;

        public PrimitiveTypes PrimitiveType;

        public MethodInfo ParseMethod
        {
            get
            {
                if (m_parseMethod == null)
                {
                    Type t = null;
                    switch (PrimitiveType)
                    {
                        case PrimitiveTypes.Bool:
                            t = typeof(bool); break;
                        case PrimitiveTypes.Double:
                            t = typeof(double); break;
                        case PrimitiveTypes.Float:
                            t = typeof(float); break;
                        case PrimitiveTypes.Int:
                            t = typeof(int); break;
                        case PrimitiveTypes.String:
                            t = typeof(string); break;
                    }
                    m_parseMethod = t.GetMethod("Parse", new Type[] { typeof(string) });
                }
                return m_parseMethod;
            }
        }

        private MethodInfo m_parseMethod;

        public override void Init()
        {
            if (Value == null)
            {
                // this must mean it is a string? no other primitive type should be nullable
                PrimitiveType = PrimitiveTypes.String;
                return;
            }

            m_valueToString = Value.ToString();
            var type = Value.GetType();

            if (type == typeof(bool))
            {
                PrimitiveType = PrimitiveTypes.Bool;
            }
            else if (type == typeof(double))
            {
                PrimitiveType = PrimitiveTypes.Double;
            }
            else if (type == typeof(float))
            {
                PrimitiveType = PrimitiveTypes.Float;
            }
            else if (type == typeof(int) || type == typeof(long) || type == typeof(uint) || type == typeof(ulong) || type == typeof(IntPtr))
            {
                PrimitiveType = PrimitiveTypes.Int;
            }
            else
            {
                if (type != typeof(string))
                {
                    MelonLogger.Log("Unsupported primitive: " + type);
                }

                PrimitiveType = PrimitiveTypes.String;
            }
        }

        public override void UpdateValue()
        {
            base.UpdateValue();

            m_valueToString = Value?.ToString();
        }

        public override void DrawValue(Rect window, float width)
        {
            if (PrimitiveType == PrimitiveTypes.Bool)
            {
                var b = (bool)Value;
                var color = "<color=" + (b ? "lime>" : "red>");
                b = GUILayout.Toggle(b, color + b.ToString() + "</color>", null);

                if (b != (bool)Value)
                {
                    SetValue(m_valueToString);
                }
            }
            else
            {
                GUILayout.Label("<color=yellow><i>" + PrimitiveType + "</i></color>", new GUILayoutOption[] { GUILayout.Width(50) });

                var _width = window.width - 200;
                if (m_valueToString.Length > 37)
                {
                    m_valueToString = GUILayout.TextArea(m_valueToString, new GUILayoutOption[] { GUILayout.MaxWidth(_width) });
                }
                else
                {
                    m_valueToString = GUILayout.TextField(m_valueToString, new GUILayoutOption[] { GUILayout.MaxWidth(_width) });
                }

                if (CanWrite)
                {
                    if (GUILayout.Button("<color=#00FF00>Apply</color>", new GUILayoutOption[] { GUILayout.Width(60) }))
                    {
                        SetValue(m_valueToString);
                    }
                }
            }
        }

        public void SetValue(string value)
        {
            if (MemberInfo == null)
            {
                MelonLogger.Log("Trying to SetValue but the MemberInfo is null!");
                return;
            }

            if (PrimitiveType == PrimitiveTypes.String)
            {
                Value = value;
            }
            else
            {
                try
                {
                    var val = ParseMethod.Invoke(null, new object[] { value });
                    Value = val;                    
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
