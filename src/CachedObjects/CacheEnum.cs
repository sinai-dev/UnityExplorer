using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;

namespace Explorer
{
    public class CacheEnum : CacheObject
    {
        private readonly Type m_enumType;
        private readonly string[] m_names;

        public CacheEnum(object obj)
        {
            m_enumType = obj.GetType();
            m_names = Enum.GetNames(obj.GetType());
        }

        public override void DrawValue(Rect window, float width)
        {
            if (MemberInfo != null)
            {
                if (GUILayout.Button("<", new GUILayoutOption[] { GUILayout.Width(25) }))
                {
                    SetEnum(ref Value, -1);
                    SetValue();
                }
                if (GUILayout.Button(">", new GUILayoutOption[] { GUILayout.Width(25) }))
                {
                    SetEnum(ref Value, 1);
                    SetValue();
                }
            }

            GUILayout.Label(Value.ToString(), null);
        }

        public override void SetValue()
        {
            if (MemberInfo == null)
            {
                MelonLogger.Log("Trying to SetValue but the MemberInfo is null!");
                return;
            }

            if (Enum.Parse(m_enumType, Value.ToString()) is object enumValue && enumValue != null)
            {
                Value = enumValue;
            }

            SetValue(Value, MemberInfo, DeclaringInstance);
        }

        public void SetEnum(ref object value, int change)
        {
            var names = m_names.ToList();

            int newindex = names.IndexOf(value.ToString()) + change;

            if ((change < 0 && newindex >= 0) || (change > 0 && newindex < names.Count))
            {
                value = Enum.Parse(m_enumType, names[newindex]);
            }
        }
    }
}
