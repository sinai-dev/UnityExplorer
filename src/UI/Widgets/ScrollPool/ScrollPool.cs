using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Input;
using UnityExplorer.UI.Models;
using UnityExplorer.UI.Panels;

namespace UnityExplorer.UI.Widgets
{
    public struct CellInfo 
    {
        public int cellIndex, dataIndex;
    }

    /// <summary>
    /// An object-pooled ScrollRect, attempts to support content of any size and provide a scrollbar for it.
    /// </summary>
    public class ScrollPool<T> : UIBehaviourModel, IEnumerable<CellInfo> where T : ICell
    {
        public ScrollPool(ScrollRect scrollRect)
        {
            this.ScrollRect = scrollRect;
        }

        public ICellPoolDataSource<T> DataSource { get; set; }

        public readonly List<T> CellPool = new List<T>();

        internal DataHeightCache<T> HeightCache;

        public float PrototypeHeight => _protoHeight ?? (float)(_protoHeight = Pool<T>.Instance.DefaultHeight);
        private float? _protoHeight;

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
        private int CurrentDataCount => bottomDataIndex + 1;

        private float TotalDataHeight => HeightCache.TotalHeight + contentLayout.padding.top + contentLayout.padding.bottom;

        /// <summary>
        /// The first and last indices of our CellPool in the transform heirarchy
        /// </summary>
        private int topPoolIndex, bottomPoolIndex;

        private Vector2 prevAnchoredPos;
        private float prevViewportHeight;

        #region Internal set tracking and update

        // A sanity check so only one thing is setting the value per frame.
        public bool WritingLocked
        {
            get => writingLocked || PanelDragger.Resizing;
            internal set
            {
                if (writingLocked == value)
                    return;
                timeofLastWriteLock = Time.realtimeSinceStartup;
                writingLocked = value;
            }
        }
        private bool writingLocked;
        private float timeofLastWriteLock;

        private float prevContentHeight = 1.0f;
        private event Action OnHeightChanged;

        public override void Update()
        {
            if (!ScrollRect || DataSource == null)
                return;

            if (writingLocked && timeofLastWriteLock.OccuredEarlierThanDefault())
                writingLocked = false;

            if (!writingLocked)
            {
                bool viewChange = CheckRecycleViewBounds(true);

                if (viewChange || Content.rect.height != prevContentHeight)
                {
                    prevContentHeight = Content.rect.height;
                    OnValueChangedListener(Vector2.zero);

                    OnHeightChanged?.Invoke();
                }
            }
            
        }
        #endregion

        // Public methods

        public void Refresh(bool setCellData, bool jumpToTop = false)
        {
            if (jumpToTop)
            { 
                bottomDataIndex = CellPool.Count - 1;
                Content.anchoredPosition = Vector2.zero;
            }

            RefreshCells(setCellData, true);
        }

        public void JumpToIndex(int index, Action<T> onJumped)
        {
            RefreshCells(true, true);

            // Slide to the normalized position of the index
            var cache = HeightCache[index];
            float normalized = (cache.startPosition + (cache.height * 0.5f)) / HeightCache.TotalHeight;

            RuntimeProvider.Instance.StartCoroutine(ForceDelayedJump(index, normalized, onJumped));
        }

        private IEnumerator ForceDelayedJump(int dataIndex, float normalizedPos, Action<T> onJumped)
        {
            // Yielding two frames seems necessary in case the Explorer tab had not been opened before the jump.
            yield return null;
            yield return null;
            slider.value = normalizedPos;

            // Get the cell containing the data index and invoke the onJumped listener for it
            foreach (var cellInfo in this)
            {
                if (cellInfo.dataIndex == dataIndex)
                {
                    onJumped?.Invoke(CellPool[cellInfo.cellIndex]);
                    break;
                }
            }
        }

        // IEnumerable

        public IEnumerator<CellInfo> GetEnumerator() => EnumerateCellPool();

        IEnumerator IEnumerable.GetEnumerator() => EnumerateCellPool();

        // Initialize

        /// <summary>Should be called only once, when the scroll pool is created.</summary>
        public void Initialize(ICellPoolDataSource<T> dataSource, Action onHeightChangedListener = null)
        {
            this.DataSource = dataSource;
            HeightCache = new DataHeightCache<T>(this);

            // Ensure the pool for the cell type is initialized.
            Pool<T>.GetPool();

            this.contentLayout = ScrollRect.content.GetComponent<VerticalLayoutGroup>();
            this.slider = ScrollRect.GetComponentInChildren<Slider>();
            slider.onValueChanged.AddListener(OnSliderValueChanged);

            ScrollRect.vertical = true;
            ScrollRect.horizontal = false;

            RuntimeProvider.Instance.StartCoroutine(InitCoroutine(onHeightChangedListener));
        }

