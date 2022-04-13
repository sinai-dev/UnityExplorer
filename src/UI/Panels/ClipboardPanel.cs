using System;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.Utility;

namespace UnityExplorer.UI.Panels
{
    public class ClipboardPanel : UEPanel
    {
        public static object Current { get; private set; }

        public override string Name => "Clipboard";
        public override UIManager.Panels PanelType => UIManager.Panels.Clipboard;

        public override int MinWidth => 500;
        public override int MinHeight => 95;
        public override Vector2 DefaultAnchorMin => new(0.1f, 0.05f);
        public override Vector2 DefaultAnchorMax => new(0.4f, 0.15f);

        public override bool CanDragAndResize => true;
        public override bool NavButtonWanted => true;
        public override bool ShouldSaveActiveState => true;
        public override bool ShowByDefault => true;

        private static Text CurrentPasteLabel;

        public ClipboardPanel(UIBase owner) : base(owner)
        {
        }

        public static void Copy(object obj)
        {
            Current = obj;
            Notification.ShowMessage("Copied!");
            UpdateCurrentPasteInfo();
        }

        public static bool TryPaste(Type targetType, out object paste)
        {
            paste = Current;
            Type pasteType = Current?.GetActualType();

            if (Current != null && !targetType.IsAssignableFrom(pasteType))
            {
                Notification.ShowMessage($"Cannot assign '{pasteType.Name}' to '{targetType.Name}'!");
                return false;
            }

            Notification.ShowMessage("Pasted!");
            return true;
        }

        public static void ClearClipboard()
        {
            Current = null;
            UpdateCurrentPasteInfo();
        }

        private static void UpdateCurrentPasteInfo()
        {
            CurrentPasteLabel.text = ToStringUtility.ToStringWithType(Current, typeof(object), false);
        }

        private static void InspectClipboard()
        {
            if (Current.IsNullOrDestroyed())
            {
                Notification.ShowMessage("Cannot inspect a null or destroyed object!");
                return;
            }

            InspectorManager.Inspect(Current);
        }

        public override void SetDefaultSizeAndPosition()
        {
            base.SetDefaultSizeAndPosition();

            this.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MinWidth);
            this.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, MinHeight);
        }

        protected override void ConstructPanelContent()
        {
            this.UIRoot.GetComponent<Image>().color = new(0.1f, 0.1f, 0.1f);

            // Actual panel content

            GameObject firstRow = UIFactory.CreateHorizontalGroup(ContentRoot, "FirstRow", false, false, true, true, 5, new(2, 2, 2, 2), new(1, 1, 1, 0));
            UIFactory.SetLayoutElement(firstRow, minHeight: 25, flexibleWidth: 999);

            // Title for "Current Paste:"
            Text currentPasteTitle = UIFactory.CreateLabel(firstRow, "CurrentPasteTitle", "Current paste:", TextAnchor.MiddleLeft, color: Color.grey);
            UIFactory.SetLayoutElement(currentPasteTitle.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 999);

            // Clear clipboard button
            UniverseLib.UI.Models.ButtonRef clearButton = UIFactory.CreateButton(firstRow, "ClearPasteButton", "Clear Clipboard");
            UIFactory.SetLayoutElement(clearButton.Component.gameObject, minWidth: 120, minHeight: 25, flexibleWidth: 0);
            clearButton.OnClick += () => Copy(null);

            // Current Paste info row
            GameObject currentPasteHolder = UIFactory.CreateHorizontalGroup(ContentRoot, "SecondRow", false, false, true, true, 0,
                new(2, 2, 2, 2), childAlignment: TextAnchor.UpperCenter);

            // Actual current paste info label
            CurrentPasteLabel = UIFactory.CreateLabel(currentPasteHolder, "CurrentPasteInfo", "not set", TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(CurrentPasteLabel.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 999, flexibleHeight: 999);
            UpdateCurrentPasteInfo();

            // Inspect button
            UniverseLib.UI.Models.ButtonRef inspectButton = UIFactory.CreateButton(currentPasteHolder, "InspectButton", "Inspect");
            UIFactory.SetLayoutElement(inspectButton.Component.gameObject, minHeight: 25, flexibleHeight: 0, minWidth: 80, flexibleWidth: 0);
            inspectButton.OnClick += InspectClipboard;
        }
    }
}
