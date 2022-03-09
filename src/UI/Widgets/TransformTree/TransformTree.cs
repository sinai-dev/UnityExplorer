using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using UnityEngine;
using UniverseLib;
using UniverseLib.UI.Widgets.ScrollView;
using UniverseLib.Utility;

namespace UnityExplorer.UI.Widgets
{
    public class TransformTree : ICellPoolDataSource<TransformCell>
    {
        public Func<IEnumerable<GameObject>> GetRootEntriesMethod;
        public Action<GameObject> OnClickOverrideHandler;

        public ScrollPool<TransformCell> ScrollPool;

        /// <summary>
        /// Key: UnityEngine.Transform instance ID<br/>
        /// Value: CachedTransform
        /// </summary>
        internal readonly OrderedDictionary cachedTransforms = new();

        // for keeping track of which actual transforms are expanded or not, outside of the cache data.
        private readonly HashSet<int> expandedInstanceIDs = new();
        private readonly HashSet<int> autoExpandedIDs = new();

        private readonly HashSet<int> visited = new();
        private bool needRefresh;
        private int displayIndex;
        int prevDisplayIndex;

        public int ItemCount => prevDisplayIndex;

        public bool Filtering => !string.IsNullOrEmpty(currentFilter);
        private bool wasFiltering;

        public string CurrentFilter
        {
            get => currentFilter;
            set
            {
                currentFilter = value ?? "";
                if (!wasFiltering && Filtering)
                    wasFiltering = true;
                else if (wasFiltering && !Filtering)
                {
                    wasFiltering = false;
                    autoExpandedIDs.Clear();
                }
            }
        }
        private string currentFilter;

        private Coroutine refreshCoroutine;
        private readonly Stopwatch traversedThisFrame = new();

        public TransformTree(ScrollPool<TransformCell> scrollPool, Func<IEnumerable<GameObject>> getRootEntriesMethod)
        {
            ScrollPool = scrollPool;
            GetRootEntriesMethod = getRootEntriesMethod;
        }

        public void Init()
        {
            ScrollPool.Initialize(this);
        }

        public void Clear()
        {
            this.cachedTransforms.Clear();
            displayIndex = 0;
            autoExpandedIDs.Clear();
            expandedInstanceIDs.Clear();
            this.ScrollPool.Refresh(true, true);
        }

        public bool IsCellExpanded(int instanceID)
        {
            return Filtering ? autoExpandedIDs.Contains(instanceID)
                             : expandedInstanceIDs.Contains(instanceID);
        }

        public void JumpAndExpandToTransform(Transform transform)
        {
            // make sure all parents of the object are expanded
            var parent = transform.parent;
            while (parent)
            {
                int pid = parent.GetInstanceID();
                if (!expandedInstanceIDs.Contains(pid))
                    expandedInstanceIDs.Add(pid);

                parent = parent.parent;
            }

            // Refresh cached transforms (no UI rebuild yet)
            RefreshData(false, false, false);

            int transformID = transform.GetInstanceID();

            // find the index of our transform in the list and jump to it
            int idx;
            for (idx = 0; idx < cachedTransforms.Count; idx++)
            {
                var cache = (CachedTransform)cachedTransforms[idx];
                if (cache.InstanceID == transformID)
                    break;
            }

            ScrollPool.JumpToIndex(idx, OnCellJumpedTo);
        }

        private void OnCellJumpedTo(TransformCell cell)
        {
            RuntimeHelper.StartCoroutine(HighlightCellCoroutine(cell));
        }

        private IEnumerator HighlightCellCoroutine(TransformCell cell)
        {
            var button = cell.NameButton.Component;
            button.StartColorTween(new Color(0.2f, 0.3f, 0.2f), false);

            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < 1.5f)
                yield return null;

            button.OnDeselect(null);
        }

        public void Rebuild()
        {
            autoExpandedIDs.Clear();
            expandedInstanceIDs.Clear();

            RefreshData(true, true, true);
        }

        public void RefreshData(bool andRefreshUI, bool jumpToTop, bool stopExistingCoroutine)
        {
            if (refreshCoroutine != null)
            {
                if (stopExistingCoroutine)
                {
                    RuntimeHelper.StopCoroutine(refreshCoroutine);
                    refreshCoroutine = null;
                }
                else
                    return;
            }

            visited.Clear();
            displayIndex = 0;
            needRefresh = false;
            traversedThisFrame.Reset();
            traversedThisFrame.Start();

            IEnumerable<GameObject> rootObjects = GetRootEntriesMethod.Invoke();

            refreshCoroutine = RuntimeHelper.StartCoroutine(RefreshCoroutine(rootObjects, andRefreshUI, jumpToTop));
        }

