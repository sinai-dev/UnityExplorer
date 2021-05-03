using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Inspectors.CacheObject;
using UnityExplorer.UI.Inspectors.CacheObject.Views;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Inspectors.IValues
{
    public class InteractiveList : InteractiveValue, IPoolDataSource<CacheListEntryCell>, ICacheObjectController
    {
        CacheObjectBase ICacheObjectController.ParentCacheObject => this.CurrentOwner;
        object ICacheObjectController.Target => this.CurrentOwner.Value;
        public Type TargetType { get; private set; }

        public override bool CanWrite => RefIList != null && !RefIList.IsReadOnly;

        public Type EntryType;
        public IEnumerable RefIEnumerable;
        public IList RefIList;

        public int ItemCount => values.Count;
        private readonly List<object> values = new List<object>();
        private readonly List<CacheListEntry> cachedEntries = new List<CacheListEntry>();

        public ScrollPool<CacheListEntryCell> ListScrollPool { get; private set; }

        public Text TopLabel;

        public override void OnBorrowed(CacheObjectBase owner)
        {
            base.OnBorrowed(owner);
        }

        public override void ReleaseFromOwner()
        {
            base.ReleaseFromOwner();

            ClearAndRelease();
        }

        private void ClearAndRelease()
        {
            values.Clear();

            foreach (var entry in cachedEntries)
                entry.ReleasePooledObjects();

            cachedEntries.Clear();
        }

        public override void SetValue(object value)
        {
            if (value == null)
            {
                // should never be null
                if (values.Any())
                    ClearAndRelease();
            }
            else
            {
                var type = value.GetActualType();
                if (type.IsGenericType)
                    EntryType = type.GetGenericArguments()[0];
                else
                    EntryType = typeof(object);

                CacheEntries(value);

                TopLabel.text = $"[{cachedEntries.Count}] {SignatureHighlighter.ParseFullType(type, false)}";
            }

            //this.ScrollPoolLayout.minHeight = Math.Min(400f, 35f * values.Count);
            this.ListScrollPool.Refresh(true, false);
        }

        private void CacheEntries(object value)
        {
            RefIEnumerable = value as IEnumerable;
            RefIList = value as IList;

            if (RefIEnumerable == null)
            {
                // todo il2cpp
                return;
            }

            values.Clear();
            int idx = 0;
            foreach (var entry in RefIEnumerable)
            {
                values.Add(entry);

                // If list count increased, create new cache entries
                CacheListEntry cache;
                if (idx >= cachedEntries.Count)
                {
                    cache = new CacheListEntry();
                    cache.SetListOwner(this, idx);
                    cachedEntries.Add(cache);
                }
                else
                    cache = cachedEntries[idx];

                cache.SetFallbackType(this.EntryType);
                cache.SetValueFromSource(entry);
                idx++;
            }

            // Remove excess cached entries if list count decreased
            if (cachedEntries.Count > values.Count)
            {
                for (int i = cachedEntries.Count - 1; i >= values.Count; i--)
                {
                    var cache = cachedEntries[i];
                    if (cache.CellView != null)
                    {
                        cache.CellView.Occupant = null;
                        cache.CellView = null;
                    }
                    cache.ReleasePooledObjects();
                    cachedEntries.RemoveAt(i);
                }
            }
        }

        // List entry scroll pool

        public override void SetLayout()
        {
            var minHeight = 5f;

            foreach (var cell in ListScrollPool.CellPool)
            {
                if (cell.Enabled)
                    minHeight += cell.Rect.rect.height;
            }

            this.scrollLayout.minHeight = Math.Min(InspectorPanel.CurrentPanelHeight - 400f, minHeight);
        }

        public void OnCellBorrowed(CacheListEntryCell cell)
        {

        }

        public void SetCell(CacheListEntryCell cell, int index)
        {
            if (index < 0 || index >= cachedEntries.Count)
            {
                if (cell.Occupant != null)
                {
                    cell.Occupant.CellView = null;
                    cell.Occupant = null;
                }

                cell.Disable();
                return;
            }

            var entry = cachedEntries[index];

            if (entry != cell.Occupant)
            {
                if (cell.Occupant != null)
                {
                    cell.Occupant.HideIValue();
                    cell.Occupant.CellView = null;
                    cell.Occupant = null;
                }

                cell.Occupant = entry;
                entry.CellView = cell;
            }

            entry.SetCell(cell);
        }

        private LayoutElement scrollLayout;

        public override GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(parent, "InteractiveList", true, true, true, true, 6, new Vector4(10, 3, 15, 4),
                new Color(0.05f, 0.05f, 0.05f));
            UIFactory.SetLayoutElement(UIRoot, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 600);
            UIRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Entries label

            TopLabel = UIFactory.CreateLabel(UIRoot, "EntryLabel", "not set", TextAnchor.MiddleLeft, fontSize: 16);
            TopLabel.horizontalOverflow = HorizontalWrapMode.Overflow;

            // entry scroll pool

            ListScrollPool = UIFactory.CreateScrollPool<CacheListEntryCell>(UIRoot, "EntryList", out GameObject scrollObj,
                out GameObject _, new Color(0.09f, 0.09f, 0.09f));
            UIFactory.SetLayoutElement(scrollObj, minHeight: 400, flexibleHeight: 0);
            ListScrollPool.Initialize(this, SetLayout);
            scrollLayout = scrollObj.GetComponent<LayoutElement>();

            return UIRoot;
        }
    }
}