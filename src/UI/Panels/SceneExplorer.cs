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

        public override void Update()
        {
            if (Time.realtimeSinceStartup - timeOfLastUpdate < 1f)
                return;
            timeOfLastUpdate = Time.realtimeSinceStartup;

            Tree.Scroller.ExternallySetting = true;
            SceneHandler.Update();
            Tree.RefreshData(true);
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
            if (obj.rect.height == previousRectHeight)
            {
                // horizontal resize, soft refresh.
                Tree.Scroller.Refresh();
                return;
            }

            // height changed, hard refresh required.
            previousRectHeight = obj.rect.height;
            Tree.Scroller.ReloadData();
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
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(panel, true, true, true, true, 0,0,0,0,0, TextAnchor.UpperLeft);
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

            //Scene selector dropdown
            var dropdownObj = UIFactory.CreateDropdown(toolbar, out sceneDropdown, "<notset>", 13, OnDropdownChanged);
            UIFactory.SetLayoutElement(dropdownObj, minHeight: 25, flexibleHeight: 0);

            SceneHandler.Update();
            PopulateSceneDropdown();
            sceneDropdown.captionText.text = sceneToDropdownOption.First().Value.text;

            //Filter input field
            var inputFieldObj = UIFactory.CreateInputField(toolbar, "FilterInput", "Search...", out InputField inputField, 13);
            UIFactory.SetLayoutElement(inputFieldObj, minHeight: 25);
            inputField.onValueChanged.AddListener(OnFilterInput);

            // Transform Tree

            var infiniteScroll = UIFactory.CreateInfiniteScroll(panelContent, "TransformTree", out GameObject scrollObj,
                out GameObject scrollContent, new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
            UIFactory.SetLayoutElement(scrollContent, flexibleHeight: 9999);

            Tree = new TransformTree(infiniteScroll);
            Tree.GetRootEntriesMethod = GetRootEntries;
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
