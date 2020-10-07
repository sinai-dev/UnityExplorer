using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if CPP
using UnhollowerRuntimeLib;
#endif
using Explorer.UI.Shared;
using Explorer.CacheObject;
using Explorer.Config;

namespace Explorer.UI
{
    public class InteractivePrimitive : InteractiveValue
    {
        private string m_valueToString;
        private bool m_isBool;
        private bool m_isString;

        public MethodInfo ParseMethod => m_parseMethod ?? (m_parseMethod = Value.GetType().GetMethod("Parse", new Type[] { typeof(string) }));
        private MethodInfo m_parseMethod;

        private bool m_canBitwiseOperate;
        private bool m_inBitwiseMode;
        private string m_bitwiseOperatorInput = "0";
        private string m_binaryInput;

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

            UpdateValue();
        }

        public override void UpdateValue()
        {
            base.UpdateValue();

            RefreshToString();
        }

        private void RefreshToString()
        {
            m_valueToString = Value?.ToString();

            if (m_canBitwiseOperate && Value != null)
            {
                var _int = (int)Value;
                m_binaryInput = Convert.ToString(_int, toBase: 2);
            }
        }

        public override void DrawValue(Rect window, float width)
        {
            if (m_isBool)
            {
                var b = (bool)Value;
                var label = $"<color={(b ? "lime" : "red")}>{b}</color>";

                if (OwnerCacheObject.CanWrite)
                {
                    b = GUILayout.Toggle(b, label, new GUILayoutOption[0]);
                    if (b != (bool)Value)
                    {
                        Value = b;
                        OwnerCacheObject.SetValue();
                    }
                }
                else
                {
                    GUILayout.Label(label, new GUILayoutOption[0]);
                }

                return;
            }

            // all other non-bool values use TextField

            GUIUnstrip.BeginVertical(new GUILayoutOption[0]);

            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);

            GUILayout.Label("<color=#2df7b2><i>" + ValueType.Name + "</i></color>", new GUILayoutOption[] { GUILayout.Width(50) });

            m_valueToString = GUIUnstrip.TextArea(m_valueToString, new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
            if (OwnerCacheObject.CanWrite)
            {
                if (GUILayout.Button("<color=#00FF00>Apply</color>", new GUILayoutOption[] { GUILayout.Width(60) }))
                {
                    SetValueFromInput();
                }
            }

            if (ModConfig.Instance.Bitwise_Support && m_canBitwiseOperate)
            {
                m_inBitwiseMode = GUILayout.Toggle(m_inBitwiseMode, "Bitwise?", new GUILayoutOption[0]);
            }

            GUIUnstrip.Space(10);

            GUILayout.EndHorizontal();

            if (ModConfig.Instance.Bitwise_Support && m_inBitwiseMode)
            {
                DrawBitwise();
            }

            GUILayout.EndVertical();
        }

        private void DrawBitwise()
        {
            if (OwnerCacheObject.CanWrite)
            {
                GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);

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

                m_bitwiseOperatorInput = GUIUnstrip.TextField(m_bitwiseOperatorInput, new GUILayoutOption[] { GUILayout.Width(55) });

                GUILayout.EndHorizontal();
            }

            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label($"<color=cyan>Binary:</color>", new GUILayoutOption[] { GUILayout.Width(60) });
            m_binaryInput = GUIUnstrip.TextField(m_binaryInput, new GUILayoutOption[0]);
            if (OwnerCacheObject.CanWrite)
            {
                if (GUILayout.Button("Apply", new GUILayoutOption[0]))
                {
                    SetValueFromBinaryInput();
                }
            }
            GUILayout.EndHorizontal();
        }

        public void SetValueFromInput()
        {
            if (m_isString)
            {
                Value = m_valueToString;
            }
            else
            {
                try
                {
                    Value = ParseMethod.Invoke(null, new object[] { m_valueToString });
                }
                catch (Exception e)
                {
                    ExplorerCore.Log("Exception parsing value: " + e.GetType() + ", " + e.Message);
                }
            }

            OwnerCacheObject.SetValue();
            RefreshToString();
        }

        private void SetValueFromBinaryInput()
        {
            try
            {
                var method = typeof(Convert).GetMethod($"To{ValueType.Name}", new Type[] { typeof(string), typeof(int) });
                Value = method.Invoke(null, new object[] { m_binaryInput, 2 });

                OwnerCacheObject.SetValue();
                RefreshToString();
            }
            catch (Exception e)
            {
                ExplorerCore.Log("Exception setting value: " + e.GetType() + ", " + e.Message);
            }
        }
    }
}
