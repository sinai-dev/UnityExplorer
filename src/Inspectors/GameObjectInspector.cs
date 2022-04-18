using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Widgets;
using UnityExplorer.UI.Widgets.AutoComplete;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets;
using UniverseLib.UI.Widgets.ScrollView;
using UniverseLib.Utility;

namespace UnityExplorer.Inspectors
{
    public class GameObjectInspector : InspectorBase
    {
        public GameObject GOTarget => Target as GameObject;

        public GameObject Content;

        public GameObjectControls GOControls;

        public TransformTree TransformTree;
        private ScrollPool<TransformCell> transformScroll;
        private readonly List<GameObject> cachedChildren = new();

        public ComponentList ComponentList;
        private ScrollPool<ComponentCell> componentScroll;

        private InputFieldRef addChildInput;
        private InputFieldRef addCompInput;

        public override void OnBorrowedFromPool(object target)
        {
            base.OnBorrowedFromPool(target);

            Target = target as GameObject;

            GOControls.UpdateGameObjectInfo(true, true);
            GOControls.UpdateTransformControlValues(true);

            RuntimeHelper.StartCoroutine(InitCoroutine());
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

            addChildInput.Text = "";
            addCompInput.Text = "";

            TransformTree.Clear();
            UpdateComponents();
        }

        public override void CloseInspector()
        {
            InspectorManager.ReleaseInspector(this);
        }

        public void OnTransformCellClicked(GameObject newTarget)
        {
            this.Target = newTarget;
            GOControls.UpdateGameObjectInfo(true, true);
            GOControls.UpdateTransformControlValues(true);
            TransformTree.RefreshData(true, false, true, false);
            UpdateComponents();
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

            GOControls.UpdateVectorSlider();
            GOControls.UpdateTransformControlValues(false);

            // Slow update
            if (timeOfLastUpdate.OccuredEarlierThan(1))
            {
                timeOfLastUpdate = Time.realtimeSinceStartup;

                GOControls.UpdateGameObjectInfo(false, false);

                TransformTree.RefreshData(true, false, false, false);
                UpdateComponents();
            }
        }

        // Child and Component Lists

        private IEnumerable<GameObject> GetTransformEntries()
        {
            if (!GOTarget)
                return Enumerable.Empty<GameObject>();

            cachedChildren.Clear();
            for (int i = 0; i < GOTarget.transform.childCount; i++)
                cachedChildren.Add(GOTarget.transform.GetChild(i).gameObject);
            return cachedChildren;
        }

        private readonly List<Component> componentEntries = new();
        private readonly HashSet<int> compInstanceIDs = new();
        private readonly List<Behaviour> behaviourEntries = new();
        private readonly List<bool> behaviourEnabledStates = new();

        // ComponentList.GetRootEntriesMethod
        private List<Component> GetComponentEntries() => GOTarget ? componentEntries : Enumerable.Empty<Component>().ToList();

        public void UpdateComponents()
        {
            if (!GOTarget)
            {
                componentEntries.Clear();
                compInstanceIDs.Clear();
                behaviourEntries.Clear();
                behaviourEnabledStates.Clear();
                ComponentList.RefreshData();
                ComponentList.ScrollPool.Refresh(true, true);
                return;
            }

            // Check if we actually need to refresh the component cells or not.
            IEnumerable<Component> comps = GOTarget.GetComponents<Component>();
            IEnumerable<Behaviour> behaviours = GOTarget.GetComponents<Behaviour>();

            bool needRefresh = false;

            int count = 0;
            foreach (Component comp in comps)
            {
                if (!comp)
                    continue;
                count++;
                if (!compInstanceIDs.Contains(comp.GetInstanceID()))
                {
                    needRefresh = true;
                    break;
                }
            }
            if (!needRefresh)
            {
                if (count != componentEntries.Count)
                    needRefresh = true;
                else
                {
                    count = 0;
                    foreach (Behaviour behaviour in behaviours)
                    {
                        if (!behaviour)
                            continue;
                        if (count >= behaviourEnabledStates.Count || behaviour.enabled != behaviourEnabledStates[count])
                        {
                            needRefresh = true;
                            break;
                        }
                        count++;
                    }
                    if (!needRefresh && count != behaviourEntries.Count)
                        needRefresh = true;
                }
            }

            if (!needRefresh)
                return;

            componentEntries.Clear();
            compInstanceIDs.Clear();
            foreach (Component comp in comps)
            {
                if (!comp) continue;
                componentEntries.Add(comp);
                compInstanceIDs.Add(comp.GetInstanceID());
            }

            behaviourEntries.Clear();
            behaviourEnabledStates.Clear();
            foreach (Behaviour behaviour in behaviours)
            {
                if (!behaviour) continue;
                behaviourEntries.Add(behaviour);
                behaviourEnabledStates.Add(behaviour.enabled);
            }

            ComponentList.RefreshData();
            ComponentList.ScrollPool.Refresh(true);
        }


        private void OnAddChildClicked(string input)
        {
            GameObject newObject = new(input);
            newObject.transform.parent = GOTarget.transform;

            TransformTree.RefreshData(true, false, true, false);
        }

