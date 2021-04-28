using System;
using System.Collections;
using System.Collections.Generic;
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

        public bool Filtering => !string.IsNullOrEmpty(currentFilter);
        private bool wasFiltering;

        public string CurrentFilter
        {
            get => currentFilter;
            set
            {
                currentFilter = value?.ToLower() ?? "";
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

        internal ScrollPool<TransformCell> ScrollPool;

        internal readonly List<CachedTransform> displayedObjects = new List<CachedTransform>();

        private readonly Dictionary<int, CachedTransform> objectCache = new Dictionary<int, CachedTransform>();

        private readonly HashSet<int> expandedInstanceIDs = new HashSet<int>();
        private readonly HashSet<int> autoExpandedIDs = new HashSet<int>();

        public int ItemCount => displayedObjects.Count;

        public TransformTree(ScrollPool<TransformCell> scrollPool)
        {
            ScrollPool = scrollPool;
        }

        public void Init()
        {
            ScrollPool.Initialize(this);
        }

        public void DisableCell(TransformCell cell, int index) => cell.Disable();


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

        public void RefreshData(bool andReload = false, bool hardReload = false)
        {
            displayedObjects.Clear();

            var rootObjects = GetRootEntriesMethod.Invoke();

            foreach (var obj in rootObjects)
            {
                if (obj)
                    Traverse(obj.transform);
            }

            if (andReload)
            {
                if (!hardReload)
                    ScrollPool.RefreshCells(true);
                else
                    ScrollPool.Rebuild();
            }
        }

        private void Traverse(Transform transform, CachedTransform parent = null, int depth = 0)
        {
            int instanceID = transform.GetInstanceID();

            if (Filtering)
            {
                //auto - expand to show results: works, but then we need to collapse after the search ends.

                if (FilterHierarchy(transform))
                {
                    if (!autoExpandedIDs.Contains(instanceID))
                        autoExpandedIDs.Add(instanceID);
                }
                else
                    return;
            }

            CachedTransform cached;
            if (objectCache.ContainsKey(instanceID))
            {
                cached = objectCache[instanceID];
                cached.Update(transform, depth);
            }
            else
            {
                cached = new CachedTransform(this, transform, depth, parent);
                objectCache.Add(instanceID, cached);
            }

            displayedObjects.Add(cached);

            if (IsCellExpanded(instanceID) && cached.Value.childCount > 0)
            {
                for (int i = 0; i < transform.childCount; i++)
                    Traverse(transform.GetChild(i), cached, depth + 1);
            }
        }

        private bool FilterHierarchy(Transform obj)
        {
            if (obj.name.ToLower().Contains(currentFilter))
                return true;

            if (obj.childCount <= 0)
                return false;

            for (int i = 0; i < obj.childCount; i++)
                if (FilterHierarchy(obj.GetChild(i)))
                    return true;

            return false;
        }

        public void SetCell(TransformCell iCell, int index)
        {
            var cell = iCell as TransformCell;

            if (index < displayedObjects.Count)
                cell.ConfigureCell(displayedObjects[index], index);
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

        public void OnCellReturned(TransformCell cell)
        {
            cell.OnExpandToggled -= ToggleExpandCell;
        }
    }
}
