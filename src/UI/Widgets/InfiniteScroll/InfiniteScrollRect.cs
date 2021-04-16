using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Models;

namespace UnityExplorer.UI.Widgets
{
    public class InfiniteScrollRect : UIBehaviourModel
    {
        public InfiniteScrollRect(ScrollRect scrollRect)
        {
            this.scrollRect = scrollRect;
            Init();
        }

        public IListDataSource DataSource;

        public int PoolCount => _cachedCells.Count;

        public override GameObject UIRoot => scrollRect.gameObject;

        /// <summary>Use <see cref="UIFactory.CreateInfiniteScroll"/></summary>
        public override void ConstructUI(GameObject parent) => throw new NotImplementedException();

        internal ScrollRect scrollRect;

        internal RectTransform PrototypeCell;
        internal Slider _slider;

        // Cell pool
        private float _cellWidth, _cellHeight;
        private List<RectTransform> _cellPool;
        private List<ICell> _cachedCells;
        private Bounds _recyclableViewBounds;

        /// <summary>
        /// Extra pooled cells above AND below the viewport (so actual extra pool is double this value).
        /// </summary>
        public int ExtraCellPoolSize = 2;

        private readonly Vector3[] _corners = new Vector3[4];
        private bool _recycling;
        private Vector2 _prevAnchoredPos;
        internal Vector2 _lastScroll;

        internal int currentItemCount; //item count corresponding to the datasource.
        internal int topMostCellIndex, bottomMostCellIndex; //Topmost and bottommost cell in the heirarchy
        internal int _topMostCellColoumn, _bottomMostCellColoumn; // used for recyling in Grid layout. top-most and bottom-most coloumn

        public bool AutoResizeHandleRect;

        public bool ExternallySetting
        {
            get => externallySetting;
            internal set
            {
                if (externallySetting == value)
                    return;
                timeOfLastExternalSet = Time.time;
                externallySetting = true;
            }
        }
        private bool externallySetting;
        private float timeOfLastExternalSet;

        private Vector2 zeroVector = Vector2.zero;

        public override void Init()
        {
            _slider = scrollRect.GetComponentInChildren<Slider>();

            _slider.onValueChanged.AddListener((float val) =>
            {
                if (this.ExternallySetting)
                    return;
                this.ExternallySetting = true;

                // Jump to val * count (ie, 0.0 would jump to top, 1.0 would jump to bottom)
                var index = Math.Floor(val * DataSource.ItemCount);
                JumpToIndex((int)index);
            });
        }

        public override void Update()
        {
            if (externallySetting && timeOfLastExternalSet < Time.time)
                externallySetting = false;
        }

        internal void OnValueChangedListener(Vector2 _)
        {
            if (ExternallySetting)
                return;

            ExternallySetting = true;

            Vector2 dir = scrollRect.content.anchoredPosition - _prevAnchoredPos;
            scrollRect.m_ContentStartPosition += ProcessValueChange(dir);
            _prevAnchoredPos = scrollRect.content.anchoredPosition;

            SetSliderFromScrollValue();

            // ExternallySetting = false;
        }

        internal void SetSliderFromScrollValue(bool forceValue = true)
        {
            int total = DataSource.ItemCount;
            total = Math.Max(total, 1);

            var spread = _cellPool.Count - (ExtraCellPoolSize * 2);

            if (forceValue)
            {
                var range = GetDisplayedRange();
                if (spread >= total)
                    _slider.value = 0f;
                else
                    // top-most displayed index divided by (totalCount - displayedRange)
                    _slider.value = (float)((decimal)range.x / Math.Max(1, (total - _cellPool.Count)));
            }

            // resize the handle rect to reflect the size of the displayed content vs. the total content height.
            if (AutoResizeHandleRect)
            {
                var viewportHeight = scrollRect.viewport.rect.height;

                var handleRatio = (decimal)spread / total;
                var handleHeight = viewportHeight * (float)Math.Min(1, handleRatio);

                // minimum handle size
                handleHeight = Math.Max(handleHeight, 15f);

                // need to resize the handle container area for the size of the handle (bigger handle = smaller area)
                var container = _slider.m_HandleContainerRect;
                container.offsetMax = new Vector2(container.offsetMax.x, -(handleHeight * 0.5f));
                container.offsetMin = new Vector2(container.offsetMin.x, handleHeight * 0.5f);

                var handle = _slider.handleRect;

                handle.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, handleHeight);

                // if slider is 100% height then make it not interactable.
                _slider.interactable = !Mathf.Approximately(handleHeight, viewportHeight);
            }
        }

