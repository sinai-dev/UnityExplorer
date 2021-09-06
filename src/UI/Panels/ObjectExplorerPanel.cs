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
using UnityExplorer.ObjectExplorer;
using UnityExplorer.UI.Widgets;

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
        private readonly List<UIModel> tabPages = new List<UIModel>();
        private readonly List<ButtonRef> tabButtons = new List<ButtonRef>();

        public void SetTab(int tabIndex)
        {
            if (SelectedTab != -1)
                DisableTab(SelectedTab);

            var content = tabPages[tabIndex];
            content.SetActive(true);

            var button = tabButtons[tabIndex];
            RuntimeProvider.Instance.SetColorBlock(button.Component, UIManager.enabledButtonColor, UIManager.enabledButtonColor * 1.2f);

            SelectedTab = tabIndex;
            SaveToConfigManager();
        }

        private void DisableTab(int tabIndex)
        {
            tabPages[tabIndex].SetActive(false);
            RuntimeProvider.Instance.SetColorBlock(tabButtons[tabIndex].Component, UIManager.disabledButtonColor, UIManager.disabledButtonColor * 1.2f);
        }

        public override void Update()
        {
            if (SelectedTab == 0)
                SceneExplorer.Update();
            else
                ObjectSearch.Update();
        }

        public override string GetSaveDataFromConfigManager() => ConfigManager.ObjectExplorerData.Value;

        public override void DoSaveToConfigElement()
        {
            ConfigManager.ObjectExplorerData.Value = this.ToSaveData();
        }

        public override string ToSaveData()
        {
            string ret = base.ToSaveData();
            ret += "|" + SelectedTab;
            return ret;
        }

        public override void ApplySaveData(string data)
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
            var tabGroup = UIFactory.CreateHorizontalGroup(content, "TabBar", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(tabGroup, minHeight: 25, flexibleHeight: 0);

            // Scene Explorer
            SceneExplorer = new SceneExplorer(this);
            SceneExplorer.ConstructUI(content);
            tabPages.Add(SceneExplorer);

            // Object search
            ObjectSearch = new ObjectSearch(this);
            ObjectSearch.ConstructUI(content);
            tabPages.Add(ObjectSearch);

            // set up tabs
            AddTabButton(tabGroup, "Scene Explorer");
            AddTabButton(tabGroup, "Object Search");

            // default active state: Active
            UIManager.SetPanelActive(PanelType, true);
        }

        private void AddTabButton(GameObject tabGroup, string label)
        {
            var button = UIFactory.CreateButton(tabGroup, $"Button_{label}", label);

            int idx = tabButtons.Count;
            //button.onClick.AddListener(() => { SetTab(idx); });
            button.OnClick += () => { SetTab(idx); };

            tabButtons.Add(button);

            DisableTab(tabButtons.Count - 1);
        }
    }
}
