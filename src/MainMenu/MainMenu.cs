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

                MainRect = GUI.Window(MainWindowID, MainRect, (GUI.WindowFunction)MainWindow, "IL2CPP Runtime Explorer");

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

            MainRect = WindowManager.ResizeWindow(MainRect, MainWindowID);

            GUILayout.EndArea();
        }

        private void MainHeader()
        {
            GUILayout.BeginHorizontal(null);
            GUILayout.Label("<b>Options:</b>", new GUILayoutOption[] { GUILayout.Width(70) });
            GUI.skin.label.alignment = TextAnchor.MiddleRight;
            GUILayout.Label("Array Limit:", new GUILayoutOption[] { GUILayout.Width(70) });
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            var _input = GUILayout.TextField(CppExplorer.ArrayLimit.ToString(), new GUILayoutOption[] { GUILayout.Width(60) });
            if (int.TryParse(_input, out int _lim))
            {
                CppExplorer.ArrayLimit = _lim;
            }
            CppExplorer.Instance.MouseInspect = GUILayout.Toggle(CppExplorer.Instance.MouseInspect, "Inspect Under Mouse (Shift + RMB)", null);
            GUILayout.EndHorizontal();

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

            GUILayout.Space(10);
            GUI.color = Color.white;
        }

        public abstract class WindowPage
        {
            public virtual string Name { get; set; }

            public Vector2 scroll = Vector2.zero;

            public abstract void Init();

            public abstract void DrawWindow();

            public abstract void Update();
        }
    }
}
