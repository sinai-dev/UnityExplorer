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

            //scrollPool.Refresh();
        }

        public override void SetDefaultPosAndAnchors()
        {
            mainPanelRect.localPosition = Vector2.zero;
            mainPanelRect.anchorMin = new Vector2(1, 0);
            mainPanelRect.anchorMax = new Vector2(1, 1);
            mainPanelRect.sizeDelta = new Vector2(-300f, mainPanelRect.sizeDelta.y);
            mainPanelRect.anchoredPosition = new Vector2(-200, 0);
            mainPanelRect.offsetMin = new Vector2(mainPanelRect.offsetMin.x, 100);  // bottom
            mainPanelRect.offsetMax = new Vector2(mainPanelRect.offsetMax.x, -50); // top
            mainPanelRect.pivot = new Vector2(0.5f, 0.5f);
        }

        private ScrollPool scrollPool;

        public override void ConstructPanelContent()
        {
            //UIRoot.GetComponent<Mask>().enabled = false;

            // temp debug
            scrollPool = UIFactory.CreateScrollPool(content, "Test", out GameObject scrollObj,
                out GameObject scrollContent, new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
            UIFactory.SetLayoutElement(scrollContent, flexibleHeight: 9999);

            var test = new DynamicListTest(scrollPool, this);
            test.Init();

            var prototype = DynamicCellTest.CreatePrototypeCell(scrollContent);
            scrollPool.PrototypeCell = prototype.GetComponent<RectTransform>();

            dummyContentHolder = new GameObject("DummyHolder");
            dummyContentHolder.SetActive(false);

            GameObject.DontDestroyOnLoad(dummyContentHolder);
            for (int i = 0; i < 10; i++)
            {
                dummyContents.Add(CreateDummyContent());
            }

            previousRectHeight = mainPanelRect.rect.height;
        }

        internal GameObject dummyContentHolder;
        internal readonly List<GameObject> dummyContents = new List<GameObject>();

        private GameObject CreateDummyContent()
        {
            var obj = UIFactory.CreateVerticalGroup(dummyContentHolder, "Content", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(obj, minHeight: 25, flexibleHeight: 9999);

            var label = UIFactory.CreateLabel(obj, "label", "Dummy " + dummyContents.Count, TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(label.gameObject, minHeight: 25, flexibleHeight: 0);

            var inputObj = UIFactory.CreateInputField(obj, "input", "...", out InputField inputField);
            inputField.lineType = InputField.LineType.MultiLineNewline;
            UIFactory.SetLayoutElement(inputObj, minHeight: 25, flexibleHeight: 9999);
            //inputObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

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

        public void Init()
        {

            Scroller.DataSource = this;
            Scroller.Initialize(this);
        }

        public ICell CreateCell(RectTransform cellTransform) => new DynamicCellTest(cellTransform.gameObject);

        public void SetCell(ICell icell, int index)
        {
            if (index < 0 || index >= ItemCount)
            {
                icell.Disable();
                return;
            }

            var root = (icell as DynamicCellTest).uiRoot;

            if (root.transform.Find("Content") is Transform existing)
            {
                ExplorerCore.Log("removing existing content");
                existing.transform.SetParent(Inspector.dummyContentHolder.transform, false);
            }

            var content = Inspector.dummyContents[index];
            content.transform.SetParent(root.transform, false);
        }
    }

    public class DynamicCellTest : ICell
    {
        public DynamicCellTest(GameObject uiRoot)
        {
            this.uiRoot = uiRoot;
        }

        public bool Enabled => m_enabled;
        private bool m_enabled;

        public GameObject uiRoot;
        public InputField input;

        public void Disable()
        {
            m_enabled = false;
            uiRoot.SetActive(false);
        }

        public void Enable()
        {
            m_enabled = true;
            uiRoot.SetActive(true);
        }

        public static GameObject CreatePrototypeCell(GameObject parent)
        {
            var prototype = UIFactory.CreateVerticalGroup(parent, "PrototypeCell", true, true, true, true, 2, default,
                new Color(0.15f, 0.15f, 0.15f), TextAnchor.MiddleCenter);
            var rect = prototype.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(25, 25);
            UIFactory.SetLayoutElement(prototype, minWidth: 100, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 9999);

            prototype.SetActive(false);

            return prototype;
        }
    }
}
