using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace Explorer
{
    public class CachePrimitive : CacheObjectBase
    {
        public enum PrimitiveTypes
        {
            Bool,
            Double,
            Float,
            Int,
            String,
            Char
        }

        private string m_valueToString;

        public PrimitiveTypes PrimitiveType;

        public MethodInfo ParseMethod
        {
            get
            {
                if (m_parseMethod == null)
                {
                    m_parseMethod = Value.GetType().GetMethod("Parse", new Type[] { typeof(string) });
                }
                return m_parseMethod;
            }
        }

        private MethodInfo m_parseMethod;

        public override void Init()
        {
            if (Value == null)
            {
                // this must mean it is a string. No other primitive type should be nullable.
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
            else if (IsInteger(type))
            {
                PrimitiveType = PrimitiveTypes.Int;
            }
            else if (type == typeof(char))
            {
                PrimitiveType = PrimitiveTypes.Char;
            }
            else
            {
                PrimitiveType = PrimitiveTypes.String;
            }
        }

        private static bool IsInteger(Type type)
        {
            // For our purposes, all types of int can be treated the same, including IntPtr.
            return _integerTypes.Contains(type);
        }

        private static readonly HashSet<Type> _integerTypes = new HashSet<Type>
        {
            typeof(int),
            typeof(uint),
            typeof(short),
            typeof(ushort),
            typeof(long),
            typeof(ulong),
            typeof(byte),
            typeof(sbyte),
            typeof(IntPtr)
        };

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
                var color = $"<color={(b ? "lime>" : "red>")}";
                var label = $"{color}{b}</color>";

                if (CanWrite)
                {
                    b = GUILayout.Toggle(b, label, null);
                    if (b != (bool)Value)
                    {
                        SetValue(m_valueToString);
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
                        SetValue(m_valueToString);
                    }
                }

                GUILayout.Space(5);
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
