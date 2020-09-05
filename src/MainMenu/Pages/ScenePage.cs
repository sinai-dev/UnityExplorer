using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Explorer
{
    public class ScenePage : WindowPage
    {
        public static ScenePage Instance;

        public override string Name { get => "Scene Explorer"; set => base.Name = value; }

        public PageHelper Pages = new PageHelper();

        private float m_timeOfLastUpdate = -1f;
        private const int PASSIVE_UPDATE_INTERVAL = 1;

        // ----- Holders for GUI elements ----- //

        private static string m_currentScene = "";

        // gameobject list
        private Transform m_currentTransform;
        private readonly List<GameObjectCache> m_objectList = new List<GameObjectCache>();

        // search bar
        private bool m_searching = false;
        private string m_searchInput = "";
        private List<GameObjectCache> m_searchResults = new List<GameObjectCache>();

        // ------------ Init and Update ------------ //

        public override void Init()
        {
            Instance = this;
        }

        public void OnSceneChange()
        {
            m_currentScene = UnityHelpers.ActiveSceneName;
            SetTransformTarget(null);
        }

        public void SetTransformTarget(Transform t)
        {
            m_currentTransform = t;

            if (m_searching)
                CancelSearch();

            Update_Impl(true);
        }

        public void TraverseUp()
        {
            if (m_currentTransform.parent != null)
            {
                SetTransformTarget(m_currentTransform.parent);
            }
            else
            {
                SetTransformTarget(null);
            }
        }

        public void Search()
        {
            m_searchResults = SearchSceneObjects(m_searchInput);
            m_searching = true;
            Pages.ItemCount = m_searchResults.Count;
        }

        public void CancelSearch()
        {
            m_searching = false;

            if (m_getRootObjectsFailed && !m_currentTransform)
            {
                GetRootObjectsManual_Impl();
            }
        }

        public List<GameObjectCache> SearchSceneObjects(string _search)
        {
            var matches = new List<GameObjectCache>();

            foreach (var obj in Resources.FindObjectsOfTypeAll(ReflectionHelpers.GameObjectType))
            {
                var go = obj.TryCast<GameObject>();
                if (go.name.ToLower().Contains(_search.ToLower()) && go.scene.name == m_currentScene)
                {
                    matches.Add(new GameObjectCache(go));
                }
            }

            return matches;
        }

        public override void Update()
        {
            if (m_searching) return;

            if (Time.time - m_timeOfLastUpdate < PASSIVE_UPDATE_INTERVAL) return;
            m_timeOfLastUpdate = Time.time;

            Update_Impl();
        }

        private void Update_Impl(bool manual = false)
        {
            List<Transform> allTransforms = new List<Transform>();

            // get current list of all transforms (either scene root or our current transform children)
            if (m_currentTransform)
            {
                for (int i = 0; i < m_currentTransform.childCount; i++)
                {
                    allTransforms.Add(m_currentTransform.GetChild(i));
                }
            }
            else
            {
                if (!manual && m_getRootObjectsFailed) return;

                if (!manual)
                {
                    try
                    {
                        var scene = SceneManager.GetSceneByName(m_currentScene);

                        allTransforms.AddRange(scene.GetRootGameObjects()
                                                    .Select(it => it.transform));
                    }
                    catch
                    {
                        m_getRootObjectsFailed = true;
                        allTransforms.AddRange(GetRootObjectsManual_Impl());
                    }
                }
                else
                {
                    allTransforms.AddRange(GetRootObjectsManual_Impl());
                }
            }

            Pages.ItemCount = allTransforms.Count;

            int offset = Pages.CalculateOffsetIndex();

            // sort by childcount
            allTransforms.Sort((a, b) => b.childCount.CompareTo(a.childCount));

            m_objectList.Clear();

            for (int i = offset; i < offset + Pages.ItemsPerPage && i < Pages.ItemCount; i++)
            {
                var child = allTransforms[i];
                m_objectList.Add(new GameObjectCache(child.gameObject));
            }
        }

        private IEnumerable<Transform> GetRootObjectsManual_Impl()
        {
            try
            {
                var array = Resources.FindObjectsOfTypeAll(ReflectionHelpers.TransformType);

                var list = new List<Transform>();
                foreach (var obj in array)
                {
                    var transform = obj.TryCast<Transform>();
                    if (transform.parent == null && transform.gameObject.scene.name == m_currentScene)
                    {
                        list.Add(transform);
                    }
                }
                return list;
            }
            catch (Exception e)
            {
                MelonLogger.Log("Exception getting root scene objects (manual): " 
                    + e.GetType() + ", " + e.Message + "\r\n" 
                    + e.StackTrace);
                return new Transform[0];
            }
        }

        // --------- GUI Draw Function --------- //        

        public override void DrawWindow()
        {
            try
            {
                DrawHeaderArea();

                GUILayout.BeginVertical(GUI.skin.box, null);

                DrawPageButtons();

                if (!m_searching)
                {
                    DrawGameObjectList();
                }
                else
                {
                    DrawSearchResultsList();
                }

                GUILayout.EndVertical();
            }
            catch (Exception e)
            {
                MelonLogger.Log("Exception drawing ScenePage! " + e.GetType() + ", " + e.Message);
                MelonLogger.Log(e.StackTrace);
                m_currentTransform = null;
            }
        }

        private void DrawHeaderArea()
        {
            GUILayout.BeginHorizontal(null);

            // Current Scene label
            GUILayout.Label("Current Scene:", new GUILayoutOption[] { GUILayout.Width(120) });
            try
            {
                // Need to do 'ToList()' so the object isn't cleaned up by Il2Cpp GC.
                var scenes = SceneManager.GetAllScenes().ToList();

                if (scenes.Count > 1)
                {
                    int changeWanted = 0;
                    if (GUILayout.Button("<", new GUILayoutOption[] { GUILayout.Width(30) }))
                    {
                        changeWanted = -1;
                    }
                    if (GUILayout.Button(">", new GUILayoutOption[] { GUILayout.Width(30) }))
                    {
                        changeWanted = 1;
                    }
                    if (changeWanted != 0)
                    {
                        int index = scenes.IndexOf(SceneManager.GetSceneByName(m_currentScene));
                        index += changeWanted;
                        if (index > scenes.Count - 1)
                        {
                            index = 0;
                        }
                        else if (index < 0)
                        {
                            index = scenes.Count - 1;
                        }
                        m_currentScene = scenes[index].name;
                    }
                }
            }
            catch { }
            GUILayout.Label("<color=cyan>" + m_currentScene + "</color>", null); //new GUILayoutOption[] { GUILayout.Width(250) });

            GUILayout.EndHorizontal();

            // ----- GameObject Search -----
            GUILayout.BeginHorizontal(GUI.skin.box, null);
            GUILayout.Label("<b>Search Scene:</b>", new GUILayoutOption[] { GUILayout.Width(100) });
            m_searchInput = GUILayout.TextField(m_searchInput, null);
            if (GUILayout.Button("Search", new GUILayoutOption[] { GUILayout.Width(80) }))
            {
                Search();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
        }

        private void DrawPageButtons()
        {
            GUILayout.BeginHorizontal(null);

            Pages.DrawLimitInputArea();

            if (Pages.ItemCount > Pages.ItemsPerPage)
            {
                if (GUILayout.Button("< Prev", new GUILayoutOption[] { GUILayout.Width(80) }))
                {
                    Pages.TurnPage(Turn.Left, ref this.scroll);

                    Update_Impl(true);
                }

                Pages.CurrentPageLabel();

                if (GUILayout.Button("Next >", new GUILayoutOption[] { GUILayout.Width(80) }))
                {
                    Pages.TurnPage(Turn.Right, ref this.scroll);

                    Update_Impl(true);
                }
            }

            GUILayout.EndHorizontal();
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
        }

        private void DrawGameObjectList()
        {
            if (m_currentTransform != null)
            {
                GUILayout.BeginHorizontal(null);
                if (GUILayout.Button("<-", new GUILayoutOption[] { GUILayout.Width(35) }))
                {
                    TraverseUp();
                }
                else
                {
                    GUILayout.Label("<color=cyan>" + m_currentTransform.GetGameObjectPath() + "</color>",
                        new GUILayoutOption[] { GUILayout.Width(MainMenu.MainRect.width - 187f) });
                }

                UIHelpers.SmallInspectButton(m_currentTransform);
                
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("Scene Root GameObjects:", null);

                if (m_getRootObjectsFailed)
                {
                    if (GUILayout.Button("Update Root Object List (auto-update failed!)", null))
                    {
                        Update_Impl(true);
                    }
                }
            }

            if (m_objectList.Count > 0)
            {
                for (int i = 0; i < m_objectList.Count; i++)
                {
                    var obj = m_objectList[i];

                    if (obj == null) continue;

                    if (!obj.RefGameObject)
                    {
                        string label = "<color=red><i>null";

                        if (obj.RefGameObject != null)
                        {
                            label += " (Destroyed)";
                        }

                        label += "</i></color>";
                        GUILayout.Label(label, null);
                    }
                    else
                    {
                        UIHelpers.GOButton_Impl(obj.RefGameObject,
                        obj.EnabledColor,
                        obj.Label,
                        obj.RefGameObject.activeSelf,
                        SetTransformTarget,
                        true,
                        MainMenu.MainRect.width - 170);
                    }
                }
            }
        }

        private void DrawSearchResultsList()
        {
            if (GUILayout.Button("<- Cancel Search", new GUILayoutOption[] { GUILayout.Width(150) }))
            {
                CancelSearch();
            }

            GUILayout.Label("Search Results:", null);

            if (m_searchResults.Count > 0)
            {
                int offset = Pages.CalculateOffsetIndex();

                for (int i = offset; i < offset + Pages.ItemsPerPage && i < m_searchResults.Count; i++)
                {
                    var obj = m_searchResults[i];

                    if (obj.RefGameObject)
                    {
                        UIHelpers.GOButton_Impl(obj.RefGameObject,
                        obj.EnabledColor,
                        obj.Label,
                        obj.RefGameObject.activeSelf,
                        SetTransformTarget,
                        true,
                        MainMenu.MainRect.width - 170);
                    }
                    else
                    {
                        GUILayout.Label("<i><color=red>Null or destroyed!</color></i>", null);
                    }
                }
            }
            else
            {
                GUILayout.Label("<color=red><i>No results found!</i></color>", null);
            }
        }

        // -------- Mini GameObjectCache class ---------- //
    
        public class GameObjectCache
        {
            public GameObject RefGameObject;
            public string Label;
            public Color EnabledColor;
            public int ChildCount;

            public GameObjectCache(GameObject obj)
            {
                RefGameObject = obj;
                ChildCount = obj.transform.childCount;

                Label = (ChildCount > 0) ? "[" + obj.transform.childCount + " children] " : "";
                Label += obj.name;

                bool enabled = obj.activeSelf;
                int childCount = obj.transform.childCount;
                if (enabled)
                {
                    if (childCount > 0)
                    {
                        EnabledColor = Color.green;
                    }
                    else
                    {
                        EnabledColor = UIStyles.LightGreen;
                    }
                }
                else
                {
                    EnabledColor = Color.red;
                }
            }
        }
    }
}
