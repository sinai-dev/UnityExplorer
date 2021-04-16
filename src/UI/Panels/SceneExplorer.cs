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
            Tree.Scroller.ExternallySetting = true;
            SceneHandler.Update();
            Tree.RefreshData(true);
            // Tree.Scroller.ExternallySetting = false;
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
            Tree.RefreshData(true);
        }

        private float previousRectHeight;

        public override void OnFinishResize(RectTransform panel)
        {
            base.OnFinishResize(panel);
            RuntimeProvider.Instance.StartCoroutine(DelayedRefresh(panel));
        }

        public override void SaveToConfigManager()
        {
            ConfigManager.SceneExplorerData.Value = this.ToSaveData();
        }

        private IEnumerator DelayedRefresh(RectTransform obj)
        {
            yield return null;

            if (obj.rect.height != previousRectHeight)
            {
                // height changed, hard refresh required.
                previousRectHeight = obj.rect.height;
                Tree.Scroller.ReloadData();
            }
            Tree.Scroller.Refresh();

        }

        public override void LoadSaveData()
        {
            var data = ConfigManager.SceneExplorerData.Value;
            ApplySaveData(data);
        }

        public override void SetDefaultPosAndAnchors()
        {
            mainPanelRect.localPosition = Vector2.zero;
            mainPanelRect.anchorMin = Vector3.zero;
            mainPanelRect.anchorMax = new Vector2(0, 1);
            mainPanelRect.sizeDelta = new Vector2(300f, mainPanelRect.sizeDelta.y);
            mainPanelRect.anchoredPosition = new Vector2(160, 0);
            mainPanelRect.offsetMin = new Vector2(mainPanelRect.offsetMin.x, 10);  // bottom
            mainPanelRect.offsetMax = new Vector2(mainPanelRect.offsetMax.x, -10); // top
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

            //// filter label
            //var label = UIFactory.CreateLabel(filterRow, "FilterLabel", "Search:", TextAnchor.MiddleLeft);
            //UIFactory.SetLayoutElement(label.gameObject, minWidth: 50, flexibleWidth: 0);

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

            var infiniteScroll = UIFactory.CreateInfiniteScroll(content, "TransformTree", out GameObject scrollObj,
                out GameObject scrollContent, new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
            UIFactory.SetLayoutElement(scrollContent, flexibleHeight: 9999);

            Tree = new TransformTree(infiniteScroll)
            {
                GetRootEntriesMethod = GetRootEntries
            };
            Tree.Init();

            // Prototype tree cell
            var prototype = TransformCell.CreatePrototypeCell(scrollContent);
            infiniteScroll.PrototypeCell = prototype.GetComponent<RectTransform>();

            // some references
            Tree.Scroller = infiniteScroll;
            previousRectHeight = mainPanelRect.rect.height;

            // Scene Loader
            try
            {
                Type sceneUtil = ReflectionUtility.GetTypeByName("UnityEngine.SceneManagement.SceneUtility");
                if (sceneUtil == null)
                    throw new Exception("This version of Unity does not ship with the 'SceneUtility' class, or it was not unstripped.");
                var method = sceneUtil.GetMethod("GetScenePathByBuildIndex", ReflectionUtility.AllFlags);

                var title2 = UIFactory.CreateLabel(content, "SceneLoaderLabel", "Scene Loader", TextAnchor.MiddleLeft, Color.white, true, 14);
                UIFactory.SetLayoutElement(title2.gameObject, minHeight: 25, flexibleHeight: 0);

                var allSceneDropObj = UIFactory.CreateDropdown(content, out Dropdown allSceneDrop, "", 14, null);
                UIFactory.SetLayoutElement(allSceneDropObj, minHeight: 25, minWidth: 150, flexibleWidth: 0, flexibleHeight: 0);

                int sceneCount = SceneManager.sceneCountInBuildSettings;
                for (int i = 0; i < sceneCount; i++)
                {
                    var scenePath = (string)method.Invoke(null, new object[] { i });
                    allSceneDrop.options.Add(new Dropdown.OptionData(Path.GetFileNameWithoutExtension(scenePath)));
                }
                allSceneDrop.value = 1;
                allSceneDrop.value = 0;

                var buttonRow = UIFactory.CreateHorizontalGroup(content, "LoadButtons", true, true, true, true, 4);

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
                UIFactory.SetLayoutElement(loadButton.gameObject, minHeight: 25, minWidth: 150);

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
                UIFactory.SetLayoutElement(loadAdditiveButton.gameObject, minHeight: 25, minWidth: 150);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Could not create the Scene Loader helper! {ex.ReflectionExToString()}");
            }

        }
    }
}
