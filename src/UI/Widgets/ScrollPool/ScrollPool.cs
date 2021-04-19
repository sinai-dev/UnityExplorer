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
    /// A ScrollRect for a list of content with cells that vary in height, using VerticalLayoutGroup and LayoutElement.
    /// </summary>
    public class ScrollPool : UIBehaviourModel
    {
        // a fancy list to track our total data height
        public class HeightCache
        {
            private readonly List<float> heightCache = new List<float>();

            public float TotalHeight => totalHeight;
            private float totalHeight;

            private static readonly float defaultCellHeight = 25f;

            public float this[int index]
            {
                get => heightCache[index];
                set => OnSetIndex(index, value);
            }

            public void Add(float value)
            {
                heightCache.Add(0f);
                OnSetIndex(heightCache.Count - 1, value);
            }

            public void Clear()
            {
                heightCache.Clear();
                totalHeight = 0f;
            }

            private void OnSetIndex(int index, float value)
            {
                if (index >= heightCache.Count)
                {
                    while (index > heightCache.Count)
                        heightCache.Add(defaultCellHeight);
                    Add(value);
                    return;
                }

                var curr = heightCache[index];
                if (curr.Equals(value))
                    return;
                var diff = value - curr;
                totalHeight += diff;
                heightCache[index] = value;
            }
        }

        // internal class used to track and manage cell views
        public class CachedCell
        {
            public ScrollPool Pool { get; }  // reference to this scrollpool
            public RectTransform Rect { get; }      // the Rect (actual UI object)
            public ICell Cell { get; }       // the ICell (to interface with DataSource)

            // used to automatically manage the Pool's TotalCellHeight
            public float Height
            {
                get => m_height;
                set
                {
                    if (value.Equals(m_height))
                        return;
                    var diff = value - m_height;
                    Pool.TotalCellHeight += diff;
                    m_height = value;
                }
            }
            private float m_height;

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

        public bool AutoResizeHandleRect { get; set; }
        public float ExtraPoolCoverageRatio = 1.3f;

        public IPoolDataSource DataSource;
        public RectTransform PrototypeCell;
        private float DefaultCellHeight => PrototypeCell?.rect.height ?? 25f;

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

        private readonly HeightCache DataHeightCache = new HeightCache();

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
                SetContentHeight();
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

        #region internal set tracking and update

        //private bool _recycling;

        public bool ExternallySetting
        {
            get => externallySetting;
            internal set
            {
                if (externallySetting == value)
                    return;
                timeOfLastExternalSet = Time.time;
                externallySetting = value;
            }
        }
        private bool externallySetting;
        private float timeOfLastExternalSet;

        public override void Update()
        {
            if (externallySetting && timeOfLastExternalSet < Time.time)
                externallySetting = false;
        }
        #endregion
       
        //  Initialize

        public void Rebuild()
        {
            Initialize(DataSource);
        }

        public void Initialize(IPoolDataSource dataSource)
        {
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

            BuildInitialHeightCache();
            CreateCellPool();

            SetContentHeight();

            UpdateSliderPositionAndSize();

            scrollRect.onValueChanged.AddListener(OnValueChangedListener);
        }

        private void BuildInitialHeightCache()
        {
            DataHeightCache.Clear();
            float defaultHeight = DefaultCellHeight;
            for (int i = 0; i < DataSource.ItemCount; i++)
            {
                if (i < CellPool.Count)
                    DataHeightCache.Add(CellPool[i].Height);
                else
                    DataHeightCache.Add(defaultHeight);
            }
        }

        private void SetRecycleViewBounds()
        {
            var extra = (Viewport.rect.height * ExtraPoolCoverageRatio) - Viewport.rect.height;
            extra *= 0.5f;
            RecycleViewBounds = new Vector2(Viewport.MinY() + extra, Viewport.MaxY() - extra);
        }

        private void SetContentHeight()
        {
            var viewRect = scrollRect.viewport;
            scrollRect.content.sizeDelta = new Vector2(
                scrollRect.content.sizeDelta.x,
                AdjustedTotalCellHeight - viewRect.rect.height);
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

        // TODO this is not quite right, it can move the content, it shouldnt move it

        public void RefreshCells(bool andReloadFromDataSource = false)
        {
            if (!CellPool.Any()) return;

            SetRecycleViewBounds();

            bool jumpToBottom = false;
            if (andReloadFromDataSource)
            {
                int count = DataSource.ItemCount;
                if (bottomDataIndex > count)
                { 
                    bottomDataIndex = Math.Max(count - 1, CellPool.Count - 1);
                    jumpToBottom = true;
                }
            }

            var enumerator = GetPoolEnumerator();
            while (enumerator.MoveNext())
            {
                var curr = enumerator.Current;
                var cell = CellPool[curr.cellIndex];

                if (andReloadFromDataSource)
                    SetCell(cell, curr.dataIndex);
                else
                {
                    cell.Height = cell.Rect.rect.height;
                    DataHeightCache[curr.dataIndex] = cell.Height;
                }
            }

            SetRecycleViewBounds();
            SetContentHeight();

            if (andReloadFromDataSource)
            {
                RecycleBottomToTop();
                RecycleTopToBottom();
            }

            SetContentHeight();
            UpdateSliderPositionAndSize();

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

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);

            // ExplorerCore.Log("Set cell, real height is " + cachedCell.Rect.rect.height + ", pref height is " + cachedCell.Rect.GetComponent<LayoutElement>().preferredHeight);

            cachedCell.Height = cachedCell.Cell.Enabled ? cachedCell.Rect.rect.height : 0f;
            DataHeightCache[dataIndex] = cachedCell.Height;
            //ExplorerCore.Log("set to cache as " + cachedCell.Height);
        }

        //private void UpdateDisplayedHeightCache()
        //{
        //    if (!CellPool.Any()) return;

        //    var enumerator = GetPoolEnumerator();
        //    while (enumerator.MoveNext())
        //    {
        //        var curr = enumerator.Current;
        //        var cell = CellPool[curr.cellIndex];
        //        cell.Height = cell.Rect.rect.height;
        //        DataHeightCache[curr.dataIndex] = cell.Height;
        //    }

        //    //int cellIdx = topPoolCellIndex;
        //    //int dataIndex = topDataIndex;
        //    //int iterated = 0;
        //    //while (iterated < CellPool.Count)
        //    //{
        //    //    var cell = CellPool[cellIdx];
        //    //    cellIdx++;
        //    //    if (cellIdx >= CellPool.Count)
        //    //        cellIdx = 0;
        //    //
        //    //    cell.Height = cell.Rect.rect.height;
        //    //    DataHeightCache[dataIndex] = cell.Height;
        //    //
        //    //    dataIndex++;
        //    //    iterated++;
        //    //}
        //}

        // Cell pool

        private void CreateCellPool()
        {
            //Reseting Pool
            if (CellPool.Any())
            {
                foreach (var cell in CellPool)
                    GameObject.Destroy(cell.Rect.gameObject);
                CellPool.Clear();
            }

            if (!PrototypeCell)
            {
                ExplorerCore.Log("no prototype cell, cannot initialize");
                return;
            }

            //Set the prototype cell active and set cell anchor as top 
            PrototypeCell.gameObject.SetActive(true);

            float currentPoolCoverage = 0f;
            float requiredCoverage = scrollRect.viewport.rect.height * ExtraPoolCoverageRatio;

            topPoolCellIndex = 0;
            //topDataIndex = 0;
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

                currentPoolCoverage += rect.rect.height + this.contentLayout.spacing;
            }

            bottomDataIndex = bottomPoolIndex;

            // after creating pool, set displayed cells.
            //posY = 0f;
            for (int i = 0; i < CellPool.Count; i++)
            {
                var cell = CellPool[i];
                SetCell(cell, i);
            }

            //Deactivate prototype cell if it is not a prefab(i.e it's present in scene)
            if (PrototypeCell.gameObject.scene.IsValid())
                PrototypeCell.gameObject.SetActive(false);
        }

        // Value change processor

        private void OnValueChangedListener(Vector2 val)
        {
            if (ExternallySetting)
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

            UpdateSliderPositionAndSize();
        }

        private bool ShouldRecycleTop => GetCellExtent(CellPool[topPoolCellIndex]) >= RecycleViewBounds.x
                                         //&& CellPool[topMostCellIndex].Rect.position.y >= Viewport.MinY()
                                         && CellPool[bottomPoolIndex].Rect.position.y > RecycleViewBounds.y;

        private bool ShouldRecycleBottom => GetCellExtent(CellPool[bottomPoolIndex]) < RecycleViewBounds.y
                                         //&& CellPool[bottomMostCellIndex].Rect.position.y < Viewport.MaxY()
                                         && CellPool[topPoolCellIndex].Rect.position.y < RecycleViewBounds.x;

        private float GetCellExtent(CachedCell cell) => cell.Rect.MaxY() - contentLayout.spacing;

        private float RecycleTopToBottom()
        {
            ExternallySetting = true;

            float recycledheight = 0f;

            while (ShouldRecycleTop && CurrentDataCount < DataSource.ItemCount)
            //while (GetCellExtent(CellPool[topMostCellIndex]) > Viewport.MinY() && CurrentDataCount < DataSource.ItemCount)
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
                //topDataIndex++;
                bottomDataIndex++;

                bottomPoolIndex = topPoolCellIndex;
                topPoolCellIndex = (topPoolCellIndex + 1) % CellPool.Count;
            }

            return -recycledheight;
        }

        private float RecycleBottomToTop()
        {
            ExternallySetting = true;

            float recycledheight = 0f;

            // works, except when moving+resizing a cell at the top, that seems to cause issues, need to fix that.

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
                //topDataIndex--;
                bottomDataIndex--;

                //set Cell
                SetCell(cell, TopDataIndex);

                // move content again for new cell size
                var newHeight = cell.Rect.rect.height;
                var diff = newHeight - prevHeight;
                if (diff != 0.0f)
                {
                    SetContentHeight();
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

        private void UpdateSliderPositionAndSize()
        {
            int total = DataSource.ItemCount;
            total = Math.Max(total, 1);

            // NAIVE TEMP DEBUG - all cells will NOT be the same height!

            var spread = CellPool.Count(it => it.Cell.Enabled);

            // TODO temp debug
            bool forceValue = true;
            if (forceValue)
            {
                if (spread >= total)
                    slider.value = 0f;
                else
                    slider.value = (float)((decimal)TopDataIndex / Math.Max(1, total - CellPool.Count));
            }

            if (AutoResizeHandleRect)
            {
                var viewportHeight = scrollRect.viewport.rect.height;

                var handleRatio = (decimal)spread / total;
                var handleHeight = viewportHeight * (float)Math.Min(1, handleRatio);

                handleHeight = Math.Max(handleHeight, 15f);

                // need to resize the handle container area for the size of the handle (bigger handle = smaller container)
                var container = slider.m_HandleContainerRect;
                container.offsetMax = new Vector2(container.offsetMax.x, -(handleHeight * 0.5f));
                container.offsetMin = new Vector2(container.offsetMin.x, handleHeight * 0.5f);

                var handle = slider.handleRect;

                handle.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, handleHeight);

                // if slider is 100% height then make it not interactable.
                slider.interactable = !Mathf.Approximately(handleHeight, viewportHeight);
            }
        }

        private void OnSliderValueChanged(float val)
        {
            if (this.ExternallySetting)
                return;
            this.ExternallySetting = true;

            // TODO this cant work until we have a cache of all data heights.
            // will need to maintain that as we go and assume default height for indeterminate cells.
        }

        private void JumpToIndex(int dataIndex)
        {
            // TODO this cant work until we have a cache of all data heights.
            // will need to maintain that as we go and assume default height for indeterminate cells.
        }


        /// <summary>Use <see cref="UIFactory.CreateScrollPool"/></summary>
        public override void ConstructUI(GameObject parent) => throw new NotImplementedException();
    }
}
