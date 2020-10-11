using System;
#if CPP
using UnhollowerBaseLib;
#endif
using UnityEngine;

namespace Explorer.UI.Shared
{
    public class ResizeDrag
    {
        private static bool RESIZE_FAILED = false;

        public static bool IsResizing = false;
        public static bool IsMouseInResizeArea = false;

        private static readonly GUIContent gcDrag = new GUIContent("<-- Drag to resize -->");
        private static Rect m_currentResize;
        private static int m_currentWindow;

        public static Rect ResizeWindow(Rect _rect, int ID)
        {
            if (!RESIZE_FAILED)
            {
                var origRect = _rect;

                try
                {
                    GUIUnstrip.BeginHorizontal(GUIContent.none, GUI.skin.box, null);

                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
#if BIE
#if CPP             // Temporary for BepInEx IL2CPP
                    GUILayout.Button("<-- Drag to resize -->", new GUILayoutOption[] { GUILayout.Height(15) });                    
#else
                    GUILayout.Button(gcDrag, GUI.skin.label, new GUILayoutOption[] { GUILayout.Height(15) });
#endif
#else
                    GUILayout.Button(gcDrag, GUI.skin.label, new GUILayoutOption[] { GUILayout.Height(15) });                    
#endif

                    var resizeDragArea = GUIUnstrip.GetLastRect();
                    var mousePos = InputManager.MousePosition;

                    try
                    {
                        var mouse = GUIUnstrip.ScreenToGUIPoint(new Vector2(mousePos.x, Screen.height - mousePos.y));
                        if (resizeDragArea.Contains(mouse))
                        {
                            IsMouseInResizeArea = true;

                            if (InputManager.GetMouseButton(0))
                            {
                                IsResizing = true;
                                m_currentWindow = ID;
                                m_currentResize = new Rect(mouse.x, mouse.y, _rect.width, _rect.height);
                            }
                        }
                        else if (!InputManager.GetMouseButton(0))
                        {
                            IsMouseInResizeArea = false;
                            IsResizing = false;
                        }

                        if (IsResizing && ID == m_currentWindow)
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
            }

            GUI.skin.label.alignment = TextAnchor.MiddleLeft;

            return _rect;
        }
    }
}
