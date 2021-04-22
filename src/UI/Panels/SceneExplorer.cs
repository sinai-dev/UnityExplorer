using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExplorer.Core;
using UnityExplorer.Core.Config;
using UnityExplorer.UI.Models;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Panels
{
    public class SceneExplorer : UIPanel
    {
        public override string Name => "Scene Explorer";

        /// <summary>
        /// Whether to automatically update per auto-update interval or not.
        /// </summary>
        public bool AutoUpdate = false;

        public TransformTree Tree;
        private float timeOfLastUpdate = -1f;

        private GameObject refreshRow;
        private Dropdown sceneDropdown;
        private readonly Dictionary<int, Dropdown.OptionData> sceneToDropdownOption = new Dictionary<int, Dropdown.OptionData>();

        public SceneExplorer()
        {
            SceneHandler.OnInspectedSceneChanged += SceneHandler_OnInspectedSceneChanged;
            SceneHandler.OnLoadedScenesChanged += SceneHandler_OnLoadedScenesChanged;
        }

        private IEnumerable<GameObject> GetRootEntries() => SceneHandler.CurrentRootObjects;

        public void ForceUpdate()
        {
            ExpensiveUpdate();
        }

        public override void Update()
        {
            if ((AutoUpdate || !SceneHandler.InspectingAssetScene) && Time.realtimeSinceStartup - timeOfLastUpdate >= 1f)
            {
                timeOfLastUpdate = Time.realtimeSinceStartup;
                ExpensiveUpdate();
            }
        }

        public void ExpensiveUpdate()
        {
            //Tree.Scroller.WritingLocked = true;
            SceneHandler.Update();
            Tree.RefreshData(true);
            ////Tree.Scroller.WritingLocked = false;
        }

        private void OnDropdownChanged(int value)
        {
            if (value < 0 || SceneHandler.LoadedScenes.Count <= value)
                return;

            SceneHandler.SelectedScene = SceneHandler.LoadedScenes[value];
            SceneHandler.Update();
            Tree.RefreshData(true);
            OnSelectedSceneChanged(SceneHandler.SelectedScene.Value);
        }

        private void SceneHandler_OnInspectedSceneChanged(Scene scene)
        {
            if (!sceneToDropdownOption.ContainsKey(scene.handle))
                PopulateSceneDropdown();

            if (sceneToDropdownOption.ContainsKey(scene.handle))
            {
                var opt = sceneToDropdownOption[scene.handle];
                int idx = sceneDropdown.options.IndexOf(opt);
                if (sceneDropdown.value != idx)
                    sceneDropdown.value = idx;
                else
                    sceneDropdown.captionText.text = opt.text;
            }

            OnSelectedSceneChanged(scene);
        }

        private void OnSelectedSceneChanged(Scene scene)
        {
            if (refreshRow)
                refreshRow.SetActive(!scene.IsValid());
        }

        private void SceneHandler_OnLoadedScenesChanged(ReadOnlyCollection<Scene> loadedScenes)
        {
            PopulateSceneDropdown();
        }

        private void PopulateSceneDropdown()
        {
            sceneToDropdownOption.Clear();
            sceneDropdown.options.Clear();

            foreach (var scene in SceneHandler.LoadedScenes)
            {
                string name = scene.name?.Trim();

                if (!scene.IsValid())
                    name = "HideAndDontSave";
                else if (string.IsNullOrEmpty(name))
                    name = "<untitled>";

                var option = new Dropdown.OptionData(name);
                sceneDropdown.options.Add(option);
                sceneToDropdownOption.Add(scene.handle, option);
            }
        }

        private void OnFilterInput(string input)
        {
            Tree.CurrentFilter = input;
            Tree.RefreshData(true, true);
        }

        //private float highestRectHeight;

        //public override void OnFinishResize(RectTransform panel)
        //{
        //    base.OnFinishResize(panel);
        //    RuntimeProvider.Instance.StartCoroutine(DelayedRefresh(panel));
        //}

        //private IEnumerator DelayedRefresh(RectTransform obj)
        //{
        //    yield return null;

        //    if (obj.rect.height > highestRectHeight)
        //    {
        //        // height increased, hard refresh required.
        //        highestRectHeight = obj.rect.height;
        //        //Tree.Scroller.ReloadData();
        //    }
        //    Tree.Scroller.RefreshCells(true);
        //}

        public override void SaveToConfigManager()
        {
            ConfigManager.SceneExplorerData.Value = this.ToSaveData();
        }

        public override void LoadSaveData()
        {
            ApplySaveData(ConfigManager.SceneExplorerData.Value);
        }

        public override void SetDefaultPosAndAnchors()
        {
            mainPanelRect.localPosition = Vector2.zero;
            mainPanelRect.anchorMin = Vector3.zero;
            mainPanelRect.anchorMax = new Vector2(0, 1);
            mainPanelRect.sizeDelta = new Vector2(300f, mainPanelRect.sizeDelta.y);
            mainPanelRect.anchoredPosition = new Vector2(200, 0);
            mainPanelRect.offsetMin = new Vector2(mainPanelRect.offsetMin.x, 100);  // bottom
            mainPanelRect.offsetMax = new Vector2(mainPanelRect.offsetMax.x, -50); // top
            mainPanelRect.pivot = new Vector2(0.5f, 0.5f);
        }

        public override void ConstructPanelContent()
        {
            // Tool bar (top area)

            var toolbar = UIFactory.CreateVerticalGroup(content, "Toolbar", true, true, true, true, 2, new Vector4(2, 2, 2, 2),
               new Color(0.15f, 0.15f, 0.15f));

            // Scene selector dropdown

            var dropdownObj = UIFactory.CreateDropdown(toolbar, out sceneDropdown, "<notset>", 13, OnDropdownChanged);
            UIFactory.SetLayoutElement(dropdownObj, minHeight: 25, flexibleHeight: 0);

            SceneHandler.Update();
            PopulateSceneDropdown();
            sceneDropdown.captionText.text = sceneToDropdownOption.First().Value.text;

            // Filter row

            var filterRow = UIFactory.CreateHorizontalGroup(toolbar, "FilterGroup", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(filterRow, minHeight: 25, flexibleHeight: 0);

            //Filter input field
            var inputFieldObj = UIFactory.CreateInputField(filterRow, "FilterInput", "Search...", out InputField inputField, 13);
            inputField.targetGraphic.color = new Color(0.2f, 0.2f, 0.2f);
            RuntimeProvider.Instance.SetColorBlock(inputField, new Color(0.4f, 0.4f, 0.4f), new Color(0.2f, 0.2f, 0.2f), new Color(0.08f, 0.08f, 0.08f));
            UIFactory.SetLayoutElement(inputFieldObj, minHeight: 25);
            inputField.onValueChanged.AddListener(OnFilterInput);

            // refresh row

            refreshRow = UIFactory.CreateHorizontalGroup(toolbar, "RefreshGroup", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(refreshRow, minHeight: 30, flexibleHeight: 0);

            var refreshButton = UIFactory.CreateButton(refreshRow, "RefreshButton", "Update", ForceUpdate);
            UIFactory.SetLayoutElement(refreshButton.gameObject, minWidth: 65, flexibleWidth: 0);

            var refreshToggle = UIFactory.CreateToggle(refreshRow, "RefreshToggle", out Toggle toggle, out Text text);
            UIFactory.SetLayoutElement(refreshToggle, flexibleWidth: 9999);
            text.text = "Auto-update (1 second)";
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.fontSize = 12;
            toggle.isOn = false;
            toggle.onValueChanged.AddListener((bool val) => AutoUpdate = val);

            refreshRow.SetActive(false);

            // Transform Tree
            
            var scrollPool = UIFactory.CreateScrollPool(content, "TransformTree", out GameObject scrollObj,
                out GameObject scrollContent, new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
            UIFactory.SetLayoutElement(scrollContent, flexibleHeight: 9999);

            Tree = new TransformTree(scrollPool) { GetRootEntriesMethod = GetRootEntries };
            Tree.Init();

            //scrollPool.Viewport.GetComponent<Mask>().enabled = false;
            //UIRoot.GetComponent<Mask>().enabled = false;

            // Scene Loader

            ConstructSceneLoader();
        }

        private const string DEFAULT_LOAD_TEXT = "[Select a scene]";

        private void ConstructSceneLoader()
        {
            // Scene Loader
            try
            {
                if (SceneHandler.WasAbleToGetScenesInBuild)
                {
                    var sceneLoaderObj = UIFactory.CreateVerticalGroup(content, "SceneLoader", true, true, true, true);
                    UIFactory.SetLayoutElement(sceneLoaderObj, minHeight: 25);
                    //sceneLoaderObj.SetActive(false);

                    var loaderTitle = UIFactory.CreateLabel(sceneLoaderObj, "SceneLoaderLabel", "Scene Loader", TextAnchor.MiddleLeft, Color.white, true, 14);
                    UIFactory.SetLayoutElement(loaderTitle.gameObject, minHeight: 25, flexibleHeight: 0);

                    var allSceneDropObj = UIFactory.CreateDropdown(sceneLoaderObj, out Dropdown allSceneDrop, "", 14, null);
                    UIFactory.SetLayoutElement(allSceneDropObj, minHeight: 25, minWidth: 150, flexibleWidth: 0, flexibleHeight: 0);

                    allSceneDrop.options.Add(new Dropdown.OptionData(DEFAULT_LOAD_TEXT));

                    foreach (var scene in SceneHandler.AllSceneNames)
                        allSceneDrop.options.Add(new Dropdown.OptionData(Path.GetFileNameWithoutExtension(scene)));

                    allSceneDrop.value = 1;
                    allSceneDrop.value = 0;

                    var buttonRow = UIFactory.CreateHorizontalGroup(sceneLoaderObj, "LoadButtons", true, true, true, true, 4);

                    var loadButton = UIFactory.CreateButton(buttonRow, "LoadSceneButton", "Load (Single)", () =>
                    {
                        TryLoadScene(LoadSceneMode.Single, allSceneDrop);
                    }, new Color(0.1f, 0.3f, 0.3f));
                    UIFactory.SetLayoutElement(loadButton.gameObject, minHeight: 25, minWidth: 150);

                    var loadAdditiveButton = UIFactory.CreateButton(buttonRow, "LoadSceneButton", "Load (Additive)", () =>
                    {
                        TryLoadScene(LoadSceneMode.Additive, allSceneDrop);
                    }, new Color(0.1f, 0.3f, 0.3f));
                    UIFactory.SetLayoutElement(loadAdditiveButton.gameObject, minHeight: 25, minWidth: 150);

                    var disabledColor = new Color(0.24f, 0.24f, 0.24f);
                    RuntimeProvider.Instance.SetColorBlock(loadButton, disabled: disabledColor);
                    RuntimeProvider.Instance.SetColorBlock(loadAdditiveButton, disabled: disabledColor);

                    loadButton.interactable = false;
                    loadAdditiveButton.interactable = false;

                    allSceneDrop.onValueChanged.AddListener((int val) => 
                    {
                        var text = allSceneDrop.options[val].text;
                        if (text == DEFAULT_LOAD_TEXT)
                        {
                            loadButton.interactable = false;
                            loadAdditiveButton.interactable = false;
                        }
                        else
                        {
                            loadButton.interactable = true;
                            loadAdditiveButton.interactable = true;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Could not create the Scene Loader helper! {ex.ReflectionExToString()}");
            }
        }

        private void TryLoadScene(LoadSceneMode mode, Dropdown allSceneDrop)
        {
            var text = allSceneDrop.options[allSceneDrop.value].text;

            if (text == DEFAULT_LOAD_TEXT)
                return;

            try
            {
                SceneManager.LoadScene(text, mode);
                allSceneDrop.value = 0;
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Unable to load the Scene! {ex.ReflectionExToString()}");
            }
        }
    }
}
