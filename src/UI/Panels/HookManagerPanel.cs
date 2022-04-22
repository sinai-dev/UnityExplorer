using System;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Hooks;
using UnityExplorer.UI.Widgets;
using UnityExplorer.UI.Widgets.AutoComplete;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets;
using UniverseLib.UI.Widgets.ScrollView;

namespace UnityExplorer.UI.Panels
{
    public class HookManagerPanel : UEPanel
    {
        public static HookManagerPanel Instance { get; private set; }

        public enum Pages
        {
            ClassMethodSelector,
            HookSourceEditor,
            GenericArgsSelector,
        }

        public static HookCreator hookCreator;
        public static HookList hookList;
        public static GenericConstructorWidget genericArgsHandler;

        // Panel
        public override UIManager.Panels PanelType => UIManager.Panels.HookManager;
        public override string Name => "Hooks";
        public override bool ShowByDefault => false;
        public override int MinWidth => 400;
        public override int MinHeight => 400;
        public override Vector2 DefaultAnchorMin => new(0.5f, 0.5f);
        public override Vector2 DefaultAnchorMax => new(0.5f, 0.5f);

        public Pages CurrentPage { get; private set; } = Pages.ClassMethodSelector;

        public HookManagerPanel(UIBase owner) : base(owner)
        {
        }

        public void SetPage(Pages page)
        {
            switch (page)
            {
                case Pages.ClassMethodSelector:
                    HookCreator.AddHooksRoot.SetActive(true);
                    HookCreator.EditorRoot.SetActive(false);
                    genericArgsHandler.UIRoot.SetActive(false);
                    break;

                case Pages.HookSourceEditor:
                    HookCreator.AddHooksRoot.SetActive(false);
                    HookCreator.EditorRoot.SetActive(true);
                    genericArgsHandler.UIRoot.SetActive(false);
                    break;

                case Pages.GenericArgsSelector:
                    HookCreator.AddHooksRoot.SetActive(false);
                    HookCreator.EditorRoot.SetActive(false);
                    genericArgsHandler.UIRoot.SetActive(true);
                    break;
            }
        }

        public override void SetDefaultSizeAndPosition()
        {
            base.SetDefaultSizeAndPosition();

            this.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MinWidth);
            this.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, MinHeight);
        }

        protected override void ConstructPanelContent()
        {
            Instance = this;
            hookList = new();
            hookCreator = new();
            genericArgsHandler = new();

            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(ContentRoot, true, false);

            // GameObject baseHoriGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "HoriGroup", true, true, true, true);
            // UIFactory.SetLayoutElement(baseHoriGroup, flexibleWidth: 9999, flexibleHeight: 9999);

            // // Left Group

            //GameObject leftGroup = UIFactory.CreateVerticalGroup(ContentRoot, "LeftGroup", true, true, true, true);
            UIFactory.SetLayoutElement(ContentRoot.gameObject, minWidth: 300, flexibleWidth: 9999, flexibleHeight: 9999);

            hookList.ConstructUI(ContentRoot);

            // // Right Group

            //GameObject rightGroup = UIFactory.CreateVerticalGroup(ContentRoot, "RightGroup", true, true, true, true);
            UIFactory.SetLayoutElement(ContentRoot, minWidth: 300, flexibleWidth: 9999, flexibleHeight: 9999);

            hookCreator.ConstructAddHooksView(ContentRoot);

            hookCreator.ConstructEditor(ContentRoot);
            HookCreator.EditorRoot.SetActive(false);

            genericArgsHandler.ConstructUI(ContentRoot);
            genericArgsHandler.UIRoot.SetActive(false);
        }
    }
}
