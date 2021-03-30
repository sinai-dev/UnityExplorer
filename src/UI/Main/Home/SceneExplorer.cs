using System;
using System.Collections.Generic;
using System.Linq;
using UnityExplorer.UI;
using UnityExplorer.UI.Main;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI.Main.Home;
using UnityExplorer.Core.Config;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.Main.Search;

namespace UnityExplorer.UI.Main.Home
{
    public class SceneExplorer
    {
        public static SceneExplorer Instance;

        internal static Action<bool> OnToggleShow;

        public SceneExplorer()
        {
            Instance = this;

            ConstructScenePane();
        }

        internal bool Hiding;

        private const float UPDATE_INTERVAL = 1f;
        private float m_timeOfLastSceneUpdate;

        // private int m_currentSceneHandle = -1;
        public static Scene DontDestroyScene => DontDestroyObject.scene;
        internal Scene m_currentScene;
        internal Scene[] m_currentScenes = new Scene[0];

        internal GameObject[] m_allObjects = new GameObject[0];

        internal GameObject m_selectedSceneObject;
        internal int m_lastCount;

        private Dropdown m_sceneDropdown;
        private Text m_sceneDropdownText;
        private Text m_scenePathText;
        private GameObject m_mainInspectBtn;
        private GameObject m_backButtonObj;

        public PageHandler m_pageHandler;
        private GameObject m_pageContent;
        private readonly List<Text> m_shortListTexts = new List<Text>();
        private readonly List<Toggle> m_shortListToggles = new List<Toggle>();

        internal readonly List<GameObject> m_shortList = new List<GameObject>();

        private Text m_hideText;
        private GameObject m_titleObj;
        private GameObject m_sceneDropdownObj;
        private GameObject m_scenePathGroupObj;
        private GameObject m_scrollObj;
        private LayoutElement m_leftLayout;

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

            if (!ConfigManager.Last_SceneExplorer_State.Value)
                ToggleShow();
        }

        public void Update()
        {
            if (Hiding || Time.realtimeSinceStartup - m_timeOfLastSceneUpdate < UPDATE_INTERVAL)
                return;

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

                OnActiveScenesChanged(newNames);

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

            OnSceneSelected();
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

            OnGameObjectSelected(obj);

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

            RefreshSceneObjectList(m_allObjects, out int newCount);

            m_lastCount = newCount;
        }

        internal static void InspectSelectedGameObject()
        {
            InspectorManager.Instance.Inspect(Instance.m_selectedSceneObject);
        }

        internal static void InvokeOnToggleShow()
        {
            OnToggleShow?.Invoke(!Instance.Hiding);
        }

        public void ToggleShow()
        {
            if (!Hiding)
            {
                Hiding = true;

                m_hideText.text = "►";
                m_titleObj.SetActive(false);
                m_sceneDropdownObj.SetActive(false);
                m_scenePathGroupObj.SetActive(false);
                m_scrollObj.SetActive(false);
                m_pageHandler.Hide();

                m_leftLayout.minWidth = 15;
            }
            else
            {
                Hiding = false;

                m_hideText.text = "Hide Scene Explorer";
                m_titleObj.SetActive(true);
                m_sceneDropdownObj.SetActive(true);
                m_scenePathGroupObj.SetActive(true);
                m_scrollObj.SetActive(true);

                m_leftLayout.minWidth = 350;

                Update();
            }

            InvokeOnToggleShow();
        }

        public void OnActiveScenesChanged(List<string> newNames)
        {
            m_sceneDropdown.options.Clear();

            foreach (string scene in newNames)
            {
                m_sceneDropdown.options.Add(new Dropdown.OptionData { text = scene });
            }

            m_sceneDropdown.OnCancel(null);
            m_sceneDropdownText.text = newNames[0];
        }

        private void SceneListObjectClicked(int index)
        {
            if (index >= m_shortList.Count || !m_shortList[index])
            {
                return;
            }

            var obj = m_shortList[index];
            if (obj.transform.childCount > 0)
                SetTargetObject(obj);
            else
                InspectorManager.Instance.Inspect(obj);
        }

