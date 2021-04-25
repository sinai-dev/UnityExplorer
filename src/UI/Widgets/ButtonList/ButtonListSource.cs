using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Widgets
{
    public class ButtonListSource<T> : IPoolDataSource
    {
        public int GetRealIndexOfTempIndex(int index) => throw new NotImplementedException("TODO");

        internal ScrollPool Scroller;

        public int ItemCount => currentEntries.Count;
        public readonly List<T> currentEntries = new List<T>();

        public Func<List<T>> GetEntries;
        public Action<ButtonCell<T>, int> SetICell;
        public Func<T, string, bool> ShouldDisplay;
        public Action<int> OnCellClicked;

        public string CurrentFilter
        {
            get => currentFilter;
            set => currentFilter = value?.ToLower() ?? "";
        }
        private string currentFilter;

        public ButtonListSource(ScrollPool scrollPool, Func<List<T>> getEntriesMethod, 
            Action<ButtonCell<T>, int> setICellMethod, Func<T, string, bool> shouldDisplayMethod,
            Action<int> onCellClickedMethod)
        {
            Scroller = scrollPool;

            GetEntries = getEntriesMethod;
            SetICell = setICellMethod;
            ShouldDisplay = shouldDisplayMethod;
            OnCellClicked = onCellClickedMethod;
        }

        public void Init()
        {
            var proto = ButtonCell<T>.CreatePrototypeCell(Scroller.UIRoot);

            RefreshData();
            Scroller.DataSource = this;
            Scroller.Initialize(this, proto);
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

        public ICell CreateCell(RectTransform rect)
        {
            var button = rect.GetComponentInChildren<Button>();
            var text = button.GetComponentInChildren<Text>();
            var cell = new ButtonCell<T>(this, rect.gameObject, button, text);
            cell.OnClick += OnCellClicked;
            return cell;
        }

        public void SetCell(ICell cell, int index)
        {
            if (currentEntries == null)
                RefreshData();

            if (index < 0 || index >= currentEntries.Count)
                cell.Disable();
            else
            {
                cell.Enable();
                (cell as ButtonCell<T>).CurrentDataIndex = index;
                SetICell.Invoke((ButtonCell<T>)cell, index);
            }
        }

        public void DisableCell(ICell cell, int index) => cell.Disable();
    }
}
