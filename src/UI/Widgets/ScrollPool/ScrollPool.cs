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
    /// An object-pooled ScrollRect, attempts to support content of any size and provide a scrollbar for it.<br/>
    /// <br/>
    /// IMPORTANT CAVEATS:<br/>
    /// - A cell cannot be smaller than the Prototype cell's default height<br/>
    /// - (maybe?) A cell must start at the default height and only increase after being displayed for the first time<br/>
    /// </summary>
    public class ScrollPool : UIBehaviourModel
    {
        // used to track and manage cell views
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

        public int ExtraPoolCells => 10;
        public float ExtraPoolThreshold => PrototypeCell.rect.height * ExtraPoolCells;
        public float HalfPoolThreshold => ExtraPoolThreshold * 0.5f;

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

        private Vector2 RecycleViewBounds;
        private Vector2 NormalizedScrollBounds;

        /// <summary>
        /// The first and last pooled indices relative to the DataSource's list
        /// </summary>
        private int bottomDataIndex;
        private int TopDataIndex => bottomDataIndex - CellPool.Count + 1;

        private readonly List<CachedCell> CellPool = new List<CachedCell>();

        private DataHeightManager HeightCache;

        private float TotalDataHeight => HeightCache.TotalHeight + contentLayout.padding.top + contentLayout.padding.bottom;

        /// <summary>
        /// The first and last indices of our CellPool in the transform heirarchy
        /// </summary>
        private int topPoolCellIndex, bottomPoolIndex;

        private int CurrentDataCount => bottomDataIndex + 1;

        private Vector2 _prevAnchoredPos;
        private Vector2 _prevViewportSize; // TODO track viewport height and add if height increased

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
            HeightCache = new DataHeightManager(this);
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

            float start = Time.realtimeSinceStartup;
            CreateCellPool();

            NormalizedScrollBounds = new Vector2(Viewport.rect.height * 0.5f, TotalDataHeight - (Viewport.rect.height * 0.5f));

            UpdateSliderHandle();

            scrollRect.onValueChanged.AddListener(OnValueChangedListener);
        }

        private void SetRecycleViewBounds()
        {
            var extra = ExtraPoolThreshold;
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

            // ExplorerCore.Log("RefreshCells | " + Time.time);

            SetRecycleViewBounds();

            // jump to bottom if the data count went below our bottom data index
            bool jumpToBottom = false;

            if (andReloadFromDataSource)
            {
                int count = DataSource.ItemCount;
                if (bottomDataIndex > count)
                { 
                    bottomDataIndex = Math.Max(count - 1, CellPool.Count - 1);
                    jumpToBottom = true;
                }
                
                if (HeightCache.Count < count)
                    HeightCache.SetIndex(count - 1, PrototypeCell.rect.height);
                else if (HeightCache.Count > count)
                {
                    while (HeightCache.Count > count)
                        HeightCache.RemoveLast();
                }
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
            SetRecycleViewBounds();
            NormalizedScrollBounds = new Vector2(Viewport.rect.height * 0.5f, TotalDataHeight - (Viewport.rect.height * 0.5f));
            scrollRect.UpdatePrevData();
        }

        private void SetCell(CachedCell cachedCell, int dataIndex)
        {
            cachedCell.Cell.Enable();
            DataSource.SetCell(cachedCell.Cell, dataIndex);

            // DO NEED THIS! Potentially slightly expensive, but everything breaks if we dont do this.
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
            //PrototypeCell.gameObject.SetActive(true);

            float currentPoolCoverage = 0f;
            float requiredCoverage = scrollRect.viewport.rect.height + ExtraPoolThreshold;// * ExtraPoolCoverageRatio;

            topPoolCellIndex = 0;
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
                CellPool.Add(new CachedCell(this, rect, cell));
                rect.SetParent(scrollRect.content, false);

                //cell.Disable();

                currentPoolCoverage += rect.rect.height;
            }

            bottomDataIndex = CellPool.Count - 1;

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

            //ExplorerCore.Log("ScrollRect.OnValueChanged | " + Time.time + ", val: " + val.y.ToString("F5"));

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

            NormalizedScrollBounds = new Vector2(Viewport.rect.height * 0.5f, TotalDataHeight - (Viewport.rect.height * 0.5f));

            UpdateSliderHandle();
        }

        private bool ShouldRecycleTop => GetCellExtent(CellPool[topPoolCellIndex]) >= RecycleViewBounds.x
                                         && CellPool[bottomPoolIndex].Rect.position.y < Viewport.MaxY();

        private bool ShouldRecycleBottom => CellPool[bottomPoolIndex].Rect.position.y < RecycleViewBounds.y
                                            && GetCellExtent(CellPool[topPoolCellIndex]) < Viewport.MinY();

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

        private void UpdateSliderHandle(bool forcePositionValue = true)
        {
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
                    var topPos = HeightCache[TopDataIndex].startPosition;
                    var scrollPos = topPos + Content.anchoredPosition.y;

                    val = (float)((decimal)scrollPos / (decimal)(TotalDataHeight - Viewport.rect.height));
                }

                WritingLocked = true;
                slider.value = val;
                WritingLocked = false;
            }
        }

        private void OnSliderValueChanged(float val)
        {
            if (this.WritingLocked)
                return;
            this.WritingLocked = true;

            // normalize the scroll position for the scroll bounds.
            // this translates the value into saying "point at the center of the height of the viewport"
            var scrollHeight = NormalizedScrollBounds.y - NormalizedScrollBounds.x;
            var desiredPosition = val * scrollHeight + NormalizedScrollBounds.x;

            // add offset above it for viewport height
            var halfheight = Viewport.rect.height * 0.5f;
            var desiredMinY = desiredPosition - halfheight;

            // get the data index at the top of the viewport
            int topViewportIndex = HeightCache.GetDataIndexAtPosition(desiredMinY);
            topViewportIndex = Math.Max(0, topViewportIndex);

            // get the real top pooled data index to display our content
            int poolStartIndex = Math.Max(0, topViewportIndex - (int)(ExtraPoolCells * 0.5f));
            poolStartIndex = Math.Min(DataSource.ItemCount - CellPool.Count, poolStartIndex);

            // for content at the very top, just use the desired position as the anchor pos.
            if (desiredMinY < ExtraPoolThreshold * 0.5f)
            {
                Content.anchoredPosition = new Vector2(0, desiredMinY);
            }
            else // else calculate anchor pos 
            {
                var topStartPos = HeightCache[poolStartIndex].startPosition;

                // how far the actual top cell is from our desired center
                var diff = desiredMinY - topStartPos;

                Content.anchoredPosition = new Vector2(0, diff);
            }

            scrollRect.UpdatePrevData();
            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);

            //var pos = Content.anchoredPosition.y;

            bottomDataIndex = poolStartIndex + CellPool.Count - 1;
            RefreshCells(true, false);

            scrollRect.UpdatePrevData();
            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);

            UpdateSliderHandle(true);
        }

        /// <summary>Use <see cref="UIFactory.CreateScrollPool"/></summary>
        public override void ConstructUI(GameObject parent) => throw new NotImplementedException();
    }
}
