using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.ObjectPool;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Widgets
{
    public class TransformTree : IPoolDataSource<TransformCell>
    {
        public Func<IEnumerable<GameObject>> GetRootEntriesMethod;

        internal ScrollPool<TransformCell> ScrollPool;

        // Using an OrderedDictionary because we need constant-time lookup of both key and index.
        /// <summary>
        /// Key: UnityEngine.Transform instance ID<br/>
        /// Value: CachedTransform
        /// </summary>
        private readonly OrderedDictionary displayedObjects = new OrderedDictionary();

        // for keeping track of which actual transforms are expanded or not, outside of the cache data.
        private readonly HashSet<int> expandedInstanceIDs = new HashSet<int>();
        private readonly HashSet<int> autoExpandedIDs = new HashSet<int>();

        public int ItemCount => displayedObjects.Count;

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

        public TransformTree(ScrollPool<TransformCell> scrollPool)
        {
            ScrollPool = scrollPool;
        }

        public void Init()
        {
            ScrollPool.Initialize(this);
        }

        //public void DisableCell(TransformCell cell, int index) => cell.Disable();


        public bool IsCellExpanded(int instanceID)
        {
            return Filtering ? autoExpandedIDs.Contains(instanceID)
                             : expandedInstanceIDs.Contains(instanceID);
        }

        public void Rebuild()
        {
            autoExpandedIDs.Clear();
            expandedInstanceIDs.Clear();

            RefreshData(true, true);
        }

        private readonly HashSet<int> visited = new HashSet<int>();
        private bool needRefresh;
        private int displayIndex;

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
            for (int i = displayedObjects.Count - 1; i >= 0; i--)
            {
                var obj = (CachedTransform)displayedObjects[i];
                if (!visited.Contains(obj.InstanceID))
                {
                    displayedObjects.Remove(obj.InstanceID);
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
            visited.Add(instanceID);

            if (Filtering)
            {
                if (!FilterHierarchy(transform))
                    return;

                if (!autoExpandedIDs.Contains(instanceID))
                    autoExpandedIDs.Add(instanceID);
            }

            CachedTransform cached;
            if (displayedObjects.Contains(instanceID))
            {
                cached = (CachedTransform)displayedObjects[(object)instanceID];
                if (cached.Update(transform, depth))
                    needRefresh = true;
            }
            else
            {
                needRefresh = true;
                cached = new CachedTransform(this, transform, depth, parent);
                if (displayedObjects.Count <= displayIndex)
                    displayedObjects.Add(instanceID, cached);
                else
                    displayedObjects.Insert(displayIndex, instanceID, cached);
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
            if (index < displayedObjects.Count)
                cell.ConfigureCell((CachedTransform)displayedObjects[index], index);
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

        public void OnCellBorrowed(TransformCell cell)
        {
            cell.OnExpandToggled += ToggleExpandCell;
        }

        public void ReleaseCell(TransformCell cell)
        {
            cell.OnExpandToggled -= ToggleExpandCell;
        }
    }
}
