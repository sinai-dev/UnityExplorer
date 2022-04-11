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

        // IMPORTANT CAVEAT WITH OrderedDictionary:
        // While the performance is mostly good, there are two methods we should NEVER use:
        // - Remove(object)
        // - set_Item[object]
        // These two methods have extremely bad performance due to using IndexOfKey(), which iterates the whole dictionary.
        // Currently we do not use either of these methods, so everything should be constant time lookups.
        // We DO make use of get_Item[object], get_Item[index], Add, Insert, Contains and RemoveAt, which OrderedDictionary meets our needs for.
        /// <summary>
        /// Key: UnityEngine.Transform instance ID<br/>
        /// Value: CachedTransform
        /// </summary>
        internal readonly OrderedDictionary cachedTransforms = new();

        // for keeping track of which actual transforms are expanded or not, outside of the cache data.
        private readonly HashSet<int> expandedInstanceIDs = new();
        private readonly HashSet<int> autoExpandedIDs = new();

        // state for Traverse parse
        private readonly HashSet<int> visited = new();
        private bool needRefreshUI;
        private int displayIndex;
        int prevDisplayIndex;

        private Coroutine refreshCoroutine;
        private readonly Stopwatch traversedThisFrame = new();

        // ScrollPool item count. PrevDisplayIndex is the highest index + 1 from our last traverse.
        public int ItemCount => prevDisplayIndex;

        // Search filter
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

        public TransformTree(ScrollPool<TransformCell> scrollPool, Func<IEnumerable<GameObject>> getRootEntriesMethod)
        {
            ScrollPool = scrollPool;
            GetRootEntriesMethod = getRootEntriesMethod;
        }

        // Initialize and reset

        // Must be called externally from owner of this TransformTree
        public void Init()
        {
            ScrollPool.Initialize(this);
        }

        // Called to completely reset the tree, ie. switching inspected GameObject
        public void Rebuild()
        {
            autoExpandedIDs.Clear();
            expandedInstanceIDs.Clear();

            RefreshData(true, true, true, false);
        }

        // Called to completely wipe our data (ie, GameObject inspector returning to pool)
        public void Clear()
        {
            this.cachedTransforms.Clear();
            displayIndex = 0;
            autoExpandedIDs.Clear();
            expandedInstanceIDs.Clear();
            this.ScrollPool.Refresh(true, true);
            if (refreshCoroutine != null)
            {
                RuntimeHelper.StopCoroutine(refreshCoroutine);
                refreshCoroutine = null;
            }
        }

        // Checks if the given Instance ID is expanded or not
        public bool IsTransformExpanded(int instanceID)
        {
            return Filtering ? autoExpandedIDs.Contains(instanceID)
                             : expandedInstanceIDs.Contains(instanceID);
        }

        // Jumps to a specific Transform in the tree and highlights it.
        public void JumpAndExpandToTransform(Transform transform)
        {
            // make sure all parents of the object are expanded
            Transform parent = transform.parent;
            while (parent)
            {
                int pid = parent.GetInstanceID();
                if (!expandedInstanceIDs.Contains(pid))
                    expandedInstanceIDs.Add(pid);

                parent = parent.parent;
            }

            // Refresh cached transforms (no UI rebuild yet).
            // Stop existing coroutine and do it oneshot.
            RefreshData(false, false, true, true);

            int transformID = transform.GetInstanceID();

            // find the index of our transform in the list and jump to it
            int idx;
            for (idx = 0; idx < cachedTransforms.Count; idx++)
            {
                CachedTransform cache = (CachedTransform)cachedTransforms[idx];
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
            UnityEngine.UI.Button button = cell.NameButton.Component;
            button.StartColorTween(new Color(0.2f, 0.3f, 0.2f), false);

            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < 1.5f)
                yield return null;

            button.OnDeselect(null);
        }

        // Perform a Traverse and optionally refresh the ScrollPool as well.
        // If oneShot, then this happens instantly with no yield.
        public void RefreshData(bool andRefreshUI, bool jumpToTop, bool stopExistingCoroutine, bool oneShot)
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
            needRefreshUI = false;
            traversedThisFrame.Reset();
            traversedThisFrame.Start();

            refreshCoroutine = RuntimeHelper.StartCoroutine(RefreshCoroutine(andRefreshUI, jumpToTop, oneShot));
        }

        IEnumerator RefreshCoroutine(bool andRefreshUI, bool jumpToTop, bool oneShot)
        {
            // Instead of doing string.IsNullOrEmpty(CurrentFilter) many times, let's just do it once per update.
            bool filtering = Filtering;

            IEnumerable<GameObject> rootObjects = GetRootEntriesMethod();
            foreach (GameObject gameObj in rootObjects)
            {
                if (!gameObj)
                    continue;

                IEnumerator enumerator = Traverse(gameObj.transform, null, 0, oneShot, filtering);
                while (enumerator.MoveNext())
                {
                    if (!oneShot)
                        yield return enumerator.Current;
                }
            }

            // Prune displayed transforms that we didnt visit in that traverse
            for (int i = cachedTransforms.Count - 1; i >= 0; i--)
            {
                if (traversedThisFrame.ElapsedMilliseconds > 2)
                {
                    yield return null;
                    traversedThisFrame.Reset();
                    traversedThisFrame.Start();
                }

                CachedTransform cached = (CachedTransform)cachedTransforms[i];
                if (!visited.Contains(cached.InstanceID))
                {
                    cachedTransforms.RemoveAt(i);
                    needRefreshUI = true;
                }
            }

            if (andRefreshUI && needRefreshUI)
                ScrollPool.Refresh(true, jumpToTop);

            prevDisplayIndex = displayIndex;
            refreshCoroutine = null;
        }

        // Recursive method to check a Transform and its children (if expanded).
        // Parent and depth can be null/default.
        private IEnumerator Traverse(Transform transform, CachedTransform parent, int depth, bool oneShot, bool filtering)
        {
            if (traversedThisFrame.ElapsedMilliseconds > 2)
            {
                yield return null;
                traversedThisFrame.Reset();
                traversedThisFrame.Start();
            }

            int instanceID = transform.GetInstanceID();

            // Unlikely, but since this method is async it could theoretically happen in extremely rare circumstances
            if (visited.Contains(instanceID))
                yield break;

            if (filtering)
            {
                if (!FilterHierarchy(transform))
                    yield break;

                if (!autoExpandedIDs.Contains(instanceID))
                    autoExpandedIDs.Add(instanceID);
            }

            visited.Add(instanceID);

            CachedTransform cached;
            if (cachedTransforms.Contains(instanceID))
            {
                cached = (CachedTransform)cachedTransforms[(object)instanceID];
                int prevSiblingIdx = cached.SiblingIndex;
                if (cached.Update(transform, depth))
                {
                    needRefreshUI = true;

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
                needRefreshUI = true;
                cached = new CachedTransform(this, transform, depth, parent);
                if (cachedTransforms.Count <= displayIndex)
                    cachedTransforms.Add(instanceID, cached);
                else
                    cachedTransforms.Insert(displayIndex, instanceID, cached);
            }

            displayIndex++;

            if (IsTransformExpanded(instanceID) && cached.Value.childCount > 0)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    IEnumerator enumerator = Traverse(transform.GetChild(i), cached, depth + 1, oneShot, filtering);
                    while (enumerator.MoveNext())
                    {
                        if (!oneShot)
                            yield return enumerator.Current;
                    }
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
                cell.ConfigureCell((CachedTransform)cachedTransforms[index]);
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
            int instanceID = cache.InstanceID;
            if (expandedInstanceIDs.Contains(instanceID))
                expandedInstanceIDs.Remove(instanceID);
            else
                expandedInstanceIDs.Add(instanceID);

            RefreshData(true, false, true, true);
        }

        public void OnCellEnableToggled(CachedTransform cache)
        {
            cache.Value.gameObject.SetActive(!cache.Value.gameObject.activeSelf);

            RefreshData(true, false, true, true);
        }
    }
}
