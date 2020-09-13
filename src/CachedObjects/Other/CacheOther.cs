using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;

namespace Explorer
{
    public class CacheOther : CacheObjectBase
    {
        private MethodInfo m_toStringMethod;

        public MethodInfo ToStringMethod
        {
            get
            {
                if (m_toStringMethod == null)
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
                }
                return m_toStringMethod;
            }
        }

        public override void DrawValue(Rect window, float width)
        {
            string label = (string)ToStringMethod?.Invoke(Value, null) ?? Value.ToString();
            
            if (!label.Contains(ValueTypeName))
            {
                label += $" (<color=#2df7b2>{ValueTypeName}</color>)";
            }
            else
            {
                label = label.Replace(ValueTypeName, $"<color=#2df7b2>{ValueTypeName}</color>");
            }

            if (Value is UnityEngine.Object unityObj && !label.Contains(unityObj.name))
            {
                label = unityObj.name + " | " + label;
            }

            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            if (GUIUnstrip.Button(label, new GUILayoutOption[] { GUILayout.Width(width - 15) }))
            {
                WindowManager.InspectObject(Value, out bool _);
            }
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
        }
    }
}
