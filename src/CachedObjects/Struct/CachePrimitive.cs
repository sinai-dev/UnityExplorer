using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
#if CPP
using UnhollowerRuntimeLib;
#endif
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

        private bool m_canBitwiseOperate;
        private bool m_inBitwiseMode;
        private string m_bitwiseOperatorInput = "0";
        private string m_bitwiseToString;
        //private BitArray m_bitMask; // not needed I think

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

            m_canBitwiseOperate = typeof(int).IsAssignableFrom(ValueType);
        }

        public override void UpdateValue()
        {
            base.UpdateValue();

            RefreshToString();
        }

        public void RefreshToString()
        {
            m_valueToString = Value?.ToString();

            if (m_inBitwiseMode)
            {
                var _int = (int)Value;
                m_bitwiseToString = Convert.ToString(_int, toBase: 2);
            }
        }

        public override void DrawValue(Rect window, float width)
        {
            // bool uses Toggle
            if (m_isBool)
            {
                var b = (bool)Value;
                var label = $"<color={(b ? "lime" : "red")}>{b}</color>";

                if (CanWrite)
                {
                    b = GUILayout.Toggle(b, label, new GUILayoutOption[0]);
                    if (b != (bool)Value)
                    {
                        SetValueFromInput(b.ToString());
                    }
                }
                else
                {
                    GUILayout.Label(label, new GUILayoutOption[0]);
                }

                return;
            }

            // all other non-bool values use TextField

            GUILayout.BeginVertical(new GUILayoutOption[0]);

            GUILayout.BeginHorizontal(new GUILayoutOption[0]);

            // using ValueType.Name instead of ValueTypeName, because we only want the short name.
            GUILayout.Label("<color=#2df7b2><i>" + ValueType.Name + "</i></color>", new GUILayoutOption[] { GUILayout.Width(50) });

            int dynSize = 25 + (m_valueToString.Length * 15);
            var maxwidth = window.width - 310f;
            if (CanWrite) maxwidth -= 60;

            if (dynSize > maxwidth)
            {
                m_valueToString = GUIUnstrip.TextArea(m_valueToString, new GUILayoutOption[] { GUILayout.Width(maxwidth) });
            }
            else
            {
                m_valueToString = GUILayout.TextField(m_valueToString, new GUILayoutOption[] { GUILayout.Width(dynSize) });
            }

            if (CanWrite)
            {
                if (GUILayout.Button("<color=#00FF00>Apply</color>", new GUILayoutOption[] { GUILayout.Width(60) }))
                {
                    SetValueFromInput(m_valueToString);
                    RefreshToString();
                }
            }

            if (m_canBitwiseOperate)
            {
                bool orig = m_inBitwiseMode;
                m_inBitwiseMode = GUILayout.Toggle(m_inBitwiseMode, "Bitwise?", new GUILayoutOption[0]);
                if (orig != m_inBitwiseMode)
                {
                    RefreshToString();
                }
            }

            GUIUnstrip.Space(10);

            GUILayout.EndHorizontal();

            if (m_inBitwiseMode)
            {
                if (CanWrite)
                {
                    GUILayout.BeginHorizontal(new GUILayoutOption[0]);

                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label("RHS:", new GUILayoutOption[] { GUILayout.Width(35) });
                    GUI.skin.label.alignment = TextAnchor.UpperLeft;

                    if (GUILayout.Button("~", new GUILayoutOption[] { GUILayout.Width(25) }))
                    {
                        if (int.TryParse(m_bitwiseOperatorInput, out int bit))
                        {
                            Value = ~bit;
                            RefreshToString();
                        }
                    }

                    if (GUILayout.Button("<<", new GUILayoutOption[] { GUILayout.Width(25) }))
                    {
                        if (int.TryParse(m_bitwiseOperatorInput, out int bit))
                        {
                            Value = (int)Value << bit;
                            RefreshToString();
                        }
                    }
                    if (GUILayout.Button(">>", new GUILayoutOption[] { GUILayout.Width(25) }))
                    {
                        if (int.TryParse(m_bitwiseOperatorInput, out int bit))
                        {
                            Value = (int)Value >> bit;
                            RefreshToString();
                        }
                    }
                    if (GUILayout.Button("|", new GUILayoutOption[] { GUILayout.Width(25) }))
                    {
                        if (int.TryParse(m_bitwiseOperatorInput, out int bit))
                        {
                            Value = (int)Value | bit;
                            RefreshToString();
                        }
                    }
                    if (GUILayout.Button("&", new GUILayoutOption[] { GUILayout.Width(25) }))
                    {
                        if (int.TryParse(m_bitwiseOperatorInput, out int bit))
                        {
                            Value = (int)Value & bit;
                            RefreshToString();
                        }
                    }
                    if (GUILayout.Button("^", new GUILayoutOption[] { GUILayout.Width(25) }))
                    {
                        if (int.TryParse(m_bitwiseOperatorInput, out int bit))
                        {
                            Value = (int)Value ^ bit;
                            RefreshToString();
                        }
                    }

                    m_bitwiseOperatorInput = GUILayout.TextField(m_bitwiseOperatorInput, new GUILayoutOption[] { GUILayout.Width(55) });

                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                GUILayout.Label($"<color=cyan>Binary:</color>", new GUILayoutOption[] { GUILayout.Width(60) });
                GUILayout.TextField(m_bitwiseToString, new GUILayoutOption[0]);
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        public void SetValueFromInput(string valueString)
        {
            if (MemInfo == null)
            {
                ExplorerCore.Log("Trying to SetValue but the MemberInfo is null!");
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

                    //if (m_inBitwiseMode)
                    //{
                    //    var method = typeof(Convert).GetMethod($"To{ValueType.Name}", new Type[] { typeof(string), typeof(int) });
                    //    Value = method.Invoke(null, new object[] { valueString, 2 });
                    //}
                    //else
                    //{
                    //    Value = ParseMethod.Invoke(null, new object[] { valueString });
                    //}
                }
                catch (Exception e)
                {
                    ExplorerCore.Log("Exception parsing value: " + e.GetType() + ", " + e.Message);
                }
            }

            SetValue();
        }
    }
}
