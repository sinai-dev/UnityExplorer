using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Widgets
{
    public class SimpleListSource<T> : IPoolDataSource
    {
        internal ScrollPool Scroller;

        public Func<List<T>> GetEntries;
        public List<T> currentEntries;

        public int ItemCount => currentEntries.Count;

        public Func<RectTransform, SimpleCell<T>> CreateICell;
        public Action<SimpleCell<T>, int> SetICell;

        public Func<T, string, bool> ShouldFilter;

        public string CurrentFilter
        {
            get => currentFilter;
            set => currentFilter = value?.ToLower() ?? "";
        }
        private string currentFilter;

        public SimpleListSource(ScrollPool infiniteScroller, Func<List<T>> getEntriesMethod, 
            Func<RectTransform, SimpleCell<T>> createCellMethod, Action<SimpleCell<T>, int> setICellMethod, 
            Func<T, string, bool> shouldFilterMethod)
        {
            Scroller = infiniteScroller;

            GetEntries = getEntriesMethod;
            CreateICell = createCellMethod;
            SetICell = setICellMethod;
            ShouldFilter = shouldFilterMethod;
        }

        public void Init()
        {
            RuntimeProvider.Instance.StartCoroutine(InitCoroutine());
        }

        private IEnumerator InitCoroutine()
        {
            yield return null;

            RefreshData();
            Scroller.DataSource = this;
            Scroller.Initialize(this);
        }

        public void RefreshData()
        {
            var allEntries = GetEntries.Invoke();
            var list = new List<T>();

            foreach (var entry in allEntries)
            {
                if (!string.IsNullOrEmpty(currentFilter))
                {
                    if (!ShouldFilter.Invoke(entry, currentFilter))
                        continue;

                    list.Add(entry);
                }
                else
                    list.Add(entry);
            }

            currentEntries = list;
        }

        public ICell CreateCell(RectTransform cellTransform)
        {
            return CreateICell.Invoke(cellTransform);
        }

        public void SetCell(ICell cell, int index)
        {
            if (index < 0 || index >= currentEntries.Count)
                cell.Disable();
            else
            {
                cell.Enable();
                SetICell.Invoke((SimpleCell<T>)cell, index);
            }
        }
    }
}
