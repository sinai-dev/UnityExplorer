using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Config;
using UnityExplorer.Inspectors;
using UniverseLib.UI;

namespace UnityExplorer.UI.Panels
{
    public class InspectorPanel : UIPanel
    {
        public static InspectorPanel Instance { get; private set; }

        public InspectorPanel() { Instance = this; }

        public override string Name => "Inspector";
        public override UIManager.Panels PanelType => UIManager.Panels.Inspector;
        public override bool ShouldSaveActiveState => false;
        public override int MinWidth => 810;
        public override int MinHeight => 350;

        public GameObject NavbarHolder;
        public Dropdown MouseInspectDropdown;
        public GameObject ContentHolder;
        public RectTransform ContentRect;

        public static float CurrentPanelWidth => Instance.Rect.rect.width;
        public static float CurrentPanelHeight => Instance.Rect.rect.height;

        public override void Update()
        {
            InspectorManager.Update();
        }

        public override void OnFinishResize(RectTransform panel)
        {
            base.OnFinishResize(panel);

            InspectorManager.PanelWidth = this.Rect.rect.width;
            InspectorManager.OnPanelResized(panel.rect.width);
        }

        protected internal override void DoSetDefaultPosAndAnchors()
        {
            Rect.localPosition = Vector2.zero;
            Rect.pivot = new Vector2(0f, 1f);
            Rect.anchorMin = new Vector2(0.35f, 0.175f);
            Rect.anchorMax = new Vector2(0.8f, 0.925f);
        }

        public override void ConstructPanelContent()
        {
            var closeHolder = this.titleBar.transform.Find("CloseHolder").gameObject;

            // Inspect under mouse dropdown on title bar

            var mouseDropdown = UIFactory.CreateDropdown(closeHolder, "MouseInspectDropdown", out MouseInspectDropdown, "Mouse Inspect", 14,
                InspectUnderMouse.OnDropdownSelect);
            UIFactory.SetLayoutElement(mouseDropdown, minHeight: 25, minWidth: 140);
            MouseInspectDropdown.options.Add(new Dropdown.OptionData("Mouse Inspect"));
            MouseInspectDropdown.options.Add(new Dropdown.OptionData("World"));
            MouseInspectDropdown.options.Add(new Dropdown.OptionData("UI"));
            mouseDropdown.transform.SetSiblingIndex(0);

            // add close all button to titlebar

            var closeAllBtn = UIFactory.CreateButton(closeHolder.gameObject, "CloseAllBtn", "Close All",
                new Color(0.3f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(closeAllBtn.Component.gameObject, minHeight: 25, minWidth: 80);
            closeAllBtn.Component.transform.SetSiblingIndex(closeAllBtn.Component.transform.GetSiblingIndex() - 1);
            closeAllBtn.OnClick += InspectorManager.CloseAllTabs;

            // this.UIRoot.GetComponent<Mask>().enabled = false;

            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(this.uiRoot, true, true, true, true, 4, padLeft: 5, padRight: 5);

            this.NavbarHolder = UIFactory.CreateGridGroup(this.uiRoot, "Navbar", new Vector2(200, 22), new Vector2(4, 4),
                new Color(0.05f, 0.05f, 0.05f));
            //UIFactory.SetLayoutElement(NavbarHolder, flexibleWidth: 9999, minHeight: 0, preferredHeight: 0, flexibleHeight: 9999);
            NavbarHolder.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            this.ContentHolder = UIFactory.CreateVerticalGroup(this.uiRoot, "ContentHolder", true, true, true, true, 0, default,
                new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(ContentHolder, flexibleHeight: 9999);
            ContentRect = ContentHolder.GetComponent<RectTransform>();

            UIManager.SetPanelActive(PanelType, false);
        }
    }
}