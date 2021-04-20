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
    /// <summary>
    /// An object-pooled ScrollRect, attempts to support content of any size and provide a scrollbar for it.
    /// </summary>
    public class ScrollPool : UIBehaviourModel
    {
        // Some helper classes to make managing the complex parts of this a bit easier.

        public class CachedHeight
        {
            public int dataIndex;
            public float height, startPosition;

            public static implicit operator float(CachedHeight ch) => ch.height;
        }

        public class DataHeightManager
        {
            private readonly List<CachedHeight> heightCache = new List<CachedHeight>();

            public int Count => heightCache.Count;

            public float TotalHeight => totalHeight;
            private float totalHeight;

            public float DefaultHeight => 25f;

            // for efficient lookup of "which index is at this range"
            // list index: DefaultHeight * index from top of data
            // list value: the data index at this position
            private readonly List<int> rangeToDataIndexCache = new List<int>();

            public CachedHeight this[int index]
            {
                get => heightCache[index];
                set => SetIndex(index, value);
            }

            private float currentEndPosition;

            public void Add(float value)
            {
                heightCache.Add(new CachedHeight()
                {
                    height = 0f,
                    startPosition = currentEndPosition
                });

                currentEndPosition += value;
                SetIndex(heightCache.Count - 1, value);
            }

            public void Clear()
            {
                heightCache.Clear();
                totalHeight = 0f;
            }

            public void SetIndex(int dataIndex, float value)
            {
                if (dataIndex >= heightCache.Count)
                {
                    while (dataIndex > heightCache.Count)
                        Add(DefaultHeight);
                    Add(value);
                    return;
                }

                var curr = heightCache[dataIndex];
                if (curr.Equals(value))
                    return;

                var diff = value - curr;
                totalHeight += diff;

                var cache = heightCache[dataIndex];
                cache.height = value;

                if (dataIndex > 0)
                {
                    var prev = heightCache[dataIndex - 1];
                    cache.startPosition = prev.startPosition + prev.height;
                }

                if (heightCache.Count > dataIndex + 1)
                    heightCache[dataIndex + 1].startPosition += diff;

                // Update the range cache

                // If we are setting an index outside of our cached range we need to naively fill the gap
                int rangeIndex = (int)Math.Floor((decimal)cache.startPosition / (decimal)DefaultHeight);
                if (rangeToDataIndexCache.Count <= rangeIndex)
                {
                    if (!rangeToDataIndexCache.Any())
                        rangeToDataIndexCache.Add(dataIndex);
                    else
                    {
                        int lastCurrIndex = rangeToDataIndexCache[rangeToDataIndexCache.Count - 1];
                        while (rangeToDataIndexCache.Count <= rangeIndex)
                        {
                            rangeToDataIndexCache.Add(lastCurrIndex);
                            if (lastCurrIndex < dataIndex - 1)
                                lastCurrIndex++;
                        }
                    }
                }

                // set the starting 'index' of the cell
                rangeToDataIndexCache[rangeIndex] = dataIndex;

                // if the cell spreads over multiple range indices, then set those too.
                int spread = (int)Math.Floor((decimal)value / (decimal)25f);
                if (spread > 1)
                {
                    for (int i = rangeIndex + 1; i < rangeIndex + spread - 1; i++)
                    {
                        if (i > rangeToDataIndexCache.Count)
                            rangeToDataIndexCache.Add(dataIndex);
                        else
                            rangeToDataIndexCache[i] = dataIndex;
                    }
                }
            }

            public int GetDataIndexAtStartPosition(float desiredHeight)
                => GetDataIndexAtStartPosition(desiredHeight, out _);

            public int GetDataIndexAtStartPosition(float desiredHeight, out CachedHeight cache)
            {
                cache = null;

                //desiredHeight = Math.Max(0, desiredHeight);
                //desiredHeight = Math.Min(TotalHeight, desiredHeight);

                int rangeIndex = (int)Math.Floor((decimal)desiredHeight / (decimal)DefaultHeight);

                if (rangeToDataIndexCache.Count <= rangeIndex)
                    return -1;

                int dataIndex = rangeToDataIndexCache[rangeIndex];
                cache = heightCache[dataIndex];

                return dataIndex;
            }
        }

        // internal class used to track and manage cell views
        public class CachedCell
        {
            public ScrollPool Pool { get; }
            public RectTransform Rect { get; }
            public ICell Cell { get; }

            public CachedCell(ScrollPool pool, RectTransform rect, ICell cell)
            {
                this.Pool = pool;
                this.Rect = rect;
                this.Cell = cell;
            }
        }

        public ScrollPool(ScrollRect scrollRect)
        {
            this.scrollRect = scrollRect;
        }

        public float ExtraPoolCoverageRatio = 1.3f;

        public IPoolDataSource DataSource;
        public RectTransform PrototypeCell;

        // UI

        public override GameObject UIRoot => scrollRect.gameObject;

        public RectTransform Viewport => scrollRect.viewport;
        public RectTransform Content => scrollRect.content;

        internal Slider slider;
        internal ScrollRect scrollRect;
        internal VerticalLayoutGroup contentLayout;

        // Cache / tracking

        /// <summary>Extra clearance height relative to Viewport height, based on <see cref="ExtraPoolCoverageRatio"/>.</summary>
        private Vector2 RecycleViewBounds;

        private DataHeightManager HeightCache;

        /// <summary>
        /// The first and last pooled indices relative to the DataSource's list
        /// </summary>
        private int bottomDataIndex;
        private int TopDataIndex => bottomDataIndex - CellPool.Count + 1;

        private readonly List<CachedCell> CellPool = new List<CachedCell>();

        public float AdjustedTotalCellHeight => TotalCellHeight + (contentLayout.spacing * (CellPool.Count - 1));
        internal float TotalCellHeight
        {
            get => m_totalCellHeight;
            set
            {
                if (TotalCellHeight.Equals(value))
                    return;
                m_totalCellHeight = value;
                //SetContentHeight();
            }
        }
        private float m_totalCellHeight;

        /// <summary>
        /// The first and last indices of our CellPool in the transform heirarchy
        /// </summary>
        private int topPoolCellIndex, bottomPoolIndex;

        private int CurrentDataCount => bottomDataIndex + 1;

        private Vector2 _prevAnchoredPos;
        private Vector2 _prevViewportSize; // TODO track viewport height and rebuild on change

        #region Internal set tracking and update

        // A sanity check so only one thing is setting the value per frame.
        public bool WritingLocked
        {
            get => writingLocked;
            internal set
            {
                if (writingLocked == value)
                    return;
                timeofLastWriteLock = Time.time;
                writingLocked = value;
            }
        }
        private bool writingLocked;
        private float timeofLastWriteLock;

        public override void Update()
        {
            if (writingLocked && timeofLastWriteLock < Time.time)
                writingLocked = false;
        }
        #endregion
       
        //  Initialize

        public void Rebuild()
        {
            Initialize(DataSource);
        }

        public void Initialize(IPoolDataSource dataSource)
        {
            HeightCache = new DataHeightManager();
            DataSource = dataSource;

            this.contentLayout = scrollRect.content.GetComponent<VerticalLayoutGroup>();
            this.slider = scrollRect.GetComponentInChildren<Slider>();
            slider.onValueChanged.AddListener(OnSliderValueChanged);

            scrollRect.vertical = true;
            scrollRect.horizontal = false;

            scrollRect.onValueChanged.RemoveListener(OnValueChangedListener);
            RuntimeProvider.Instance.StartCoroutine(InitCoroutine());
        }

        private IEnumerator InitCoroutine()
        {
            scrollRect.content.anchoredPosition = Vector2.zero;

            yield return null;

            _prevAnchoredPos = scrollRect.content.anchoredPosition;
            _prevViewportSize = new Vector2(scrollRect.viewport.rect.width, scrollRect.viewport.rect.height);

            SetRecycleViewBounds();

            ExplorerCore.Log("Creating cell pool");
            float start = Time.realtimeSinceStartup;
            CreateCellPool();

            SetSliderPositionAndSize();
            ExplorerCore.Log("Done");

            scrollRect.onValueChanged.AddListener(OnValueChangedListener);
        }

        private void SetRecycleViewBounds()
        {
            var extra = (Viewport.rect.height * ExtraPoolCoverageRatio) - Viewport.rect.height;
            extra *= 0.5f;
            RecycleViewBounds = new Vector2(Viewport.MinY() + extra, Viewport.MaxY() - extra);
        }

        // Refresh methods

        private struct CellInfo { public int cellIndex, dataIndex; }

        private IEnumerator<CellInfo> GetPoolEnumerator()
        {
            int cellIdx = topPoolCellIndex;
            int dataIndex = TopDataIndex;
            int iterated = 0;
            while (iterated < CellPool.Count)
            {
                yield return new CellInfo()
                {
                    cellIndex = cellIdx,
                    dataIndex = dataIndex
                };

                cellIdx++;
                if (cellIdx >= CellPool.Count)
                    cellIdx = 0;

                dataIndex++;
                iterated++;
            }
        }

        public void RefreshCells(bool andReloadFromDataSource = false, bool setSlider = true)
        {
            if (!CellPool.Any()) return;

            SetRecycleViewBounds();

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);

            bool jumpToBottom = false;
            if (andReloadFromDataSource)
            {
                int count = DataSource.ItemCount;
                if (bottomDataIndex > count)
                { 
                    bottomDataIndex = Math.Max(count - 1, CellPool.Count - 1);
                    jumpToBottom = true;
                }
                else if (HeightCache.Count < count)
                    HeightCache.SetIndex(count - 1, PrototypeCell?.rect.height ?? 25f);
            }

            // update date height cache, and set cells if 'andReload'
            var enumerator = GetPoolEnumerator();
            while (enumerator.MoveNext())
            {
                var curr = enumerator.Current;
                var cell = CellPool[curr.cellIndex];

                if (andReloadFromDataSource)
                    SetCell(cell, curr.dataIndex);
                else
                    HeightCache.SetIndex(curr.dataIndex, cell.Rect.rect.height);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
            SetRecycleViewBounds();

            // force check recycles
            if (andReloadFromDataSource)
            {
                RecycleBottomToTop();
                RecycleTopToBottom();
            }

            if (setSlider)
                SetSliderPositionAndSize();

            if (jumpToBottom)
            {
                var diff = Viewport.MaxY() - CellPool[bottomPoolIndex].Rect.MaxY();
                Content.anchoredPosition += Vector2.up * diff;
            }
        }

        private void SetCell(CachedCell cachedCell, int dataIndex)
        {
            cachedCell.Cell.Enable();
            DataSource.SetCell(cachedCell.Cell, dataIndex);

            LayoutRebuilder.ForceRebuildLayoutImmediate(cachedCell.Rect);

            HeightCache.SetIndex(dataIndex, cachedCell.Cell.Enabled ? cachedCell.Rect.rect.height : 0f);
        }

        // Cell pool

        private void CreateCellPool()
        {
            if (CellPool.Any())
            {
                foreach (var cell in CellPool)
                    GameObject.Destroy(cell.Rect.gameObject);
                CellPool.Clear();
            }

            if (!PrototypeCell)
                throw new Exception("No prototype cell set, cannot initialize");

            //Set the prototype cell active and set cell anchor as top 
            PrototypeCell.gameObject.SetActive(true);

            float currentPoolCoverage = 0f;
            float requiredCoverage = scrollRect.viewport.rect.height * ExtraPoolCoverageRatio;

            topPoolCellIndex = 0;
            bottomPoolIndex = -1;

            // create cells until the Pool area is covered.
            // use minimum default height so that maximum pool count is reached.
            while (currentPoolCoverage < requiredCoverage)
            {
                bottomPoolIndex++;

                //Instantiate and add to Pool
                RectTransform rect = GameObject.Instantiate(PrototypeCell.gameObject).GetComponent<RectTransform>();
                rect.name = $"Cell_{CellPool.Count + 1}";
                var cell = DataSource.CreateCell(rect);
                CellPool.Add(new CachedCell(this, rect, cell));
                rect.SetParent(scrollRect.content, false);

                cell.Disable();

                currentPoolCoverage += rect.rect.height + this.contentLayout.spacing;
            }

            bottomDataIndex = bottomPoolIndex;

            // after creating pool, set displayed cells.
            for (int i = 0; i < CellPool.Count; i++)
            {
                var cell = CellPool[i];
                SetCell(cell, i);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);

            //Deactivate prototype cell if it is not a prefab(i.e it's present in scene)
            if (PrototypeCell.gameObject.scene.IsValid())
                PrototypeCell.gameObject.SetActive(false);
        }

        // Value change processor

        private void OnValueChangedListener(Vector2 val)
        {
            if (WritingLocked)
                return;

            SetRecycleViewBounds();
            RefreshCells();

            float yChange = (scrollRect.content.anchoredPosition - _prevAnchoredPos).y;
            float adjust = 0f;

            if (yChange > 0) // Scrolling down
            {
                if (ShouldRecycleTop)
                    adjust = RecycleTopToBottom();

            }
            else if (yChange < 0) // Scrolling up
            {
                if (ShouldRecycleBottom)
                    adjust = RecycleBottomToTop();
            }

            var vector = new Vector2(0, adjust);
            scrollRect.m_ContentStartPosition += vector;
            scrollRect.m_PrevPosition += vector;

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
            _prevAnchoredPos = scrollRect.content.anchoredPosition;

            SetSliderPositionAndSize();
        }

        private bool ShouldRecycleTop => GetCellExtent(CellPool[topPoolCellIndex]) >= RecycleViewBounds.x
                                         && CellPool[bottomPoolIndex].Rect.position.y > RecycleViewBounds.y;

        private bool ShouldRecycleBottom => GetCellExtent(CellPool[bottomPoolIndex]) < RecycleViewBounds.y
                                         && CellPool[topPoolCellIndex].Rect.position.y < RecycleViewBounds.x;

        private float GetCellExtent(CachedCell cell) => cell.Rect.MaxY() - contentLayout.spacing;

        private float RecycleTopToBottom()
        {
            WritingLocked = true;

            float recycledheight = 0f;

            while (ShouldRecycleTop && CurrentDataCount < DataSource.ItemCount)
            {
                var cell = CellPool[topPoolCellIndex];

                //Move top cell to bottom
                cell.Rect.SetAsLastSibling();
                var prevHeight = cell.Rect.rect.height;

                // update content position
                Content.anchoredPosition -= Vector2.up * prevHeight;
                recycledheight += prevHeight + contentLayout.spacing;

                //set Cell
                SetCell(cell, CurrentDataCount);

                //set new indices
                bottomDataIndex++;

                bottomPoolIndex = topPoolCellIndex;
                topPoolCellIndex = (topPoolCellIndex + 1) % CellPool.Count;
            }

            return -recycledheight;
        }

        private float RecycleBottomToTop()
        {
            WritingLocked = true;

            float recycledheight = 0f;

            while (ShouldRecycleBottom && CurrentDataCount > CellPool.Count)
            {
                var cell = CellPool[bottomPoolIndex];

                //Move bottom cell to top
                cell.Rect.SetAsFirstSibling();
                var prevHeight = cell.Rect.rect.height;

                // update content position
                Content.anchoredPosition += Vector2.up * prevHeight;
                recycledheight += prevHeight + contentLayout.spacing;

                //set new index
                bottomDataIndex--;

                //set Cell
                SetCell(cell, TopDataIndex);

                // move content again for new cell size
                var newHeight = cell.Rect.rect.height;
                var diff = newHeight - prevHeight;
                if (diff != 0.0f)
                {
                    Content.anchoredPosition += Vector2.up * diff;
                    recycledheight += diff;
                }

                //set new indices
                topPoolCellIndex = bottomPoolIndex;
                bottomPoolIndex = (bottomPoolIndex - 1 + CellPool.Count) % CellPool.Count;
            }

            return recycledheight;
        }

        // Slider 

        private void SetSliderPositionAndSize()
        {
            var dataHeight = HeightCache.TotalHeight;

            // calculate handle size based on viewport / total data height
            var viewportHeight = Viewport.rect.height;
            var handleHeight = viewportHeight * Math.Min(1, viewportHeight / dataHeight);
            handleHeight = Math.Max(15f, handleHeight);

            // resize the handle container area for the size of the handle (bigger handle = smaller container)
            var container = slider.m_HandleContainerRect;
            container.offsetMax = new Vector2(container.offsetMax.x, -(handleHeight * 0.5f));
            container.offsetMin = new Vector2(container.offsetMin.x, handleHeight * 0.5f);

            // set handle size
            slider.handleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, handleHeight);

            // if slider is 100% height then make it not interactable
            slider.interactable = !Mathf.Approximately(handleHeight, viewportHeight);

            GetDisplayedCellLimits(out CellInfo? topVisibleCell, out _);
            if (topVisibleCell != null)
            {
                var topCell = CellPool[topVisibleCell.Value.cellIndex];

                // get the starting height of the top displayed cell
                float startHeight = contentLayout.padding.top;
                int dataIndex = topVisibleCell.Value.dataIndex;
                for (int i = 0; i < dataIndex; i++)
                    startHeight += HeightCache[i];
                startHeight += topCell.Rect.MinY() - Viewport.MinY(); // add the amount above the viewport min it is

                // set the value of the slider
                WritingLocked = true;
                slider.value = (float)((decimal)startHeight / (decimal)(HeightCache.TotalHeight - Viewport.rect.height));
            }
            else
            {
                slider.value = 0f;
            }
        }

        private void GetDisplayedCellLimits(out CellInfo? top, out CellInfo? bottom)
        {
            // get the index of the top displayed cell
            top = null;
            bottom = null;
            var enumerator = GetPoolEnumerator();
            while (enumerator.MoveNext())
            {
                var curr = enumerator.Current;
                var cell = CellPool[curr.cellIndex];
                // if cell bottom is below viewport top
                if (cell.Rect.MaxY() < Viewport.MinY())
                {
                    // and if this is the top-most displayed cell
                    if (top == null || CellPool[top.Value.cellIndex].Rect.position.y < cell.Rect.position.y)
                        top = curr;
                }
                // if cell top is above viewport bottom
                if (cell.Rect.MinY() > Viewport.MaxY())
                {
                    // and if this is the bottom-most displayed cell
                    if (bottom == null || CellPool[bottom.Value.cellIndex].Rect.position.y > cell.Rect.position.y)
                        bottom = curr;
                }
            }

            return;
        }

        // Mostly works, just little bit jumpy and jittery, needs some refining.

        private void OnSliderValueChanged(float val)
        {
            if (this.WritingLocked)
                return;
            this.WritingLocked = true;

            var desiredPosition = val * HeightCache.TotalHeight;

            // add the top and bottom extra area for recycle bounds
            var recycleExtra = Viewport.rect.height * ExtraPoolCoverageRatio - Viewport.rect.height;
            var realMin = Math.Max(0f, desiredPosition - recycleExtra);
            realMin = Math.Min(realMin, HeightCache.TotalHeight - Viewport.rect.height);

            var realBottomIndex = HeightCache.GetDataIndexAtStartPosition(realMin);
            if (realBottomIndex == -1)
                realBottomIndex = DataSource.ItemCount - 1;
            // calculate which data index should be at bottom of pool
            bottomDataIndex = realBottomIndex + CellPool.Count - 1;
            bottomDataIndex = Math.Min(bottomDataIndex, DataSource.ItemCount - 1);

            ExplorerCore.Log("set bottom data index to " + bottomDataIndex);
            RefreshCells(true, false);

            var realDesiredIndex = HeightCache.GetDataIndexAtStartPosition(desiredPosition);

            GetDisplayedCellLimits(out CellInfo? top, out CellInfo? bottom);

            // TODO this is not quite right, I think this is causing the jittery jumpiness

            // calculate how much we need to move up. use height cache for indices above top displayed, move that much.
            float move = 0f;
            if (realDesiredIndex < top.Value.dataIndex)
            {
                ExplorerCore.Log("desired cell is above viewport");
                var enumerator = GetPoolEnumerator();
                while (enumerator.MoveNext())
                {
                    var curr = enumerator.Current;
                    if (curr.dataIndex == realDesiredIndex)
                    {
                        var cell = CellPool[curr.cellIndex];
                        move = Viewport.MinY() - cell.Rect.MinY();
                        ExplorerCore.Log("desired index is " + move + " above the viewport min");
                        break;
                    }
                }
            }
            else if (realDesiredIndex > bottom.Value.dataIndex)
            {
                ExplorerCore.Log("desired cell is below viewport");
                var enumerator = GetPoolEnumerator();
                while (enumerator.MoveNext())
                {
                    var curr = enumerator.Current;
                    if (curr.dataIndex == realDesiredIndex)
                    {
                        var cell = CellPool[curr.cellIndex];
                        move = Viewport.MaxY() - cell.Rect.MaxY();
                        ExplorerCore.Log("desired index is " + move + " below the viewport min");
                        break;
                    }
                }
            }

            // TODO move should account for desired actual position, otherwise we just snap to cells.

            if (move != 0.0f)
            {
                ExplorerCore.Log("Content should move " + move);
                Content.anchoredPosition += Vector2.up * move;
                scrollRect.m_PrevPosition += Vector2.up * move;
            }

            SetSliderPositionAndSize();
        }

        /// <summary>Use <see cref="UIFactory.CreateScrollPool"/></summary>
        public override void ConstructUI(GameObject parent) => throw new NotImplementedException();
    }
}
