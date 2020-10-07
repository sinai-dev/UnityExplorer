using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Explorer.UI.Shared
{
    public class Buttons
    {
        public static void InstantiateButton(Object obj, float width = 100)
        {
            if (GUILayout.Button("Instantiate", new GUILayoutOption[] { GUILayout.Width(width) }))
            {
                var newobj = Object.Instantiate(obj);
                WindowManager.InspectObject(newobj, out _);
            }
        }

        public static void InspectButton(object obj)
        {
            if (GUILayout.Button("Inspect", new GUILayoutOption[0]))
            {
                WindowManager.InspectObject(obj, out bool _);
            }
        }

        public static void GameObjectButton(object _obj, Action<Transform> inspectOverride = null, bool showSmallInspect = true, float width = 380)
        {
            var go = (_obj as GameObject) ?? (_obj as Transform).gameObject;

            if (!go) return;

            bool hasChild = go.transform.childCount > 0;

            string label = hasChild ? $"[{go.transform.childCount} children] " : "";
            label += go.name;

            bool enabled = go.activeSelf;
            int childCount = go.transform.childCount;
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

            // ------ toggle active button ------

            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
            GUI.skin.button.alignment = TextAnchor.UpperLeft;

            GUI.color = color;

            enabled = GUILayout.Toggle(enabled, "", new GUILayoutOption[] { GUILayout.Width(18) });
            if (go.activeSelf != enabled)
            {
                go.SetActive(enabled);
            }

            // ------- actual button ---------

            if (GUILayout.Button(label, new GUILayoutOption[] { GUILayout.Height(22), GUILayout.Width(width) }))
            {
                if (inspectOverride != null)
                {
                    inspectOverride(go.transform);
                }
                else
                {
                    WindowManager.InspectObject(_obj, out bool _);
                }
            }

            // ------ small "Inspect" button on the right ------

            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            GUI.color = Color.white;

            if (showSmallInspect)
            {
                InspectButton(_obj);
            }

            GUILayout.EndHorizontal();
        }
    }
}
