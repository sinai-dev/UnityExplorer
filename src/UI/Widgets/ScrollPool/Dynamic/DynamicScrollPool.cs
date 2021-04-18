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
    /// A <see cref="ScrollPool"/> for content with cells that vary in height, using VerticalLayoutGroup and LayoutElement.
    /// </summary>
    public class DynamicScrollPool : UIBehaviourModel, IScrollPool
    {
        // internal class used to track and manage cell views
        public class CachedCell
        {
            public DynamicScrollPool Pool { get; }  // reference to this scrollpool
            public RectTransform Rect { get; }      // the Rect (actual UI object)
            public IDynamicCell Cell { get; }       // the ICell (to interface with DataSource)

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

            public CachedCell(DynamicScrollPool pool, RectTransform rect, IDynamicCell cell)
            {
                this.Pool = pool;
                this.Rect = rect;
                this.Cell = cell;

                // TODO temp debug
                (cell as Panels.DynamicCellTest).Cached = this;
            }
        }

        public DynamicScrollPool(ScrollRect scrollRect)
        {
            this.scrollRect = scrollRect;
        }

        public bool AutoResizeHandleRect { get; set; }
        public float ExtraPoolCoverageRatio = 1.2f;
        public IDynamicDataSource DataSource;
        public RectTransform PrototypeCell;

        // UI

        public override GameObject UIRoot => scrollRect.gameObject;

        public RectTransform Viewport => scrollRect.viewport;
        public RectTransform Content => scrollRect.content;

        internal Slider slider;
        internal ScrollRect scrollRect;
        internal VerticalLayoutGroup contentLayout;

        // Cache / tracking

        // 1.2x height of Viewport height.
        private Vector2 RecycleViewBounds;

        private readonly List<CachedCell> CellPool = new List<CachedCell>();
        // private readonly List<Vector2> DataHeightCache = new List<Vector2>();
        private readonly List<float> DataHeightCache = new List<float>();

        public float AdjustedTotalCellHeight => TotalCellHeight + (contentLayout.spacing * (CellPool.Count - 1));
        internal float TotalCellHeight
        {
            get => m_totalCellHeight;
            set
            {
                if (TotalCellHeight.Equals(value))
                    return;
                m_totalCellHeight = value;
                SetDisplayedContentHeight();
            }
        }
        private float m_totalCellHeight;

        /// <summary>
        /// The first and last displayed indexes relative to the DataSource's list
        /// </summary>
        private int topDataIndex, bottomDataIndex;

        public bool IsDisplayed(int index) => index >= topDataIndex && index <= bottomDataIndex;

        /// <summary>
        /// For keeping track of where cellPool[0] and cellPool[last] actually are in the transform heirarchy
        /// </summary>
        private int topMostCellIndex, bottomMostCellIndex;

        private int CurrentDataCount => bottomDataIndex + 1;

        private Vector2 _prevAnchoredPos;
        private Vector2 _prevViewportSize; // TODO track viewport height and rebuild on change? or leave that to datasource?

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

        public void Initialize(IDynamicDataSource dataSource)
        {
            this.slider = scrollRect.GetComponentInChildren<Slider>();
            slider.onValueChanged.AddListener(OnSliderValueChanged);
            this.contentLayout = scrollRect.content.GetComponent<VerticalLayoutGroup>();

            DataSource = dataSource;

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
            //RefreshDataHeightCache();

            CreateCellPool();
            BuildInitialHeightCache();

            SetDisplayedContentHeight();

            UpdateSliderPositionAndSize();

            scrollRect.onValueChanged.AddListener(OnValueChangedListener);
        }

        private void BuildInitialHeightCache()
        {
            float defaultHeight = DataSource.DefaultCellHeight;
            for (int i = 0; i < DataSource.ItemCount; i++)
            {
                if (i < CellPool.Count)
                    DataHeightCache.Add(CellPool[i].Height);
                else
                    DataHeightCache.Add(defaultHeight);
            }
        }

        private float GetStartPositionOfData(int index)
        {
            float start = 0f;
            for (int i = 0; i < index; i++)
                start += DataHeightCache[i] + contentLayout.spacing;
            return start;
        }

        // Refresh methods

        // TODO need a quick refresh method / populate cells (?)

        public void SetRecycleViewBounds()
        {
            var extra = (Viewport.rect.height * ExtraPoolCoverageRatio) - Viewport.rect.height;
            RecycleViewBounds = new Vector2(Viewport.MinY() + extra, Viewport.MaxY() - extra);
        }

        public void SetDisplayedContentHeight()
        {
            var viewRect = scrollRect.viewport;
            scrollRect.content.sizeDelta = new Vector2(
                scrollRect.content.sizeDelta.x, 
                AdjustedTotalCellHeight - viewRect.rect.height);
        }

        public void UpdateDisplayedHeightCache()
        {
            if (!CellPool.Any()) return;

            int cellIdx = topMostCellIndex;
            int dataIndex = topDataIndex;
            int iterated = 0;
            while (iterated < CellPool.Count)
            {
                var cell = CellPool[cellIdx];
                cellIdx++;
                if (cellIdx >= CellPool.Count)
                    cellIdx = 0;

                cell.Height = cell.Rect.rect.height;
                DataHeightCache[dataIndex] = cell.Height;

                dataIndex++;
                iterated++;
            }
        }

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

            //Set the prototype cell active and set cell anchor as top 
            PrototypeCell.gameObject.SetActive(true);

            //Temps
            float currentPoolCoverage = 0f;

            float requiredCoverage = scrollRect.viewport.rect.height * ExtraPoolCoverageRatio;

            topMostCellIndex = 0;
            topDataIndex = 0;
            bottomMostCellIndex = -1;

            // create cells until the Pool area is covered.
            // use minimum default height so that maximum pool count is reached.
            while (currentPoolCoverage < requiredCoverage)
            {
                bottomMostCellIndex++;

                //Instantiate and add to Pool
                RectTransform rect = GameObject.Instantiate(PrototypeCell.gameObject).GetComponent<RectTransform>();
                rect.name = $"Cell_{CellPool.Count + 1}";
                var cell = (IDynamicCell)DataSource.CreateCell(rect);
                CellPool.Add(new CachedCell(this, rect, cell));
                rect.SetParent(scrollRect.content, false);

                currentPoolCoverage += rect.rect.height + this.contentLayout.spacing;
            }

            bottomDataIndex = bottomMostCellIndex;

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

        private void SetCell(CachedCell cell, int dataIndex)
        {
            cell.Cell.Enable();
            DataSource.SetCell(cell.Cell, dataIndex);
            cell.Height = cell.Cell.Enabled ? cell.Rect.rect.height : 0f;
        }

        // Value change processor

        internal void OnValueChangedListener(Vector2 val)
        {
            if (ExternallySetting)
                return;

            // ExternallySetting = true;
            SetRecycleViewBounds();
            UpdateDisplayedHeightCache();
            // ExternallySetting = false;

            Vector2 dir = scrollRect.content.anchoredPosition - _prevAnchoredPos;
            var adjust = ProcessValueChange(dir.y);
            scrollRect.m_ContentStartPosition += adjust;
            scrollRect.m_PrevPosition += adjust;
            _prevAnchoredPos = scrollRect.content.anchoredPosition;

            UpdateSliderPositionAndSize();
        }

        internal Vector2 ProcessValueChange(float yChange)
        {
            if (ExternallySetting)
                return Vector2.zero;

            SetRecycleViewBounds();

            float adjust = 0f;
            var topCell = CellPool[topMostCellIndex].Rect;
            var bottomCell = CellPool[bottomMostCellIndex].Rect;

            if (yChange > 0) // Scrolling down
            {
                if (topCell.position.y >= RecycleViewBounds.x)
                    adjust = RecycleTopToBottom();

            }
            else if (yChange < 0) // Scrolling up
            {
                if (bottomCell.MaxY() < RecycleViewBounds.y)
                    adjust = RecycleBottomToTop();
            }

            return new Vector2(0, adjust);
        }

        /// <summary>
        /// Recycles cells from top to bottom in the List heirarchy
        /// </summary>
        private float RecycleTopToBottom()
        {
            ExternallySetting = true;

            float recycledheight = 0f;
            //float posY;

            while (CellPool[topMostCellIndex].Rect.position.y > RecycleViewBounds.x && CurrentDataCount < DataSource.ItemCount)
            {
                var cell = CellPool[topMostCellIndex];

                //Move top cell to bottom
                cell.Rect.SetAsLastSibling();
                LayoutRebuilder.ForceRebuildLayoutImmediate(Content);

                // update content position
                Content.anchoredPosition -= Vector2.up * cell.Height;
                recycledheight += cell.Height + contentLayout.spacing;

                //set Cell
                DataSource.SetCell(cell.Cell, CurrentDataCount);
                cell.Height = cell.Rect.rect.height;

                LayoutRebuilder.ForceRebuildLayoutImmediate(Content);

                //set new indices
                topDataIndex++;
                bottomDataIndex++;

                bottomMostCellIndex = topMostCellIndex;
                topMostCellIndex = (topMostCellIndex + 1) % CellPool.Count;
            }

            return -recycledheight;
        }

        /// <summary>
        /// Recycles cells from bottom to top in the List heirarchy
        /// </summary>
        private float RecycleBottomToTop()
        {
            ExternallySetting = true;

            float recycledheight = 0f;
            //float posY;

            while (CellPool[bottomMostCellIndex].Rect.MaxY() < RecycleViewBounds.y && CurrentDataCount > CellPool.Count)
            {
                var cell = CellPool[bottomMostCellIndex];

                //Move bottom cell to top
                cell.Rect.SetAsFirstSibling();
                LayoutRebuilder.ForceRebuildLayoutImmediate(Content);

                // update content position
                Content.anchoredPosition += Vector2.up * cell.Height;
                recycledheight += cell.Height + contentLayout.spacing;

                LayoutRebuilder.ForceRebuildLayoutImmediate(Content);

                //set new index
                topDataIndex--;
                bottomDataIndex--;

                //set Cell
                DataSource.SetCell(cell.Cell, topDataIndex);
                cell.Height = cell.Rect.rect.height;

                //set new indices
                topMostCellIndex = bottomMostCellIndex;
                bottomMostCellIndex = (bottomMostCellIndex - 1 + CellPool.Count) % CellPool.Count;
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
                    slider.value = (float)((decimal)topDataIndex / Math.Max(1, total - CellPool.Count));
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
