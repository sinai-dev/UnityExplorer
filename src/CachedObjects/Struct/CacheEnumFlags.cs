using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Explorer
{
    public class CacheEnumFlags : CacheObjectBase, IExpandHeight
    {
        public string[] EnumNames = new string[0];
        public bool[] m_enabledFlags = new bool[0];

        public bool IsExpanded { get; set; }
        public float WhiteSpace { get; set; } = 215f;

        public override void Init()
        {
            base.Init();

            if (ValueType == null && Value != null)
            {
                ValueType = Value.GetType();
            }

            if (ValueType != null)
            {
                EnumNames = Enum.GetNames(ValueType);

                m_enabledFlags = new bool[EnumNames.Length];

                UpdateValue();
            }
            else
            {
                ReflectionException = "Unknown, could not get Enum names.";
            }
        }

        public void SetFlagsFromInput()
        {
            string val = "";
            for (int i = 0; i < EnumNames.Length; i++)
            {
                if (m_enabledFlags[i])
                {
                    if (val != "") val += ", ";
                    val += EnumNames[i];
                }
            }
            Value = Enum.Parse(ValueType, val);
            SetValue();
        }

        public override void UpdateValue()
        {
            base.UpdateValue();

            try
            {
                var enabledNames = Value.ToString().Split(',').Select(it => it.Trim());

                for (int i = 0; i < EnumNames.Length; i++)
                {
                    m_enabledFlags[i] = enabledNames.Contains(EnumNames[i]);
                }
            }
            catch (Exception e)
            {
                ExplorerCore.Log(e.ToString());
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

            GUILayout.Label(Value.ToString() + "<color=#2df7b2><i> (" + ValueType + ")</i></color>", new GUILayoutOption[0]);

            if (IsExpanded)
            {
                GUILayout.EndHorizontal();

                var whitespace = CalcWhitespace(window);

                for (int i = 0; i < EnumNames.Length; i++)
                {
                    GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                    GUIUnstrip.Space(whitespace);

                    m_enabledFlags[i] = GUILayout.Toggle(m_enabledFlags[i], EnumNames[i], new GUILayoutOption[0]);

                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                GUIUnstrip.Space(whitespace);
                if (GUILayout.Button("<color=lime>Apply</color>", new GUILayoutOption[] { GUILayout.Width(155) }))
                {
                    SetFlagsFromInput();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            }
        }
    }
}
