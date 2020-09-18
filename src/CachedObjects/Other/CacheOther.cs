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

            if (Value is UnityEngine.Object)
            {
                int typeStart = label.LastIndexOf("(");                 // get where the '(Type)' starts
                var newLabel = label.Substring(0, typeStart + 1);       // get just the name and first '('
                newLabel += $"<color={classColor}>";                    // add color tag
                newLabel += label.Substring(typeStart + 1);             // add the TypeName back in
                newLabel = newLabel.Substring(0, newLabel.Length - 1);  // remove the ending ')'
                newLabel += "</color>)";                                // close color tag and put the ')' back.
                label = newLabel;
            }
            else
            {
                string classLabel = $"<color={classColor}>{ValueTypeName}</color>";

                if (!label.Contains(ValueTypeName))
                {
                    label += $" ({classLabel})";
                }
                else
                {
                    label = label.Replace(ValueTypeName, $"<color={classColor}>{ValueTypeName}</color>");
                }
            }

            return m_btnLabel = label;
        }
    }
}
