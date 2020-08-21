using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Explorer
{
    public class UIHelpers
    {
        // helper for "Instantiate" button on UnityEngine.Objects
        public static void InstantiateButton(Object obj, float width = 100)
        {
            if (GUILayout.Button("Instantiate", new GUILayoutOption[] { GUILayout.Width(width) }))
            {
                var newobj = Object.Instantiate(obj);

                WindowManager.InspectObject(newobj, out _);
            }
        }

        // helper for drawing a styled button for a GameObject or Transform
        public static void GameobjButton(GameObject obj, Action<GameObject> specialInspectMethod = null, bool showSmallInspectBtn = true, float width = 380)
        {
            bool children = obj.transform.childCount > 0;

            string label = children ? "[" + obj.transform.childCount + " children] " : "";
            label += obj.name;

            bool enabled = obj.activeSelf;
            int childCount = obj.transform.childCount;
            Color color;

            if (enabled)
            {
                if (childCount > 0)
                {
                    color = Color.green;
                }
                else
                {
                    color = UIStyles.LightGreen;
                }
            }
            else
            {
                color = Color.red;
            }

            FastGameobjButton(obj, color, label, obj.activeSelf, specialInspectMethod, showSmallInspectBtn, width);
        }

        public static void FastGameobjButton(GameObject obj, Color activeColor, string label, bool enabled, Action<GameObject> specialInspectMethod = null, bool showSmallInspectBtn = true, float width = 380)
        {
            if (!obj)
            {
                GUILayout.Label("<i><color=red>null</color></i>", null);
                return;
            }

            // ------ toggle active button ------

            GUILayout.BeginHorizontal(null);
            GUI.skin.button.alignment = TextAnchor.UpperLeft;

            GUI.color = activeColor;

            enabled = GUILayout.Toggle(enabled, "", new GUILayoutOption[] { GUILayout.Width(18) });
            if (obj.activeSelf != enabled)
            {
                obj.SetActive(enabled);
            }

            // ------- actual button ---------

            if (GUILayout.Button(label, new GUILayoutOption[] { GUILayout.Height(22), GUILayout.Width(width) }))
            {
                if (specialInspectMethod != null)
                {
                    specialInspectMethod(obj);
                }
                else
                {
                    WindowManager.InspectObject(obj, out bool _);
                }
            }

            // ------ small "Inspect" button on the right ------

            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            GUI.color = Color.white;

            if (showSmallInspectBtn)
            {
                if (GUILayout.Button("Inspect", null))
                {
                    WindowManager.InspectObject(obj, out bool _);
                }
            }

            GUILayout.EndHorizontal();
        }
    }
}
