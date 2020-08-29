using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Explorer
{
    public class TabViewWindow : UIWindow
    {
        public override string Title => "Tab View";

        public static TabViewWindow Instance => m_instance ?? (m_instance = new TabViewWindow());
        private static TabViewWindow m_instance;

        public int TargetTabID = 0;

        public override bool IsTabViewWindow => true;

        public TabViewWindow()
        {
            m_rect = new Rect(570, 0, 550, 700);
        }

        public override void Init() { }
        public override void Update() { }

        public override void WindowFunction(int windowID)
        {
            try
            {
                Header();

                GUILayout.BeginArea(new Rect(5, 25, m_rect.width - 10, m_rect.height - 35), GUI.skin.box);

                GUILayout.BeginVertical(GUI.skin.box, null);
                GUILayout.BeginHorizontal(null);
                GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                int tabPerRow = Mathf.FloorToInt((float)((decimal)m_rect.width / 238));
                int rowCount = 0;
                for (int i = 0; i < WindowManager.Windows.Count; i++)
                {
                    if (rowCount >= tabPerRow)
                    {
                        rowCount = 0;
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal(null);
                    }
                    rowCount++;

                    bool focused = i == TargetTabID;
                    string color = focused ? "<color=lime>" : "<color=orange>";

                    var window = WindowManager.Windows[i];
                    if (GUILayout.Button(color + window.Title + "</color>", new GUILayoutOption[] { GUILayout.Width(200) }))
                    {
                        TargetTabID = i;
                    }
                    if (GUILayout.Button("<color=red><b>X</b></color>", new GUILayoutOption[] { GUILayout.Width(22) }))
                    {
                        window.DestroyWindow();
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUI.skin.button.alignment = TextAnchor.MiddleCenter;

                while (TargetTabID >= WindowManager.Windows.Count)
                {
                    TargetTabID--;
                }

                if (TargetTabID >= 0)
                {
                    var window = WindowManager.Windows[TargetTabID];
                    window.WindowFunction(window.windowID);
                }

                try
                {
                    m_rect = ResizeDrag.ResizeWindow(m_rect, windowID);
                }
                catch { }

                GUILayout.EndArea();
            }
            catch { }
        }
    }
}