        private void OnAddComponentClicked(string input)
        {
            if (ReflectionUtility.GetTypeByName(input) is Type type)
            {
                try
                {
                    RuntimeHelper.AddComponent<Component>(GOTarget, type);
                    UpdateComponents();
                }
                catch (Exception ex)
                {
                    ExplorerCore.LogWarning($"Exception adding component: {ex.ReflectionExToString()}");
                }
            }
            else
            {
                ExplorerCore.LogWarning($"Could not find any Type by the name '{input}'!");
            }
        }

        #region UI Construction

        public override GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(parent, "GameObjectInspector", true, false, true, true, 5,
                new Vector4(4, 4, 4, 4), new Color(0.065f, 0.065f, 0.065f));

            GameObject scrollObj = UIFactory.CreateScrollView(UIRoot, "GameObjectInspector", out Content, out AutoSliderScrollbar scrollbar,
                new Color(0.065f, 0.065f, 0.065f));
            UIFactory.SetLayoutElement(scrollObj, minHeight: 250, preferredHeight: 300, flexibleHeight: 0, flexibleWidth: 9999);

            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(Content, spacing: 3, padTop: 2, padBottom: 2, padLeft: 2, padRight: 2);

            // Construct GO Controls
            GOControls = new GameObjectControls(this);

            ConstructLists();

            return UIRoot;
        }

        // Child and Comp Lists

        private void ConstructLists()
        {
            GameObject listHolder = UIFactory.CreateUIObject("ListHolders", UIRoot);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(listHolder, false, true, true, true, 8, 2, 2, 2, 2);
            UIFactory.SetLayoutElement(listHolder, minHeight: 150, flexibleWidth: 9999, flexibleHeight: 9999);

            // Left group (Children)

            GameObject leftGroup = UIFactory.CreateUIObject("ChildrenGroup", listHolder);
            UIFactory.SetLayoutElement(leftGroup, flexibleWidth: 9999, flexibleHeight: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(leftGroup, false, false, true, true, 2);

            Text childrenLabel = UIFactory.CreateLabel(leftGroup, "ChildListTitle", "Children", TextAnchor.MiddleCenter, default, false, 16);
            UIFactory.SetLayoutElement(childrenLabel.gameObject, flexibleWidth: 9999);

            // Add Child
            GameObject addChildRow = UIFactory.CreateUIObject("AddChildRow", leftGroup);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(addChildRow, false, false, true, true, 2);

            addChildInput = UIFactory.CreateInputField(addChildRow, "AddChildInput", "Enter a name...");
            UIFactory.SetLayoutElement(addChildInput.Component.gameObject, minHeight: 25, preferredWidth: 9999);

            ButtonRef addChildButton = UIFactory.CreateButton(addChildRow, "AddChildButton", "Add Child");
            UIFactory.SetLayoutElement(addChildButton.Component.gameObject, minHeight: 25, minWidth: 80);
            addChildButton.OnClick += () => { OnAddChildClicked(addChildInput.Text); };

            // TransformTree

            transformScroll = UIFactory.CreateScrollPool<TransformCell>(leftGroup, "TransformTree", out GameObject transformObj,
                out GameObject transformContent, new Color(0.11f, 0.11f, 0.11f));

            TransformTree = new TransformTree(transformScroll, GetTransformEntries, OnTransformCellClicked);

            // Right group (Components)

            GameObject rightGroup = UIFactory.CreateUIObject("ComponentGroup", listHolder);
            UIFactory.SetLayoutElement(rightGroup, flexibleWidth: 9999, flexibleHeight: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(rightGroup, false, false, true, true, 2);

            Text compLabel = UIFactory.CreateLabel(rightGroup, "CompListTitle", "Components", TextAnchor.MiddleCenter, default, false, 16);
            UIFactory.SetLayoutElement(compLabel.gameObject, flexibleWidth: 9999);

            // Add Comp
            GameObject addCompRow = UIFactory.CreateUIObject("AddCompRow", rightGroup);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(addCompRow, false, false, true, true, 2);

            addCompInput = UIFactory.CreateInputField(addCompRow, "AddCompInput", "Enter a Component type...");
            UIFactory.SetLayoutElement(addCompInput.Component.gameObject, minHeight: 25, preferredWidth: 9999);

            ButtonRef addCompButton = UIFactory.CreateButton(addCompRow, "AddCompButton", "Add Comp");
            UIFactory.SetLayoutElement(addCompButton.Component.gameObject, minHeight: 25, minWidth: 80);
            addCompButton.OnClick += () => { OnAddComponentClicked(addCompInput.Text); };

            // comp autocompleter
            new TypeCompleter(typeof(Component), addCompInput);

            // Component List

            componentScroll = UIFactory.CreateScrollPool<ComponentCell>(rightGroup, "ComponentList", out GameObject compObj,
                out GameObject compContent, new Color(0.11f, 0.11f, 0.11f));
            UIFactory.SetLayoutElement(compObj, flexibleHeight: 9999);
            UIFactory.SetLayoutElement(compContent, flexibleHeight: 9999);

            ComponentList = new ComponentList(componentScroll, GetComponentEntries)
            {
                Parent = this
            };
            componentScroll.Initialize(ComponentList);
        }


        #endregion
    }
}
