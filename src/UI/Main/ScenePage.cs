using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Explorer.UI.Shared;
using Explorer.CacheObject;

namespace Explorer.UI.Main
{
    public class ScenePage : BaseMainMenuPage
    {
        public static ScenePage Instance;

        public override string Name { get => "Scenes"; }

        public PageHelper Pages = new PageHelper();

        private float m_timeOfLastUpdate = -1f;
        private const int PASSIVE_UPDATE_INTERVAL = 1;

        private static string m_currentScene = "";

        // gameobject list
        private Transform m_currentTransform;
        private readonly List<CacheObjectBase> m_objectList = new List<CacheObjectBase>();

        // search bar
        private bool m_searching = false;
        private string m_searchInput = "";
        private List<CacheObjectBase> m_searchResults = new List<CacheObjectBase>();

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

            Update_Impl();
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
        }

        public List<CacheObjectBase> SearchSceneObjects(string _search)
        {
            var matches = new List<CacheObjectBase>();

            foreach (var obj in Resources.FindObjectsOfTypeAll(ReflectionHelpers.GameObjectType))
            {
#if CPP
                var go = obj.TryCast<GameObject>();
#else
                var go = obj as GameObject;
#endif
                if (go.name.ToLower().Contains(_search.ToLower()) && go.scene.name == m_currentScene)
                {
                    matches.Add(CacheFactory.GetCacheObject(go));
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

        private void Update_Impl()
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
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);

                    if (scene.name == m_currentScene)
                    {
                        var rootObjects =
#if CPP
                            Unstrip.Scenes.SceneUnstrip.GetRootGameObjects(scene)
                                                  .Select(it => it.transform);
#else
                            scene.GetRootGameObjects().Select(it => it.transform);
#endif
                        allTransforms.AddRange(rootObjects);

                        break;
                    }
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
                m_objectList.Add(CacheFactory.GetCacheObject(child));
            }
        }

        // --------- GUI Draw Function --------- //

        public override void DrawWindow()
        {
            try
            {
                DrawHeaderArea();

                GUIUnstrip.BeginVertical(GUIContent.none, GUI.skin.box, null);

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
                if (!e.Message.Contains("in a group with only"))
                {
                    ExplorerCore.Log(e.ToString());
                }
            }
        }

        private void DrawHeaderArea()
        {
            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);

            // Current Scene label
            GUILayout.Label("Current Scene:", new GUILayoutOption[] { GUILayout.Width(120) });
            SceneChangeButtons();
            GUILayout.Label("<color=cyan>" + m_currentScene + "</color>", new GUILayoutOption[0]);

            GUILayout.EndHorizontal();

            // ----- GameObject Search -----
            GUIUnstrip.BeginHorizontal(GUIContent.none, GUI.skin.box, null);
            GUILayout.Label("<b>Search Scene:</b>", new GUILayoutOption[] { GUILayout.Width(100) });

            m_searchInput = GUIUnstrip.TextField(m_searchInput, new GUILayoutOption[0]);

            if (GUILayout.Button("Search", new GUILayoutOption[] { GUILayout.Width(80) }))
            {
                Search();
            }
            GUILayout.EndHorizontal();

            GUIUnstrip.Space(5);
        }

        private void SceneChangeButtons()
        {
            var scenes = new List<Scene>();
            var names = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                names.Add(scene.name);
                scenes.Add(scene);
            }

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
                    int index = names.IndexOf(m_currentScene);
                    index += changeWanted;
                    
                    if (index >= 0 && index < SceneManager.sceneCount)
                    {
                        m_currentScene = scenes[index].name;
                        Update_Impl();
                    }
                }
            }
        }

        private void DrawPageButtons()
        {
            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);

            Pages.DrawLimitInputArea();

            if (Pages.ItemCount > Pages.ItemsPerPage)
            {
                if (GUILayout.Button("< Prev", new GUILayoutOption[] { GUILayout.Width(80) }))
                {
                    Pages.TurnPage(Turn.Left, ref this.scroll);

                    Update_Impl();
                }

                Pages.CurrentPageLabel();

                if (GUILayout.Button("Next >", new GUILayoutOption[] { GUILayout.Width(80) }))
                {
                    Pages.TurnPage(Turn.Right, ref this.scroll);

                    Update_Impl();
                }
            }

            GUILayout.EndHorizontal();
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
        }

        private void DrawGameObjectList()
        {
            if (m_currentTransform != null)
            {
                GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
                if (GUILayout.Button("<-", new GUILayoutOption[] { GUILayout.Width(35) }))
                {
                    TraverseUp();
                }
                else
                {
                    GUILayout.Label("<color=cyan>" + m_currentTransform.GetGameObjectPath() + "</color>",
                        new GUILayoutOption[] { GUILayout.Width(MainMenu.MainRect.width - 187f) });
                }

                Buttons.InspectButton(m_currentTransform);

                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("Scene Root GameObjects:", new GUILayoutOption[0]);
            }

            if (m_objectList.Count > 0)
            {
                for (int i = 0; i < m_objectList.Count; i++)
                {
                    var obj = m_objectList[i];

                    if (obj == null) continue;

                    try
                    {
                        var go = obj.IValue.Value as GameObject ?? (obj.IValue.Value as Transform)?.gameObject;

                        if (!go)
                        {
                            string label = "<color=red><i>null";

                            if (go != null)
                            {
                                label += " (Destroyed)";
                            }

                            label += "</i></color>";
                            GUILayout.Label(label, new GUILayoutOption[0]);
                        }
                        else
                        {
                            Buttons.GameObjectButton(go, SetTransformTarget, true, MainMenu.MainRect.width - 170f);
                        }
                    }
                    catch { }
                }
            }
        }

        private void DrawSearchResultsList()
        {
            if (GUILayout.Button("<- Cancel Search", new GUILayoutOption[] { GUILayout.Width(150) }))
            {
                CancelSearch();
            }

            GUILayout.Label("Search Results:", new GUILayoutOption[0]);

            if (m_searchResults.Count > 0)
            {
                int offset = Pages.CalculateOffsetIndex();

                for (int i = offset; i < offset + Pages.ItemsPerPage && i < m_searchResults.Count; i++)
                {
                    var obj = m_searchResults[i].IValue.Value as GameObject;

                    if (obj)
                    {
                        Buttons.GameObjectButton(obj, SetTransformTarget, true, MainMenu.MainRect.width - 170);
                    }
                    else
                    {
                        GUILayout.Label("<i><color=red>Null or destroyed!</color></i>", new GUILayoutOption[0]);
                    }
                }
            }
            else
            {
                GUILayout.Label("<color=red><i>No results found!</i></color>", new GUILayoutOption[0]);
            }
        }
    }
}
