using System;
using System.Reflection;
using UnityEngine;

namespace Explorer
{
    public class CacheOther : CacheObjectBase
    {
        public string ButtonLabel => m_btnLabel ?? GetButtonLabel();
        private string m_btnLabel;

        public MethodInfo ToStringMethod => m_toStringMethod ?? GetToStringMethod();
        private MethodInfo m_toStringMethod;

        public override void UpdateValue()
        {
            base.UpdateValue();

            GetButtonLabel();
        }

        public override void DrawValue(Rect window, float width)
        {
            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            if (GUILayout.Button(ButtonLabel, new GUILayoutOption[] { GUILayout.Width(width - 15) }))
            {
                WindowManager.InspectObject(Value, out bool _);
            }
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
        }

        private MethodInfo GetToStringMethod()
        {
            try
            {
                m_toStringMethod = ReflectionHelpers.GetActualType(Value).GetMethod("ToString", new Type[0])
                                   ?? typeof(object).GetMethod("ToString", new Type[0]);

                // test invoke
                m_toStringMethod.Invoke(Value, null);
            }
            catch
            {
                m_toStringMethod = typeof(object).GetMethod("ToString", new Type[0]);
            }
            return m_toStringMethod;
        }

        private string GetButtonLabel()
        {
            if (Value == null) return null;

            string label = (string)ToStringMethod?.Invoke(Value, null) ?? Value.ToString();

            var classColor = ValueType.IsAbstract && ValueType.IsSealed
                ? UIStyles.Syntax.Class_Static
                : UIStyles.Syntax.Class_Instance;

            string typeLabel = $"<color={classColor}>{ValueType.FullName}</color>";

            if (Value is UnityEngine.Object)
            {
                label = label.Replace($"({ValueType.FullName})", $"({typeLabel})");
            }
            else
            {
                if (!label.Contains(ValueType.FullName))
                {
                    label += $" ({typeLabel})";
                }
                else
                {
                    label = label.Replace(ValueType.FullName, typeLabel);
                }
            }

            return m_btnLabel = label;
        }
    }
}
