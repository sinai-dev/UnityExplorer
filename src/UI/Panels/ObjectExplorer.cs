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
    public class ObjectExplorer : UIPanel
    {
        public override string Name => "Object Explorer";
        public override UIManager.Panels PanelType => UIManager.Panels.ObjectExplorer;

        public SceneExplorer SceneExplorer;
        public ObjectSearch ObjectSearch;

        public int SelectedTab = -1;
        private readonly List<UIModel> tabPages = new List<UIModel>();
        private readonly List<Button> tabButtons = new List<Button>();

        public void SetTab(int tabIndex)
        {
            if (SelectedTab != -1)
                DisableTab(SelectedTab);

            var content = tabPages[tabIndex];
            content.SetActive(true);

            var button = tabButtons[tabIndex];
            RuntimeProvider.Instance.SetColorBlock(button, UIManager.navButtonEnabledColor, UIManager.navButtonEnabledColor * 1.2f);

            SelectedTab = tabIndex;
            SaveToConfigManager();
        }

        private void DisableTab(int tabIndex)
        {
            tabPages[tabIndex].SetActive(false);
            RuntimeProvider.Instance.SetColorBlock(tabButtons[tabIndex], UIManager.navButtonDisabledColor, UIManager.navButtonDisabledColor * 1.2f);
        }

        public override void Update()
        {
            if (SelectedTab == 0)
                SceneExplorer.Update();
        }

        public override void SaveToConfigManager()
        {
            ConfigManager.SceneExplorerData.Value = this.ToSaveData();
        }

        public override void LoadSaveData()
        {
            ApplySaveData(ConfigManager.SceneExplorerData.Value);
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

            SetTab(SelectedTab);
        }

        public override void SetDefaultPosAndAnchors()
        {
            mainPanelRect.localPosition = Vector2.zero;
            mainPanelRect.anchorMin = Vector3.zero;
            mainPanelRect.anchorMax = new Vector2(0, 1);
            mainPanelRect.sizeDelta = new Vector2(320f, mainPanelRect.sizeDelta.y);
            mainPanelRect.anchoredPosition = new Vector2(200, 0);
            mainPanelRect.offsetMin = new Vector2(mainPanelRect.offsetMin.x, 100);  // bottom
            mainPanelRect.offsetMax = new Vector2(mainPanelRect.offsetMax.x, -50); // top
            mainPanelRect.pivot = new Vector2(0.5f, 0.5f);
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
            button.onClick.AddListener(() => { SetTab(idx); });

            tabButtons.Add(button);

            DisableTab(tabButtons.Count - 1);
        }
    }
}
