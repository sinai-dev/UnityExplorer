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
using System.IO;
using UnityExplorer.Core;

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
        private GameObject m_sceneDropdownObj;
        private GameObject m_scenePathGroupObj;
        private GameObject m_mainContent;

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

        public void ToggleShow()
        {
            if (!Hiding)
            {
                Hiding = true;

                m_hideText.text = "►";
                m_mainContent.SetActive(false);
                m_pageHandler.Hide();
            }
            else
            {
                Hiding = false;

                m_hideText.text = "◄";
                m_mainContent.SetActive(true);
                m_pageHandler.Show();

                Update();
            }

            InvokeOnToggleShow();
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
            var coreGroup = UIFactory.CreateHorizontalGroup(HomePage.Instance.Content, "SceneExplorer", true, true, true, true, 4, new Vector4(2, 2, 2, 2),
                new Color(0.05f, 0.05f, 0.05f));

            // hide button

            var hideButton = UIFactory.CreateButton(coreGroup, "HideButton", "◄", ToggleShow, new Color(0.15f, 0.15f, 0.15f));
            hideButton.GetComponentInChildren<Text>().fontSize = 13;
            m_hideText = hideButton.GetComponentInChildren<Text>();
            UIFactory.SetLayoutElement(hideButton.gameObject, minWidth: 20, minHeight: 20, flexibleWidth: 0, flexibleHeight: 9999);

            m_mainContent = UIFactory.CreateVerticalGroup(coreGroup, "SceneGroup", true, false, true, true, 0, default,
                new Color(72f / 255f, 72f / 255f, 72f / 255f));
            UIFactory.SetLayoutElement(m_mainContent, minWidth: 350, flexibleWidth: 0);

            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(m_mainContent, true, true, true, true, spacing: 4, padTop: 8, 4, 4, 4);

            var titleObj = UIFactory.CreateLabel(m_mainContent, "SceneExplorerTitle", "Scene Explorer", TextAnchor.UpperLeft, default, true, 20).gameObject;
            UIFactory.SetLayoutElement(titleObj, minHeight: 30, flexibleHeight: 0);

            m_sceneDropdownObj = UIFactory.CreateDropdown(m_mainContent, out m_sceneDropdown, "", 14, null);
            UIFactory.SetLayoutElement(m_sceneDropdownObj, minHeight: 40, minWidth: 320, flexibleWidth: 0, flexibleHeight: 0);

            m_sceneDropdownText = m_sceneDropdown.transform.Find("Label").GetComponent<Text>();
            m_sceneDropdown.onValueChanged.AddListener((int val) => { SetTargetScene(val); });

            m_scenePathGroupObj = UIFactory.CreateHorizontalGroup(m_mainContent, "ScenePathGroup", true, true, true, true, 5, default, new Color(1, 1, 1, 0f));
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

            var scrollObj = UIFactory.CreateScrollView(m_mainContent, "SceneExplorerScrollView",
                out m_pageContent, out SliderScrollbar scroller, new Color(0.1f, 0.1f, 0.1f));

            m_pageHandler = new PageHandler(scroller);
            m_pageHandler.ConstructUI(m_mainContent);
            m_pageHandler.OnPageChanged += OnSceneListPageTurn;

            // Scene Loader
            try
            {
                Type sceneUtil = ReflectionUtility.GetTypeByName("UnityEngine.SceneManagement.SceneUtility");
                if (sceneUtil == null)
                    throw new Exception("This version of Unity does not ship with the 'SceneUtility' class, or it was not unstripped.");
                var method = sceneUtil.GetMethod("GetScenePathByBuildIndex", ReflectionUtility.AllFlags);

                var title2 = UIFactory.CreateLabel(m_mainContent, "SceneLoaderLabel", "Scene Loader", TextAnchor.MiddleLeft, Color.white, true, 20);
                UIFactory.SetLayoutElement(title2.gameObject, minHeight: 30, flexibleHeight: 0);

                var allSceneDropObj = UIFactory.CreateDropdown(m_mainContent, out Dropdown allSceneDrop, "", 14, null);
                UIFactory.SetLayoutElement(allSceneDropObj, minHeight: 40, minWidth: 320, flexibleWidth: 0, flexibleHeight: 0);

                int sceneCount = SceneManager.sceneCountInBuildSettings;
                for (int i = 0; i < sceneCount; i++)
                {
                    var scenePath = (string)method.Invoke(null, new object[] { i });
                    allSceneDrop.options.Add(new Dropdown.OptionData(Path.GetFileNameWithoutExtension(scenePath)));
                }
                allSceneDrop.value = 1;
                allSceneDrop.value = 0;

                var buttonRow = UIFactory.CreateHorizontalGroup(m_mainContent, "LoadButtons", true, true, true, true, 4);

                var loadButton = UIFactory.CreateButton(buttonRow, "LoadSceneButton", "Load (Single)", () =>
                {
                    try
                    {
                        SceneManager.LoadScene(allSceneDrop.options[allSceneDrop.value].text);
                    }
                    catch (Exception ex)
                    {
                        ExplorerCore.LogWarning($"Unable to load the Scene! {ex.ReflectionExToString()}");
                    }
                }, new Color(0.1f, 0.3f, 0.3f));
                UIFactory.SetLayoutElement(loadButton.gameObject, minHeight: 40, minWidth: 150);

                var loadAdditiveButton = UIFactory.CreateButton(buttonRow, "LoadSceneButton", "Load (Additive)", () =>
                {
                    try
                    {
                        SceneManager.LoadScene(allSceneDrop.options[allSceneDrop.value].text, LoadSceneMode.Additive);
                    }
                    catch (Exception ex)
                    {
                        ExplorerCore.LogWarning($"Unable to load the Scene! {ex.ReflectionExToString()}");
                    }
                }, new Color(0.1f, 0.3f, 0.3f));
                UIFactory.SetLayoutElement(loadAdditiveButton.gameObject, minHeight: 40, minWidth: 150);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Could not create the Scene Loader helper! {ex.ReflectionExToString()}");
            }
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
            mainColors = RuntimeProvider.Instance.SetColorBlock(mainColors, new Color(0.1f, 0.1f, 0.1f), 
                new Color(0.2f, 0.2f, 0.2f), new Color(0.05f, 0.05f, 0.05f));

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
            inspectColors = RuntimeProvider.Instance.SetColorBlock(inspectColors, new Color(0.15f, 0.15f, 0.15f),
                new Color(0.2f, 0.2f, 0.2f), new Color(0.1f, 0.1f, 0.1f));

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
