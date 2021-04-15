using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Models;

namespace UnityExplorer.UI.Widgets.InfiniteScroll
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

        private readonly Vector3[] _corners = new Vector3[4];
        private bool _recycling;
        private Vector2 _prevAnchoredPos;
        internal Vector2 _lastScroll;

        internal int currentItemCount; //item count corresponding to the datasource.
        internal int topMostCellIndex, bottomMostCellIndex; //Topmost and bottommost cell in the heirarchy
        internal int _topMostCellColoumn, _bottomMostCellColoumn; // used for recyling in Grid layout. top-most and bottom-most coloumn

        // Flag to keep track of when we are manually setting our slider/scrollrect value directly, to avoid callback loops.
        //private bool ExternallySetting = false;

        // external sources use this flag, it will stay true until the start of the next frame to prevent our update overwriting it.
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

                ExplorerCore.Log("Slider value changed");

                // Jump to val * count (ie, 0.0 would jump to top, 1.0 would jump to bottom)
                var index = Math.Floor(val * DataSource.ItemCount);
                JumpToIndex((int)index);

                this.ExternallySetting = false;
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
            //SetSliderFromScrollValue();
            //internallySetting = false;
        }

        internal void SetSliderFromScrollValue()
        {
            // calculate where slider handle should be based on displayed range.
            var range = GetDisplayedRange();
            int total = DataSource.ItemCount;
            var spread = range.y - range.x;

            if (spread >= total)
                _slider.value = 0f;
            else
                // top-most displayed index divided by (totalCount - displayedRange)
                _slider.value = (float)((decimal)range.x / (decimal)(total - spread));
        }

        public void JumpToIndex(int index)
        {
            var realCount = DataSource.ItemCount;

            index = Math.Min(index, realCount - 1);

            var indexBuffer = (int)(_cachedCells.Count * (1 - (index / (decimal)(realCount - 1))));

            currentItemCount = index + indexBuffer;
            currentItemCount = Math.Max(Math.Min(currentItemCount, realCount), _cachedCells.Count);
            Refresh();

            var y = 0f;
            var displayRange = scrollRect.viewport.rect.height / _cellHeight;
            var poolRange = scrollRect.content.rect.height / _cellHeight;
            var poolExtra = poolRange - displayRange;

            if (index >= realCount - poolExtra)
                y = _cellHeight * (index - realCount + (poolExtra * 2));

            scrollRect.content.anchoredPosition = new Vector2(scrollRect.content.anchoredPosition.x, y);
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
            float threshHold = _cellHeight * 2; //RecyclingThreshold * (_corners[2].y - _corners[0].y);
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
            float requiredCoverage = scrollRect.viewport.rect.height + (_cellHeight * 4);

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

    //public class InfiniteScrollRect : ScrollRect
    //{

    //    public IListDataSource DataSource;

    //    internal RectTransform PrototypeCell;
    //    internal Slider _slider;

    //    // Cell pool
    //    private float _cellWidth, _cellHeight;
    //    private List<RectTransform> _cellPool;
    //    private List<ICell> _cachedCells;
    //    private Bounds _recyclableViewBounds;

    //    private readonly Vector3[] _corners = new Vector3[4];
    //    private bool _recycling;
    //    private Vector2 _prevAnchoredPos;
    //    internal Vector2 _lastScroll;

    //    internal int currentItemCount; //item count corresponding to the datasource.
    //    internal int topMostCellIndex, bottomMostCellIndex; //Topmost and bottommost cell in the heirarchy
    //    internal int _topMostCellColoumn, _bottomMostCellColoumn; // used for recyling in Grid layout. top-most and bottom-most coloumn

    //    // Flag to keep track of when we are manually setting our slider/scrollrect value directly, to avoid callback loops.
    //    private bool internallySetting = false;

    //    // external sources use this flag, it will stay true until the start of the next frame to prevent our update overwriting it.
    //    public bool ExternallySetting
    //    {
    //        get => externallySetting;
    //        internal set
    //        {
    //            if (externallySetting == value)
    //                return;
    //            timeOfLastExternalSet = Time.time;
    //            externallySetting = true;
    //        }
    //    }
    //    private bool externallySetting;
    //    private float timeOfLastExternalSet;

    //    private Vector2 zeroVector = Vector2.zero;

    //    public int PoolCount => _cachedCells.Count;

    //    #region MONOBEHAVIOUR

    //    internal new void Start()
    //    {
    //        // Link up the Slider and ScrollRect onValueChanged to sync them.

    //        _slider = this.GetComponentInChildren<Slider>();

    //        onValueChanged.AddListener((Vector2 val) =>
    //        {
    //            try
    //            {
    //                if (internallySetting || ExternallySetting)
    //                    return;
    //                internallySetting = true;

    //                SetSliderFromScrollValue();

    //                internallySetting = false;
    //            }
    //            catch (Exception ex)
    //            {
    //                ExplorerCore.Log(ex);
    //            }
    //        });

    //        _slider.onValueChanged.AddListener((float val) =>
    //        {
    //            if (internallySetting || ExternallySetting)
    //                return;
    //            internallySetting = true;

    //            // Jump to val * count (ie, 0.0 would jump to top, 1.0 would jump to bottom)
    //            var index = Math.Floor(val * DataSource.ItemCount);
    //            JumpToIndex((int)index);

    //            internallySetting = false;
    //        });
    //    }

    //    internal void Update()
    //    {
    //        if (externallySetting && timeOfLastExternalSet < Time.time)
    //            externallySetting = false;
    //    }

    //    #endregion

    //    #region LISTENERS

    //    internal void OnValueChangedListener(Vector2 normalizedPos)
    //    {
    //        Vector2 dir = base.content.anchoredPosition - _prevAnchoredPos;
    //        m_ContentStartPosition += ProcessValueChange(dir);
    //        _prevAnchoredPos = base.content.anchoredPosition;
    //    }

    //    #endregion

    //    public Vector2 GetDisplayedRange()
    //    {
    //        int max = currentItemCount;
    //        int min = max - _cachedCells.Count;
    //        return new Vector2(min, max);
    //    }

    //    public void Initialize(IListDataSource dataSource)
    //    {
    //        DataSource = dataSource;

    //        vertical = true;
    //        horizontal = false;

    //        _prevAnchoredPos = base.content.anchoredPosition;
    //        onValueChanged.RemoveListener(OnValueChangedListener);

    //        RuntimeProvider.Instance.StartCoroutine(InitCoroutine(() =>
    //        {
    //            onValueChanged.AddListener(OnValueChangedListener);
    //        }));
    //    }

    //    public void ReloadData()
    //    {
    //        ReloadData(DataSource);
    //    }

    //    public void ReloadData(IListDataSource dataSource)
    //    {
    //        if (onValueChanged == null)
    //            return;

    //        StopMovement();

    //        onValueChanged.RemoveListener(OnValueChangedListener);

    //        DataSource = dataSource;

    //        RuntimeProvider.Instance.StartCoroutine(InitCoroutine(() =>
    //            onValueChanged.AddListener(OnValueChangedListener)
    //        ));

    //        _prevAnchoredPos = base.content.anchoredPosition;
    //    }

    //    public void Refresh()
    //    {
    //        if (DataSource == null || _cellPool == null)
    //            return;

    //        int count = DataSource.ItemCount;
    //        if (currentItemCount > count)
    //            currentItemCount = Math.Max(count, _cellPool.Count);

    //        SetRecyclingBounds();
    //        RecycleBottomToTop();
    //        RecycleTopToBottom();

    //        PopulateCells();

    //        RefreshContentSize();

    //        // Close, but not quite accurate enough to be useful.
    //        internallySetting = true;
    //        SetSliderFromScrollValue();
    //        internallySetting = false;
    //    }

    //    internal void SetSliderFromScrollValue()
    //    {
    //        // calculate where slider handle should be based on displayed range.
    //        var range = GetDisplayedRange();
    //        int total = DataSource.ItemCount;
    //        var spread = range.y - range.x;

    //        //var orig = _slider.value;

    //        if (spread >= total)
    //            _slider.value = 0f;
    //        else
    //            // top-most displayed index divided by (totalCount - displayedRange)
    //            _slider.value = (float)((decimal)range.x / (decimal)(total - spread));
    //    }

    //    public void JumpToIndex(int index)
    //    {
    //        var realCount = DataSource.ItemCount;

    //        index = Math.Min(index, realCount - 1);

    //        var indexBuffer = (int)(_cachedCells.Count * (1 - (index / (decimal)(realCount - 1))));

    //        currentItemCount = index + indexBuffer;
    //        currentItemCount = Math.Max(Math.Min(currentItemCount, realCount), _cachedCells.Count);
    //        Refresh();

    //        var y = 0f;

    //        var displayRange = viewport.rect.height / _cellHeight;
    //        var poolRange = content.rect.height / _cellHeight;
    //        var poolExtra = poolRange - displayRange;

    //        if (index >= realCount - poolExtra)
    //            y = _cellHeight * (index - realCount + poolExtra);

    //        content.anchoredPosition = new Vector2(content.anchoredPosition.x, y);
    //    }

    //    public void PopulateCells()
    //    {
    //        var width = viewport.GetComponent<RectTransform>().rect.width;
    //        content.sizeDelta = new Vector2(width, content.sizeDelta.y);

    //        int cellIndex = topMostCellIndex;
    //        var itemIndex = currentItemCount - _cachedCells.Count;
    //        int iterated = 0;
    //        while (iterated < _cachedCells.Count)
    //        {
    //            var cell = _cachedCells[cellIndex];
    //            cellIndex++;
    //            if (cellIndex < 0)
    //                continue;
    //            if (cellIndex >= _cachedCells.Count)
    //                cellIndex = 0;
    //            DataSource.SetCell(cell, itemIndex);
    //            itemIndex++;

    //            var rect = _cellPool[cellIndex].GetComponent<RectTransform>();
    //            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);

    //            iterated++;
    //        }
    //    }

    //    #region RECYCLING INIT

    //    private IEnumerator InitCoroutine(Action onInitialized)
    //    {
    //        yield return null;
    //        SetTopAnchor(content);
    //        content.anchoredPosition = Vector3.zero;

    //        yield return null;
    //        SetRecyclingBounds();

    //        //Cell Poool
    //        CreateCellPool();
    //        currentItemCount = _cellPool.Count;
    //        topMostCellIndex = 0;
    //        bottomMostCellIndex = _cellPool.Count - 1;

    //        //Set content height according to no of rows
    //        RefreshContentSize();

    //        SetTopAnchor(content);

    //        onInitialized?.Invoke();
    //    }

    //    private void RefreshContentSize()
    //    {
    //        int noOfRows = _cachedCells.Where(it => it.Enabled).Count();
    //        float contentYSize = noOfRows * _cellHeight;
    //        content.sizeDelta = new Vector2(content.sizeDelta.x, contentYSize);
    //    }

    //    private void SetRecyclingBounds()
    //    {
    //        viewport.GetWorldCorners(_corners);
    //        float threshHold = _cellHeight / 2; //RecyclingThreshold * (_corners[2].y - _corners[0].y);
    //        _recyclableViewBounds.min = new Vector3(_corners[0].x, _corners[0].y - threshHold);
    //        _recyclableViewBounds.max = new Vector3(_corners[2].x, _corners[2].y + threshHold);
    //    }

    //    private void CreateCellPool()
    //    {
    //        //Reseting Pool
    //        if (_cellPool != null)
    //        {
    //            _cellPool.ForEach((RectTransform item) => Destroy(item.gameObject));
    //            _cellPool.Clear();
    //            _cachedCells.Clear();
    //        }
    //        else
    //        {
    //            _cachedCells = new List<ICell>();
    //            _cellPool = new List<RectTransform>();
    //        }

    //        //Set the prototype cell active and set cell anchor as top 
    //        PrototypeCell.gameObject.SetActive(true);
    //        SetTopAnchor(PrototypeCell);

    //        //Reset
    //        _topMostCellColoumn = _bottomMostCellColoumn = 0;

    //        //Temps
    //        float currentPoolCoverage = 0;
    //        int poolSize = 0;
    //        float posY = 0;

    //        //set new cell size according to its aspect ratio
    //        _cellWidth = content.rect.width;
    //        _cellHeight = PrototypeCell.rect.height;

    //        //Get the required pool coverage and mininum size for the Cell pool
    //        float requiredCoverage = viewport.rect.height + (_cellHeight * 2);

    //        //create cells untill the Pool area is covered
    //        while (currentPoolCoverage < requiredCoverage)
    //        {
    //            //Instantiate and add to Pool
    //            RectTransform item = Instantiate(PrototypeCell.gameObject).GetComponent<RectTransform>();
    //            item.name = $"Cell_{_cachedCells.Count + 1}";
    //            item.sizeDelta = new Vector2(_cellWidth, _cellHeight);
    //            _cellPool.Add(item);
    //            item.SetParent(content, false);

    //            item.anchoredPosition = new Vector2(0, posY);
    //            posY = item.anchoredPosition.y - item.rect.height;
    //            currentPoolCoverage += item.rect.height;

    //            //Setting data for Cell
    //            _cachedCells.Add(item.GetComponent<ICell>());
    //            DataSource.SetCell(_cachedCells[_cachedCells.Count - 1], poolSize);

    //            //Update the Pool size
    //            poolSize++;
    //        }

    //        //Deactivate prototype cell if it is not a prefab(i.e it's present in scene)
    //        if (PrototypeCell.gameObject.scene.IsValid())
    //            PrototypeCell.gameObject.SetActive(false);
    //    }
    //    #endregion

    //    #region RECYCLING

    //    public Vector2 ProcessValueChange(Vector2 direction)
    //    {
    //        if (_recycling || _cellPool == null || _cellPool.Count == 0) return zeroVector;

    //        //Updating Recyclable view bounds since it can change with resolution changes.
    //        SetRecyclingBounds();

    //        _lastScroll = direction;

    //        if (direction.y > 0 && _cellPool[bottomMostCellIndex].MaxY() > _recyclableViewBounds.min.y)
    //        {
    //            return RecycleTopToBottom();
    //        }
    //        else if (direction.y < 0 && _cellPool[topMostCellIndex].MinY() < _recyclableViewBounds.max.y)
    //        {
    //            return RecycleBottomToTop();
    //        }

    //        return zeroVector;
    //    }

    //    /// <summary>
    //    /// Recycles cells from top to bottom in the List heirarchy
    //    /// </summary>
    //    private Vector2 RecycleTopToBottom()
    //    {
    //        _recycling = true;

    //        int n = 0;
    //        float posY;

    //        //to determine if content size needs to be updated
    //        //Recycle until cell at Top is avaiable and current item count smaller than datasource
    //        while (_cellPool[topMostCellIndex].MinY() > _recyclableViewBounds.max.y && currentItemCount < DataSource.ItemCount)
    //        {
    //            //Move top cell to bottom
    //            posY = _cellPool[bottomMostCellIndex].anchoredPosition.y - _cellPool[bottomMostCellIndex].sizeDelta.y;
    //            _cellPool[topMostCellIndex].anchoredPosition = new Vector2(_cellPool[topMostCellIndex].anchoredPosition.x, posY);

    //            //Cell for row at
    //            DataSource.SetCell(_cachedCells[topMostCellIndex], currentItemCount);

    //            //set new indices
    //            bottomMostCellIndex = topMostCellIndex;
    //            topMostCellIndex = (topMostCellIndex + 1) % _cellPool.Count;

    //            currentItemCount++;
    //            n++;
    //        }

    //        //Content anchor position adjustment.
    //        _cellPool.ForEach((RectTransform cell) => cell.anchoredPosition += n * Vector2.up * _cellPool[topMostCellIndex].sizeDelta.y);
    //        content.anchoredPosition -= n * Vector2.up * _cellPool[topMostCellIndex].sizeDelta.y;
    //        _recycling = false;
    //        return -new Vector2(0, n * _cellPool[topMostCellIndex].sizeDelta.y);
    //    }

    //    /// <summary>
    //    /// Recycles cells from bottom to top in the List heirarchy
    //    /// </summary>
    //    private Vector2 RecycleBottomToTop()
    //    {
    //        _recycling = true;

    //        int n = 0;
    //        float posY = 0;

    //        //to determine if content size needs to be updated
    //        //Recycle until cell at bottom is avaiable and current item count is greater than cellpool size
    //        while (_cellPool[bottomMostCellIndex].MaxY() < _recyclableViewBounds.min.y && currentItemCount > _cellPool.Count)
    //        {
    //            //Move bottom cell to top
    //            posY = _cellPool[topMostCellIndex].anchoredPosition.y + _cellPool[topMostCellIndex].sizeDelta.y;
    //            _cellPool[bottomMostCellIndex].anchoredPosition = new Vector2(_cellPool[bottomMostCellIndex].anchoredPosition.x, posY);
    //            n++;

    //            currentItemCount--;

    //            //Cell for row at
    //            DataSource.SetCell(_cachedCells[bottomMostCellIndex], currentItemCount - _cellPool.Count);

    //            //set new indices
    //            topMostCellIndex = bottomMostCellIndex;
    //            bottomMostCellIndex = (bottomMostCellIndex - 1 + _cellPool.Count) % _cellPool.Count;
    //        }

    //        _cellPool.ForEach((RectTransform cell) => cell.anchoredPosition -= n * Vector2.up * _cellPool[topMostCellIndex].sizeDelta.y);
    //        content.anchoredPosition += n * Vector2.up * _cellPool[topMostCellIndex].sizeDelta.y;
    //        _recycling = false;
    //        return new Vector2(0, n * _cellPool[topMostCellIndex].sizeDelta.y);
    //    }

    //    #endregion

    //    #region  HELPERS

    //    /// <summary>
    //    /// Anchoring cell and content rect transforms to top preset. Makes repositioning easy.
    //    /// </summary>
    //    /// <param name="rectTransform"></param>
    //    private void SetTopAnchor(RectTransform rectTransform)
    //    {
    //        //Saving to reapply after anchoring. Width and height changes if anchoring is change. 
    //        float width = rectTransform.rect.width;
    //        float height = rectTransform.rect.height;

    //        //Setting top anchor 
    //        rectTransform.anchorMin = new Vector2(0.5f, 1);
    //        rectTransform.anchorMax = new Vector2(0.5f, 1);
    //        rectTransform.pivot = new Vector2(0.5f, 1);

    //        //Reapply size
    //        rectTransform.sizeDelta = new Vector2(width, height);
    //    }

    //    #endregion
    //}

}