        internal void RefreshSceneObjectList(GameObject[] allObjects, out int newCount)
        {
            var objects = allObjects;
            m_pageHandler.ListCount = objects.Length;

            //int startIndex = m_sceneListPageHandler.StartIndex;

            newCount = 0;

            foreach (var itemIndex in m_pageHandler)
            {
                newCount++;

                // normalized index starting from 0
                var i = itemIndex - m_pageHandler.StartIndex;

                if (itemIndex >= objects.Length)
                {
                    if (i > SceneExplorer.Instance.m_lastCount || i >= m_shortListTexts.Count)
                        break;

                    GameObject label = m_shortListTexts[i].transform.parent.parent.gameObject;
                    if (label.activeSelf)
                        label.SetActive(false);
                }
                else
                {
                    GameObject obj = objects[itemIndex];

                    if (!obj)
                        continue;

                    if (i >= m_shortList.Count)
                    {
                        m_shortList.Add(obj);
                        AddObjectListButton();
                    }
                    else
                    {
                        m_shortList[i] = obj;
                    }

                    var text = m_shortListTexts[i];

                    var name = obj.name;

                    if (obj.transform.childCount > 0)
                        name = $"<color=grey>[{obj.transform.childCount}]</color> {name}";

                    text.text = name;
                    text.color = obj.activeSelf ? Color.green : Color.red;

                    var tog = m_shortListToggles[i];
                    tog.isOn = obj.activeSelf;

                    var label = text.transform.parent.parent.gameObject;
                    if (!label.activeSelf)
                    {
                        label.SetActive(true);
                    }
                }
            }
        }

        private void OnSceneListPageTurn()
        {
            RefreshSceneObjectList();
        }

        private void OnToggleClicked(int index, bool val)
        {
            if (index >= m_shortList.Count || !m_shortList[index])
                return;

            var obj = m_shortList[index];
            obj.SetActive(val);
        }

        internal void OnGameObjectSelected(GameObject obj)
        {
            m_scenePathText.text = obj.name;
            if (!m_backButtonObj.activeSelf)
            {
                m_backButtonObj.SetActive(true);
                m_mainInspectBtn.SetActive(true);
            }
        }

        internal void OnSceneSelected()
        {
            if (m_backButtonObj.activeSelf)
            {
                m_backButtonObj.SetActive(false);
                m_mainInspectBtn.SetActive(false);
            }

            m_scenePathText.text = "Scene root:";
            //m_scenePathText.ForceMeshUpdate();
        }

        #region UI CONSTRUCTION

        public void ConstructScenePane()
        {
            GameObject leftPane = UIFactory.CreateVerticalGroup(HomePage.Instance.Content, "SceneGroup", true, false, true, true, 0, default,
                new Color(72f / 255f, 72f / 255f, 72f / 255f));

            m_leftLayout = leftPane.AddComponent<LayoutElement>();
            m_leftLayout.minWidth = 350;
            m_leftLayout.flexibleWidth = 0;

            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(leftPane, true, true, true, true, spacing: 4, padTop: 8, 4, 4, 4);

            m_titleObj = UIFactory.CreateLabel(leftPane, "SceneExplorerTitle", "Scene Explorer", TextAnchor.UpperLeft, default, true, 20).gameObject;
            UIFactory.SetLayoutElement(m_titleObj, minHeight: 30, flexibleHeight: 0);

            m_sceneDropdownObj = UIFactory.CreateDropdown(leftPane, out m_sceneDropdown, "<notset>", 14, null);
            UIFactory.SetLayoutElement(m_sceneDropdownObj, minHeight: 40, minWidth: 320, flexibleWidth: 0, flexibleHeight: 0);

            m_sceneDropdownText = m_sceneDropdown.transform.Find("Label").GetComponent<Text>();
            m_sceneDropdown.onValueChanged.AddListener((int val) => { SetTargetScene(val); });

            m_scenePathGroupObj = UIFactory.CreateHorizontalGroup(leftPane, "ScenePathGroup", true, true, true, true, 5, default, new Color(1, 1, 1, 0f));
            UIFactory.SetLayoutElement(m_scenePathGroupObj, minHeight: 20, minWidth: 335);

            var backBtnObj = UIFactory.CreateButton(m_scenePathGroupObj, 
                "BackButton", 
                "◄", 
                () => { SetSceneObjectParent(); }, 
                new Color(0.12f, 0.12f, 0.12f));

            m_backButtonObj = backBtnObj.gameObject;

            UIFactory.SetLayoutElement(m_backButtonObj, minWidth: 40, flexibleWidth: 0);

            GameObject scenePathLabel = UIFactory.CreateHorizontalGroup(m_scenePathGroupObj, "ScenePathLabel", false, false, false, false);
            Image image = scenePathLabel.GetComponent<Image>();
            image.color = Color.white;
            scenePathLabel.AddComponent<Mask>().showMaskGraphic = false;

            UIFactory.SetLayoutElement(scenePathLabel, minWidth: 210, minHeight: 20, flexibleWidth: 120);

            m_scenePathText = UIFactory.CreateLabel(scenePathLabel, "ScenePathLabelText", "Scene root:", TextAnchor.MiddleLeft, default, true, 15);
            m_scenePathText.horizontalOverflow = HorizontalWrapMode.Overflow;

            UIFactory.SetLayoutElement(m_scenePathText.gameObject, minWidth: 210, flexibleWidth: 120, minHeight: 20);

            var mainInspectButton = UIFactory.CreateButton(m_scenePathGroupObj,
                "MainInspectButton",
                "Inspect",
                () => { InspectSelectedGameObject(); },
                new Color(0.12f, 0.12f, 0.12f));

            m_mainInspectBtn = mainInspectButton.gameObject;
            UIFactory.SetLayoutElement(m_mainInspectBtn, minWidth: 65);

            m_scrollObj = UIFactory.CreateScrollView(leftPane, "SceneExplorerScrollView",
                out m_pageContent, out SliderScrollbar scroller, new Color(0.1f, 0.1f, 0.1f));

            m_pageHandler = new PageHandler(scroller);
            m_pageHandler.ConstructUI(leftPane);
            m_pageHandler.OnPageChanged += OnSceneListPageTurn;

            // hide button

            var hideButton = UIFactory.CreateButton(leftPane, "HideButton", "Hide Scene Explorer", ToggleShow, new Color(0.15f, 0.15f, 0.15f));
            hideButton.GetComponentInChildren<Text>().fontSize = 13;
            m_hideText = hideButton.GetComponentInChildren<Text>();

            UIFactory.SetLayoutElement(hideButton.gameObject, minWidth: 20, minHeight: 20);
        }

