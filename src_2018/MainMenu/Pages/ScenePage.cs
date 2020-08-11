using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Explorer
{
    public class ScenePage : MainMenu.WindowPage
    {
        public static ScenePage Instance;

        public override string Name { get => "Scene Explorer"; set => base.Name = value; }

        // ----- Holders for GUI elements ----- //

        private string m_currentScene = "";

        // gameobject list
        private Transform m_currentTransform;
        private List<GameObject> m_objectList = new List<GameObject>();

        // search bar
        private bool m_searching = false;
        private string m_searchInput = "";
        private List<GameObject> m_searchResults = new List<GameObject>();

        // ------------ Init and Update ------------ //

        public override void Init()
        {
            Instance = this;
        }

        public void OnSceneChange()
        {
            m_currentScene = CppExplorer.ActiveSceneName;

            m_currentTransform = null;
            CancelSearch();

        }

        public override void Update()
        {
            if (!m_searching)
            {
                m_objectList = new List<GameObject>();
                if (m_currentTransform)
                {
                    var noChildren = new List<GameObject>();
                    for (int i = 0; i < m_currentTransform.childCount; i++)
                    {
                        var child = m_currentTransform.GetChild(i);

                        if (child)
                        {
                            if (child.childCount > 0)
                                m_objectList.Add(child.gameObject);
                            else
                                noChildren.Add(child.gameObject);
                        }
                    }
                    m_objectList.AddRange(noChildren);
                    noChildren = null;
                }
                else
                {
                    var scene = SceneManager.GetSceneByName(m_currentScene);
                    var rootObjects = scene.GetRootGameObjects();

                    // add objects with children first
                    foreach (var obj in rootObjects.Where(x => x.transform.childCount > 0))
                    {
                        m_objectList.Add(obj);
                    }
                    foreach (var obj in rootObjects.Where(x => x.transform.childCount == 0))
                    {
                        m_objectList.Add(obj);
                    }
                }
            }
        }

        // --------- GUI Draw Functions --------- //        

        public override void DrawWindow()
        {
            try
            {
                GUILayout.BeginHorizontal(null);
                // Current Scene label
                GUILayout.Label("Current Scene:", new GUILayoutOption[] { GUILayout.Width(120) });
                if (SceneManager.sceneCount > 1)
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
                        var scenes = SceneManager.GetAllScenes();
                        int index = scenes.IndexOf(SceneManager.GetSceneByName(m_currentScene));
                        index += changeWanted;
                        if (index >= scenes.Count - 1)
                        {
                            index = 0;
                        }
                        else if (index > 0)
                        {
                            index = scenes.Count - 1;
                        }
                        m_currentScene = scenes[index].name;
                    }
                }
                GUILayout.Label("<color=cyan>" + m_currentScene + "</color>", null);
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

                GUILayout.Space(15);

                // ************** GameObject list ***************

                // ----- main explorer ------
                if (!m_searching)
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
                            GUILayout.Label(CppExplorer.GetGameObjectPath(m_currentTransform), null);
                        }
                        GUILayout.EndHorizontal();
                    }
                    else
                    {
                        GUILayout.Label("Scene Root GameObjects:", null);
                    }

                    if (m_objectList.Count > 0)
                    {
                        foreach (var obj in m_objectList)
                        {
                            UIStyles.GameobjButton(obj, SetTransformTarget, true, MainMenu.MainRect.width - 170);
                        }
                    }
                    else
                    {
                        // if m_currentTransform != null ...
                    }
                }
                else // ------ Scene Search results ------
                {
                    if (GUILayout.Button("<-", new GUILayoutOption[] { GUILayout.Width(35) }))
                    {
                        CancelSearch();
                    }

                    GUILayout.Label("Search Results:", null);

                    if (m_searchResults.Count > 0)
                    {
                        foreach (var obj in m_searchResults)
                        {
                            UIStyles.GameobjButton(obj, SetTransformTarget, true, MainMenu.MainRect.width - 170);
                        }
                    }
                    else
                    {
                        GUILayout.Label("<color=red><i>No results found!</i></color>", null);
                    }
                }
            }
            catch
            {
                m_currentTransform = null;
            }
        }



        // -------- Actual Methods (not drawing GUI) ---------- //

        public void SetTransformTarget(GameObject obj)
        {
            m_currentTransform = obj.transform;
            CancelSearch();
        }

        public void TraverseUp()
        {
            if (m_currentTransform.parent != null)
            {
                m_currentTransform = m_currentTransform.parent;
            }
            else
            {
                m_currentTransform = null;
            }
        }

        public void Search()
        {
            m_searchResults = SearchSceneObjects(m_searchInput);
            m_searching = true;
        }

        public void CancelSearch()
        {
            m_searching = false;
        }

        public List<GameObject> SearchSceneObjects(string _search)
        {
            var matches = new List<GameObject>();

            foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (obj.name.ToLower().Contains(_search.ToLower()) && obj.scene.name == m_currentScene)
                {
                    matches.Add(obj);
                }
            }

            return matches;
        }
    }
}
