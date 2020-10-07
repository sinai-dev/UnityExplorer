using System;
#if CPP
using UnhollowerBaseLib;
#endif
using UnityEngine;

namespace Explorer.UI.Shared
{
    public class ResizeDrag
    {
#if CPP
        private static bool RESIZE_FAILED = false;
#endif

        private static readonly GUIContent gcDrag = new GUIContent("<-- Drag to resize -->");
        private static bool isResizing = false;
        private static Rect m_currentResize;
        private static int m_currentWindow;

        public static Rect ResizeWindow(Rect _rect, int ID)
        {
#if CPP
            if (!RESIZE_FAILED)
            {
                var origRect = _rect;

                try
                {
                    GUIUnstrip.BeginHorizontal(GUIContent.none, GUI.skin.box, null);

                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
#if ML
                    GUILayout.Button(gcDrag, GUI.skin.label, new GUILayoutOption[] { GUILayout.Height(15) });
#else
                    GUILayout.Button("<-- Drag to resize -->", new GUILayoutOption[] { GUILayout.Height(15) });
#endif

                    var r = GUIUnstrip.GetLastRect();

                    var mousePos = InputManager.MousePosition;

                    try
                    {
                        var mouse = GUIUnstrip.ScreenToGUIPoint(new Vector2(mousePos.x, Screen.height - mousePos.y));
                        if (r.Contains(mouse) && InputManager.GetMouseButtonDown(0))
                        {
                            isResizing = true;
                            m_currentWindow = ID;
                            m_currentResize = new Rect(mouse.x, mouse.y, _rect.width, _rect.height);
                        }
                        else if (!InputManager.GetMouseButton(0))
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
                    catch
                    {
                        // throw safe Managed exception
                        throw new Exception("");
                    }

                    GUILayout.EndHorizontal();
                }
                catch (Exception e) when (e.Message.StartsWith("System.ArgumentException"))
                {
                    // suppress
                    return origRect;
                }
                catch (Exception e)
                {
                    RESIZE_FAILED = true;
                    ExplorerCore.Log("Exception on GuiResize: " + e.GetType() + ", " + e.Message);
                    //ExplorerCore.Log(e.StackTrace);
                    return origRect;
                }

                GUI.skin.label.alignment = TextAnchor.UpperLeft;
            }
            else
            {
                GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);

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

#else      // mono

            GUIUnstrip.BeginHorizontal(GUIContent.none, GUI.skin.box, null);

            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUILayout.Button(gcDrag, GUI.skin.label, new GUILayoutOption[] { GUILayout.Height(15) });

            //var r = GUILayoutUtility.GetLastRect();
            var r = GUILayoutUtility.GetLastRect();

            var mousePos = InputManager.MousePosition;

            var mouse = GUIUnstrip.ScreenToGUIPoint(new Vector2(mousePos.x, Screen.height - mousePos.y));
            if (r.Contains(mouse) && InputManager.GetMouseButtonDown(0))
            {
                isResizing = true;
                m_currentWindow = ID;
                m_currentResize = new Rect(mouse.x, mouse.y, _rect.width, _rect.height);
            }
            else if (!InputManager.GetMouseButton(0))
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

#endif
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;

            return _rect;
        }
    }
}