        private WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

        private IEnumerator InitCoroutine(Action onHeightChangedListener)
        {
            ScrollRect.content.anchoredPosition = Vector2.zero;
            yield return waitForEndOfFrame ?? (waitForEndOfFrame = new WaitForEndOfFrame());

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);

            // set intial bounds
            prevAnchoredPos = Content.anchoredPosition;
            CheckRecycleViewBounds(false);

            // create initial cell pool and set cells
            CreateCellPool();

            foreach (var cell in this)
                SetCell(CellPool[cell.cellIndex], cell.dataIndex);

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
            prevContentHeight = Content.rect.height;
            // update slider
            SetScrollBounds();
            UpdateSliderHandle();

            // add onValueChanged listener after setup
            ScrollRect.onValueChanged.AddListener(OnValueChangedListener);

            OnHeightChanged += onHeightChangedListener;
            onHeightChangedListener?.Invoke();
        }

        private void SetScrollBounds()
        {
            NormalizedScrollBounds = new Vector2(Viewport.rect.height * 0.5f, TotalDataHeight - (Viewport.rect.height * 0.5f));
        }

        /// <summary>
        /// return value = viewport changed height
        /// </summary>
        private bool CheckRecycleViewBounds(bool extendPoolIfGrown)
        {
            RecycleViewBounds = new Vector2(Viewport.MinY() + HalfThreshold, Viewport.MaxY() - HalfThreshold);

            if (extendPoolIfGrown && prevViewportHeight < Viewport.rect.height && prevViewportHeight != 0.0f)
                CheckExtendCellPool();

            bool ret = prevViewportHeight == Viewport.rect.height;
            prevViewportHeight = Viewport.rect.height;
            return ret;
        }

        // Cell pool

        private CellInfo _current;

        private IEnumerator<CellInfo> EnumerateCellPool()
        {
            int cellIdx = topPoolIndex;
            int dataIndex = TopDataIndex;
            int iterated = 0;
            while (iterated < CellPool.Count)
            {
                _current.cellIndex = cellIdx;
                _current.dataIndex = dataIndex;
                yield return _current;

                cellIdx++;
                if (cellIdx >= CellPool.Count)
                    cellIdx = 0;

                dataIndex++;
                iterated++;
            }
        }

        private void CreateCellPool()
        {
            //ReleaseCells();

            CheckDataSourceCountChange(out _);

            float currentPoolCoverage = 0f;
            float requiredCoverage = ScrollRect.viewport.rect.height + RecycleThreshold;

            topPoolIndex = 0;
            bottomPoolIndex = -1;

            WritingLocked = true;
            // create cells until the Pool area is covered.
            // use minimum default height so that maximum pool count is reached.
            while (currentPoolCoverage <= requiredCoverage)
            {
                bottomPoolIndex++;

                var cell = Pool<T>.Borrow();
                CellPool.Add(cell);
                DataSource.OnCellBorrowed(cell);
                cell.Rect.SetParent(ScrollRect.content, false);

                currentPoolCoverage += PrototypeHeight;
            }

            bottomDataIndex = CellPool.Count - 1;

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
        }

        private bool CheckExtendCellPool()
        {
            CheckDataSourceCountChange();

            var requiredCoverage = Math.Abs(RecycleViewBounds.y - RecycleViewBounds.x);
            var currentCoverage = CellPool.Count * PrototypeHeight;
            int cellsRequired = (int)Math.Floor((decimal)(requiredCoverage - currentCoverage) / (decimal)PrototypeHeight);
            if (cellsRequired > 0)
            {
                WritingLocked = true;

                bottomDataIndex += cellsRequired;

                // TODO sometimes still jumps a litte bit, need to figure out why.
                float prevAnchor = Content.localPosition.y;
                float prevHeight = Content.rect.height;

                for (int i = 0; i < cellsRequired; i++)
                {
                    var cell = Pool<T>.Borrow();
                    DataSource.OnCellBorrowed(cell);
                    cell.Rect.SetParent(ScrollRect.content, false);
                    CellPool.Add(cell);

                    if (CellPool.Count > 1)
                    {
                        int index = CellPool.Count - 1 - (topPoolIndex % (CellPool.Count - 1));
                        cell.Rect.SetSiblingIndex(index + 1);

                        if (bottomPoolIndex == index - 1)
                            bottomPoolIndex++;
                    }
                }

                RefreshCells(true, true);

                //ExplorerCore.Log("Anchor: " + Content.localPosition.y + ", prev: " + prevAnchor);
                //ExplorerCore.Log("Height: " + Content.rect.height + ", prev:" + prevHeight);

                if (Content.localPosition.y != prevAnchor)
                {
                    var diff = Content.localPosition.y - prevAnchor;
                    Content.localPosition = new Vector3(Content.localPosition.x, Content.localPosition.y - diff);
                }

                if (Content.rect.height != prevHeight)
                {
                    var diff = Content.rect.height - prevHeight;
                    //ExplorerCore.Log("Height diff: " + diff);
                    //Content.localPosition = new Vector3(Content.localPosition.x, Content.localPosition.y - diff);
                }

                return true;
            }

            return false;
        }

