using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityExplorer.ObjectExplorer;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;

namespace UnityExplorer.UI.Panels
{
    public class ObjectExplorerPanel : UIPanel
    {
        public override string Name => "Object Explorer";
        public override UIManager.Panels PanelType => UIManager.Panels.ObjectExplorer;
        public override int MinWidth => 350;
        public override int MinHeight => 200;

        public SceneExplorer SceneExplorer;
        public ObjectSearch ObjectSearch;

        public override bool ShowByDefault => true;
        public override bool ShouldSaveActiveState => true;

        public int SelectedTab = 0;
        private readonly List<UIModel> tabPages = new();
        private readonly List<ButtonRef> tabButtons = new();

        public void SetTab(int tabIndex)
        {
            if (SelectedTab != -1)
                DisableTab(SelectedTab);

            UIModel content = tabPages[tabIndex];
            content.SetActive(true);

            ButtonRef button = tabButtons[tabIndex];
            RuntimeHelper.SetColorBlock(button.Component, UniversalUI.EnabledButtonColor, UniversalUI.EnabledButtonColor * 1.2f);

            SelectedTab = tabIndex;
            SaveInternalData();
        }

        private void DisableTab(int tabIndex)
        {
            tabPages[tabIndex].SetActive(false);
            RuntimeHelper.SetColorBlock(tabButtons[tabIndex].Component, UniversalUI.DisabledButtonColor, UniversalUI.DisabledButtonColor * 1.2f);
        }

        public override void Update()
        {
            if (SelectedTab == 0)
                SceneExplorer.Update();
            else
                ObjectSearch.Update();
        }

        public override string ToSaveData()
        {
            return string.Join("|", new string[] { base.ToSaveData(), SelectedTab.ToString() });
        }

        protected override void ApplySaveData(string data)
        {
            base.ApplySaveData(data);

            try
            {
                int tab = int.Parse(data.Split('|').Last());
                SelectedTab = tab;
            }
            catch
            {
                SelectedTab = 0;
            }

            SelectedTab = Math.Max(0, SelectedTab);
            SelectedTab = Math.Min(1, SelectedTab);

            SetTab(SelectedTab);
        }

        protected internal override void DoSetDefaultPosAndAnchors()
        {
            Rect.localPosition = Vector2.zero;
            Rect.pivot = new Vector2(0f, 1f);
            Rect.anchorMin = new Vector2(0.125f, 0.175f);
            Rect.anchorMax = new Vector2(0.325f, 0.925f);
        }

        public override void ConstructPanelContent()
        {
            // Tab bar
            GameObject tabGroup = UIFactory.CreateHorizontalGroup(uiContent, "TabBar", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(tabGroup, minHeight: 25, flexibleHeight: 0);

            // Scene Explorer
            SceneExplorer = new SceneExplorer(this);
            SceneExplorer.ConstructUI(uiContent);
            tabPages.Add(SceneExplorer);

            // Object search
            ObjectSearch = new ObjectSearch(this);
            ObjectSearch.ConstructUI(uiContent);
            tabPages.Add(ObjectSearch);

            // set up tabs
            AddTabButton(tabGroup, "Scene Explorer");
            AddTabButton(tabGroup, "Object Search");

            // default active state: Active
            UIManager.SetPanelActive(PanelType, true);
        }

        private void AddTabButton(GameObject tabGroup, string label)
        {
            ButtonRef button = UIFactory.CreateButton(tabGroup, $"Button_{label}", label);

            int idx = tabButtons.Count;
            //button.onClick.AddListener(() => { SetTab(idx); });
            button.OnClick += () => { SetTab(idx); };

            tabButtons.Add(button);

            DisableTab(tabButtons.Count - 1);
        }
    }
}
