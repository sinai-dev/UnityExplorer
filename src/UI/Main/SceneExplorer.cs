using System;
using System.Collections.Generic;
using System.Linq;
using ExplorerBeta.Helpers;
using ExplorerBeta.UI.Main.Inspectors;
using ExplorerBeta.UI.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ExplorerBeta.Unstrip.Scenes;

namespace ExplorerBeta.UI.Main
{
    public class SceneExplorer
    {
        public static SceneExplorer Instance;

        public SceneExplorer()
        { 
            Instance = this; 
            ConstructScenePane();
        }

        private const float UPDATE_INTERVAL = 1f;
        private float m_timeOfLastSceneUpdate;

        private GameObject m_selectedSceneObject;
        private int m_currentSceneHandle = -1;
        private int m_lastCount;

        public PageHandler m_sceneListPageHandler;

        private GameObject[] m_allSceneListObjects = new GameObject[0];
        private readonly List<GameObject> m_sceneShortList = new List<GameObject>();
        private readonly List<Text> m_sceneListTexts = new List<Text>();

        public static int DontDestroyHandle;
        
        private GameObject m_sceneListCanvas;
        private Dropdown m_sceneDropdown;
        private Text m_scenePathText;
        private GameObject m_mainInspectBtn;
        private GameObject m_backButtonObj;

        //private readonly Dictionary<string, int> m_sceneHandles = new Dictionary<string, int>();

        public void Init()
        {
            // Get DontDestroyOnLoad scene handle. I think it's always -12, but best to be safe.
            GameObject test = new GameObject();
            GameObject.DontDestroyOnLoad(test);
            DontDestroyHandle = test.scene.handle;
            GameObject.Destroy(test);

            RefreshActiveScenes();
        }

        public void Update()
        {
            if (Time.realtimeSinceStartup - m_timeOfLastSceneUpdate < UPDATE_INTERVAL)
            {
                return;
            }

            RefreshActiveScenes();

            if (!m_selectedSceneObject)
            {
                if (m_currentSceneHandle != -1)
                {
                    SetSceneObjectList(SceneUnstrip.GetRootGameObjects(m_currentSceneHandle));
                }
            }
            else
            {
                RefreshSelectedSceneObject();
            }
        }

        //private int StoreScenehandle(Scene scene)
        //{
        //    if (scene == null || scene.handle == -1)
        //        return -1;

        //    if (!m_sceneHandles.ContainsKey(scene.name))
        //    {
        //        m_sceneHandles.Add(scene.name, scene.handle);
        //    }
        //    return scene.handle;
        //}

