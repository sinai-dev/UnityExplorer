using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Input;
using UnityExplorer.UI.Models;
using UnityExplorer.UI.ObjectPool;

namespace UnityExplorer.UI.Widgets
{
    public struct CellInfo 
    {
        public int cellIndex, dataIndex;
    }

    /// <summary>
    /// An object-pooled ScrollRect, attempts to support content of any size and provide a scrollbar for it.
    /// </summary>
    public class ScrollPool<T> : UIBehaviourModel where T : ICell
    {
        public ScrollPool(ScrollRect scrollRect)
        {
            this.ScrollRect = scrollRect;
        }

        public IPoolDataSource<T> DataSource { get; set; }

        public readonly List<T> CellPool = new List<T>();

        internal DataHeightCache<T> HeightCache;

        public float PrototypeHeight => _protoHeight ?? (float)(_protoHeight = Pool<T>.Instance.DefaultHeight);
        private float? _protoHeight;

        //private float PrototypeHeight => DefaultHeight.rect.height;

        public int ExtraPoolCells => 10;
        public float RecycleThreshold => PrototypeHeight * ExtraPoolCells;
        public float HalfThreshold => RecycleThreshold * 0.5f;

        // UI

        public override GameObject UIRoot
        {
            get
            {
                if (ScrollRect)
                    return ScrollRect.gameObject;
                return null;
            }
        }

        public RectTransform Viewport => ScrollRect.viewport;
        public RectTransform Content => ScrollRect.content;

        internal Slider slider;
        internal ScrollRect ScrollRect;
        internal VerticalLayoutGroup contentLayout;

        // Cache / tracking

        private Vector2 RecycleViewBounds;
        private Vector2 NormalizedScrollBounds;

        /// <summary>
        /// The first and last pooled indices relative to the DataSource's list
        /// </summary>
        private int bottomDataIndex;
        private int TopDataIndex => Math.Max(0, bottomDataIndex - CellPool.Count + 1);

        private float TotalDataHeight => HeightCache.TotalHeight + contentLayout.padding.top + contentLayout.padding.bottom;

        /// <summary>
        /// The first and last indices of our CellPool in the transform heirarchy
        /// </summary>
        private int topPoolIndex, bottomPoolIndex;

        private int CurrentDataCount => bottomDataIndex + 1;

        private Vector2 prevAnchoredPos;
        private float prevViewportHeight;

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

        private float prevContentHeight = 1.0f;

        public void SetUninitialized()
        {
            m_initialized = false;
        }

        public override void Update()
        {
            if (!m_initialized || !ScrollRect || DataSource == null)
                return;

            if (writingLocked && timeofLastWriteLock < Time.time)
                writingLocked = false;

            if (prevContentHeight <= 1f && Content?.rect.height > 1f)
            {
                prevContentHeight = Content.rect.height;
            }
            else if (Content.rect.height != prevContentHeight)
            {
                prevContentHeight = Content.rect.height;
                OnValueChangedListener(Vector2.zero);
            }
        }
        #endregion

        // Public methods

        public void Rebuild()
        {
            SetRecycleViewBounds(false);
            SetScrollBounds();

            RecreateCellPool(true, true);
            writingLocked = false;
            Content.anchoredPosition = Vector2.zero;
            UpdateSliderHandle(true);

            m_initialized = true;
        }

        public void RefreshAndJumpToTop()
        {
            bottomDataIndex = CellPool.Count - 1;
            RefreshCells(true);
            Content.anchoredPosition = Vector2.zero;
            UpdateSliderHandle(true);
        }

        public void RecreateHeightCache()
        {
            HeightCache = new DataHeightCache<T>(this);
            CheckDataSourceCountChange(out _);
        }

        public void RefreshCells(bool reloadData)
        {
            RefreshCells(reloadData, true);
        }

        // Initialize

        private bool m_initialized;

        public void Initialize(IPoolDataSource<T> dataSource)
        {
            // Ensure the pool for the cell type is initialized.
            Pool<T>.GetPool();

            HeightCache = new DataHeightCache<T>(this);
            DataSource = dataSource;

            this.contentLayout = ScrollRect.content.GetComponent<VerticalLayoutGroup>();
            this.slider = ScrollRect.GetComponentInChildren<Slider>();
            slider.onValueChanged.AddListener(OnSliderValueChanged);

            ScrollRect.vertical = true;
            ScrollRect.horizontal = false;

            ScrollRect.onValueChanged.RemoveListener(OnValueChangedListener);
            RuntimeProvider.Instance.StartCoroutine(InitCoroutine());
        }

