using System;
using UnityEngine;

namespace Explorer
{
    public abstract class UIWindow
    {
        public abstract string Title { get; }

        public object Target;

        public int windowID;
        public Rect m_rect = new Rect(0,0, ModConfig.Instance.Default_Window_Size.x,ModConfig.Instance.Default_Window_Size.y);

        public Vector2 scroll = Vector2.zero;

        public virtual bool IsTabViewWindow => false;

        public abstract void Init();
        public abstract void WindowFunction(int windowID);
        public abstract void Update();

        public static UIWindow CreateWindow<T>(object target) where T : UIWindow
        {
            var window = Activator.CreateInstance<T>();

            window.Target = target;
            window.windowID = WindowManager.NextWindowID();
            window.m_rect = WindowManager.GetNewWindowRect();

            WindowManager.Windows.Add(window);

            window.Init();

            return window;
        }

        public void DestroyWindow()
        {
            WindowManager.DestroyWindow(this);
        }

        public void OnGUI()
        {
#if CPP
            m_rect = GUI.Window(windowID, m_rect, (GUI.WindowFunction)WindowFunction, GUIContent.Temp(Title), GUI.skin.window);
#else
            m_rect = GUI.Window(windowID, m_rect, WindowFunction, Title);
#endif
        }

        public void Header()
        {
            if (!WindowManager.TabView)
            {
                GUI.DragWindow(new Rect(0, 0, m_rect.width - 90, 20));

#if CPP
                if (GUI.Button(new Rect(m_rect.width - 90, 2, 80, 20), GUIContent.Temp("<color=red><b>X</b></color>"), GUI.skin.button))
#else
                if (GUI.Button(new Rect(m_rect.width - 90, 2, 80, 20), "<color=red><b>X</b></color>", GUI.skin.button))
#endif
                {
                    DestroyWindow();
                    return;
                }
            }
        }
    }
}