        public int GetSceneHandle(string sceneName)
        {
            if (sceneName == "DontDestroyOnLoad")
                return DontDestroyHandle;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == sceneName)
                    return scene.handle;
            }
            return -1;
        }

        internal void OnSceneChange()
        {
            m_sceneDropdown.OnCancel(null);
            RefreshActiveScenes();
        }

        private void RefreshActiveScenes()
        {
            var names = new List<string>();
            var handles = new List<int>();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                int handle = scene.handle;

                if (scene == null || handle == -1 || string.IsNullOrEmpty(scene.name))
                    continue;

                handles.Add(handle);
                names.Add(scene.name);
            }

            names.Add("DontDestroyOnLoad");
            handles.Add(DontDestroyHandle);

            m_sceneDropdown.options.Clear();

            foreach (string scene in names)
            {
                m_sceneDropdown.options.Add(new Dropdown.OptionData
                {
                    text = scene
                });
            }

            if (!handles.Contains(m_currentSceneHandle))
            {
                ExplorerCore.Log("Reverting to default scene");
                m_sceneDropdown.transform.Find("Label").GetComponent<Text>().text = names[0];
                SetScene(handles[0]);
            }
        }

        public void SetScene(string name) => SetScene(GetSceneHandle(name));

        public void SetScene(int handle)
        {
            if (handle == -1)
                return;

            m_currentSceneHandle = handle;

            GameObject[] rootObjs = SceneUnstrip.GetRootGameObjects(handle);
            SetSceneObjectList(rootObjs);

            m_selectedSceneObject = null;

            if (m_backButtonObj.activeSelf)
            {
                m_backButtonObj.SetActive(false);
                m_mainInspectBtn.SetActive(false);
            }

            m_scenePathText.text = "Scene root:";
            //m_scenePathText.ForceMeshUpdate();
        }

        public void SetSceneListObject(GameObject obj)
        {
            m_scenePathText.text = obj.name;
            //m_scenePathText.ForceMeshUpdate();

            m_selectedSceneObject = obj;

            RefreshSelectedSceneObject();

            if (!m_backButtonObj.activeSelf)
            {
                m_backButtonObj.SetActive(true);
                m_mainInspectBtn.SetActive(true);
            }
        }

        private void SceneListObjectClicked(int index)
        {
            if (index >= m_sceneShortList.Count || !m_sceneShortList[index])
            {
                return;
            }

            SetSceneListObject(m_sceneShortList[index]);
        }

        private void OnSceneListPageTurn()
        {
            SetSceneObjectList(m_allSceneListObjects);
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
            m_timeOfLastSceneUpdate = Time.realtimeSinceStartup;

            m_allSceneListObjects = objects;
            m_sceneListPageHandler.ListCount = objects.Length;

            int startIndex = m_sceneListPageHandler.IndexOffset;

            int newCount = 0;

            for (int i = 0; i < m_sceneListPageHandler.ItemsPerPage; i++)
            {
                newCount++;

                if (i + startIndex >= objects.Length)
                {
                    if (i > m_lastCount || i >= m_sceneListTexts.Count)
                        break;

                    GameObject label = m_sceneListTexts[i].transform.parent.parent.gameObject;
                    if (label.activeSelf)
                        label.SetActive(false);
                }
                else
                {
                    GameObject obj = objects[i + startIndex];

                    if (i >= m_sceneShortList.Count)
                    {
                        m_sceneShortList.Add(obj);
                        AddSceneButton();
                    }
                    else
                    {
                        m_sceneShortList[i] = obj;
                    }

                    var text = m_sceneListTexts[i];

                    var name = obj.name;

                    if (obj.transform.childCount > 0)
                        name = $"<color=grey>[{obj.transform.childCount}]</color> {name}";

                    text.text = name;
                    text.color = obj.activeSelf ? Color.green : Color.red;

                    var label = text.transform.parent.parent.gameObject;
                    if (!label.activeSelf)
                    {
                        label.SetActive(true);
                    }
                }
            }

            m_lastCount = newCount;
        }

        #region SCENE PANE

        public void ConstructScenePane()
        {
            GameObject leftPane = UIFactory.CreateVerticalGroup(HomePage.Instance.Content, new Color(72f / 255f, 72f / 255f, 72f / 255f));
            LayoutElement leftLayout = leftPane.AddComponent<LayoutElement>();
            leftLayout.minWidth = 350;
            leftLayout.flexibleWidth = 0;

            VerticalLayoutGroup leftGroup = leftPane.GetComponent<VerticalLayoutGroup>();
            leftGroup.padding.left = 8;
            leftGroup.padding.right = 8;
            leftGroup.padding.top = 8;
            leftGroup.padding.bottom = 8;
            leftGroup.spacing = 5;
            leftGroup.childControlWidth = true;
            leftGroup.childControlHeight = true;
            leftGroup.childForceExpandWidth = true;
            leftGroup.childForceExpandHeight = true;

            GameObject titleObj = UIFactory.CreateLabel(leftPane, TextAnchor.UpperLeft);
            Text titleLabel = titleObj.GetComponent<Text>();
            titleLabel.text = "Scene Explorer";
            titleLabel.fontSize = 20;
            LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
            titleLayout.minHeight = 30;
            titleLayout.flexibleHeight = 0;

            GameObject sceneDropdownObj = UIFactory.CreateDropdown(leftPane, out m_sceneDropdown);
            LayoutElement dropdownLayout = sceneDropdownObj.AddComponent<LayoutElement>();
            dropdownLayout.minHeight = 40;
            dropdownLayout.flexibleHeight = 0;
            dropdownLayout.minWidth = 320;
            dropdownLayout.flexibleWidth = 2;

#if CPP
            m_sceneDropdown.onValueChanged.AddListener(new Action<int>((int val) => { SetSceneFromDropdown(val); }));
#else
            m_sceneDropdown.onValueChanged.AddListener((int val) => { SetSceneFromDropdown(val); });
#endif
            void SetSceneFromDropdown(int val)
            {
                string scene = m_sceneDropdown.options[val].text;
                SetScene(scene);
            }

            GameObject scenePathGroupObj = UIFactory.CreateHorizontalGroup(leftPane, new Color(1, 1, 1, 0f));
            HorizontalLayoutGroup scenePathGroup = scenePathGroupObj.GetComponent<HorizontalLayoutGroup>();
            scenePathGroup.childControlHeight = true;
            scenePathGroup.childControlWidth = true;
            scenePathGroup.childForceExpandHeight = true;
            scenePathGroup.childForceExpandWidth = true;
            scenePathGroup.spacing = 5;
            LayoutElement scenePathLayout = scenePathGroupObj.AddComponent<LayoutElement>();
            scenePathLayout.minHeight = 20;
            scenePathLayout.minWidth = 335;
            scenePathLayout.flexibleWidth = 0;

            m_backButtonObj = UIFactory.CreateButton(scenePathGroupObj);
            Text backButtonText = m_backButtonObj.GetComponentInChildren<Text>();
            backButtonText.text = "<";
            LayoutElement backButtonLayout = m_backButtonObj.AddComponent<LayoutElement>();
            backButtonLayout.minWidth = 40;
            backButtonLayout.flexibleWidth = 0;
            Button backButton = m_backButtonObj.GetComponent<Button>();
#if CPP
            backButton.onClick.AddListener(new Action(() => { SetSceneObjectParent(); }));
#else
            backButton.onClick.AddListener(() => { SetSceneObjectParent(); });
#endif

            void SetSceneObjectParent()
            {
                if (!m_selectedSceneObject || !m_selectedSceneObject.transform.parent)
                {
                    m_selectedSceneObject = null;
                    SetScene(m_currentSceneHandle);
                }
                else
                {
                    SetSceneListObject(m_selectedSceneObject.transform.parent.gameObject);
                }
            }

            GameObject scenePathLabel = UIFactory.CreateHorizontalGroup(scenePathGroupObj);
            Image image = scenePathLabel.GetComponent<Image>();
            image.color = Color.white;

            LayoutElement scenePathLabelLayout = scenePathLabel.AddComponent<LayoutElement>();
            scenePathLabelLayout.minWidth = 210;
            scenePathLabelLayout.minHeight = 20;
            scenePathLabelLayout.flexibleHeight = 0;
            scenePathLabelLayout.flexibleWidth = 120;

            scenePathLabel.AddComponent<Mask>().showMaskGraphic = false;

            GameObject scenePathLabelText = UIFactory.CreateLabel(scenePathLabel, TextAnchor.MiddleLeft);
            m_scenePathText = scenePathLabelText.GetComponent<Text>();
            m_scenePathText.text = "Scene root:";
            m_scenePathText.fontSize = 15;
            m_scenePathText.horizontalOverflow = HorizontalWrapMode.Overflow;

            LayoutElement textLayout = scenePathLabelText.gameObject.AddComponent<LayoutElement>();
            textLayout.minWidth = 210;
            textLayout.flexibleWidth = 120;
            textLayout.minHeight = 20;
            textLayout.flexibleHeight = 0;

            m_mainInspectBtn = UIFactory.CreateButton(scenePathGroupObj);
            Text inspectButtonText = m_mainInspectBtn.GetComponentInChildren<Text>();
            inspectButtonText.text = "Inspect";
            LayoutElement inspectButtonLayout = m_mainInspectBtn.AddComponent<LayoutElement>();
            inspectButtonLayout.minWidth = 65;
            inspectButtonLayout.flexibleWidth = 0;
            Button inspectButton = m_mainInspectBtn.GetComponent<Button>();
#if CPP
            inspectButton.onClick.AddListener(new Action(() => { InspectorManager.Instance.Inspect(m_selectedSceneObject); }));

#else
            inspectButton.onClick.AddListener(() => { InspectorManager.Instance.Inspect(m_selectedSceneObject); });
#endif
            GameObject scrollObj = UIFactory.CreateScrollView(leftPane, out m_sceneListCanvas, new Color(0.1f, 0.1f, 0.1f));
            Scrollbar scroll = scrollObj.transform.Find("Scrollbar Vertical").GetComponent<Scrollbar>();
            ColorBlock colors = scroll.colors;
            colors.normalColor = new Color(0.6f, 0.6f, 0.6f, 1.0f);
            scroll.colors = colors;

            VerticalLayoutGroup sceneGroup = m_sceneListCanvas.GetComponent<VerticalLayoutGroup>();
            sceneGroup.childControlHeight = true;
            sceneGroup.spacing = 2;

            m_sceneListPageHandler = new PageHandler();
            m_sceneListPageHandler.ConstructUI(leftPane);
            m_sceneListPageHandler.OnPageChanged += OnSceneListPageTurn;
        }

        private void AddSceneButton()
        {
            int thisIndex = m_sceneListTexts.Count();

            GameObject btnGroupObj = UIFactory.CreateHorizontalGroup(m_sceneListCanvas, new Color(0.1f, 0.1f, 0.1f));
            HorizontalLayoutGroup btnGroup = btnGroupObj.GetComponent<HorizontalLayoutGroup>();
            btnGroup.childForceExpandWidth = true;
            btnGroup.childControlWidth = true;
            btnGroup.childForceExpandHeight = false;
            btnGroup.childControlHeight = true;
            LayoutElement btnLayout = btnGroupObj.AddComponent<LayoutElement>();
            btnLayout.flexibleWidth = 320;
            btnLayout.minHeight = 25;
            btnLayout.flexibleHeight = 0;
            btnGroupObj.AddComponent<Mask>();

            GameObject mainButtonObj = UIFactory.CreateButton(btnGroupObj);
            LayoutElement mainBtnLayout = mainButtonObj.AddComponent<LayoutElement>();
            mainBtnLayout.minHeight = 25;
            mainBtnLayout.flexibleHeight = 0;
            mainBtnLayout.minWidth = 240;
            mainBtnLayout.flexibleWidth = 0;
            Button mainBtn = mainButtonObj.GetComponent<Button>();
            ColorBlock mainColors = mainBtn.colors;
            mainColors.normalColor = new Color(0.1f, 0.1f, 0.1f);
            mainColors.highlightedColor = new Color(0.2f, 0.2f, 0.2f, 1);
            mainBtn.colors = mainColors;
#if CPP
            mainBtn.onClick.AddListener(new Action(() => { SceneListObjectClicked(thisIndex); }));
#else
            mainBtn.onClick.AddListener(() => { SceneListObjectClicked(thisIndex); });
#endif

            Text mainText = mainButtonObj.GetComponentInChildren<Text>();
            mainText.alignment = TextAnchor.MiddleLeft;
            m_sceneListTexts.Add(mainText);

            GameObject inspectBtnObj = UIFactory.CreateButton(btnGroupObj);
            LayoutElement inspectBtnLayout = inspectBtnObj.AddComponent<LayoutElement>();
            inspectBtnLayout.minWidth = 60;
            inspectBtnLayout.flexibleWidth = 0;
            inspectBtnLayout.minHeight = 25;
            inspectBtnLayout.flexibleHeight = 0;
            Text inspectText = inspectBtnObj.GetComponentInChildren<Text>();
            inspectText.text = "Inspect";
            inspectText.color = Color.white;

            Button inspectBtn = inspectBtnObj.GetComponent<Button>();
            ColorBlock inspectColors = inspectBtn.colors;
            inspectColors.normalColor = new Color(0.15f, 0.15f, 0.15f);
            mainColors.highlightedColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            inspectBtn.colors = inspectColors;
#if CPP
            inspectBtn.onClick.AddListener(new Action(() => { InspectorManager.Instance.Inspect(m_sceneShortList[thisIndex]); }));
#else
            inspectBtn.onClick.AddListener(() => { InspectorManager.Instance.Inspect(m_sceneShortList[thisIndex]); });
#endif
        }

#endregion
    }
}
