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
    public class InspectorTest : UIPanel
    {
        public override string Name => "Inspector";
        public override UIManager.Panels PanelType => UIManager.Panels.Inspector;
        public override bool ShouldSaveActiveState => false;

        //public SimpleListSource<Component> ComponentList;

        public override void Update()
        {

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

        internal static DynamicListTest listInstance;

        private ScrollPool scrollPool;

        public override void ConstructPanelContent()
        {
            // temp test
            scrollPool = UIFactory.CreateScrollPool(content, "Test", out GameObject scrollObj,
                out GameObject scrollContent, new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
            UIFactory.SetLayoutElement(scrollContent, flexibleHeight: 9999);

            //// disable masks for debug
            //UIRoot.GetComponent<Mask>().enabled = false;
            //scrollPool.Viewport.GetComponent<Mask>().enabled = false;
            //scrollPool.Content.gameObject.AddComponent<Image>().color = new Color(1f, 0f, 1f, 0.3f);

            listInstance = new DynamicListTest(scrollPool, this);
            listInstance.Init();

            //var prototype = DynamicCell.CreatePrototypeCell(scrollContent);
            //scrollPool.PrototypeCell = prototype.GetComponent<RectTransform>();

            contentHolder = new GameObject("DummyHolder");
            contentHolder.SetActive(false);
            contentHolder.transform.SetParent(this.content.transform, false);

            ExplorerCore.Log("Creating dummy objects");
            for (int i = 0; i < 150; i++)
            {
                dummyContents.Add(CreateDummyContent());
            }
            ExplorerCore.Log("Done");

            //previousRectHeight = mainPanelRect.rect.height;

            UIManager.SetPanelActive(PanelType, false);
        }

        internal GameObject contentHolder;
        internal readonly List<GameObject> dummyContents = new List<GameObject>();

        private GameObject CreateDummyContent()
        {
            var obj = UIFactory.CreateVerticalGroup(contentHolder, "Content", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            obj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var horiGroup = UIFactory.CreateHorizontalGroup(obj, "topGroup", true, true, true, true);
            UIFactory.SetLayoutElement(horiGroup, minHeight: 25, flexibleHeight: 0);

            var mainLabel = UIFactory.CreateLabel(horiGroup, "label", "Dummy " + dummyContents.Count, TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(mainLabel.gameObject, minHeight: 25, flexibleHeight: 0);

            var expandButton = UIFactory.CreateButton(horiGroup, "Expand", "V");
            UIFactory.SetLayoutElement(expandButton.gameObject, minWidth: 25, flexibleWidth: 0);

            var subContent = UIFactory.CreateVerticalGroup(obj, "SubContent", true, true, true, true);

            var inputObj = UIFactory.CreateInputField(subContent, "input", "...", out var inputField);
            UIFactory.SetLayoutElement(inputObj, minHeight: 25, flexibleHeight: 9999);
            inputObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            inputField.lineType = InputField.LineType.MultiLineNewline;

            int numLines = UnityEngine.Random.Range(0, 10);
            inputField.text = "This field has " + numLines + " lines";
            for (int i = 0; i < numLines; i++)
                inputField.text += "\r\n";

            subContent.SetActive(false);

            var btnLabel = expandButton.GetComponentInChildren<Text>();
            expandButton.onClick.AddListener(OnExpand);
            void OnExpand()
            {
                bool active = !subContent.activeSelf;
                if (active)
                {
                    subContent.SetActive(true);
                    btnLabel.text = "^";
                }
                else
                {
                    subContent.SetActive(false);
                    btnLabel.text = "V";
                }
            }

            return obj;
        }
    }

    public class DynamicListTest : IPoolDataSource
    {
        internal ScrollPool ScrollPool;
        internal InspectorTest Inspector;

        public DynamicListTest(ScrollPool scroller, InspectorTest inspector) 
        {
            ScrollPool = scroller;
            Inspector = inspector;
        }

        public int ItemCount => filtering ? filteredIndices.Count : Inspector.dummyContents.Count;

        private bool filtering;
        private readonly List<int> filteredIndices = new List<int>();

        public int GetRealIndexOfTempIndex(int index)
        {
            if (index < 0 || index >= filteredIndices.Count)
                return -1;
            return filteredIndices[index];
        }

        public void ToggleFilter()
        {
            if (filtering)
            {
                DisableFilter();
                ScrollPool.DisableTempCache();
            }
            else
            {
                EnableRandomFilter();
                ScrollPool.EnableTempCache();
            }

            ExplorerCore.Log("Filter toggled, new count: " + ItemCount);
            ScrollPool.Rebuild();
        }

        public void EnableRandomFilter()
        {
            filteredIndices.Clear();
            filtering = true;

            int counter = UnityEngine.Random.Range(0, Inspector.dummyContents.Count);
            while (filteredIndices.Count < counter)
            {
                var i = UnityEngine.Random.Range(0, Inspector.dummyContents.Count);
                if (!filteredIndices.Contains(i))
                    filteredIndices.Add(i);
            }
            filteredIndices.Sort();
        }

        public void DisableFilter()
        {
            filtering = false;
        }

        public void OnDisableCell(CellViewHolder cell, int dataIndex)
        {
            if (cell.UIRoot.transform.Find("Content") is Transform existing)
                existing.transform.SetParent(Inspector.contentHolder.transform, false);
        }

        public void Init()
        {
            var prototype = CellViewHolder.CreatePrototypeCell(ScrollPool.UIRoot);

            ScrollPool.DataSource = this;
            ScrollPool.Initialize(this, prototype);
        }

        public ICell CreateCell(RectTransform cellTransform) => new CellViewHolder(cellTransform.gameObject);

        public void DisableCell(ICell icell, int index)
        {
            var root = (icell as CellViewHolder).UIRoot;
            DisableContent(root);
            icell.Disable();
        }

        public void SetCell(ICell icell, int index)
        {
            var root = (icell as CellViewHolder).UIRoot;

            if (index < 0 || index >= ItemCount)
            {
                DisableContent(root);
                icell.Disable();
                return;
            }

            if (filtering)
                index = GetRealIndexOfTempIndex(index);

            var content = Inspector.dummyContents[index];

            if (content.transform.parent.ReferenceEqual(root.transform))
                return;

            DisableContent(root);

            content.transform.SetParent(root.transform, false);
        }

        private void DisableContent(GameObject cellRoot)
        {
            if (cellRoot.transform.Find("Content") is Transform existing)
                existing.transform.SetParent(Inspector.contentHolder.transform, false);
        }
    }
}
