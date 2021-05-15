using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.ObjectPool;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Inspectors
{
    public class GameObjectInspector : InspectorBase
    {
        public GameObject GOTarget => Target as GameObject;

        // Top info

        private InputFieldRef NameInput;
        private GameObject PathRow;
        private InputFieldRef PathInput;

        // Child and comp lists

        public TransformTree TransformTree;
        private ScrollPool<TransformCell> transformScroll;
        private readonly List<GameObject> cachedChildren = new List<GameObject>();

        public ButtonListSource<Component> ComponentList;
        private ScrollPool<ButtonCell> componentScroll;

        public override void OnBorrowedFromPool(object target)
        {
            base.OnBorrowedFromPool(target);

            Target = target as GameObject;
            Tab.TabText.text = $"[G] {GOTarget.name}";

            RuntimeProvider.Instance.StartCoroutine(InitCoroutine());
        }

        private IEnumerator InitCoroutine()
        {
            yield return null;

            LayoutRebuilder.ForceRebuildLayoutImmediate(InspectorPanel.Instance.ContentRect);

            TransformTree.Rebuild();

            ComponentList.ScrollPool.Refresh(true, true);
            UpdateComponents();
        }

        public override void OnReturnToPool()
        {
            base.OnReturnToPool();
        }
        protected override void OnCloseClicked()
        {
            InspectorManager.ReleaseInspector(this);
        }

        private float timeOfLastUpdate;

        public override void Update()
        {
            if (!this.IsActive)
                return;

            if (Target.IsNullOrDestroyed(false))
            {
                InspectorManager.ReleaseInspector(this);
                return;
            }

            if (timeOfLastUpdate.OccuredEarlierThan(1))
            {
                timeOfLastUpdate = Time.realtimeSinceStartup;

                // Refresh children and components
                TransformTree.RefreshData(true, false);

                UpdateComponents();

                Tab.TabText.text = $"[G] {GOTarget.name}";
            }
        }

        private void RefreshTopInfo()
        {

        }


        #region Transform and Component Lists 

        private IEnumerable<GameObject> GetTransformEntries()
        {
            cachedChildren.Clear();
            for (int i = 0; i < GOTarget.transform.childCount; i++)
                cachedChildren.Add(GOTarget.transform.GetChild(i).gameObject);
            return cachedChildren;
        }

        private readonly List<Component> _componentEntries = new List<Component>();
        private readonly HashSet<int> _compInstanceIDs = new HashSet<int>();

        private List<Component> GetComponentEntries()
        {
            return _componentEntries;
        }

        private void UpdateComponents()
        {
            // Check if we actually need to refresh the component cells or not.
            // Doing this check is far more efficient than blindly setting cells.

            var comps = GOTarget.GetComponents<Component>();

            bool needRefresh = false;
            if (comps.Length != _componentEntries.Count)
                needRefresh = true;
            else
            {
                foreach (var comp in comps)
                {
                    if (!_compInstanceIDs.Contains(comp.GetInstanceID()))
                    {
                        needRefresh = true;
                        break;
                    }
                }
            }

            if (!needRefresh)
                return;

            _componentEntries.Clear();
            _compInstanceIDs.Clear();

            foreach (var comp in comps)
            {
                _componentEntries.Add(comp);
                _compInstanceIDs.Add(comp.GetInstanceID());
            }

            ComponentList.RefreshData();
            ComponentList.ScrollPool.Refresh(true);
        }

        private static readonly Dictionary<string, string> compToStringCache = new Dictionary<string, string>();

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

            if (!compToStringCache.ContainsKey(type.AssemblyQualifiedName))
            {
                compToStringCache.Add(type.AssemblyQualifiedName, SignatureHighlighter.Parse(type, true));
            }

            cell.Button.ButtonText.text = compToStringCache[type.AssemblyQualifiedName];
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

        #endregion


        #region UI Construction

        public override GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(Pool<GameObjectInspector>.Instance.InactiveHolder,
                "GameObjectInspector", true, true, true, true, 5, new Vector4(4, 4, 4, 4), new Color(0.12f, 0.12f, 0.12f));

            // Title row

            var titleRow = UIFactory.CreateUIObject("TitleRow", UIRoot);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(titleRow, false, false, true, true, 5);

            var titleLabel = UIFactory.CreateLabel(titleRow, "Title", SignatureHighlighter.Parse(typeof(GameObject), true), 
                TextAnchor.MiddleLeft, fontSize: 17);
            UIFactory.SetLayoutElement(titleLabel.gameObject, minHeight: 30, flexibleHeight: 0, flexibleWidth: 9999);

            // Update button
            var updateBtn = UIFactory.CreateButton(titleRow, "UpdateButton", "Update Info", new Color(0.2f, 0.3f, 0.2f));
            UIFactory.SetLayoutElement(updateBtn.Component.gameObject, minHeight: 25, minWidth: 200);
            updateBtn.OnClick += () => { ExplorerCore.Log("TODO!"); };

            // ~~~~~~ Top info ~~~~~~

            // parent / path row

            this.PathRow = UIFactory.CreateUIObject("ParentRow", UIRoot);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(this.PathRow, false, false, true, true, 5, 2, 2, 2, 2);
            UIFactory.SetLayoutElement(this.PathRow, minHeight: 25, flexibleHeight: 100, flexibleWidth: 9999);

            var viewParentButton = UIFactory.CreateButton(PathRow, "ViewParentButton", "View Parent", new Color(0.3f, 0.3f, 0.3f));
            UIFactory.SetLayoutElement(viewParentButton.Component.gameObject, minHeight: 25, minWidth: 80);
            viewParentButton.OnClick += () => { ExplorerCore.LogWarning("TODO!"); };

            this.PathInput = UIFactory.CreateInputField(PathRow, "PathInput", "No parent (root object)");
            UIFactory.SetLayoutElement(PathInput.UIRoot, minHeight: 25, minWidth: 100, flexibleWidth: 9999);


            // ~~~~~~ Child and comp lists ~~~~~~

            var listTitles = UIFactory.CreateUIObject("ListTitles", UIRoot);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(listTitles, true, true, true, true, 5, 2, 2, 2, 2);
            UIFactory.SetLayoutElement(listTitles, flexibleWidth: 9999, flexibleHeight: 30);
            UIFactory.CreateLabel(listTitles, "ChildListTitle", "Children", TextAnchor.MiddleCenter, default, false, 15);
            UIFactory.CreateLabel(listTitles, "CompListTitle", "Components", TextAnchor.MiddleCenter, default, false, 15);
            
            var listHolder = UIFactory.CreateHorizontalGroup(UIRoot, "ListHolder", true, true, true, true, 5, new Vector4(2, 2, 2, 2));
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

            return UIRoot;
        }

        #endregion
    }
}
