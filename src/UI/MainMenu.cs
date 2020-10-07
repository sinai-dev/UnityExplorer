using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Explorer.Config;
using Explorer.UI.Main;
using Explorer.UI.Shared;
using Explorer.UI.Inspectors;

namespace Explorer.UI
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
            Pages.Add(new OptionsPage());

            for (int i = 0; i < Pages.Count; i++)
            {
                var page = Pages[i];
                page.Init();

                // If page failed to init, it will remove itself from the list. Lower the iterate counter.
                if (!Pages.Contains(page)) i--;
            }
        }

        public const int MainWindowID = 5000;
        public static Rect MainRect = new Rect(5, 5, ModConfig.Instance.Default_Window_Size.x, ModConfig.Instance.Default_Window_Size.y);

        public static readonly List<BaseMainMenuPage> Pages = new List<BaseMainMenuPage>();
        private static int m_currentPage = 0;

        public static void SetCurrentPage(int index)
        {
            if (index < 0 || Pages.Count <= index)
            {
                ExplorerCore.Log("cannot set page " + index);
                return;
            }
            m_currentPage = index;
            GUIUnstrip.BringWindowToFront(MainWindowID);
            GUI.FocusWindow(MainWindowID);
        }

        public void Update()
        {
            Pages[m_currentPage].Update();
        }

        public void OnGUI()
        {
            MainRect = GUIUnstrip.Window(MainWindowID, MainRect, (GUI.WindowFunction)MainWindow, ExplorerCore.NAME);
        }

        private void MainWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, MainRect.width - 90, 20));

            if (GUIUnstrip.Button(new Rect(MainRect.width - 90, 2, 80, 20), $"Hide ({ModConfig.Instance.Main_Menu_Toggle})"))
            {
                ExplorerCore.ShowMenu = false;
                return;
            }

            GUIUnstrip.BeginArea(new Rect(5, 25, MainRect.width - 10, MainRect.height - 35), GUI.skin.box);

            MainHeader();

            var page = Pages[m_currentPage];

            page.scroll = GUIUnstrip.BeginScrollView(page.scroll);

            page.DrawWindow();

            GUIUnstrip.EndScrollView();

            MainRect = ResizeDrag.ResizeWindow(MainRect, MainWindowID);

            GUIUnstrip.EndArea();
        }

        private void MainHeader()
        {
            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
            for (int i = 0; i < Pages.Count; i++)
            {
                if (m_currentPage == i)
                    GUI.color = Color.green;
                else
                    GUI.color = Color.white;

                if (GUILayout.Button(Pages[i].Name, new GUILayoutOption[0]))
                {
                    m_currentPage = i;
                }
            }
            GUILayout.EndHorizontal();

            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
            GUI.color = Color.white;
            InspectUnderMouse.EnableInspect = GUILayout.Toggle(InspectUnderMouse.EnableInspect, "Inspect Under Mouse (Shift + RMB)", new GUILayoutOption[0]);

            bool mouseState = ForceUnlockCursor.Unlock;
            bool setMouse = GUILayout.Toggle(mouseState, "Force Unlock Mouse (Left Alt)", new GUILayoutOption[0]);
            if (setMouse != mouseState) ForceUnlockCursor.Unlock = setMouse;

            //WindowManager.TabView = GUILayout.Toggle(WindowManager.TabView, "Tab View", new GUILayoutOption[0]);
            GUILayout.EndHorizontal();

            //GUIUnstrip.Space(10);
            GUIUnstrip.Space(10);

            GUI.color = Color.white;
        }
    }
}
