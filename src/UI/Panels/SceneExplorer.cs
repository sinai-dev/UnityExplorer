using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExplorer.Core;
using UnityExplorer.UI.Models;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Panels
{
    public class SceneExplorer : UIBehaviourModel
    {
        public override GameObject UIRoot => uiRoot;
        private GameObject uiRoot;

        /// <summary>
        /// Whether to automatically update per auto-update interval or not.
        /// </summary>
        public bool AutoUpdate = false;

        public TransformTree Tree;
        private float timeOfLastUpdate = -1f;

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
            if (AutoUpdate && Time.realtimeSinceStartup - timeOfLastUpdate >= 1f)
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

        private void SceneExplorer_OnFinishResize(RectTransform obj)
        {
            RuntimeProvider.Instance.StartCoroutine(DelayedRefresh(obj));
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

        public override void ConstructUI(GameObject parent)
        {
            var panel = UIFactory.CreatePanel("SceneExplorer", out GameObject panelContent);
            uiRoot = panel;
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector3.zero;
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.sizeDelta = new Vector2(300f, panelRect.sizeDelta.y);
            panelRect.anchoredPosition = new Vector2(160, 0);
            panelRect.offsetMin = new Vector2(panelRect.offsetMin.x, 10);  // bottom
            panelRect.offsetMax = new Vector2(panelRect.offsetMax.x, -10); // top
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(panel, true, true, true, true, 0, 0, 0, 0, 0, TextAnchor.UpperLeft);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(panelContent, true, true, true, true, 2, 2, 2, 2, 2, TextAnchor.UpperLeft);

            // Title bar

            var titleBar = UIFactory.CreateLabel(panelContent, "TitleBar", "Scene Explorer", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(titleBar.gameObject, minHeight: 25, flexibleHeight: 0);

            new PanelDragger(titleBar.GetComponent<RectTransform>(), panelRect)
                .OnFinishResize += SceneExplorer_OnFinishResize;

            // Tool bar

            var toolbar = UIFactory.CreateVerticalGroup(panelContent, "Toolbar", true, true, true, true, 2, new Vector4(2, 2, 2, 2),
               new Color(0.15f, 0.15f, 0.15f));
            //UIFactory.SetLayoutElement(toolbar, minHeight: 25, flexibleHeight: 0);

            // refresh row

            var refreshRow = UIFactory.CreateHorizontalGroup(toolbar, "RefreshGroup", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
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

            // Transform Tree

            var infiniteScroll = UIFactory.CreateInfiniteScroll(panelContent, "TransformTree", out GameObject scrollObj,
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

            // Setup references
            Tree.Scroller = infiniteScroll;

            previousRectHeight = panelRect.rect.height;
        }
    }
}
