using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.CacheObject;
using UnityExplorer.CacheObject.Views;
using UnityExplorer.Config;
using UniverseLib;
using UniverseLib.Input;
using UniverseLib.UI;
using UniverseLib.UI.Widgets;
using UniverseLib.Utility;

namespace UnityExplorer.UI.Panels
{
    public class ClipboardPanel : UIPanel
    {
        public static object Current { get; private set; }

        public override UIManager.Panels PanelType => UIManager.Panels.Clipboard;
        public override string Name => "Clipboard";
        public override int MinWidth => 500;
        public override int MinHeight => 95;
        public override bool CanDragAndResize => true;
        public override bool NavButtonWanted => true;
        public override bool ShouldSaveActiveState => true;
        public override bool ShowByDefault => true;

        private static Text CurrentPasteLabel;

        public static void Copy(object obj)
        {
            Current = obj;
            Notification.ShowMessage("Copied!");
            UpdateCurrentPasteInfo();
        }

        public static bool TryPaste(Type targetType, out object paste)
        {
            paste = Current;
            var pasteType = Current?.GetActualType();

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
        
        protected internal override void DoSetDefaultPosAndAnchors()
        {
            this.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MinWidth);
            this.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, MinHeight);
            this.Rect.anchorMin = new Vector2(0.1f, 0.05f);
            this.Rect.anchorMax = new Vector2(0.4f, 0.15f);
        }

        public override void ConstructPanelContent()
        {
            this.UIRoot.GetComponent<Image>().color = new(0.1f, 0.1f, 0.1f);

            // Actual panel content

            var firstRow = UIFactory.CreateHorizontalGroup(uiContent, "FirstRow", false, false, true, true, 5, new(2,2,2,2), new(1,1,1,0));
            UIFactory.SetLayoutElement(firstRow, minHeight: 25, flexibleWidth: 999);

            // Title for "Current Paste:"
            var currentPasteTitle = UIFactory.CreateLabel(firstRow, "CurrentPasteTitle", "Current paste:", TextAnchor.MiddleLeft, color: Color.grey);
            UIFactory.SetLayoutElement(currentPasteTitle.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 999);

            // Clear clipboard button
            var clearButton = UIFactory.CreateButton(firstRow, "ClearPasteButton", "Clear Clipboard");
            UIFactory.SetLayoutElement(clearButton.Component.gameObject, minWidth: 120, minHeight: 25, flexibleWidth: 0);
            clearButton.OnClick += () => Copy(null);

            // Current Paste info row
            var currentPasteHolder = UIFactory.CreateHorizontalGroup(uiContent, "SecondRow", false, false, true, true, 0, 
                new(2, 2, 2, 2), childAlignment: TextAnchor.UpperCenter);

            // Actual current paste info label
            CurrentPasteLabel = UIFactory.CreateLabel(currentPasteHolder, "CurrentPasteInfo", "not set", TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(CurrentPasteLabel.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 999, flexibleHeight: 999);
            UpdateCurrentPasteInfo();

            // Inspect button
            var inspectButton = UIFactory.CreateButton(currentPasteHolder, "InspectButton", "Inspect");
            UIFactory.SetLayoutElement(inspectButton.Component.gameObject, minHeight: 25, flexibleHeight: 0, minWidth: 80, flexibleWidth: 0);
            inspectButton.OnClick += InspectClipboard;
        }
    }
}
