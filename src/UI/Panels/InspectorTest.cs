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
                //scrollPool.ReloadData();
            }

            scrollPool.RefreshCells(true);
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

            var test = new DynamicListTest(scrollPool, this);
            test.Init();

            //var prototype = DynamicCell.CreatePrototypeCell(scrollContent);
            //scrollPool.PrototypeCell = prototype.GetComponent<RectTransform>();

            dummyContentHolder = new GameObject("DummyHolder");
            dummyContentHolder.SetActive(false);

            GameObject.DontDestroyOnLoad(dummyContentHolder);
            ExplorerCore.Log("Creating dummy objects");
            for (int i = 0; i < 100; i++)
            {
                dummyContents.Add(CreateDummyContent());
            }
            ExplorerCore.Log("Done");

            previousRectHeight = mainPanelRect.rect.height;
        }

        internal GameObject dummyContentHolder;
        internal readonly List<GameObject> dummyContents = new List<GameObject>();

        private GameObject CreateDummyContent()
        {
            var obj = UIFactory.CreateVerticalGroup(dummyContentHolder, "Content", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
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
        internal ScrollPool Scroller;
        internal InspectorTest Inspector;

        public DynamicListTest(ScrollPool scroller, InspectorTest inspector) 
        {
            Scroller = scroller;
            Inspector = inspector;
        }

        public int ItemCount => Inspector.dummyContents.Count;

        public void OnDisableCell(CellViewHolder cell, int dataIndex)
        {
            if (cell.UIRoot.transform.Find("Content") is Transform existing)
                existing.transform.SetParent(Inspector.dummyContentHolder.transform, false);
        }

        public void Init()
        {
            var prototype = CellViewHolder.CreatePrototypeCell(Scroller.UIRoot);

            Scroller.DataSource = this;
            Scroller.Initialize(this, prototype);
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

            var content = Inspector.dummyContents[index];

            if (content.transform.parent.ReferenceEqual(root.transform))
                return;

            DisableContent(root);

            content.transform.SetParent(root.transform, false);
        }

        private void DisableContent(GameObject cellRoot)
        {
            if (cellRoot.transform.Find("Content") is Transform existing)
                existing.transform.SetParent(Inspector.dummyContentHolder.transform, false);
        }
    }
}
