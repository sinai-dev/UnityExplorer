using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExplorerBeta;
using ExplorerBeta.UI;
using ExplorerBeta.UI.Main;
using ExplorerBeta.UI.Shared;
using ExplorerBeta.Unstrip.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Explorer.UI.Main.Pages
{
    public class HomePage : BaseMenuPage
    {
        public override string Name => "Home";

        // private PageHandler m_sceneListPages;
        private Dropdown m_sceneDropdown;

        private Dictionary<string, int> m_sceneHandles = new Dictionary<string, int>();

        // is tihs needed?
        private int m_currentSceneHandle;

        private List<GameObject> m_currentObjectList = new List<GameObject>();
        private GameObject m_sceneListContent;
        private readonly List<Text> m_sceneListTexts = new List<Text>();
        // todo
        private GameObject m_currentSceneObject;

        private int m_lastMaxIndex;

        public override void Init()
        {
            ConstructMenu();

            // get DontDestroyScene handle
            var test = new GameObject();
            GameObject.DontDestroyOnLoad(test);
            GetSceneHandle(test.scene);
            GameObject.Destroy(test);

            RefreshActiveScenes();
        }

        public override void Update()
        {
            // refresh active scenes? maybe on a timer to limit frequency

            // refresh scene objects on limited frequency
        }

        private int GetSceneHandle(Scene scene)
        {
            if (!m_sceneHandles.ContainsKey(scene.name))
            {
                m_sceneHandles.Add(scene.name, scene.handle);
            }
            return scene.handle;
        }

        private int GetSceneHandle(string sceneName)
        {
            if (!m_sceneHandles.ContainsKey(sceneName))
                return -1;
            else
                return m_sceneHandles[sceneName];
        }

        public void SetScene(string name)
        {
            var handle = GetSceneHandle(name);

            if (handle == -1)
            {
                ExplorerCore.LogWarning($"Error: Could not get handle for scene '{name}'");
                return;
            }

            m_currentSceneHandle = handle;

            var rootObjs = SceneUnstrip.GetRootGameObjects(handle);

            SetSceneObjectList(rootObjs);
        }

        private void RefreshActiveScenes(bool firstTime = false)
        {
            var activeScene = SceneManager.GetActiveScene().name;
            var otherScenes = new List<string>();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                GetSceneHandle(scene);

                if (!firstTime || scene.name != activeScene)
                    otherScenes.Add(scene.name);
            }
            otherScenes.Add("DontDestroyOnLoad");

            m_sceneDropdown.options.Clear();

            if (firstTime)
            {
                m_sceneDropdown.options.Add(new Dropdown.OptionData
                {
                    text = activeScene
                });
            }

            foreach (var scene in otherScenes)
            {
                m_sceneDropdown.options.Add(new Dropdown.OptionData
                {
                    text = scene
                });
            }

            SetScene(activeScene);
        }

        private void SceneButtonClicked(int index)
        {
            var obj = m_currentObjectList[index];

            ExplorerCore.Log("Clicked " + obj.name);
        }

        private void SetSceneObjectList(IEnumerable<GameObject> objects)
        {
            m_currentObjectList.Clear();

            int index = 0;
            foreach (var obj in objects)
            {
                m_currentObjectList.Add(obj);

                if (index >= m_sceneListTexts.Count)
                {
                    AddSceneButton();
                }

                m_sceneListTexts[index].text = obj.name;

                var parent = m_sceneListTexts[index].transform.parent.gameObject;
                if (!parent.activeSelf)
                    parent.SetActive(true);

                index++;
            }

            var origIndex = index;
            while (index < m_sceneListTexts.Count && index < m_lastMaxIndex)
            {
                var obj = m_sceneListTexts[index].transform.parent.gameObject;

                if (obj.activeSelf)
                    obj.SetActive(false);

                index++;
            }
            m_lastMaxIndex = origIndex;
        }

        #region UI Construction

        private void ConstructMenu()
        {
            var parent = MainMenu.Instance.PageViewport;

            Content = UIFactory.CreateHorizontalGroup(parent);
            var mainGroup = Content.GetComponent<HorizontalLayoutGroup>();
            mainGroup.padding.left = 3;
            mainGroup.padding.right = 3;
            mainGroup.padding.top = 3;
            mainGroup.padding.bottom = 3;
            mainGroup.spacing = 5;
            mainGroup.childForceExpandHeight = true;
            mainGroup.childForceExpandWidth = true;
            mainGroup.childControlHeight = true;
            mainGroup.childControlWidth = true;

            var leftPaneObj = UIFactory.CreateVerticalGroup(Content, new Color(72f / 255f, 72f / 255f, 72f / 255f));
            var leftLayout = leftPaneObj.AddComponent<LayoutElement>();
            leftLayout.minWidth = 350;
            leftLayout.flexibleWidth = 0;

            var leftGroup = leftPaneObj.GetComponent<VerticalLayoutGroup>();
            leftGroup.padding.left = 8;
            leftGroup.padding.right = 8;
            leftGroup.padding.top = 8;
            leftGroup.padding.bottom = 8;
            leftGroup.spacing = 5;
            leftGroup.childControlWidth = true;
            leftGroup.childControlHeight = true;
            leftGroup.childForceExpandWidth = false;
            leftGroup.childForceExpandHeight = true;

            var rightPaneObj = UIFactory.CreateVerticalGroup(Content, new Color(72f / 255f, 72f / 255f, 72f / 255f));
            var rightLayout = rightPaneObj.AddComponent<LayoutElement>();
            rightLayout.flexibleWidth = 999999;

            var rightGroup = rightPaneObj.GetComponent<VerticalLayoutGroup>();
            rightGroup.childForceExpandHeight = true;
            rightGroup.childForceExpandWidth = true;
            rightGroup.childControlHeight = true;
            rightGroup.childControlWidth = true;

            ConstructScenePane(leftPaneObj);
        }

        private void ConstructScenePane(GameObject leftPane)
        {
            var activeSceneLabelObj = UIFactory.CreateLabel(leftPane, TextAnchor.UpperLeft);
            var activeSceneLabel = activeSceneLabelObj.GetComponent<Text>();
            activeSceneLabel.text = "Scene Explorer";
            activeSceneLabel.fontSize = 25;
            var activeSceneLayout = activeSceneLabelObj.AddComponent<LayoutElement>();
            activeSceneLayout.minHeight = 30;
            activeSceneLayout.flexibleHeight = 0;

            var sceneDropdownObj = UIFactory.CreateDropdown(leftPane, out m_sceneDropdown);
            var dropdownLayout = sceneDropdownObj.AddComponent<LayoutElement>();
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
                var scene = m_sceneDropdown.options[val].text;
                SetScene(scene);
            }

            // Scene list(TODO)

            //m_sceneListPages = new PageHandler(100);
            //m_sceneListPages.ConstructUI(leftPane);
            //m_sceneListPages.OnPageChanged += RefreshSceneObjectList;

            var scrollTest = UIFactory.CreateScrollView(leftPane, out m_sceneListContent, new Color(0.15f, 0.15f, 0.15f, 1));
            for (int i = 0; i < 50; i++)
            {
                AddSceneButton();
            }
            m_lastMaxIndex = 51;
        }

        private void AddSceneButton()
        {
            var obj = UIFactory.CreateButton(m_sceneListContent);

            var btn = obj.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.15f, 0.15f, 0.15f, 1);
            btn.colors = colors;

            int thisIndex = m_sceneListTexts.Count();

#if CPP
            btn.onClick.AddListener(new Action(() => { SceneButtonClicked(thisIndex); }));
#else
            btn.onClick.AddListener(() => { SceneButtonClicked(thisIndex); });
#endif

            var text = obj.GetComponentInChildren<Text>();
            text.text = "button " + thisIndex;
            text.alignment = TextAnchor.MiddleLeft;

            m_sceneListTexts.Add(text);
        }

#endregion
    }
}
