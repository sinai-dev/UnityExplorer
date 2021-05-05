using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.UI.Inspectors;

namespace UnityExplorer.UI.Panels
{
    public class InspectorPanel : UIPanel
    {
        public static InspectorPanel Instance { get; private set; }

        public InspectorPanel() { Instance = this; }

        public override string Name => "Inspector";
        public override UIManager.Panels PanelType => UIManager.Panels.Inspector;
        public override bool ShouldSaveActiveState => false;
        public override int MinWidth => 550;
        public override int MinHeight => 350;

        public GameObject NavbarHolder;
        public GameObject ContentHolder;
        public RectTransform ContentRect;

        public static float CurrentPanelWidth => Instance.mainPanelRect.rect.width;
        public static float CurrentPanelHeight => Instance.mainPanelRect.rect.height;

        public override void Update()
        {
            InspectorManager.Update();
        }

        public override void OnFinishResize(RectTransform panel)
        {
            base.OnFinishResize(panel);

            InspectorManager.PanelWidth = this.mainPanelRect.rect.width;
            InspectorManager.OnPanelResized(panel.rect.width);
        }

        public override string GetSaveData() => ConfigManager.InspectorData.Value;

        //public override void LoadSaveData()
        //{
        //    ApplySaveData(ConfigManager.InspectorData.Value);
        //
        //    InspectorManager.PanelWidth = this.mainPanelRect.rect.width;
        //}

        public override void DoSaveToConfigElement()
        {
            ConfigManager.InspectorData.Value = this.ToSaveData();
        }

        protected internal override void DoSetDefaultPosAndAnchors()
        {
            mainPanelRect.localPosition = Vector2.zero;
            mainPanelRect.pivot = new Vector2(0f, 1f);
            mainPanelRect.anchorMin = new Vector2(0.35f, 0.175f);
            mainPanelRect.anchorMax = new Vector2(0.8f, 0.925f);
        }

        public override void ConstructPanelContent()
        {
            // this.UIRoot.GetComponent<Mask>().enabled = false;

            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(this.content, true, true, true, true, 4, padLeft: 5, padRight: 5);

            this.NavbarHolder = UIFactory.CreateGridGroup(this.content, "Navbar", new Vector2(200, 22), new Vector2(4, 4),
                new Color(0.05f, 0.05f, 0.05f));
            //UIFactory.SetLayoutElement(NavbarHolder, flexibleWidth: 9999, minHeight: 0, preferredHeight: 0, flexibleHeight: 9999);
            NavbarHolder.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            this.ContentHolder = UIFactory.CreateVerticalGroup(this.content, "ContentHolder", true, true, true, true, 0, default,
                new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(ContentHolder, flexibleHeight: 9999);
            ContentRect = ContentHolder.GetComponent<RectTransform>();

            UIManager.SetPanelActive(PanelType, false);
        }
    }
}