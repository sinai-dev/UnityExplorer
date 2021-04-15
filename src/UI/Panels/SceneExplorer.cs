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

        public override bool NeedsUpdate => true;

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

            Tree.infiniteScroll.ExternallySetting = true;
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
                int idx = sceneDropdown.value = sceneDropdown.options.IndexOf(sceneToDropdownOption[scene.handle]);
                if (sceneDropdown.value != idx)
                    sceneDropdown.value = idx;
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

                sceneDropdown.options.Add(new Dropdown.OptionData(name));
                sceneToDropdownOption.Add(scene.handle, sceneDropdown.options.Last());
            }
        }

        private void OnFilterInput(string input)
        {
            Tree.CurrentFilter = input;
            Tree.RefreshData(true);
        }

        private void SceneExplorer_OnFinishResize(RectTransform obj)
        {
            int curIdx = Tree.infiniteScroll.currentItemCount;
            // Set it to 0 (its going to jump to top anyway)
            Tree.infiniteScroll.currentItemCount = 0;
            // Need to do complete rebuild so that anchors and offsets can recalculated.
            Tree.infiniteScroll.ReloadData();
            // Try jump back to previous idx
            RuntimeProvider.Instance.StartCoroutine(DelayedJump(curIdx));
        }

        private IEnumerator DelayedJump(int idx)
        {
            yield return null;
            Tree.infiniteScroll.JumpToIndex(0);
            yield return null;
            Tree.infiniteScroll.JumpToIndex(idx);
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

            Tree = panel.AddComponent<TransformTree>();
            Tree.GetRootEntriesMethod = GetRootEntries;

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
            sceneDropdown.itemText.text = sceneToDropdownOption.First().Value.text;

            //Filter input field
            var inputFieldObj = UIFactory.CreateInputField(toolbar, "FilterInput", "Search...", out InputField inputField, 13);
            UIFactory.SetLayoutElement(inputFieldObj, minHeight: 25);
            inputField.onValueChanged.AddListener(OnFilterInput);

            // Transform Tree

            var infiniteScroll = UIFactory.CreateInfiniteScroll(panelContent, "TransformTree", out GameObject scrollContent,
               new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(infiniteScroll.gameObject, flexibleHeight: 9999);
            UIFactory.SetLayoutElement(scrollContent, flexibleHeight: 9999);

            // Prototype tree cell
            var prototype = Tree.CreatePrototypeCell(scrollContent, Tree);
            infiniteScroll.PrototypeCell = prototype.GetComponent<RectTransform>();

            // Setup references
            Tree.infiniteScroll = infiniteScroll;
        }
    }
}
