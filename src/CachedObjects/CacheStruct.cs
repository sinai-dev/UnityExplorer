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
    public class CacheStruct : CacheObject
    {
        public MethodInfo ToStringMethod { get; private set; }
        private static readonly MethodInfo m_defaultToString = typeof(object).GetMethod("ToString");

        public CacheStruct(object obj)
        {
            try
            {
                var methods = obj.GetType().GetMethods(ReflectionHelpers.CommonFlags).Where(x => x.Name == "ToString");
                var enumerator = methods.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    ToStringMethod = enumerator.Current;
                    break;
                }
            }
            catch
            {
                ToStringMethod = m_defaultToString;
            }
        }

        public override void DrawValue(Rect window, float width)
        {
            string label;
            try
            {
                label = (string)ToStringMethod.Invoke(Value, null);
            }
            catch
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
            throw new NotImplementedException("TODO");
        }

        //public override void UpdateValue(object obj)
        //{
            
        //}
    }
}
