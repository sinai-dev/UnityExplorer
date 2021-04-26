using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.ObjectPool;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Inspectors
{
    public class GameObjectInspector : InspectorBase
    {
        public GameObject Target;

        public override GameObject UIRoot => uiRoot;
        private GameObject uiRoot;

        private Text NameText;

        public TransformTree TransformTree;
        private ScrollPool<TransformCell> transformScroll;

        public ButtonListSource<Component> ComponentList;
        private ScrollPool<ButtonCell> componentScroll;

        public override GameObject CreateContent(GameObject parent)
        {
            uiRoot = UIFactory.CreateVerticalGroup(Pool<GameObjectInspector>.Instance.InactiveHolder,
                "GameObjectInspector", true, true, true, true, 5, new Vector4(4, 4, 4, 4), new Color(0.12f, 0.12f, 0.12f));

            NameText = UIFactory.CreateLabel(uiRoot, "Title", "not set", TextAnchor.MiddleLeft, fontSize: 20);
            UIFactory.SetLayoutElement(NameText.gameObject, minHeight: 30, flexibleHeight: 0);

            var listHolder = UIFactory.CreateHorizontalGroup(uiRoot, "ListHolder", true, true, true, true, 5, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(listHolder, flexibleWidth: 9999, flexibleHeight: 9999);

            transformScroll = UIFactory.CreateScrollPool<TransformCell>(listHolder, "TransformTree", out GameObject transformObj,
                out GameObject transformContent, new Color(0.11f, 0.11f, 0.11f));
            UIFactory.SetLayoutElement(transformObj, flexibleHeight: 9999);
            UIFactory.SetLayoutElement(transformContent, flexibleHeight: 9999);

            componentScroll = UIFactory.CreateScrollPool<ButtonCell>(listHolder, "ComponentList", out GameObject compObj,
                out GameObject compContent, new Color(0.11f, 0.11f, 0.11f));
            UIFactory.SetLayoutElement(compObj, flexibleHeight: 9999);
            UIFactory.SetLayoutElement(compContent, flexibleHeight: 9999);

            TransformTree = new TransformTree(transformScroll) { GetRootEntriesMethod = GetTransformEntries };
            TransformTree.Init();

            ComponentList = new ButtonListSource<Component>(componentScroll, GetComponentEntries, SetComponentCell, ShouldDisplay, OnComponentClicked);
            componentScroll.Initialize(ComponentList);

            return uiRoot;
        }

        private readonly List<GameObject> _rootEntries = new List<GameObject>();

        private IEnumerable<GameObject> GetTransformEntries()
        {
            _rootEntries.Clear();
            for (int i = 0; i < Target.transform.childCount; i++)
                _rootEntries.Add(Target.transform.GetChild(i).gameObject);
            return _rootEntries;
        }

        private readonly List<Component> _componentEntries = new List<Component>();

        private List<Component> GetComponentEntries()
        {
            return _componentEntries;
        }

        private static readonly Dictionary<Type, string> compToStringCache = new Dictionary<Type, string>();

        private void SetComponentCell(ButtonCell cell, int index)
        {
            if (index < 0 || index >= _componentEntries.Count)
            { 
                cell.Disable();
                return;
            }

            cell.Enable();

            var comp = _componentEntries[index];
            var type = comp.GetActualType();

            if (!compToStringCache.ContainsKey(type))
            {
                compToStringCache.Add(type, 
                    $"<color={SignatureHighlighter.NAMESPACE}>{type.Namespace}</color>.{SignatureHighlighter.HighlightTypeName(type)}");
            }

            cell.Button.ButtonText.text = compToStringCache[type];
        }

        private bool ShouldDisplay(Component comp, string filter) => true;

        private void OnComponentClicked(int index)
        {
            if (index < 0 || index >= _componentEntries.Count)
                return;

            var comp = _componentEntries[index];
            if (comp)
                InspectorManager.Inspect(comp);
        }

        public override void OnBorrowedFromPool(object target)
        {
            base.OnBorrowedFromPool(target);

            Target = target as GameObject;

            NameText.text = Target.name;
            this.Tab.TabText.text = $"[G] {Target.name}";

            TransformTree.Rebuild();

            ComponentList.ScrollPool.Rebuild();
            UpdateComponents();
        }

        public override void OnReturnToPool()
        {
            base.OnReturnToPool();

            // release component and transform lists
            this.TransformTree.ScrollPool.ReturnCells();
            this.TransformTree.ScrollPool.SetUninitialized();

            this.ComponentList.ScrollPool.ReturnCells();
            this.ComponentList.ScrollPool.SetUninitialized();
        }

        private float timeOfLastUpdate;

        public override void Update()
        {
            // todo update tab title? or put that in InspectorBase update?

            if (!this.IsActive)
                return;

            if (Time.time - timeOfLastUpdate > 1f)
            {
                timeOfLastUpdate = Time.time;

                // Refresh children and components
                TransformTree.RefreshData(true, false);

                UpdateComponents();
            }
        }

        private void UpdateComponents()
        {
            _componentEntries.Clear();
            foreach (var comp in Target.GetComponents<Component>())
                _componentEntries.Add(comp);

            ComponentList.RefreshData();
            ComponentList.ScrollPool.RefreshCells(true);
        }

        protected override void OnCloseClicked()
        {
            InspectorManager.ReleaseInspector(this);
        }
    }
}
