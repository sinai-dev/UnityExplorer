using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Explorer
{
    public class CacheIl2CppObject : CacheObject
    {
        public override void DrawValue(Rect window, float width)
        {
            var label = ValueType ?? Value.ToString();
            if (!label.Contains(ValueType))
            {
                label += $" ({ValueType})";
            }
            if (Value is UnityEngine.Object unityObj)
            {
                label = unityObj.name + " | " + label;
            }

            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            if (GUILayout.Button("<color=yellow>" + label + "</color>", new GUILayoutOption[] { GUILayout.MaxWidth(width) }))
            {
                WindowManager.InspectObject(Value, out bool _);
            }
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
        }

        public override void SetValue()
        {
            throw new NotImplementedException("TODO");
        }
    }
}
