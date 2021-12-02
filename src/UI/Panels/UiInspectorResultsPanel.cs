using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExplorer.Inspectors.MouseInspectors;
using UnityExplorer.UI.Widgets;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Widgets;

namespace UnityExplorer.UI.Panels
{
    public class UiInspectorResultsPanel : UIPanel
    {
        public override UIManager.Panels PanelType => UIManager.Panels.UIInspectorResults;

        public override string Name => "UI Inspector Results";

        public override int MinWidth => 500;
        public override int MinHeight => 500;
        public override bool CanDragAndResize => true;
        public override bool NavButtonWanted => false;
        public override bool ShouldSaveActiveState => false;
        public override bool ShowByDefault => false;

        private ButtonListHandler<GameObject, ButtonCell> dataHandler;
        private ScrollPool<ButtonCell> buttonScrollPool;

        public override void ConstructPanelContent()
        {
            dataHandler = new ButtonListHandler<GameObject, ButtonCell>(buttonScrollPool, GetEntries, SetCell, ShouldDisplayCell, OnCellClicked);

            buttonScrollPool = UIFactory.CreateScrollPool<ButtonCell>(this.content, "ResultsList", out GameObject scrollObj,
                out GameObject scrollContent);

            buttonScrollPool.Initialize(dataHandler);
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
        }

        public void ShowResults()
        {
            dataHandler.RefreshData();
            buttonScrollPool.Refresh(true, true);
        }

        private List<GameObject> GetEntries() => UiInspector.LastHitObjects;

        private bool ShouldDisplayCell(object cell, string filter) => true;

        private void OnCellClicked(int index)
        {
            if (index >= UiInspector.LastHitObjects.Count)
                return;

            InspectorManager.Inspect(UiInspector.LastHitObjects[index]);
        }

        private void SetCell(ButtonCell cell, int index)
        {
            if (index >= UiInspector.LastHitObjects.Count)
                return;

            var obj = UiInspector.LastHitObjects[index];
            cell.Button.ButtonText.text = $"<color=cyan>{obj.name}</color> ({obj.transform.GetTransformPath(true)})";
        }

        protected internal override void DoSetDefaultPosAndAnchors()
        {
            this.Rect.anchorMin = new Vector2(0.5f, 0.5f);
            this.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            this.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 500f);
            this.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 500f);
        }

        public override void DoSaveToConfigElement() { }
        public override string GetSaveDataFromConfigManager() => null;
    }
}
