using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Widgets.InfiniteScroll;

namespace UnityExplorer.UI.Widgets
{
    public class TransformTree : IListDataSource
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

        internal InfiniteScrollRect Scroller;

        internal readonly List<CachedTransform> displayedObjects = new List<CachedTransform>();

        private readonly Dictionary<int, CachedTransform> objectCache = new Dictionary<int, CachedTransform>();

        private readonly HashSet<int> expandedInstanceIDs = new HashSet<int>();
        private readonly HashSet<int> autoExpandedIDs = new HashSet<int>();

        public int ItemCount => displayedObjects.Count;

        public TransformTree(InfiniteScrollRect infiniteScroller)
        {
            Scroller = infiniteScroller;
        }

        public void Init()
        {
            RuntimeProvider.Instance.StartCoroutine(InitCoroutine());

            //test
            var root = new GameObject("StressTest");
            for (int i = 0; i < 100; i++)
            {
                var obj = new GameObject("GameObject " + i);
                obj.transform.parent = root.transform;
                for (int j = 0; j < 100; j++)
                {
                    new GameObject("Child " + j).transform.parent = obj.transform;
                }
            }
        }

        private IEnumerator InitCoroutine()
        {
            yield return null;

            RefreshData();
            Scroller.DataSource = this;
            Scroller.Initialize(this);
        }

        public ICell CreateCell(RectTransform cellTransform)
        {
            var nameButton = cellTransform.Find("NameButton").GetComponent<Button>();
            var expandButton = cellTransform.Find("ExpandButton").GetComponent<Button>();
            var spacer = cellTransform.Find("Spacer").GetComponent<LayoutElement>();

            return new TransformCell(this, cellTransform.gameObject, nameButton, expandButton, spacer);
        }

        public bool IsCellExpanded(int instanceID)
        {
            return Filtering ? autoExpandedIDs.Contains(instanceID)
                             : expandedInstanceIDs.Contains(instanceID);
        }

        public void RefreshData(bool andReload = false)
        {
            displayedObjects.Clear();

            var rootObjects = GetRootEntriesMethod.Invoke();

            foreach (var obj in rootObjects)
            {
                if (obj)
                    Traverse(obj.transform);
            }

            if (andReload)
                Scroller.Refresh();
        }

        private void Traverse(Transform transform, CachedTransform parent = null)
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
                cached.Update(transform);
            }
            else
            {
                cached = new CachedTransform(this, transform, parent);
                objectCache.Add(instanceID, cached);
            }

            displayedObjects.Add(cached);

            if (IsCellExpanded(instanceID) && cached.Value.childCount > 0)
            {
                for (int i = 0; i < transform.childCount; i++)
                    Traverse(transform.GetChild(i), cached);
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

        public void SetCell(ICell iCell, int index)
        {
            var cell = iCell as TransformCell;

            if (index < displayedObjects.Count)
                cell.ConfigureCell(displayedObjects[index], index);
            else
                cell.Disable();
        }

        public void ToggleExpandCell(TransformCell cell)
        {
            var instanceID = cell.cachedTransform.InstanceID;
            if (expandedInstanceIDs.Contains(instanceID))
                expandedInstanceIDs.Remove(instanceID);
            else
                expandedInstanceIDs.Add(instanceID);

            RefreshData(true);
        }
    }
}