        private IEnumerator InitCoroutine()
        {
            ScrollRect.content.anchoredPosition = Vector2.zero;
            yield return null;

            // set intial bounds
            prevAnchoredPos = Content.anchoredPosition;
            SetRecycleViewBounds(false);

            // create initial cell pool and set cells
            CreateCellPool();

            // update slider
            SetScrollBounds();
            UpdateSliderHandle();

            // add onValueChanged listener after setup
            ScrollRect.onValueChanged.AddListener(OnValueChangedListener);

            m_initialized = true;
        }

        private void SetScrollBounds()
        {
            NormalizedScrollBounds = new Vector2(Viewport.rect.height * 0.5f, TotalDataHeight - (Viewport.rect.height * 0.5f));
        }

        // Cell pool

        public void ReturnCells()
        {
            if (CellPool.Any())
            {
                foreach (var cell in CellPool)
                {
                    DataSource.OnCellReturned(cell);
                    Pool<T>.Return(cell);
                }
                CellPool.Clear();
            }
        }

        private void CreateCellPool(bool andResetDataIndex = true)
        {
            ReturnCells();

            float currentPoolCoverage = 0f;
            float requiredCoverage = ScrollRect.viewport.rect.height + RecycleThreshold;

            topPoolIndex = 0;
            bottomPoolIndex = -1;

            // create cells until the Pool area is covered.
            // use minimum default height so that maximum pool count is reached.
            while (currentPoolCoverage <= requiredCoverage)
            {
                bottomPoolIndex++;

                var cell = Pool<T>.Borrow();
                DataSource.OnCellBorrowed(cell);
                //var rect = cell.Rect;
                CellPool.Add(cell);
                cell.Rect.SetParent(ScrollRect.content, false);

                currentPoolCoverage += PrototypeHeight;
            }

            if (andResetDataIndex)
                bottomDataIndex = CellPool.Count - 1;

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);

            // after creating pool, set displayed cells.
            var enumerator = GetPoolEnumerator();
            while (enumerator.MoveNext())
                SetCell(CellPool[enumerator.Current.cellIndex], enumerator.Current.dataIndex);
        }

        /// <summary>ret = cell pool was extended</summary>
        private bool SetRecycleViewBounds(bool checkHeightGrow)
        {
            bool ret = false;

            RecycleViewBounds = new Vector2(Viewport.MinY() + HalfThreshold, Viewport.MaxY() - HalfThreshold);

            if (checkHeightGrow && prevViewportHeight < Viewport.rect.height && prevViewportHeight != 0.0f)
                ret = RecreateCellPool(false, false);

            prevViewportHeight = Viewport.rect.height;

            return ret;
        }

        private bool RecreateCellPool(bool forceRecreate, bool resetDataIndex)
        {
            CheckDataSourceCountChange(out _);

            var requiredCoverage = Math.Abs(RecycleViewBounds.y - RecycleViewBounds.x);
            var currentCoverage = CellPool.Count * PrototypeHeight;
            int cellsRequired = (int)Math.Floor((decimal)(requiredCoverage - currentCoverage) / (decimal)PrototypeHeight);
            if (cellsRequired > 0 || forceRecreate)
            {
                WritingLocked = true;

                bottomDataIndex += cellsRequired;
                int maxDataIndex = Math.Max(CellPool.Count + cellsRequired - 1, DataSource.ItemCount - 1);
                if (bottomDataIndex > maxDataIndex)
                    bottomDataIndex = maxDataIndex;

                float curAnchor = Content.localPosition.y;
                float curHeight = Content.rect.height;

                CreateCellPool(resetDataIndex);

                // fix slight jumping when resizing panel and size increases

                if (Content.rect.height != curHeight)
                {
                    var diff = Content.rect.height - curHeight;
                    Content.localPosition = new Vector3(Content.localPosition.x, Content.localPosition.y + (diff * 0.5f));
                }

                ScrollRect.UpdatePrevData();

                SetScrollBounds();
                UpdateSliderHandle(true);

                return true;
            }

            return false;
        }

        // Refresh methods

        private CellInfo _cellInfo = new CellInfo();

