using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI.Models;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Panels
{
    public class GameObjectInspector : UIPanel
    {
        public override string Name => "GameObject Inspector";

        public SimpleListSource<Component> ComponentList;

        public override void Update()
        {

        }

        public List<Component> GetEntries()
        {
            var comp = Camera.main;
            return new List<Component>
            {
                comp, comp, comp, comp, comp
            };
        }

        public SimpleCell<Component> CreateCell(RectTransform rect)
        {
            var button = rect.GetComponentInChildren<Button>();
            var text = button.GetComponentInChildren<Text>();
            var cell = new SimpleCell<Component>(ComponentList, rect.gameObject, button, text);
            cell.OnClick += OnCellClicked;
            return cell;
        }

        public void OnCellClicked(SimpleCell<Component> cell)
        {
            ExplorerCore.Log("Cell clicked!");
        }

        public void SetCell(SimpleCell<Component> cell, int index)
        {
            var comp = ComponentList.currentEntries[index];
            if (!comp)
                cell.buttonText.text = "<color=red>[Destroyed]</color>";
            else
                cell.buttonText.text = ToStringUtility.GetDefaultLabel(comp, ReflectionProvider.Instance.GetActualType(comp), true, false);
        }

        public bool ShouldFilter(Component comp, string filter)
        {
            return comp.name.ToLower().Contains(filter);
        }

        public override void LoadSaveData()
        {
            ApplySaveData(ConfigManager.GameObjectInspectorData.Value);
        }

        public override void SaveToConfigManager()
        {
            ConfigManager.GameObjectInspectorData.Value = this.ToSaveData();
        }

        public override void OnFinishResize(RectTransform panel)
        {
            base.OnFinishResize(panel);
            RuntimeProvider.Instance.StartCoroutine(DelayedRefresh(panel));
        }

        private float previousRectHeight;

        private IEnumerator DelayedRefresh(RectTransform obj)
        {
            yield return null;

            if (obj.rect.height != previousRectHeight)
            {
                // height changed, hard refresh required.
                previousRectHeight = obj.rect.height;
                ComponentList.Scroller.ReloadData();
            }

            ComponentList.Scroller.Refresh();
        }

        public override void SetDefaultPosAndAnchors()
        {
            mainPanelRect.localPosition = Vector2.zero;
            mainPanelRect.anchorMin = new Vector2(0.6f, 0.3f);
            mainPanelRect.anchorMax = new Vector2(0.95f, 0.9f);
            mainPanelRect.sizeDelta = new Vector2(-300f, mainPanelRect.sizeDelta.y);
            mainPanelRect.anchoredPosition = new Vector2(-160, 0);
            mainPanelRect.offsetMin = new Vector2(mainPanelRect.offsetMin.x, 10);  // bottom
            mainPanelRect.offsetMax = new Vector2(mainPanelRect.offsetMax.x, -10); // top
            mainPanelRect.pivot = new Vector2(0.5f, 0.5f);
        }

        public override void ConstructPanelContent()
        {
            // Transform Tree

            var infiniteScroll = UIFactory.CreateInfiniteScroll(content, "ComponentList", out GameObject scrollObj,
                out GameObject scrollContent, new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
            UIFactory.SetLayoutElement(scrollContent, flexibleHeight: 9999);

            ComponentList = new SimpleListSource<Component>(infiniteScroll, GetEntries, CreateCell, SetCell, ShouldFilter);
            ComponentList.Init();

            // Prototype tree cell
            var prototype = SimpleCell<Component>.CreatePrototypeCell(scrollContent);
            infiniteScroll.PrototypeCell = prototype.GetComponent<RectTransform>();

            // some references
            previousRectHeight = mainPanelRect.rect.height;
        }
    }
}
