using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Explorer
{
    public class CacheOther : CacheObject
    {
        public override void DrawValue(Rect window, float width)
        {
            string label;
            if (Value is UnityEngine.Object uObj)
            {
                label = uObj.name;
            }
            else
            {
                label = Value.ToString();
            }

            string typeLabel = Value.GetType().FullName;

            if (!label.Contains(typeLabel))
            {
                label += $" ({typeLabel})";
            }

            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            if (GUILayout.Button("<color=yellow>" + label + "</color>", new GUILayoutOption[] { GUILayout.MaxWidth(window.width - 230) }))
            {
                WindowManager.InspectObject(Value, out bool _);
            }
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
        }

        public override void SetValue()
        {
            
        }

        public override void UpdateValue(object obj)
        {
            
        }
    }
}