        /// <summary>
        /// Try to jump to the specified index. Pretty accurate, not perfect. Currently assumes all elements are the same height.
        /// </summary>
        public void JumpToIndex(int index)
        {
            var realCount = DataSource.ItemCount;

            // clamp to real index limit
            index = Math.Min(index, realCount - 1);

            // add the buffer count to desired index and set our currentItemCount to that.
            currentItemCount = index + _cachedCells.Count;
            currentItemCount = Math.Max(Math.Min(currentItemCount, realCount - 1), _cachedCells.Count);
            Refresh();

            // if we're jumping to the very bottom we need to show the extra pooled cells which are normally hidden.
            var y = 0f;
            if (index >= realCount - (ExtraCellPoolSize * 4))
                y = _cellHeight * (index - realCount + (4 * ExtraCellPoolSize)) + ExtraCellPoolSize; // add +1 to show the last entry.

            scrollRect.content.anchoredPosition = new Vector2(scrollRect.content.anchoredPosition.x, y);
        }

        /// <summary>
        /// Get the start and end indexes (relative to DataSource) of the cell pool
        /// </summary>
        public Vector2 GetDisplayedRange()
        {
            int max = currentItemCount;
            int min = max - _cachedCells.Count;
            return new Vector2(min, max);
        }

        /// <summary>
        /// Initialize with the provided DataSource
        /// </summary>
        /// <param name="dataSource"></param>
        public void Initialize(IListDataSource dataSource)
        {
            DataSource = dataSource;

            scrollRect.vertical = true;
            scrollRect.horizontal = false;

            _prevAnchoredPos = scrollRect.content.anchoredPosition;
            scrollRect.onValueChanged.RemoveListener(OnValueChangedListener);

            RuntimeProvider.Instance.StartCoroutine(InitCoroutine(() =>
            {
                scrollRect.onValueChanged.AddListener(OnValueChangedListener);
            }));
        }

        public void ReloadData()
        {
            ReloadData(DataSource);
        }

        public void ReloadData(IListDataSource dataSource)
        {
            if (scrollRect.onValueChanged == null)
                return;

            scrollRect.StopMovement();

            scrollRect.onValueChanged.RemoveListener(OnValueChangedListener);

            DataSource = dataSource;

            RuntimeProvider.Instance.StartCoroutine(InitCoroutine(() =>
                scrollRect.onValueChanged.AddListener(OnValueChangedListener)
            ));

            _prevAnchoredPos = scrollRect.content.anchoredPosition;
        }

        public void Refresh()
        {
            if (DataSource == null || _cellPool == null)
                return;

            int count = DataSource.ItemCount;
            if (currentItemCount > count)
                currentItemCount = Math.Max(count, _cellPool.Count);

            SetRecyclingBounds();
            RecycleBottomToTop();
            RecycleTopToBottom();

            PopulateCells();

            RefreshContentSize();

            //internallySetting = true;
            SetSliderFromScrollValue(false);
            //internallySetting = false;
        }

