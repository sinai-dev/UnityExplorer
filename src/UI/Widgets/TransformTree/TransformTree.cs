using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

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
        internal readonly OrderedDictionary cachedTransforms = new OrderedDictionary();

        // for keeping track of which actual transforms are expanded or not, outside of the cache data.
        private readonly HashSet<int> expandedInstanceIDs = new HashSet<int>();
        private readonly HashSet<int> autoExpandedIDs = new HashSet<int>();

        private readonly HashSet<int> visited = new HashSet<int>();
        private bool needRefresh;
        private int displayIndex;

        public int ItemCount => cachedTransforms.Count;

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

        public void OnCellBorrowed(TransformCell cell)
        {
            cell.OnExpandToggled += ToggleExpandCell;
            cell.OnGameObjectClicked += OnGameObjectClicked;
        }

        private void OnGameObjectClicked(GameObject obj)
        {
            if (OnClickOverrideHandler != null)
                OnClickOverrideHandler.Invoke(obj);
            else
                InspectorManager.Inspect(obj);
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
            RefreshData(false);

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
            RuntimeProvider.Instance.StartCoroutine(HighlightCellCoroutine(cell));
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

            RefreshData(true, true);
        }

        public void RefreshData(bool andReload = false, bool jumpToTop = false)
        {
            visited.Clear();
            displayIndex = 0;
            needRefresh = false;

            var rootObjects = GetRootEntriesMethod.Invoke();

            //int displayIndex = 0;
            foreach (var obj in rootObjects)
                if (obj) Traverse(obj.transform);

            // Prune displayed transforms that we didnt visit in that traverse
            for (int i = cachedTransforms.Count - 1; i >= 0; i--)
            {
                var obj = (CachedTransform)cachedTransforms[i];
                if (!visited.Contains(obj.InstanceID))
                {
                    cachedTransforms.Remove(obj.InstanceID);
                    needRefresh = true;
                }
            }

            if (!needRefresh)
                return;

            //displayedObjects.Clear();

            if (andReload)
            {
                if (!jumpToTop)
                    ScrollPool.Refresh(true);
                else
                    ScrollPool.Refresh(true, true);
            }
        }

        private void Traverse(Transform transform, CachedTransform parent = null, int depth = 0)
        {
            int instanceID = transform.GetInstanceID();

            if (visited.Contains(instanceID))
                return;

            if (Filtering)
            {
                if (!FilterHierarchy(transform))
                    return;

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
                if (cached.Update(transform, depth))
                    needRefresh = true;
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
                    Traverse(transform.GetChild(i), cached, depth + 1);
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
                    {
                        cell.NameButton.ButtonText.color = Color.green;
                    }
                }
            }
            else
                cell.Disable();
        }

        public void ToggleExpandCell(CachedTransform cache)
        {
            var instanceID = cache.InstanceID;
            if (expandedInstanceIDs.Contains(instanceID))
                expandedInstanceIDs.Remove(instanceID);
            else
                expandedInstanceIDs.Add(instanceID);

            RefreshData(true);
        }
    }
}
