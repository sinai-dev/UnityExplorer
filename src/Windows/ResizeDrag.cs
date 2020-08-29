using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MelonLoader;
using UnhollowerBaseLib;

namespace Explorer
{
    public class ResizeDrag
    {
        private static bool RESIZE_FAILED = false;

        private static readonly GUIContent gcDrag = new GUIContent("<-- Drag to resize -->");
        private static bool isResizing = false;
        private static Rect m_currentResize;
        private static int m_currentWindow;

        public static Rect ResizeWindow(Rect _rect, int ID)
        {
            if (RESIZE_FAILED) return _rect;

            var origRect = _rect;

            try
            {
                GUILayout.BeginHorizontal(GUI.skin.box, null);

                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUILayout.Button(gcDrag, GUI.skin.label, new GUILayoutOption[] { GUILayout.Height(15) });

                var r = GUILayoutUtility.GetLastRect();

                Vector2 mouse = GUIUtility.ScreenToGUIPoint(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));

                if (r.Contains(mouse) && Input.GetMouseButtonDown(0))
                {
                    isResizing = true;
                    m_currentWindow = ID;
                    m_currentResize = new Rect(mouse.x, mouse.y, _rect.width, _rect.height);
                }
                else if (!Input.GetMouseButton(0))
                {
                    isResizing = false;
                }

                if (isResizing && ID == m_currentWindow)
                {
                    _rect.width = Mathf.Max(100, m_currentResize.width + (mouse.x - m_currentResize.x));
                    _rect.height = Mathf.Max(100, m_currentResize.height + (mouse.y - m_currentResize.y));
                    _rect.xMax = Mathf.Min(Screen.width, _rect.xMax);  // modifying xMax affects width, not x
                    _rect.yMax = Mathf.Min(Screen.height, _rect.yMax);  // modifying yMax affects height, not y
                }

                GUILayout.EndHorizontal();
            }
            catch (Il2CppException e) when (e.Message.StartsWith("System.ArgumentException")) 
            {
                // suppress
                return origRect;
            }
            catch (Exception e)
            {
                RESIZE_FAILED = true;
                MelonLogger.Log("Exception on GuiResize: " + e.GetType() + ", " + e.Message);
                return origRect;
            }

            GUI.skin.label.alignment = TextAnchor.UpperLeft;

            return _rect;
        }
    }
}
