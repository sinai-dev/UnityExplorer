using System;
using System.Collections.Generic;
using UnityEngine;
using Explorer.UI.Inspectors;

namespace Explorer.UI
{
    public class WindowManager
    {
        public static WindowManager Instance;

        public static bool TabView = Config.ModConfig.Instance.Tab_View;

        public static bool IsMouseInWindow
        {
            get
            {
                if (!ExplorerCore.ShowMenu)
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

        public static List<WindowBase> Windows = new List<WindowBase>();
        public static int CurrentWindowID { get; set; } = 500000;
        private static Rect m_lastWindowRect;

        private static readonly List<WindowBase> m_windowsToDestroy = new List<WindowBase>();

        public WindowManager()
        {
            Instance = this;
        }

        public static void DestroyWindow(WindowBase window)
        {
            m_windowsToDestroy.Add(window);
        }

        public static WindowBase InspectObject(object obj, out bool createdNew, bool forceReflection = false)
        {
            createdNew = false;

            //if (InputManager.GetKey(KeyCode.LeftShift))
            //{
            //    forceReflection = true;
            //}

#if CPP
            Il2CppSystem.Object iObj = null;
            if (obj is Il2CppSystem.Object isObj)
            {
                iObj = isObj;
            }
#else
            var iObj = obj;
#endif

            if (!forceReflection)
            {
                foreach (var window in Windows)
                {
                    bool equals = ReferenceEquals(obj, window.Target);

#if CPP
                    if (!equals && iObj is Il2CppSystem.Object iCurrent && window.Target is Il2CppSystem.Object iTarget)
                    {
                        if (iCurrent.GetIl2CppType().FullName != iTarget.GetIl2CppType().FullName)
                        {
                            if (iCurrent is Transform transform)
                            {
                                iCurrent = transform.gameObject;
                            }
                        }

                        equals = iCurrent.Pointer == iTarget.Pointer;
                    }
#endif

                    if (equals)
                    {
                        FocusWindow(window);
                        return window;
                    }
                }
            }

            createdNew = true;
            if (!forceReflection && (obj is GameObject || obj is Transform))
            {
                return InspectGameObject(obj as GameObject ?? (obj as Transform).gameObject);
            }
            else
            {
                return InspectReflection(obj);
            }
        }

        private static void FocusWindow(WindowBase window)
        {
            if (!TabView)
            {
                GUIHelper.BringWindowToFront(window.windowID);
                GUI.FocusWindow(window.windowID);
            }
            else
            {
                TabViewWindow.Instance.TargetTabID = Windows.IndexOf(window);
            }
        }

        private static WindowBase InspectGameObject(GameObject obj)
        {
            var new_window = WindowBase.CreateWindow<GameObjectInspector>(obj);
            FocusWindow(new_window);

            return new_window;
        }

        private static WindowBase InspectReflection(object obj)
        {
            var new_window = WindowBase.CreateWindow<InstanceInspector>(obj);
            FocusWindow(new_window);

            return new_window;
        }

        public static StaticInspector InspectStaticReflection(Type type)
        {
            var new_window = WindowBase.CreateWindowStatic(type);
            FocusWindow(new_window);

            return new_window;
        }

        private static bool RectContainsMouse(Rect rect)
        {
            var mousePos = InputManager.MousePosition;
            return rect.Contains(new Vector2(mousePos.x, Screen.height - mousePos.y));
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

        // ============= instance methods ===============

        public void Update()
        {
            if (m_windowsToDestroy.Count > 0)
            {
                foreach (var window in m_windowsToDestroy)
                {
                    if (Windows.Contains(window))
                    {
                        Windows.Remove(window);
                    }
                }

                m_windowsToDestroy.Clear();
            }

            if (TabView)
            {
                TabViewWindow.Instance.Update();
            }
            else
            {
                for (int i = 0; i < Windows.Count; i++)
                {
                    var window = Windows[i];
                    if (window != null)
                    {
                        window.Update();
                    }
                }
            }
        }

        public void OnGUI()
        {
            if (TabView)
            {
                if (Windows.Count > 0)
                {
                    TabViewWindow.Instance.OnGUI();
                }
            }
            else
            {
                foreach (var window in Windows)
                {
                    window.OnGUI();
                }
            }
        }
    }
}