        // Refresh methods

        private bool CheckDataSourceCountChange() => CheckDataSourceCountChange(out _);

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

            CheckRecycleViewBounds(true);

            CheckDataSourceCountChange(out bool jumpToBottom);

            // update date height cache, and set cells if 'andReload'
            foreach (var cellInfo in this)
            {
                var cell = CellPool[cellInfo.cellIndex];
                if (andReloadFromDataSource)
                    SetCell(cell, cellInfo.dataIndex);
                else
                    HeightCache.SetIndex(cellInfo.dataIndex, cell.Rect.rect.height);
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

            if (andReloadFromDataSource)
                LayoutRebuilder.ForceRebuildLayoutImmediate(Content);

            SetScrollBounds();
            ScrollRect.UpdatePrevData();
        }

        private void RefreshCellHeightsFast()
        {
            foreach (var cellInfo in this)
                HeightCache.SetIndex(cellInfo.dataIndex, CellPool[cellInfo.cellIndex].Rect.rect.height);
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
            if (WritingLocked || DataSource == null)
                return;

            if (InputManager.MouseScrollDelta != Vector2.zero)
                ScrollRect.StopMovement();

            RefreshCellHeightsFast();

            CheckRecycleViewBounds(true);

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
            UpdateSliderHandle();
        }

        private bool ShouldRecycleTop => GetCellExtent(CellPool[topPoolIndex].Rect) > RecycleViewBounds.x
                                         && GetCellExtent(CellPool[bottomPoolIndex].Rect) > RecycleViewBounds.y;

        private bool ShouldRecycleBottom => CellPool[bottomPoolIndex].Rect.position.y < RecycleViewBounds.y
                                            && CellPool[topPoolIndex].Rect.position.y < RecycleViewBounds.x;

        private float GetCellExtent(RectTransform cell) => cell.MaxY() - contentLayout.spacing;

        private float RecycleTopToBottom()
        {
            float recycledheight = 0f;

            while (ShouldRecycleTop && CurrentDataCount < DataSource.ItemCount)
            {
                WritingLocked = true;
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

            return -recycledheight;
        }

        private float RecycleBottomToTop()
        {
            float recycledheight = 0f;

            while (ShouldRecycleBottom && CurrentDataCount > CellPool.Count)
            {
                WritingLocked = true;
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

            return recycledheight;
        }

        // Slider 

        private void OnSliderValueChanged(float val)
        {
            // Prevent spam invokes unless value is 0 or 1 (so we dont skip over the start/end)
            if (DataSource == null || (WritingLocked && val != 0 && val != 1))
                return;
            //this.WritingLocked = true;

            ScrollRect.StopMovement();
            RefreshCellHeightsFast();

            // normalize the scroll position for the scroll bounds.
            // point at the center of the viewport
            var desiredPosition = val * (NormalizedScrollBounds.y - NormalizedScrollBounds.x) + NormalizedScrollBounds.x;

            // add offset above it for viewport height
            var halfView = Viewport.rect.height * 0.5f;
            var desiredMinY = desiredPosition - halfView;

            // get the data index at the top of the viewport
            int topViewportIndex = HeightCache.GetFirstDataIndexAtPosition(desiredMinY);
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
                // cells will be the same, do nothing
            }
            else if (TopDataIndex > poolStartIndex && TopDataIndex < desiredBottomIndex)
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
                foreach (var info in this)
                {
                    var cell = CellPool[info.cellIndex];
                    SetCell(cell, info.dataIndex);
                }
            }

            CheckRecycleViewBounds(true);

            SetScrollBounds();
            ScrollRect.UpdatePrevData();

            UpdateSliderHandle();
        }

        private void UpdateSliderHandle()// bool forcePositionValue = true)
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

            float val = 0f;
            if (TotalDataHeight > 0f)
            {
                float topPos = 0f;
                if (HeightCache.Count > 0)
                    topPos = HeightCache[TopDataIndex].startPosition;

                var scrollPos = topPos + Content.anchoredPosition.y;

                var viewHeight = TotalDataHeight - Viewport.rect.height;
                if (viewHeight != 0.0f)
                    val = (float)((decimal)scrollPos / (decimal)(viewHeight));
                else
                    val = 0f;
            }

            slider.Set(val, false);
        }

        /// <summary>Use <see cref="UIFactory.CreateScrollPool"/></summary>
        public override void ConstructUI(GameObject parent) => throw new NotImplementedException();
    }
}
