using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using MelonLoader;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Explorer
{
    public class WindowManager
    {
        public static WindowManager Instance;

        public static List<UIWindow> Windows = new List<UIWindow>();
        public static int CurrentWindowID { get; set; } = 500000;
        private static Rect m_lastWindowRect;

        public WindowManager()
        {
            Instance = this;
        }

        public void Update()
        {
            foreach (var window in Windows)
            {
                window.Update();
            }
        }

        public void OnGUI()
        {
            foreach (var window in Windows)
            {
                window.OnGUI();
            }
        }

        // ========= Public Helpers =========

        public static bool IsMouseInWindow
        {
            get
            {
                if (!CppExplorer.ShowMenu)
                {
                    return false;
                }

                foreach (var window in Windows)
                {
                    if (RectContainsMouse(window.m_rect))
                    {
                        return true;
                    }
                }
                return RectContainsMouse(MainMenu.MainRect);
            }
        }

        private static bool RectContainsMouse(Rect rect)
        {
            return rect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
        }

        public static int NextWindowID()
        {
            return CurrentWindowID++;
        }

        public static Rect GetNewWindowRect()
        {
            return GetNewWindowRect(ref m_lastWindowRect);
        }

        public static Rect GetNewWindowRect(ref Rect lastRect)
        {
            Rect rect = new Rect(0, 0, 550, 700);

            var mainrect = MainMenu.MainRect;
            if (mainrect.x <= (Screen.width - mainrect.width - 100))
            {
                rect = new Rect(mainrect.x + mainrect.width + 20, mainrect.y, rect.width, rect.height);
            }

            if (lastRect.x == rect.x)
            {
                rect = new Rect(rect.x + 25, rect.y + 25, rect.width, rect.height);
            }

            lastRect = rect;

            return rect;
        }

        public static UIWindow InspectObject(object obj, out bool createdNew)
        {
            createdNew = false;

            UnityEngine.Object uObj = null;
            if (obj is UnityEngine.Object)
            {
                uObj = obj as UnityEngine.Object;
            }

            foreach (var window in Windows)
            {
                bool equals;
                equals = ReferenceEquals(obj, window.Target);

                if (!equals && uObj != null && window.Target is UnityEngine.Object uTarget)
                {
                    equals = uObj.m_CachedPtr == uTarget.m_CachedPtr;
                }

                if (equals)
                {                    
                    return window;
                }
            }

            createdNew = true;
            if (obj is GameObject || obj is Transform)
            {
                return InspectGameObject(obj as GameObject ?? (obj as Transform).gameObject);
            }
            else
            {
                return InspectReflection(obj);
            }
        }

        private static UIWindow InspectGameObject(GameObject obj)
        {
            var new_window = UIWindow.CreateWindow<GameObjectWindow>(obj);
            GUI.FocusWindow(new_window.windowID);

            return new_window;
        }

        public static UIWindow InspectReflection(object obj)
        {
            var new_window = UIWindow.CreateWindow<ReflectionWindow>(obj);
            GUI.FocusWindow(new_window.windowID);

            return new_window;
        }

        // ============= Resize Window Helper ============

        // static readonly GUIContent gcDrag = new GUIContent("<->", "drag to resize");
        private static readonly GUIContent gcDrag = new GUIContent("<->");
        private static bool isResizing = false;
        private static Rect m_currentResize;
        private static int m_currentWindow;

        public static Rect ResizeWindow(Rect _rect, int ID)
        {
            try
            {
                GUILayout.BeginHorizontal(null);
                GUILayout.Space(_rect.width - 35);

                GUILayout.Button(gcDrag, GUI.skin.label, new GUILayoutOption[] { GUILayout.Width(25), GUILayout.Height(25) });

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
            catch //(Exception e)
            {
                //MelonLogger.Log("Exception on GuiResize: " + e.GetType() + ", " + e.Message);
            }

            return _rect;
        }
    }
}