        private void AddObjectListButton()
        {
            int thisIndex = m_shortListTexts.Count();

            GameObject btnGroupObj = UIFactory.CreateHorizontalGroup(m_pageContent, "SceneObjectButton", true, false, true, true, 
                0, default, new Color(0.1f, 0.1f, 0.1f));

            UIFactory.SetLayoutElement(btnGroupObj, flexibleWidth: 320, minHeight: 25);

            btnGroupObj.AddComponent<Mask>();

            var toggleObj = UIFactory.CreateToggle(btnGroupObj, "ObjectToggleButton", out Toggle toggle, out Text toggleText, new Color(0.1f, 0.1f, 0.1f));
            var toggleLayout = toggleObj.AddComponent<LayoutElement>();
            toggleLayout.minHeight = 25;
            toggleLayout.minWidth = 25;
            toggleText.text = "";
            toggle.isOn = false;
            m_shortListToggles.Add(toggle);
            toggle.onValueChanged.AddListener((bool val) => { OnToggleClicked(thisIndex, val); });

            ColorBlock mainColors = new ColorBlock();
            mainColors.normalColor = new Color(0.1f, 0.1f, 0.1f);
            mainColors.highlightedColor = new Color(0.2f, 0.2f, 0.2f);
            mainColors.pressedColor = new Color(0.05f, 0.05f, 0.05f);

            var mainButton = UIFactory.CreateButton(btnGroupObj, 
                "MainButton", 
                "",
                () => { SceneListObjectClicked(thisIndex); },
                mainColors);

            UIFactory.SetLayoutElement(mainButton.gameObject, minHeight: 25, minWidth: 230);

            Text mainText = mainButton.GetComponentInChildren<Text>();
            mainText.alignment = TextAnchor.MiddleLeft;
            mainText.horizontalOverflow = HorizontalWrapMode.Overflow;
            m_shortListTexts.Add(mainText);

            ColorBlock inspectColors = new ColorBlock();
            inspectColors.normalColor = new Color(0.15f, 0.15f, 0.15f);
            inspectColors.highlightedColor = new Color(0.2f, 0.2f, 0.2f);
            inspectColors.pressedColor = new Color(0.1f, 0.1f, 0.1f);

            var inspectButton = UIFactory.CreateButton(btnGroupObj, 
                "InspectButton", 
                "Inspect",
                () => { InspectorManager.Instance.Inspect(m_shortList[thisIndex]); },
                inspectColors);

            UIFactory.SetLayoutElement(inspectButton.gameObject, minWidth: 60, minHeight: 25);
        }

        #endregion
    }
}
