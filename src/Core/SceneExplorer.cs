using System;
using System.Collections.Generic;
using System.Linq;
using UnityExplorer.UI;
using UnityExplorer.UI.Main;
using UnityExplorer.UI.Reusable;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI.Main.Home;

namespace UnityExplorer.Core.Inspectors
{
    public class SceneExplorer
    {
        public static SceneExplorer Instance;

        public static SceneExplorerUI UI;

        internal static Action OnToggleShow;

        public SceneExplorer()
        { 
            Instance = this;

            UI = new SceneExplorerUI();
            UI.ConstructScenePane();
        }

        private const float UPDATE_INTERVAL = 1f;
        private float m_timeOfLastSceneUpdate;

        // private int m_currentSceneHandle = -1;
        public static Scene DontDestroyScene => DontDestroyObject.scene;
        internal Scene m_currentScene;
        internal Scene[] m_currentScenes = new Scene[0];

        internal GameObject[] m_allObjects = new GameObject[0];

        internal GameObject m_selectedSceneObject;
        internal int m_lastCount;

        internal static GameObject DontDestroyObject
        {
            get
            {
                if (!s_dontDestroyObject)
                {
                    s_dontDestroyObject = new GameObject("DontDestroyMe");
                    GameObject.DontDestroyOnLoad(s_dontDestroyObject);
                }
                return s_dontDestroyObject;
            }
        }
        internal static GameObject s_dontDestroyObject;

        public void Init()
        {
            RefreshSceneSelector();
        }

        public void Update()
        {
            if (SceneExplorerUI.Hiding || Time.realtimeSinceStartup - m_timeOfLastSceneUpdate < UPDATE_INTERVAL)
            {
                return;
            }

            RefreshSceneSelector();

            if (!m_selectedSceneObject)
            {
                if (m_currentScene != default)
                {
                    var rootObjects = RuntimeProvider.Instance.GetRootGameObjects(m_currentScene);
                    SetSceneObjectList(rootObjects);
                }
            }
            else
            {
                RefreshSelectedSceneObject();
            }
        }

        private void RefreshSceneSelector()
        {
            var newNames = new List<string>();
            var newScenes = new List<Scene>();

            if (m_currentScenes == null)
                m_currentScenes = new Scene[0];

            bool anyChange = SceneManager.sceneCount != m_currentScenes.Length - 1;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (scene == default)
                    continue;

                int handle = RuntimeProvider.Instance.GetSceneHandle(scene);

                if (!anyChange && !m_currentScenes.Any(it => handle == RuntimeProvider.Instance.GetSceneHandle(it)))
                    anyChange = true;

                newScenes.Add(scene);
                newNames.Add(scene.name);
            }

            if (anyChange)
            {
                newNames.Add("DontDestroyOnLoad");
                newScenes.Add(DontDestroyScene);
                m_currentScenes = newScenes.ToArray();

                UI.OnActiveScenesChanged(newNames);

                SetTargetScene(newScenes[0]);

                SearchPage.Instance.OnSceneChange();
            }
        }

        public void SetTargetScene(int index)
            => SetTargetScene(m_currentScenes[index]);

        public void SetTargetScene(Scene scene)
        {
            if (scene == default)
                return;

            m_currentScene = scene;
            var rootObjs = RuntimeProvider.Instance.GetRootGameObjects(scene);
            SetSceneObjectList(rootObjs);

            m_selectedSceneObject = null;

            UI.OnSceneSelected();
        }

        public void SetSceneObjectParent()
        {
            if (!m_selectedSceneObject || !m_selectedSceneObject.transform.parent?.gameObject)
            {
                m_selectedSceneObject = null;
                SetTargetScene(m_currentScene);
            }
            else
            {
                SetTargetObject(m_selectedSceneObject.transform.parent.gameObject);
            }
        }

        public void SetTargetObject(GameObject obj)
        {
            if (!obj)
                return;

            UI.OnGameObjectSelected(obj);

            m_selectedSceneObject = obj;

            RefreshSelectedSceneObject();
        }

        private void RefreshSelectedSceneObject()
        {
            GameObject[] list = new GameObject[m_selectedSceneObject.transform.childCount];
            for (int i = 0; i < m_selectedSceneObject.transform.childCount; i++)
            {
                list[i] = m_selectedSceneObject.transform.GetChild(i).gameObject;
            }

            SetSceneObjectList(list);
        }

        private void SetSceneObjectList(GameObject[] objects)
        {
            m_allObjects = objects;
            RefreshSceneObjectList();
        }

        internal void RefreshSceneObjectList()
        {
            m_timeOfLastSceneUpdate = Time.realtimeSinceStartup;

            UI.RefreshSceneObjectList(m_allObjects, out int newCount);

            m_lastCount = newCount;
        }

        internal static void InspectSelectedGameObject()
        {
            InspectorManager.Instance.Inspect(Instance.m_selectedSceneObject);
        }

        internal static void InvokeOnToggleShow()
        {
            OnToggleShow?.Invoke();
        }
    }
}