        private IEnumerator<CellInfo> GetPoolEnumerator()
        {
            int cellIdx = topPoolIndex;
            int dataIndex = TopDataIndex;
            int iterated = 0;
            while (iterated < CellPool.Count)
            {
                _cellInfo.cellIndex = cellIdx;
                _cellInfo.dataIndex = dataIndex;
                yield return _cellInfo;

                cellIdx++;
                if (cellIdx >= CellPool.Count)
                    cellIdx = 0;

                dataIndex++;
                iterated++;
            }
        }

        private bool CheckDataSourceCountChange(out bool shouldJumpToBottom)
        {
            shouldJumpToBottom = false;

            int count = DataSource.ItemCount;
            if (bottomDataIndex > count && bottomDataIndex >= CellPool.Count)
            {
                bottomDataIndex = Math.Max(count - 1, CellPool.Count - 1);
                shouldJumpToBottom = true;
            }

            if (HeightCache.Count < count)
            {
                HeightCache.SetIndex(count - 1, PrototypeHeight);
                return true;
            }
            else if (HeightCache.Count > count)
            {
                while (HeightCache.Count > count)
                    HeightCache.RemoveLast();
                return false;
            }

            return false;
        }

        private void RefreshCells(bool andReloadFromDataSource, bool setSlider)
        {
            if (!CellPool.Any()) return;

            SetRecycleViewBounds(true);

            CheckDataSourceCountChange(out bool jumpToBottom);

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

            // force check recycles
            if (andReloadFromDataSource)
            {
                RecycleBottomToTop();
                RecycleTopToBottom();
            }

            if (setSlider)
                UpdateSliderHandle();

            if (jumpToBottom)
            {
                var diff = Viewport.MaxY() - CellPool[bottomPoolIndex].Rect.MaxY();
                Content.anchoredPosition += Vector2.up * diff;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
            SetScrollBounds();
            ScrollRect.UpdatePrevData();
        }

        private void SetCell(T cachedCell, int dataIndex)
        {
            cachedCell.Enable();
            DataSource.SetCell(cachedCell, dataIndex);

            LayoutRebuilder.ForceRebuildLayoutImmediate(cachedCell.Rect);
            HeightCache.SetIndex(dataIndex, cachedCell.Rect.rect.height);
        }

        // Value change processor

        private void OnValueChangedListener(Vector2 val)
        {
            if (WritingLocked || !m_initialized)
                return;

            if (InputManager.MouseScrollDelta != Vector2.zero)
                ScrollRect.StopMovement();

            SetRecycleViewBounds(true);

            float yChange = ((Vector2)ScrollRect.content.localPosition - prevAnchoredPos).y;
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
            ScrollRect.m_ContentStartPosition += vector;
            ScrollRect.m_PrevPosition += vector;

            prevAnchoredPos = ScrollRect.content.anchoredPosition;

            SetScrollBounds();

            //WritingLocked = true;
            UpdateSliderHandle();
        }

        private bool ShouldRecycleTop => GetCellExtent(CellPool[topPoolIndex].Rect) > RecycleViewBounds.x
                                         && GetCellExtent(CellPool[bottomPoolIndex].Rect) > RecycleViewBounds.y;

        private bool ShouldRecycleBottom => CellPool[bottomPoolIndex].Rect.position.y < RecycleViewBounds.y
                                            && CellPool[topPoolIndex].Rect.position.y < RecycleViewBounds.x;

        private float GetCellExtent(RectTransform cell) => cell.MaxY() - contentLayout.spacing;

        private float RecycleTopToBottom()
        {
            WritingLocked = true;

            float recycledheight = 0f;

            while (ShouldRecycleTop && CurrentDataCount < DataSource.ItemCount)
            {
                var cell = CellPool[topPoolIndex];

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

                bottomPoolIndex = topPoolIndex;
                topPoolIndex = (topPoolIndex + 1) % CellPool.Count;
            }

            //LayoutRebuilder.ForceRebuildLayoutImmediate(Content);

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
                topPoolIndex = bottomPoolIndex;
                bottomPoolIndex = (bottomPoolIndex - 1 + CellPool.Count) % CellPool.Count;
            }

            //LayoutRebuilder.ForceRebuildLayoutImmediate(Content);

            return recycledheight;
        }

        // Slider 

