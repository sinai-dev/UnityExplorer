using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityExplorer.UI.Widgets
{
    public class ButtonListHandler<TData, TCell> : ICellPoolDataSource<TCell> where TCell : ButtonCell
    {
        internal ScrollPool<TCell> ScrollPool;

        public int ItemCount => currentEntries.Count;
        public readonly List<TData> currentEntries = new List<TData>();

        public Func<List<TData>> GetEntries;
        public Action<TCell, int> SetICell;
        public Func<TData, string, bool> ShouldDisplay;
        public Action<int> OnCellClicked;

        public string CurrentFilter
        {
            get => currentFilter;
            set => currentFilter = value ?? "";
        }
        private string currentFilter;

        public ButtonListHandler(ScrollPool<TCell> scrollPool, Func<List<TData>> getEntriesMethod,
            Action<TCell, int> setICellMethod, Func<TData, string, bool> shouldDisplayMethod,
            Action<int> onCellClickedMethod)
        {
            ScrollPool = scrollPool;

            GetEntries = getEntriesMethod;
            SetICell = setICellMethod;
            ShouldDisplay = shouldDisplayMethod;
            OnCellClicked = onCellClickedMethod;
        }

        public void RefreshData()
        {
            var allEntries = GetEntries();
            currentEntries.Clear();

            foreach (var entry in allEntries)
            {
                if (!string.IsNullOrEmpty(currentFilter))
                {
                    if (!ShouldDisplay(entry, currentFilter))
                        continue;

                    currentEntries.Add(entry);
                }
                else
                    currentEntries.Add(entry);
            }
        }

        public virtual void OnCellBorrowed(TCell cell)
        {
            cell.OnClick += OnCellClicked;
        }

        public virtual void SetCell(TCell cell, int index)
        {
            if (currentEntries == null)
                RefreshData();

            if (index < 0 || index >= currentEntries.Count)
                cell.Disable();
            else
            {
                cell.Enable();
                cell.CurrentDataIndex = index;
                SetICell(cell, index);
            }
        }
    }
}
