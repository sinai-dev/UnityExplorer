using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI.Inspectors;
using UnityExplorer.UI.Models;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Panels
{
    public class InspectorPanel : UIPanel
    {
        public static InspectorPanel Instance { get; private set; }

        public InspectorPanel() { Instance = this; }

        public override string Name => "Inspector";
        public override UIManager.Panels PanelType => UIManager.Panels.Inspector;
        public override bool ShouldSaveActiveState => false;

        public GameObject NavbarHolder;
        public GameObject ContentHolder;

        public static float CurrentPanelWidth => Instance.mainPanelRect.rect.width;

        public override void Update()
        {
            InspectorManager.Update();
        }

        public override void OnFinishResize(RectTransform panel)
        {
            base.OnFinishResize(panel);

            InspectorManager.OnPanelResized();
        }

        public override void LoadSaveData()
        {
            ApplySaveData(ConfigManager.GameObjectInspectorData.Value);
        }

        public override void SaveToConfigManager()
        {
            ConfigManager.GameObjectInspectorData.Value = this.ToSaveData();
        }

        public override void SetDefaultPosAndAnchors()
        {
            mainPanelRect.localPosition = Vector2.zero;
            mainPanelRect.pivot = new Vector2(0.5f, 0.5f);
            mainPanelRect.anchorMin = new Vector2(0.5f, 0);
            mainPanelRect.anchorMax = new Vector2(0.5f, 1);
            mainPanelRect.offsetMin = new Vector2(mainPanelRect.offsetMin.x, 100);  // bottom
            mainPanelRect.offsetMax = new Vector2(mainPanelRect.offsetMax.x, -50); // top
            mainPanelRect.sizeDelta = new Vector2(700f, mainPanelRect.sizeDelta.y);
            mainPanelRect.anchoredPosition = new Vector2(-150, 0);
        }

        public override void ConstructPanelContent()
        {
            // this.UIRoot.GetComponent<Mask>().enabled = false;

            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(this.content, forceHeight: true, spacing: 10, padLeft: 5, padRight: 5);

            this.NavbarHolder = UIFactory.CreateGridGroup(this.content, "Navbar", new Vector2(200, 22), new Vector2(4, 2),
                new Color(0.12f, 0.12f, 0.12f));
            //UIFactory.SetLayoutElement(NavbarHolder, flexibleWidth: 9999, minHeight: 0, preferredHeight: 0, flexibleHeight: 9999);
            NavbarHolder.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            this.ContentHolder = UIFactory.CreateVerticalGroup(this.content, "ContentHolder", true, true, true, true, 0, default,
                new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(ContentHolder, flexibleHeight: 9999);

            UIManager.SetPanelActive(PanelType, false);
        }
    }
}