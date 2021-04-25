using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core;
using UnityExplorer.Core.Config;
using UnityExplorer.UI;
using UnityExplorer.UI.CacheObject;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.InteractiveValues
{
    public class InteractiveEnumerable : InteractiveValue, IPoolDataSource
    {
        // IPoolDataSource

        public ScrollPool ScrollPool;
        public GameObject InactiveHolder;
        internal LayoutElement listLayout;

        public int ItemCount => m_entries?.Count ?? 0;

        public void SetCell(ICell icell, int index)
        {
            var cell = icell as CellViewHolder;

            if (index < 0 || index >= ItemCount)
            {
                var existing = cell.DisableContent();
                if (existing)
                    existing.transform.SetParent(InactiveHolder.transform, false);
                return;
            }

            var cache = m_entries[index];
            cache.Enable();

            var prev = cell.SetContent(cache.UIRoot);
            if (prev)
                prev.transform.SetParent(InactiveHolder.transform, false);
        }

        public void DisableCell(ICell cell, int index)
        {
            var content = (cell as CellViewHolder).DisableContent();
            if (content)
                content.transform.SetParent(InactiveHolder.transform, false);
        }

        //public void SetCell(ICell cell, int index)
        //{
        //    var root = (cell as CellViewHolder).UIRoot;

        //    if (index < 0 || index >= ItemCount)
        //    {
        //        DisableContent(root);
        //        cell.Disable();
        //        return;
        //    }

        //    var cache = m_entries[index];
        //    cache.Enable();

        //    var content = cache.UIRoot;

        //    if (content.transform.parent.ReferenceEqual(root.transform))
        //        return;

        //    DisableContent(root);

        //    content.transform.SetParent(root.transform, false);
        //}

        //public void DisableCell(ICell cell, int index)
        //{
        //    var root = (cell as CellViewHolder).UIRoot;
        //    DisableContent(root);
        //    cell.Disable();
        //}

        //private void DisableContent(GameObject cellRoot)
        //{
        //    if (cellRoot.transform.childCount > 0 && cellRoot.transform.GetChild(0) is Transform existing)
        //        existing.transform.SetParent(InactiveHolder.transform, false);
        //}

        public ICell CreateCell(RectTransform cellTransform) => new CellViewHolder(cellTransform.gameObject);

        public int GetRealIndexOfTempIndex(int tempIndex) => throw new NotImplementedException("Filtering not supported");

        // InteractiveEnumerable

        public InteractiveEnumerable(object value, Type valueType) : base(value, valueType) 
        {
            if (valueType.IsGenericType)
                m_baseEntryType = valueType.GetGenericArguments()[0];
            else
                m_baseEntryType = typeof(object);
        }

        public override bool WantInspectBtn => false;
        public override bool HasSubContent => true;
        public override bool SubContentWanted
        {
            get
            {
                if (m_recacheWanted && Value != null)
                    return true;
                else return m_entries.Count > 0;
            }
        }


        internal IEnumerable RefIEnumerable;
        internal IList RefIList;

        internal readonly Type m_baseEntryType;

        internal readonly List<CacheEnumerated> m_entries = new List<CacheEnumerated>();
        internal readonly CacheEnumerated[] m_displayedEntries = new CacheEnumerated[ConfigManager.Default_Page_Limit.Value];
        internal bool m_recacheWanted = true;

        public override void OnValueUpdated()
        {
            RefIEnumerable = Value as IEnumerable;
            RefIList = Value as IList;

            if (m_subContentParent && m_subContentParent.activeSelf)
            {
                ToggleSubcontent();
                //GetCacheEntries();
                //RefreshDisplay();
            }

            m_recacheWanted = true;

            base.OnValueUpdated();
        }

        public override void OnException(CacheMember member)
        {
            base.OnException(member);
        }

        public override void RefreshUIForValue()
        {
            GetDefaultLabel();

            if (Value != null)
            {
                string count = "?";
                if (m_recacheWanted && RefIList != null)
                    count = RefIList.Count.ToString();
                else if (!m_recacheWanted)
                    count = m_entries.Count.ToString();

                m_baseLabel.text = $"[{count}] {m_richValueType}";
            }
            else
            {
                m_baseLabel.text = DefaultLabel;
            }
        }

        public void GetCacheEntries()
        {
            if (m_entries.Any())
            {
                foreach (var entry in m_entries)
                    entry.Destroy();

                m_entries.Clear();
            }

            if (RefIEnumerable == null && Value != null)
                RefIEnumerable = RuntimeProvider.Instance.Reflection.EnumerateEnumerable(Value);

            if (RefIEnumerable != null)
            {
                int index = 0;
                foreach (var entry in RefIEnumerable)
                {
                    var cache = new CacheEnumerated(index, this, RefIList, this.InactiveHolder);
                    cache.CreateIValue(entry, m_baseEntryType);
                    m_entries.Add(cache);

                    cache.Disable();

                    index++;
                }
            }

            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            ScrollPool.RefreshCells(true);

            listLayout.minHeight = Math.Min(500f, m_entries.Count * 32f);
        }

        internal override void OnToggleSubcontent(bool active)
        {
            base.OnToggleSubcontent(active);

            if (active && m_recacheWanted)
            {
                m_recacheWanted = false;
                GetCacheEntries();
                RefreshUIForValue();
            }

            RefreshDisplay();
        }

        public override void ConstructUI(GameObject parent, GameObject subGroup)
        {
            base.ConstructUI(parent, subGroup);

            InactiveHolder = new GameObject("InactiveHolder");
            InactiveHolder.transform.SetParent(parent.transform, false);
            InactiveHolder.SetActive(false);
        }

        public override void ConstructSubcontent()
        {
            base.ConstructSubcontent();

            ScrollPool = UIFactory.CreateScrollPool(m_subContentParent, "ListEntries", out GameObject scrollRoot, out GameObject scrollContent,
                new Color(0.05f, 0.05f, 0.05f));

            listLayout = scrollRoot.AddComponent<LayoutElement>();

            var proto = CellViewHolder.CreatePrototypeCell(scrollRoot);
            proto.sizeDelta = new Vector2(100, 30);
            ScrollPool.Initialize(this, proto);
        }
    }
}
