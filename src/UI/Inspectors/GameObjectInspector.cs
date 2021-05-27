using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Input;
using UnityExplorer.UI.Models;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Widgets;
using UnityExplorer.UI.Widgets.AutoComplete;

namespace UnityExplorer.UI.Inspectors
{
    public class GameObjectInspector : InspectorBase
    {
        public GameObject GOTarget => Target as GameObject;

        public GameObject Content;

        public GameObjectControls GOControls;

        public TransformTree TransformTree;
        private ScrollPool<TransformCell> transformScroll;
        private readonly List<GameObject> cachedChildren = new List<GameObject>();

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

            addChildInput.Text = "";
            addCompInput.Text = "";

            TransformTree.Clear();
            UpdateComponents();
        }

        public override void CloseInspector()
        {
            InspectorManager.ReleaseInspector(this);
        }

        public void ChangeTarget(GameObject newTarget)
        {
            this.Target = newTarget;
            GOControls.UpdateGameObjectInfo(true, true);
            GOControls.UpdateTransformControlValues(true);
            TransformTree.RefreshData(true, false);
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

                TransformTree.RefreshData(true, false);
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

        private readonly List<Component> componentEntries = new List<Component>();
        private readonly HashSet<int> compInstanceIDs = new HashSet<int>();
        private readonly List<Behaviour> behaviourEntries = new List<Behaviour>();
        private readonly List<bool> behaviourEnabledStates = new List<bool>();

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
            var comps = GOTarget.GetComponents<Component>();
            var behaviours = GOTarget.GetComponents<Behaviour>();

            bool needRefresh = false;
            if (comps.Length != componentEntries.Count || behaviours.Length != behaviourEntries.Count)
            {
                needRefresh = true;
            }
            else
            {
                foreach (var comp in comps)
                {
                    if (!compInstanceIDs.Contains(comp.GetInstanceID()))
                    {
                        needRefresh = true;
                        break;
                    }
                }

                if (!needRefresh)
                {
                    for (int i = 0; i < behaviours.Length; i++)
                    {
                        var behaviour = behaviours[i];
                        if (behaviour.enabled != behaviourEnabledStates[i])
                        {
                            needRefresh = true;
                            break;
                        }
                    }
                }
            }

            if (!needRefresh)
                return;

            componentEntries.Clear();
            compInstanceIDs.Clear();

            foreach (var comp in comps)
            {
                componentEntries.Add(comp);
                compInstanceIDs.Add(comp.GetInstanceID());
            }

            behaviourEntries.Clear();
            behaviourEnabledStates.Clear();
            foreach (var behaviour in behaviours)
            {
                behaviourEntries.Add(behaviour);
                behaviourEnabledStates.Add(behaviour.enabled);
            }

            ComponentList.RefreshData();
            ComponentList.ScrollPool.Refresh(true);
        }


        private void OnAddChildClicked(string input)
        {
            var newObject = new GameObject(input);
            newObject.transform.parent = GOTarget.transform;

            TransformTree.RefreshData(true, false);
        }
        
        private void OnAddComponentClicked(string input)
        {
            if (ReflectionUtility.AllTypes.TryGetValue(input, out Type type))
            {
                try
                {
                    RuntimeProvider.Instance.AddComponent<Component>(GOTarget, type);
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

            var scrollObj = UIFactory.CreateScrollView(UIRoot, "GameObjectInspector", out Content, out var scrollbar, 
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
            var listHolder = UIFactory.CreateUIObject("ListHolders", UIRoot);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(listHolder, false, true, true, true, 8, 2, 2, 2, 2);
            UIFactory.SetLayoutElement(listHolder, minHeight: 150, flexibleWidth: 9999, flexibleHeight: 9999);

            // Left group (Children)

            var leftGroup = UIFactory.CreateUIObject("ChildrenGroup", listHolder);
            UIFactory.SetLayoutElement(leftGroup, flexibleWidth: 9999, flexibleHeight: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(leftGroup, false, false, true, true, 2);

            var childrenLabel = UIFactory.CreateLabel(leftGroup, "ChildListTitle", "Children", TextAnchor.MiddleCenter, default, false, 16);
            UIFactory.SetLayoutElement(childrenLabel.gameObject, flexibleWidth: 9999);

            // Add Child
            var addChildRow = UIFactory.CreateUIObject("AddChildRow", leftGroup);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(addChildRow, false, false, true, true, 2);

            addChildInput = UIFactory.CreateInputField(addChildRow, "AddChildInput", "Enter a name...");
            UIFactory.SetLayoutElement(addChildInput.Component.gameObject, minHeight: 25, preferredWidth: 9999);

            var addChildButton = UIFactory.CreateButton(addChildRow, "AddChildButton", "Add Child");
            UIFactory.SetLayoutElement(addChildButton.Component.gameObject, minHeight: 25, minWidth: 80);
            addChildButton.OnClick += () => { OnAddChildClicked(addChildInput.Text); };

            // TransformTree

            transformScroll = UIFactory.CreateScrollPool<TransformCell>(leftGroup, "TransformTree", out GameObject transformObj,
                out GameObject transformContent, new Color(0.11f, 0.11f, 0.11f));
            UIFactory.SetLayoutElement(transformObj, flexibleHeight: 9999);
            UIFactory.SetLayoutElement(transformContent, flexibleHeight: 9999);

            TransformTree = new TransformTree(transformScroll, GetTransformEntries);
            TransformTree.Init();
            TransformTree.OnClickOverrideHandler = ChangeTarget;

            // Right group (Components)

            var rightGroup = UIFactory.CreateUIObject("ComponentGroup", listHolder);
            UIFactory.SetLayoutElement(rightGroup, flexibleWidth: 9999, flexibleHeight: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(rightGroup, false, false, true, true, 2);

            var compLabel = UIFactory.CreateLabel(rightGroup, "CompListTitle", "Components", TextAnchor.MiddleCenter, default, false, 16);
            UIFactory.SetLayoutElement(compLabel.gameObject, flexibleWidth: 9999);

            // Add Comp
            var addCompRow = UIFactory.CreateUIObject("AddCompRow", rightGroup);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(addCompRow, false, false, true, true, 2);

            addCompInput = UIFactory.CreateInputField(addCompRow, "AddCompInput", "Enter a Component type...");
            UIFactory.SetLayoutElement(addCompInput.Component.gameObject, minHeight: 25, preferredWidth: 9999);

            var addCompButton = UIFactory.CreateButton(addCompRow, "AddCompButton", "Add Comp");
            UIFactory.SetLayoutElement(addCompButton.Component.gameObject, minHeight: 25, minWidth: 80);
            addCompButton.OnClick += () => { OnAddComponentClicked(addCompInput.Text); };

            // comp autocompleter
            new TypeCompleter(typeof(Component), addCompInput);

            // Component List

            componentScroll = UIFactory.CreateScrollPool<ComponentCell>(rightGroup, "ComponentList", out GameObject compObj,
                out GameObject compContent, new Color(0.11f, 0.11f, 0.11f));
            UIFactory.SetLayoutElement(compObj, flexibleHeight: 9999);
            UIFactory.SetLayoutElement(compContent, flexibleHeight: 9999);

            ComponentList = new ComponentList(componentScroll, GetComponentEntries);
            ComponentList.Parent = this;
            componentScroll.Initialize(ComponentList);
        }


        #endregion
    }
}
