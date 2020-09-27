using UnityEngine;

namespace Explorer
{
    public class TabViewWindow : UIWindow
    {
        public override string Title => $"Tabs ({WindowManager.Windows.Count})";

        public static TabViewWindow Instance => m_instance ?? (m_instance = new TabViewWindow());
        private static TabViewWindow m_instance;

        private UIWindow m_targetWindow;
        public int TargetTabID = 0;

        public override bool IsTabViewWindow => true;

        public TabViewWindow()
        {
            m_rect = new Rect(570, 0, 550, 700);
        }

        public override void Init() { }

        public override void Update() 
        {
            while (TargetTabID >= WindowManager.Windows.Count)
            {
                TargetTabID--;
            }

            if (TargetTabID == -1 && WindowManager.Windows.Count > 0)
            {
                TargetTabID = 0;
            }

            if (TargetTabID >= 0)
            {
                m_targetWindow = WindowManager.Windows[TargetTabID];                
            }
            else
            {
                m_targetWindow = null;
            }

            m_targetWindow?.Update();
        }

        public override void WindowFunction(int windowID)
        {
            try
            {
                GUI.DragWindow(new Rect(0, 0, m_rect.width - 90, 20));
                if (GUIUnstrip.Button(new Rect(m_rect.width - 90, 2, 80, 20), "<color=red>Close All</color>"))
                {
                    foreach (var window in WindowManager.Windows)
                    {
                        window.DestroyWindow();
                    }
                    return;
                }

                GUIUnstrip.BeginArea(new Rect(5, 25, m_rect.width - 10, m_rect.height - 35), GUI.skin.box);

                GUILayout.BeginVertical(GUIContent.none, GUI.skin.box, null);
                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                int tabPerRow = Mathf.FloorToInt((float)((decimal)m_rect.width / 238));
                int rowCount = 0;
                for (int i = 0; i < WindowManager.Windows.Count; i++)
                {
                    if (rowCount >= tabPerRow)
                    {
                        rowCount = 0;
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                    }
                    rowCount++;

                    bool focused = i == TargetTabID;
                    string color = focused ? "<color=lime>" : "<color=orange>";
                    GUI.color = focused ? Color.green : Color.white;

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
                GUI.color = Color.white;
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUI.skin.button.alignment = TextAnchor.MiddleCenter;

                m_targetWindow.WindowFunction(m_targetWindow.windowID);

                try
                {
                    m_rect = ResizeDrag.ResizeWindow(m_rect, windowID);
                }
                catch { }

                GUIUnstrip.EndArea();
            }
            catch { }
        }
    }
}
