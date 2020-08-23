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
    public class CacheOther : CacheObject
    {
        private MethodInfo m_toStringMethod;
        private bool m_triedToGetMethod;

        public MethodInfo ToStringMethod
        {
            get
            {
                if (m_toStringMethod == null && !m_triedToGetMethod)
                {
                    if (Value == null) return null;

                    m_triedToGetMethod = true;

                    try
                    {
                        var methods = ReflectionHelpers.GetActualType(Value)
                                .GetMethods(ReflectionHelpers.CommonFlags)
                                .Where(x => x.Name == "ToString")
                                .GetEnumerator();

                        while (methods.MoveNext())
                        {
                            // just get the first (top-most level) method, then break.
                            m_toStringMethod = methods.Current;
                            break;
                        }
                    }
                    catch { }
                }
                return m_toStringMethod;
            }
        }

        public override void DrawValue(Rect window, float width)
        {
            string label = (string)ToStringMethod?.Invoke(Value, null) ?? Value.ToString();
            
            if (!label.Contains(ValueType))
            {
                label += $" ({ValueType})";
            }
            if (Value is UnityEngine.Object unityObj && !label.Contains(unityObj.name))
            {
                label = unityObj.name + " | " + label;
            }

            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            if (GUILayout.Button("<color=yellow>" + label + "</color>", new GUILayoutOption[] { GUILayout.MaxWidth(width + 40) }))
            {
                WindowManager.InspectObject(Value, out bool _);
            }
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
        }
    }
}
