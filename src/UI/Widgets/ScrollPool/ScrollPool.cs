using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Input;
using UnityExplorer.UI.Models;

namespace UnityExplorer.UI.Widgets
{
    /// <summary>
    /// An object-pooled ScrollRect, attempts to support content of any size and provide a scrollbar for it.
    /// </summary>
    public class ScrollPool : UIBehaviourModel
    {
        public ScrollPool(ScrollRect scrollRect)
        {
            this.ScrollRect = scrollRect;
        }

        public IPoolDataSource DataSource;
        public RectTransform PrototypeCell;

        private float PrototypeHeight => PrototypeCell.rect.height;

        public int ExtraPoolCells => 10;
        public float RecycleThreshold => PrototypeHeight * ExtraPoolCells;
        public float HalfThreshold => RecycleThreshold * 0.5f;

        // UI

        public override GameObject UIRoot => ScrollRect.gameObject;

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

        private readonly List<ICell> CellPool = new List<ICell>();

        internal DataHeightCache HeightCache;
        internal DataHeightCache tempHeightCache;

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

        private float prevContentHeight;

        public override void Update()
        {
            if (writingLocked && timeofLastWriteLock < Time.time)
                writingLocked = false;

            if (prevContentHeight == 0.0f && Content?.rect.height != 0.0f)
                prevContentHeight = Content.rect.height;
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
            RecreateCellPool(true, true, null);
        }

        public void EnableTempCache()
        {
            if (tempHeightCache == null)
                tempHeightCache = HeightCache;

            HeightCache = new DataHeightCache(this, tempHeightCache);
        }

        public void DisableTempCache()
        {
            if (tempHeightCache == null)
                return;

            HeightCache = tempHeightCache;
            tempHeightCache = null;
        }

        public void RefreshCells(bool reloadData)
        {
            RefreshCells(reloadData, true);
        }

        // Initialize

        public void Initialize(IPoolDataSource dataSource, RectTransform prototypeCell)
        {
            if (!prototypeCell)
                throw new Exception("No prototype cell set, cannot initialize");

            this.PrototypeCell = prototypeCell;
            PrototypeCell.transform.SetParent(Viewport, false);

            HeightCache = new DataHeightCache(this);
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
        }

        private void SetScrollBounds()
        {
            NormalizedScrollBounds = new Vector2(Viewport.rect.height * 0.5f, TotalDataHeight - (Viewport.rect.height * 0.5f));
        }

        // Cell pool

        private void CreateCellPool(bool andResetDataIndex = true)
        {
            if (CellPool.Any())
            {
                foreach (var cell in CellPool)
                    GameObject.Destroy(cell.Rect.gameObject);
                CellPool.Clear();
            }

            float currentPoolCoverage = 0f;
            float requiredCoverage = ScrollRect.viewport.rect.height + RecycleThreshold;

            topPoolIndex = 0;
            bottomPoolIndex = -1;

            // create cells until the Pool area is covered.
            // use minimum default height so that maximum pool count is reached.
            while (currentPoolCoverage <= requiredCoverage)
            {
                bottomPoolIndex++;

                //Instantiate and add to Pool
                RectTransform rect = GameObject.Instantiate(PrototypeCell.gameObject).GetComponent<RectTransform>();
                rect.gameObject.SetActive(true);
                rect.name = $"Cell_{CellPool.Count + 1}";
                var cell = DataSource.CreateCell(rect);
                CellPool.Add(cell);
                rect.SetParent(ScrollRect.content, false);

                currentPoolCoverage += rect.rect.height;
            }

            if (andResetDataIndex)
                bottomDataIndex = CellPool.Count - 1;

            // after creating pool, set displayed cells.
            var enumerator = GetPoolEnumerator();
            while (enumerator.MoveNext())
                SetCell(CellPool[enumerator.Current.cellIndex], enumerator.Current.dataIndex);

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
        }

        /// <summary>ret = cell pool was extended</summary>
        private bool SetRecycleViewBounds(bool checkHeightGrow)
        {
            bool ret = false;

            RecycleViewBounds = new Vector2(Viewport.MinY() + HalfThreshold, Viewport.MaxY() - HalfThreshold);

            if (checkHeightGrow && prevViewportHeight < Viewport.rect.height && prevViewportHeight != 0.0f)
                ret = RecreateCellPool(false, false, null);

            prevViewportHeight = Viewport.rect.height;

            return ret;
        }

        private bool RecreateCellPool(bool forceRecreate, bool resetDataIndex, bool? setTempCacheEnabledTo)
        {
            if (setTempCacheEnabledTo != null)
            {
                if (setTempCacheEnabledTo == true)
                    EnableTempCache();
                else if (setTempCacheEnabledTo == false)
                    DisableTempCache();
            }

            CheckDataSourceCountChange(out _);

            var requiredCoverage = Math.Abs(RecycleViewBounds.y - RecycleViewBounds.x);
            var currentCoverage = CellPool.Count * PrototypeHeight;
            int cellsRequired = (int)Math.Ceiling((decimal)(requiredCoverage - currentCoverage) / (decimal)PrototypeHeight);
            if (cellsRequired > 0 || forceRecreate)
            {
                WritingLocked = true;

                // Disable cells so DataSource can handle its content if need be
                var enumerator = GetPoolEnumerator();
                while (enumerator.MoveNext())
                {
                    var curr = enumerator.Current;
                    DataSource.DisableCell(CellPool[curr.cellIndex], curr.dataIndex);
                }

                bottomDataIndex += cellsRequired;
                int maxDataIndex = Math.Max(CellPool.Count + cellsRequired - 1, DataSource.ItemCount - 1);
                if (bottomDataIndex > maxDataIndex)
                    bottomDataIndex = maxDataIndex;

                // CreateCellPool will destroy existing cells and recreate list.
                CreateCellPool(resetDataIndex);

                LayoutRebuilder.ForceRebuildLayoutImmediate(Content);

                //Content.anchoredPosition = new Vector2(0, pos);
                ScrollRect.UpdatePrevData();

                SetScrollBounds();
                UpdateSliderHandle(true);

                return true;
            }

            return false;
        }

        // Refresh methods

        private struct CellInfo { public int cellIndex, dataIndex; }

        private IEnumerator<CellInfo> GetPoolEnumerator()
        {
            int cellIdx = topPoolIndex;
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

        private void SetCell(ICell cachedCell, int dataIndex)
        {
            cachedCell.Enable();
            DataSource.SetCell(cachedCell, dataIndex);

            LayoutRebuilder.ForceRebuildLayoutImmediate(cachedCell.Rect);
            HeightCache.SetIndex(dataIndex, cachedCell.Rect.rect.height);
        }

        // Value change processor

        private void OnValueChangedListener(Vector2 val)
        {
            if (WritingLocked)
                return;

            if (InputManager.MouseScrollDelta != Vector2.zero)
                ScrollRect.StopMovement();

            if (!SetRecycleViewBounds(true))
                RefreshCells(false);

            float yChange = (ScrollRect.content.anchoredPosition - prevAnchoredPos).y;
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

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
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
            if (this.WritingLocked)
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

            bottomDataIndex = poolStartIndex + CellPool.Count - 1;
            RefreshCells(true, false);

            //UpdateSliderHandle(true);
        }

        /// <summary>Use <see cref="UIFactory.CreateScrollPool"/></summary>
        public override void ConstructUI(GameObject parent) => throw new NotImplementedException();
    }
}
