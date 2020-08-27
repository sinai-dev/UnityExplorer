using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MelonLoader;

namespace Explorer
{
    public class MainMenu
    {
        public static MainMenu Instance;

        public MainMenu()
        {
            Instance = this;

            Pages.Add(new ScenePage());
            Pages.Add(new SearchPage());
            Pages.Add(new ConsolePage());

            foreach (var page in Pages)
            {
                page.Init();
            }
        }

        public const int MainWindowID = 10;
        public static Rect MainRect = new Rect(5, 5, 550, 700);
        private static readonly List<WindowPage> Pages = new List<WindowPage>();
        private static int m_currentPage = 0;

        public static void SetCurrentPage(int index)
        {
            if (index < 0 || Pages.Count <= index)
            {
                MelonLogger.Log("cannot set page " + index);
                return;
            }
            m_currentPage = index;
            GUI.BringWindowToFront(MainWindowID);
            GUI.FocusWindow(MainWindowID);
        }

        public void Update()
        {
            Pages[m_currentPage].Update();
        }

        public void OnGUI()
        {
            if (CppExplorer.ShowMenu)
            {
                var origSkin = GUI.skin;
                GUI.skin = UIStyles.WindowSkin;

                MainRect = GUI.Window(MainWindowID, MainRect, (GUI.WindowFunction)MainWindow, CppExplorer.NAME);

                GUI.skin = origSkin;
            }
        }

        private void MainWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, MainRect.width - 90, 20));

            if (GUI.Button(new Rect(MainRect.width - 90, 2, 80, 20), "Hide (F7)"))
            {
                CppExplorer.ShowMenu = false;
                return;
            }

            GUILayout.BeginArea(new Rect(5, 25, MainRect.width - 10, MainRect.height - 35), GUI.skin.box);

            MainHeader();

            var page = Pages[m_currentPage];
            page.scroll = GUILayout.BeginScrollView(page.scroll, GUI.skin.scrollView);
            page.DrawWindow();
            GUILayout.EndScrollView();

            MainRect = ResizeDrag.ResizeWindow(MainRect, MainWindowID);

            GUILayout.EndArea();
        }

        private void MainHeader()
        {
            GUILayout.BeginHorizontal(null);
            for (int i = 0; i < Pages.Count; i++)
            {
                if (m_currentPage == i)
                    GUI.color = Color.green;
                else
                    GUI.color = Color.white;

                if (GUILayout.Button(Pages[i].Name, null))
                {
                    m_currentPage = i;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(null);
            GUI.color = Color.white;
            InspectUnderMouse.EnableInspect = GUILayout.Toggle(InspectUnderMouse.EnableInspect, "Inspect Under Mouse (Shift + RMB)", null);

            bool mouseState = CppExplorer.ForceUnlockMouse;
            bool setMouse = GUILayout.Toggle(mouseState, "Force Unlock Mouse (Left Alt)", null);
            if (setMouse != mouseState) CppExplorer.ForceUnlockMouse = setMouse;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUI.color = Color.white;
        }
    }
}
