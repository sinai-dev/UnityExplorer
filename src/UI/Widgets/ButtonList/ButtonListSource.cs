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
        internal ScrollPool Scroller;

        public int ItemCount => currentEntries.Count;
        public List<T> currentEntries;

        public Func<List<T>> GetEntries;
        public Action<ButtonCell<T>, int> SetICell;
        public Func<T, string, bool> ShouldDisplay;
        public Action<ButtonCell<T>> OnCellClicked;

        public string CurrentFilter
        {
            get => currentFilter;
            set => currentFilter = value?.ToLower() ?? "";
        }
        private string currentFilter;

        public ButtonListSource(ScrollPool infiniteScroller, Func<List<T>> getEntriesMethod, 
            Action<ButtonCell<T>, int> setICellMethod, Func<T, string, bool> shouldDisplayMethod,
            Action<ButtonCell<T>> onCellClickedMethod)
        {
            Scroller = infiniteScroller;

            GetEntries = getEntriesMethod;
            SetICell = setICellMethod;
            ShouldDisplay = shouldDisplayMethod;
            OnCellClicked = onCellClickedMethod;
        }

        public void Init()
        {
            RuntimeProvider.Instance.StartCoroutine(InitCoroutine());
        }

        private IEnumerator InitCoroutine()
        {
            yield return null;

            var proto = ButtonCell<T>.CreatePrototypeCell(Scroller.UIRoot);

            RefreshData();
            Scroller.DataSource = this;
            Scroller.Initialize(this, proto);
        }

        public void RefreshData()
        {
            var allEntries = GetEntries.Invoke();
            var list = new List<T>();

            foreach (var entry in allEntries)
            {
                if (!string.IsNullOrEmpty(currentFilter))
                {
                    if (!ShouldDisplay.Invoke(entry, currentFilter))
                        continue;

                    list.Add(entry);
                }
                else
                    list.Add(entry);
            }

            currentEntries = list;
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
            if (index < 0 || index >= currentEntries.Count)
                cell.Disable();
            else
            {
                cell.Enable();
                SetICell.Invoke((ButtonCell<T>)cell, index);
            }
        }

        public void DisableCell(ICell cell, int index) => cell.Disable();
    }
}
