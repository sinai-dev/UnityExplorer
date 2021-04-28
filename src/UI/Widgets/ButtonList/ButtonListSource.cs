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
    public class ButtonListSource<T> : IPoolDataSource<ButtonCell>
    {
        internal ScrollPool<ButtonCell> ScrollPool;

        public int ItemCount => currentEntries.Count;
        public readonly List<T> currentEntries = new List<T>();

        public Func<List<T>> GetEntries;
        public Action<ButtonCell, int> SetICell;
        public Func<T, string, bool> ShouldDisplay;
        public Action<int> OnCellClicked;

        public string CurrentFilter
        {
            get => currentFilter;
            set => currentFilter = value?.ToLower() ?? "";
        }
        private string currentFilter;

        public ButtonListSource(ScrollPool<ButtonCell> scrollPool, Func<List<T>> getEntriesMethod, 
            Action<ButtonCell, int> setICellMethod, Func<T, string, bool> shouldDisplayMethod,
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
            var allEntries = GetEntries.Invoke();
            currentEntries.Clear();

            foreach (var entry in allEntries)
            {
                if (!string.IsNullOrEmpty(currentFilter))
                {
                    if (!ShouldDisplay.Invoke(entry, currentFilter))
                        continue;

                    currentEntries.Add(entry);
                }
                else
                    currentEntries.Add(entry);
            }
        }

        public void OnCellBorrowed(ButtonCell cell)
        {
            cell.OnClick += OnCellClicked;
        }

        public void OnCellReturned(ButtonCell cell)
        {
            cell.OnClick -= OnCellClicked;
        }

        public void SetCell(ButtonCell cell, int index)
        {
            if (currentEntries == null)
                RefreshData();

            if (index < 0 || index >= currentEntries.Count)
                cell.Disable();
            else
            {
                cell.Enable();
                cell.CurrentDataIndex = index;
                SetICell.Invoke(cell, index);
            }
        }

        public void DisableCell(ButtonCell cell, int index) => cell.Disable();
    }
}
