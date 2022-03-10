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
        // Currently we do not use either of these methods, so everything should be constant time hash lookups.
        // We DO make use of get_Item[object], get_Item[index], Add, Insert and RemoveAt, which OrderedDictionary perfectly meets our needs for.
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
            var parent = transform.parent;
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

            IEnumerable<GameObject> rootObjects = GetRootEntriesMethod.Invoke();

            refreshCoroutine = RuntimeHelper.StartCoroutine(RefreshCoroutine(rootObjects, andRefreshUI, jumpToTop, oneShot));
        }

        // Coroutine for batched updates, max 2000 gameobjects per frame so FPS doesn't get tanked when there is like 100k gameobjects.
        // if "oneShot", then this will NOT be batched (if we need an immediate full update).
        IEnumerator RefreshCoroutine(IEnumerable<GameObject> rootObjects, bool andRefreshUI, bool jumpToTop, bool oneShot)
        {
            foreach (var gameObj in rootObjects)
            {
                if (gameObj)
                {
                    var enumerator = Traverse(gameObj.transform, null, 0, oneShot);
                    while (enumerator.MoveNext())
                    {
                        if (!oneShot)
                            yield return enumerator.Current;
                    }
                }
            }

            // Prune displayed transforms that we didnt visit in that traverse
            for (int i = cachedTransforms.Count - 1; i >= 0; i--)
            {
                var cached = (CachedTransform)cachedTransforms[i];
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
        private IEnumerator Traverse(Transform transform, CachedTransform parent, int depth, bool oneShot)
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
                    var enumerator = Traverse(transform.GetChild(i), cached, depth + 1, oneShot);
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

            RefreshData(true, false, true, false);
        }

        public void OnCellEnableToggled(CachedTransform cache)
        {
            cache.Value.gameObject.SetActive(!cache.Value.gameObject.activeSelf);

            RefreshData(true, false, true, false);
        }
    }
}