        public void PopulateCells()
        {
            var width = scrollRect.viewport.GetComponent<RectTransform>().rect.width;
            scrollRect.content.sizeDelta = new Vector2(width, scrollRect.content.sizeDelta.y);

            int cellIndex = topMostCellIndex;
            var itemIndex = currentItemCount - _cachedCells.Count;
            int iterated = 0;
            while (iterated < _cachedCells.Count)
            {
                var cell = _cachedCells[cellIndex];
                cellIndex++;
                if (cellIndex < 0)
                    continue;
                if (cellIndex >= _cachedCells.Count)
                    cellIndex = 0;
                DataSource.SetCell(cell, itemIndex);
                itemIndex++;

                var rect = _cellPool[cellIndex];
                rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);

                iterated++;
            }
        }

        #region RECYCLING INIT

        private IEnumerator InitCoroutine(Action onInitialized)
        {
            yield return null;
            SetTopAnchor(scrollRect.content);
            scrollRect.content.anchoredPosition = Vector3.zero;

            yield return null;
            SetRecyclingBounds();

            //Cell Poool
            CreateCellPool();
            currentItemCount = _cellPool.Count;
            topMostCellIndex = 0;
            bottomMostCellIndex = _cellPool.Count - 1;

            //Set content height according to no of rows
            RefreshContentSize();

            SetTopAnchor(scrollRect.content);

            onInitialized?.Invoke();
        }

        private void RefreshContentSize()
        {
            int noOfRows = 0;
            foreach (var cell in _cachedCells)
                if (cell.Enabled) noOfRows++;
            float contentYSize = noOfRows * _cellHeight;
            scrollRect.content.sizeDelta = new Vector2(scrollRect.content.sizeDelta.x, contentYSize);
        }

        private void SetRecyclingBounds()
        {
            scrollRect.viewport.GetCorners(_corners);
            float threshHold = _cellHeight * ExtraCellPoolSize; //RecyclingThreshold * (_corners[2].y - _corners[0].y);
            _recyclableViewBounds.min = new Vector3(_corners[0].x, _corners[0].y - threshHold);
            _recyclableViewBounds.max = new Vector3(_corners[2].x, _corners[2].y + threshHold);
        }

        private void CreateCellPool()
        {
            //Reseting Pool
            if (_cellPool != null)
            {
                _cellPool.ForEach((RectTransform item) => GameObject.Destroy(item.gameObject));
                _cellPool.Clear();
                _cachedCells.Clear();
            }
            else
            {
                _cachedCells = new List<ICell>();
                _cellPool = new List<RectTransform>();
            }

            //Set the prototype cell active and set cell anchor as top 
            PrototypeCell.gameObject.SetActive(true);
            SetTopAnchor(PrototypeCell);

            //Reset
            _topMostCellColoumn = _bottomMostCellColoumn = 0;

            //Temps
            float currentPoolCoverage = 0;
            int poolSize = 0;
            float posY = 0;

            //set new cell size according to its aspect ratio
            _cellWidth = scrollRect.content.rect.width;
            _cellHeight = PrototypeCell.rect.height;

            //Get the required pool coverage and mininum size for the Cell pool
            float requiredCoverage = scrollRect.viewport.rect.height + (_cellHeight * (ExtraCellPoolSize * 2));

            //create cells untill the Pool area is covered
            while (currentPoolCoverage < requiredCoverage)
            {
                //Instantiate and add to Pool
                RectTransform item = GameObject.Instantiate(PrototypeCell.gameObject).GetComponent<RectTransform>();
                item.name = $"Cell_{_cachedCells.Count + 1}";
                item.sizeDelta = new Vector2(_cellWidth, _cellHeight);
                _cellPool.Add(item);
                item.SetParent(scrollRect.content, false);

                item.anchoredPosition = new Vector2(0, posY);
                posY = item.anchoredPosition.y - item.rect.height;
                currentPoolCoverage += item.rect.height;

                //Setting data for Cell
                var cell = DataSource.CreateCell(item);
                _cachedCells.Add(cell);
                DataSource.SetCell(_cachedCells[_cachedCells.Count - 1], poolSize);

                //Update the Pool size
                poolSize++;
            }

            //Deactivate prototype cell if it is not a prefab(i.e it's present in scene)
            if (PrototypeCell.gameObject.scene.IsValid())
                PrototypeCell.gameObject.SetActive(false);
        }
        #endregion

        #region RECYCLING

        public Vector2 ProcessValueChange(Vector2 direction)
        {
            if (_recycling || _cellPool == null || _cellPool.Count == 0)
                return zeroVector;

            //Updating Recyclable view bounds since it can change with resolution changes.
            SetRecyclingBounds();

            _lastScroll = direction;

            if (direction.y > 0 && _cellPool[bottomMostCellIndex].MaxY() > _recyclableViewBounds.min.y)
            {
                return RecycleTopToBottom();
            }
            else if (direction.y < 0 && _cellPool[topMostCellIndex].MinY() < _recyclableViewBounds.max.y)
            {
                return RecycleBottomToTop();
            }

            return zeroVector;
        }

        /// <summary>
        /// Recycles cells from top to bottom in the List heirarchy
        /// </summary>
        private Vector2 RecycleTopToBottom()
        {
            _recycling = true;

            int n = 0;
            float posY;

            //to determine if content size needs to be updated
            //Recycle until cell at Top is avaiable and current item count smaller than datasource
            while (_cellPool[topMostCellIndex].MinY() > _recyclableViewBounds.max.y && currentItemCount < DataSource.ItemCount)
            {
                //Move top cell to bottom
                posY = _cellPool[bottomMostCellIndex].anchoredPosition.y - _cellPool[bottomMostCellIndex].sizeDelta.y;
                _cellPool[topMostCellIndex].anchoredPosition = new Vector2(_cellPool[topMostCellIndex].anchoredPosition.x, posY);

                //Cell for row at
                DataSource.SetCell(_cachedCells[topMostCellIndex], currentItemCount);

                //set new indices
                bottomMostCellIndex = topMostCellIndex;
                topMostCellIndex = (topMostCellIndex + 1) % _cellPool.Count;

                currentItemCount++;
                n++;
            }

            //Content anchor position adjustment.
            _cellPool.ForEach((RectTransform cell) => cell.anchoredPosition += n * Vector2.up * _cellPool[topMostCellIndex].sizeDelta.y);
            scrollRect.content.anchoredPosition -= n * Vector2.up * _cellPool[topMostCellIndex].sizeDelta.y;
            _recycling = false;
            return -new Vector2(0, n * _cellPool[topMostCellIndex].sizeDelta.y);
        }

        /// <summary>
        /// Recycles cells from bottom to top in the List heirarchy
        /// </summary>
        private Vector2 RecycleBottomToTop()
        {
            _recycling = true;

            int n = 0;
            float posY = 0;

            //to determine if content size needs to be updated
            //Recycle until cell at bottom is avaiable and current item count is greater than cellpool size
            while (_cellPool[bottomMostCellIndex].MaxY() < _recyclableViewBounds.min.y && currentItemCount > _cellPool.Count)
            {
                //Move bottom cell to top
                posY = _cellPool[topMostCellIndex].anchoredPosition.y + _cellPool[topMostCellIndex].sizeDelta.y;
                _cellPool[bottomMostCellIndex].anchoredPosition = new Vector2(_cellPool[bottomMostCellIndex].anchoredPosition.x, posY);
                n++;

                currentItemCount--;

                //Cell for row at
                DataSource.SetCell(_cachedCells[bottomMostCellIndex], currentItemCount - _cellPool.Count);

                //set new indices
                topMostCellIndex = bottomMostCellIndex;
                bottomMostCellIndex = (bottomMostCellIndex - 1 + _cellPool.Count) % _cellPool.Count;
            }

            _cellPool.ForEach((RectTransform cell) => cell.anchoredPosition -= n * Vector2.up * _cellPool[topMostCellIndex].sizeDelta.y);
            scrollRect.content.anchoredPosition += n * Vector2.up * _cellPool[topMostCellIndex].sizeDelta.y;
            _recycling = false;
            return new Vector2(0, n * _cellPool[topMostCellIndex].sizeDelta.y);
        }

        #endregion

        #region  HELPERS

        /// <summary>
        /// Anchoring cell and content rect transforms to top preset. Makes repositioning easy.
        /// </summary>
        /// <param name="rectTransform"></param>
        private void SetTopAnchor(RectTransform rectTransform)
        {
            //Saving to reapply after anchoring. Width and height changes if anchoring is change. 
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;

            //Setting top anchor 
            rectTransform.anchorMin = new Vector2(0.5f, 1);
            rectTransform.anchorMax = new Vector2(0.5f, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);

            //Reapply size
            rectTransform.sizeDelta = new Vector2(width, height);
        }

        #endregion
    }
}
