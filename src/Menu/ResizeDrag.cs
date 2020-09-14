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
            if (!RESIZE_FAILED)
            {
                var origRect = _rect;

                try
                {
                    GUILayout.BeginHorizontal(GUI.skin.box, null);

                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUILayout.Button(gcDrag, GUI.skin.label, new GUILayoutOption[] { GUILayout.Height(15) });

                    //var r = GUILayoutUtility.GetLastRect();
                    var r = LayoutUtilityUnstrip.GetLastRect();

                    var mousePos = InputHelper.mousePosition;

                    try
                    {
                        var mouse = GUIUtility.ScreenToGUIPoint(new Vector2(mousePos.x, Screen.height - mousePos.y));
                        if (r.Contains(mouse) && InputHelper.GetMouseButtonDown(0))
                        {
                            isResizing = true;
                            m_currentWindow = ID;
                            m_currentResize = new Rect(mouse.x, mouse.y, _rect.width, _rect.height);
                        }
                        else if (!InputHelper.GetMouseButton(0))
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
                    }
                    catch { }

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
                    //MelonLogger.Log(e.StackTrace);
                    return origRect;
                }

                GUI.skin.label.alignment = TextAnchor.UpperLeft;
            }
            else
            {
                GUILayout.BeginHorizontal(GUI.skin.box, null);

                GUILayout.Label("Resize window:", new GUILayoutOption[] { GUILayout.Width(100) });

                GUI.skin.label.alignment = TextAnchor.MiddleRight;
                GUILayout.Label("<color=cyan>Width:</color>", new GUILayoutOption[] { GUILayout.Width(60) });
                if (GUIUnstrip.RepeatButton("-", new GUILayoutOption[] { GUILayout.Width(20) }))
                {
                    _rect.width -= 5f;
                }
                if (GUIUnstrip.RepeatButton("+", new GUILayoutOption[] { GUILayout.Width(20) }))
                {
                    _rect.width += 5f;
                }
                GUILayout.Label("<color=cyan>Height:</color>", new GUILayoutOption[] { GUILayout.Width(60) });
                if (GUIUnstrip.RepeatButton("-", new GUILayoutOption[] { GUILayout.Width(20) }))
                {
                    _rect.height -= 5f;
                }
                if (GUIUnstrip.RepeatButton("+", new GUILayoutOption[] { GUILayout.Width(20) }))
                {
                    _rect.height += 5f;
                }

                GUILayout.EndHorizontal();
                GUI.skin.label.alignment = TextAnchor.UpperLeft;
            }

            return _rect;
        }
    }
}
