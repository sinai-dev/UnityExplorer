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

        //public SimpleListSource<Component> ComponentList;

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

        public void OnCellClicked(SimpleCell<Component> cell)
        {
            ExplorerCore.Log("Cell clicked!");
        }

        //public void SetCell(SimpleCell<Component> cell, int index)
        //{
        //    var comp = ComponentList.currentEntries[index];
        //    if (!comp)
        //        cell.buttonText.text = "<color=red>[Destroyed]</color>";
        //    else
        //        cell.buttonText.text = ToStringUtility.GetDefaultLabel(comp, ReflectionProvider.Instance.GetActualType(comp), true, false);
        //}

        public bool ShouldDisplay(Component comp, string filter)
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

        private DynamicScrollPool scrollPool;

        public override void ConstructPanelContent()
        {
            // temp debug
            scrollPool = UIFactory.CreateDynamicScrollPool(content, "Test", out GameObject scrollObj,
                out GameObject scrollContent, new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
            UIFactory.SetLayoutElement(scrollContent, flexibleHeight: 9999);

            var test = new DynamicListTest(scrollPool);
            test.Init();

            var prototype = DynamicCellTest.CreatePrototypeCell(scrollContent);
            scrollPool.PrototypeCell = prototype.GetComponent<RectTransform>();

            //// Component list

            //var scrollPool = (ScrollPool)UIFactory.CreateScrollPool<ScrollPool>(content, "ComponentList", out GameObject scrollObj,
            //    out GameObject scrollContent, new Color(0.15f, 0.15f, 0.15f));
            //UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
            //UIFactory.SetLayoutElement(scrollContent, flexibleHeight: 9999);

            //ComponentList = new SimpleListSource<Component>(scrollPool, GetEntries, CreateCell, SetCell, ShouldDisplay);
            //ComponentList.Init();

            //// Prototype tree cell
            //var prototype = SimpleCell<Component>.CreatePrototypeCell(scrollContent);
            //scrollPool.PrototypeCell = prototype.GetComponent<RectTransform>();


            previousRectHeight = mainPanelRect.rect.height;
        }
    }

    public class DynamicListTest : IDynamicDataSource
    {
        public struct Data
        {
            public float height; public Color color; 
            public Data(float f, Color c) { height = f; color = c; }
        };

        public int ItemCount => imaginaryData.Count;

        public float DefaultCellHeight => 25f;

        // private List<DynamicCellTest> cellCache = new List<DynamicCellTest>();

        private List<Data> imaginaryData;

        internal DynamicScrollPool Scroller;

        public DynamicListTest(DynamicScrollPool scroller) { Scroller = scroller; }

        public void Init()
        {
            imaginaryData = new List<Data>();
            for (int i = 0; i < 100; i++)
            {
                imaginaryData.Add(new Data
                {
                    height = (UnityEngine.Random.Range(25f, 800f)
                        + UnityEngine.Random.Range(25f, 50f)
                        + 50f)
                        * 0.25f,
                    //height = 25f,
                    color = new Color
                    {
                        r = UnityEngine.Random.Range(0.1f, 0.8f),
                        g = UnityEngine.Random.Range(0.1f, 0.8f),
                        b = UnityEngine.Random.Range(0.1f, 0.8f),
                        a = 1
                    }
                });
            }

            RuntimeProvider.Instance.StartCoroutine(InitCoroutine());
        }

        private IEnumerator InitCoroutine()
        {
            yield return null;

            //RefreshData();
            Scroller.DataSource = this;
            Scroller.Initialize(this);
        }

        public ICell CreateCell(RectTransform cellTransform)
        {
            var cell = new DynamicCellTest(cellTransform.gameObject, 
                cellTransform.GetComponentInChildren<Image>(), 
                cellTransform.Find("Label").GetComponent<Text>());

            return cell;
        }

        //public float GetHeightForCell(int index)
        //{
        //    if (index < 0 || index >= imaginaryData.Count)
        //        return 0f;
        //    return imaginaryData[index].height;
        //}

        //public float GetTotalHeight()
        //{
        //    float ret = 0f;
        //    foreach (var f in imaginaryData)
        //        ret += f.height;
        //    return ret;
        //}

        public void SetCell(ICell icell, int index)
        {
            if (index < 0 || index >= imaginaryData.Count)
            {
                icell.Disable();
                return;
            }

            var cell = icell as DynamicCellTest;
            var data = imaginaryData[index];
            cell.image.color = data.color;
            cell.text.text = $"{index}: {data.height}";
            cell.Height = data.height;
        }
    }

    public class DynamicCellTest : IDynamicCell
    {
        public DynamicScrollPool.CachedCell Cached;

        public DynamicCellTest(GameObject uiRoot, Image image, Text text)
        {
            this.uiRoot = uiRoot;
            this.image = image;
            this.text = text;

            var button = uiRoot.GetComponentInChildren<Button>();
            var layout = uiRoot.GetComponent<LayoutElement>();
            button.onClick.AddListener(() => 
            {
                if (!expanded)
                {
                    layout.minHeight = this.Height;
                    expanded = true;
                    if (Cached != null) Cached.Height = Height;
                }
                else
                {
                    this.Height = 25;// default height
                    layout.minHeight = Height; 
                    expanded = false;
                    if (Cached != null) Cached.Height = Height;
                }
            });
        }

        private bool expanded;

        public bool Enabled => m_enabled;
        private bool m_enabled;

        public float Height;

        public GameObject uiRoot;
        public Image image;
        public Text text;

        public LayoutGroup layoutGroup;
        public Button button;

        public void Disable()
        {
            m_enabled = false;
            uiRoot.SetActive(false);
            //image.color = Color.red;
        }

        public void Enable()
        {
            m_enabled = true;
            uiRoot.SetActive(true);
        }

        public float GetHeight()
        {
            return Height;
        }

        public static GameObject CreatePrototypeCell(GameObject parent)
        {
            var prototype = UIFactory.CreateHorizontalGroup(parent, "PrototypeCell", true, true, true, true, 2, default,
                new Color(0.15f, 0.15f, 0.15f), TextAnchor.MiddleCenter);
            //var cell = prototype.AddComponent<TransformCell>();
            var rect = prototype.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(25, 25);
            UIFactory.SetLayoutElement(prototype, minWidth: 100, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 0);

            var text = UIFactory.CreateLabel(prototype, "Label", "notset", TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(text.gameObject, minHeight: 25, minWidth: 200, flexibleWidth: 0);

            var button = UIFactory.CreateButton(prototype, "button", "Toggle");
            UIFactory.SetLayoutElement(button.gameObject, minWidth: 50, flexibleWidth: 0, minHeight: 25);

            prototype.SetActive(false);

            return prototype;
        }
    }
}
