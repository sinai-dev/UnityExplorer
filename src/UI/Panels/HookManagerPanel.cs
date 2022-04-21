using System;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Hooks;
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

        internal static HookCreator hookCreator;
        internal static HookList hookList;
        internal static GenericHookHandler genericArgsHandler;

        // Panel
        public override UIManager.Panels PanelType => UIManager.Panels.HookManager;
        public override string Name => "Hooks";
        public override bool ShowByDefault => false;
        public override int MinWidth => 750;
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
                    GenericHookHandler.UIRoot.SetActive(false);
                    break;

                case Pages.HookSourceEditor:
                    HookCreator.AddHooksRoot.SetActive(false);
                    HookCreator.EditorRoot.SetActive(true);
                    GenericHookHandler.UIRoot.SetActive(false);
                    break;

                case Pages.GenericArgsSelector:
                    HookCreator.AddHooksRoot.SetActive(false);
                    HookCreator.EditorRoot.SetActive(false);
                    GenericHookHandler.UIRoot.SetActive(true);
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

            GameObject baseHoriGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "HoriGroup", true, true, true, true);
            UIFactory.SetLayoutElement(baseHoriGroup, flexibleWidth: 9999, flexibleHeight: 9999);

            // Left Group

            GameObject leftGroup = UIFactory.CreateVerticalGroup(baseHoriGroup, "LeftGroup", true, true, true, true);
            UIFactory.SetLayoutElement(leftGroup.gameObject, minWidth: 300, flexibleWidth: 9999, flexibleHeight: 9999);

            hookList.ConstructUI(leftGroup);

            // Right Group

            GameObject rightGroup = UIFactory.CreateVerticalGroup(baseHoriGroup, "RightGroup", true, true, true, true);
            UIFactory.SetLayoutElement(rightGroup, minWidth: 300, flexibleWidth: 9999, flexibleHeight: 9999);

            hookCreator.ConstructAddHooksView(rightGroup);

            hookCreator.ConstructEditor(rightGroup);
            HookCreator.EditorRoot.SetActive(false);

            genericArgsHandler.ConstructUI(rightGroup);
            GenericHookHandler.UIRoot.SetActive(false);
        }
    }
}
