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
    public abstract class UIWindow
    {
        public abstract string Name { get; set; }

        public object Target;

        public int windowID;
        public Rect m_rect = new Rect(0, 0, 550, 700);

        public Vector2 scroll = Vector2.zero;

        public static UIWindow CreateWindow<T>(object target) where T : UIWindow
        {
            //var component = (UIWindow)AddToGameObject<T>(Instance.gameObject);
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
            try
            {
                WindowManager.Windows.Remove(this);
            }
            catch (Exception e)
            {
                MelonLogger.Log("Exception removing Window from WindowManager.Windows list!");
                MelonLogger.Log($"{e.GetType()} : {e.Message}\r\n{e.StackTrace}");
            }
            //Destroy(this);
        }

        public abstract void Init();
        public abstract void WindowFunction(int windowID);
        public abstract void Update();

        public void OnGUI()
        {
            if (CppExplorer.ShowMenu)
            {
                var origSkin = GUI.skin;

                GUI.skin = UIStyles.WindowSkin;
                m_rect = GUI.Window(windowID, m_rect, (GUI.WindowFunction)WindowFunction, Name);

                GUI.skin = origSkin;
            }
        }

        public void Header()
        {
            GUI.DragWindow(new Rect(0, 0, m_rect.width - 90, 20));

            if (GUI.Button(new Rect(m_rect.width - 90, 2, 80, 20), "<color=red><b>X</b></color>"))
            {
                DestroyWindow();
                return;
            }
        }
    }
}