        private void UpdateSliderHandle(bool forcePositionValue = true)
        {
            CheckDataSourceCountChange(out _);

            var dataHeight = TotalDataHeight;

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

            if (forcePositionValue)
            {
                float val = 0f;
                if (TotalDataHeight > 0f)
                {
                    float topPos = 0f;
                    if (HeightCache.Count > 0)
                        topPos = HeightCache[TopDataIndex].startPosition;

                    var scrollPos = topPos + Content.anchoredPosition.y;

                    val = (float)((decimal)scrollPos / (decimal)(TotalDataHeight - Viewport.rect.height));
                }

                bool prev = writingLocked;
                WritingLocked = true;
                slider.value = val;
                WritingLocked = prev;
            }
        }

        private void OnSliderValueChanged(float val)
        {
            if (this.WritingLocked || !m_initialized)
                return;
            this.WritingLocked = true;

            ScrollRect.StopMovement();

            // normalize the scroll position for the scroll bounds.
            // this translates the value into saying "point at the center of the height of the viewport"
            var scrollHeight = NormalizedScrollBounds.y - NormalizedScrollBounds.x;
            var desiredPosition = val * scrollHeight + NormalizedScrollBounds.x;

            // add offset above it for viewport height
            var halfView = Viewport.rect.height * 0.5f;
            var desiredMinY = desiredPosition - halfView;

            // get the data index at the top of the viewport
            int topViewportIndex = HeightCache.GetDataIndexAtPosition(desiredMinY);
            topViewportIndex = Math.Max(0, topViewportIndex);
            topViewportIndex = Math.Min(DataSource.ItemCount - 1, topViewportIndex);

            // get the real top pooled data index to display our content
            int poolStartIndex = Math.Max(0, topViewportIndex - (int)(ExtraPoolCells * 0.5f));
            poolStartIndex = Math.Min(Math.Max(0, DataSource.ItemCount - CellPool.Count), poolStartIndex);

            var topStartPos = HeightCache[poolStartIndex].startPosition;

            float desiredAnchor;
            if (desiredMinY < HalfThreshold)
                desiredAnchor = desiredMinY;
            else
                desiredAnchor = desiredMinY - topStartPos;
            Content.anchoredPosition = new Vector2(0, desiredAnchor);

            int desiredBottomIndex = poolStartIndex + CellPool.Count - 1;

            // check if our pool indices contain the desired index. If so, rotate and set
            if (bottomDataIndex == desiredBottomIndex)
            {
                // cells will be the same, do nothing?
            }
            else
            {
                if (TopDataIndex > poolStartIndex && TopDataIndex < desiredBottomIndex)
                {
                    // top cell falls within the new range, rotate around that
                    int rotate = TopDataIndex - poolStartIndex;
                    for (int i = 0; i < rotate; i++)
                    {
                        CellPool[bottomPoolIndex].Rect.SetAsFirstSibling();

                        //set new indices
                        topPoolIndex = bottomPoolIndex;
                        bottomPoolIndex = (bottomPoolIndex - 1 + CellPool.Count) % CellPool.Count;
                        bottomDataIndex--;

                        SetCell(CellPool[topPoolIndex], TopDataIndex);
                    }
                }
                else if (bottomDataIndex > poolStartIndex && bottomDataIndex < desiredBottomIndex)
                {
                    // bottom cells falls within the new range, rotate around that
                    int rotate = desiredBottomIndex - bottomDataIndex;
                    for (int i = 0; i < rotate; i++)
                    {
                        CellPool[topPoolIndex].Rect.SetAsLastSibling();

                        //set new indices
                        bottomPoolIndex = topPoolIndex;
                        topPoolIndex = (topPoolIndex + 1) % CellPool.Count;
                        bottomDataIndex++;

                        SetCell(CellPool[bottomPoolIndex], bottomDataIndex);
                    }
                }
                else
                {
                    bottomDataIndex = desiredBottomIndex;
                    var enumerator = GetPoolEnumerator();
                    while (enumerator.MoveNext())
                    {
                        var curr = enumerator.Current;
                        var cell = CellPool[curr.cellIndex];
                        SetCell(cell, curr.dataIndex);
                    }
                }
            }

            SetRecycleViewBounds(true);

            SetScrollBounds();
            ScrollRect.UpdatePrevData();

            UpdateSliderHandle(false);
        }

        /// <summary>Use <see cref="UIFactory.CreateScrollPool"/></summary>
        public override void ConstructUI(GameObject parent) => throw new NotImplementedException();
    }
}
