using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace Explorer
{
    public class CachePrimitive : CacheObjectBase
    {
        private bool m_isBool;
        private bool m_isString;

        private string m_valueToString;

        public MethodInfo ParseMethod => m_parseMethod ?? (m_parseMethod = Value.GetType().GetMethod("Parse", new Type[] { typeof(string) }));
        private MethodInfo m_parseMethod;

        public override void Init()
        {
            if (ValueType == null)
            {
                ValueType = Value?.GetType();

                // has to be a string at this point
                if (ValueType == null)
                {
                    ValueType = typeof(string);
                }
            }

            if (ValueType == typeof(string))
            {
                m_isString = true;
            }
            else if (ValueType == typeof(bool))
            {
                m_isBool = true;
            }
        }

        public override void UpdateValue()
        {
            base.UpdateValue();

            m_valueToString = Value?.ToString();
        }

        public override void DrawValue(Rect window, float width)
        {
            if (m_isBool)
            {
                var b = (bool)Value;
                var label = $"<color={(b ? "lime" : "red")}>{b}</color>";

                if (CanWrite)
                {
                    b = GUIUnstrip.Toggle(b, label);
                    if (b != (bool)Value)
                    {
                        SetValueFromInput(b.ToString());
                    }
                }
                else
                {
                    GUIUnstrip.Label(label);
                }
            }
            else
            {
                // using ValueType.Name instead of ValueTypeName, because we only want the short name.
                GUIUnstrip.Label("<color=#2df7b2><i>" + ValueType.Name + "</i></color>", new GUILayoutOption[] { GUILayout.Width(50) });

                int dynSize = 25 + (m_valueToString.Length * 15);
                var maxwidth = window.width - 310f;
                if (CanWrite) maxwidth -= 60;

                if (dynSize > maxwidth)
                {
                    m_valueToString = GUIUnstrip.TextArea(m_valueToString, new GUILayoutOption[] { GUILayout.MaxWidth(maxwidth) });
                }
                else
                {
                    m_valueToString = GUIUnstrip.TextField(m_valueToString, new GUILayoutOption[] { GUILayout.MaxWidth(dynSize) });
                }

                if (CanWrite)
                {
                    if (GUIUnstrip.Button("<color=#00FF00>Apply</color>", new GUILayoutOption[] { GUILayout.Width(60) }))
                    {
                        SetValueFromInput(m_valueToString);
                    }
                }

                GUIUnstrip.Space(10);
            }
        }

        public void SetValueFromInput(string valueString)
        {
            if (MemInfo == null)
            {
                MelonLogger.Log("Trying to SetValue but the MemberInfo is null!");
                return;
            }

            if (m_isString)
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