        private IEnumerator RefreshCoroutine(IEnumerable<GameObject> rootObjects, bool andRefreshUI, bool jumpToTop)
        {
            var thisCoro = refreshCoroutine;
            foreach (var gameObj in rootObjects)
            {
                if (gameObj)
                {
                    var enumerator = Traverse(gameObj.transform);
                    while (enumerator.MoveNext())
                        yield return enumerator.Current;
                }
            }

            // Prune displayed transforms that we didnt visit in that traverse
            for (int i = cachedTransforms.Count - 1; i >= 0; i--)
            {
                var cached = (CachedTransform)cachedTransforms[i];
                if (!visited.Contains(cached.InstanceID))
                {
                    cachedTransforms.RemoveAt(i);
                    needRefresh = true;
                }
            }

            if (andRefreshUI && needRefresh)
                ScrollPool.Refresh(true, jumpToTop);

            prevDisplayIndex = displayIndex;
        }

        private IEnumerator Traverse(Transform transform, CachedTransform parent = null, int depth = 0)
        {
            // Let's only tank 2ms of each frame (60->53fps)
            if (traversedThisFrame.ElapsedMilliseconds > 2)
            {
                yield return null;
                traversedThisFrame.Reset();
                traversedThisFrame.Start();
            }

            int instanceID = transform.GetInstanceID();

            if (visited.Contains(instanceID))
                yield break;

            if (Filtering)
            {
                if (!FilterHierarchy(transform))
                    yield break;

                visited.Add(instanceID);

                if (!autoExpandedIDs.Contains(instanceID))
                    autoExpandedIDs.Add(instanceID);
            }
            else
                visited.Add(instanceID);

            CachedTransform cached;
            if (cachedTransforms.Contains(instanceID))
            {
                cached = (CachedTransform)cachedTransforms[(object)instanceID];
                int prevSiblingIdx = cached.SiblingIndex;
                if (cached.Update(transform, depth))
                {
                    needRefresh = true;

                    // If the sibling index changed, we need to shuffle it in our cached transforms list.
                    if (prevSiblingIdx != cached.SiblingIndex)
                    {
                        cachedTransforms.Remove(instanceID);
                        if (cachedTransforms.Count <= displayIndex)
                            cachedTransforms.Add(instanceID, cached);
                        else
                            cachedTransforms.Insert(displayIndex, instanceID, cached);
                    }
                }
            }
            else
            {
                needRefresh = true;
                cached = new CachedTransform(this, transform, depth, parent);
                if (cachedTransforms.Count <= displayIndex)
                    cachedTransforms.Add(instanceID, cached);
                else
                    cachedTransforms.Insert(displayIndex, instanceID, cached);
            }

            displayIndex++;

            if (IsCellExpanded(instanceID) && cached.Value.childCount > 0)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    var enumerator = Traverse(transform.GetChild(i), cached, depth + 1);
                    while (enumerator.MoveNext())
                        yield return enumerator.Current;
                }
            }
        }

        private bool FilterHierarchy(Transform obj)
        {
            if (obj.name.ContainsIgnoreCase(currentFilter))
                return true;

            if (obj.childCount <= 0)
                return false;

            for (int i = 0; i < obj.childCount; i++)
                if (FilterHierarchy(obj.GetChild(i)))
                    return true;

            return false;
        }

        public void SetCell(TransformCell cell, int index)
        {
            if (index < cachedTransforms.Count)
            {
                cell.ConfigureCell((CachedTransform)cachedTransforms[index], index);
                if (Filtering)
                {
                    if (cell.cachedTransform.Name.ContainsIgnoreCase(currentFilter))
                        cell.NameButton.ButtonText.color = Color.green;
                }
            }
            else
                cell.Disable();
        }

        public void OnCellBorrowed(TransformCell cell)
        {
            cell.OnExpandToggled += OnCellExpandToggled;
            cell.OnGameObjectClicked += OnGameObjectClicked;
            cell.OnEnableToggled += OnCellEnableToggled;
        }

        private void OnGameObjectClicked(GameObject obj)
        {
            if (OnClickOverrideHandler != null)
                OnClickOverrideHandler.Invoke(obj);
            else
                InspectorManager.Inspect(obj);
        }

        public void OnCellExpandToggled(CachedTransform cache)
        {
            var instanceID = cache.InstanceID;
            if (expandedInstanceIDs.Contains(instanceID))
                expandedInstanceIDs.Remove(instanceID);
            else
                expandedInstanceIDs.Add(instanceID);

            RefreshData(true, false, true);
        }

        public void OnCellEnableToggled(CachedTransform cache)
        {
            cache.Value.gameObject.SetActive(!cache.Value.gameObject.activeSelf);

            RefreshData(true, false, true);
        }
    }
}
