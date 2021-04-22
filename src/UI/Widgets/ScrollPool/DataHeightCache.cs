using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityExplorer.UI.Widgets
{
    public class DataViewInfo
    {
        public int dataIndex;
        public float height, startPosition;
        public int normalizedSpread;

        public static implicit operator float(DataViewInfo it) => it.height;
    }

    public class DataHeightCache
    {
        private ScrollPool ScrollPool { get; }
        private DataHeightCache sisterCache { get; }

        public DataHeightCache(ScrollPool scrollPool)
        {
            ScrollPool = scrollPool;
        }

        public DataHeightCache(ScrollPool scrollPool, DataHeightCache sisterCache) : this(scrollPool)
        {
            this.sisterCache = sisterCache;

            ExplorerCore.Log("Creating backup height cache, this count: " + scrollPool.DataSource.ItemCount);
            TakeFromSister(scrollPool.DataSource.ItemCount);
        }

        private readonly List<DataViewInfo> heightCache = new List<DataViewInfo>();

        public int Count => heightCache.Count;

        public float TotalHeight => totalHeight;
        private float totalHeight;

        public float DefaultHeight => ScrollPool.PrototypeCell.rect.height;

        private int GetNormalizedHeight(float height) => (int)Math.Floor((decimal)height / (decimal)DefaultHeight);

        // for efficient lookup of "which data index is at this position"
        // list index: DefaultHeight * index from top of data
        // list value: the data index at this position
        private readonly List<int> rangeToDataIndexCache = new List<int>();

        public DataViewInfo this[int index]
        {
            get => heightCache[index];
            set => SetIndex(index, value);
        }

        public void Add(float value)
        {
            int spread = GetNormalizedHeight(value);

            heightCache.Add(new DataViewInfo()
            {
                height = value,
                startPosition = TotalHeight,
                normalizedSpread = spread,
            });

            int dataIdx = heightCache.Count - 1;
            AppendDataSpread(dataIdx, spread);

            totalHeight += value;
        }

        public void AddRange(IEnumerable<DataViewInfo> collection)
        {
            foreach (var entry in collection)
                Add(entry);
        }

        public void TakeFromSister(int count)
        {
            for (int i = 0, i < count; i++)
                Add(sisterCache[ScrollPool.DataSource.GetRealIndexOfTempIndex(i)]);
        }

        public void RemoveLast()
        {
            if (!heightCache.Any())
                return;

            var val = heightCache[heightCache.Count - 1];
            totalHeight -= val;
            heightCache.RemoveAt(heightCache.Count - 1);
        }

        private void AppendDataSpread(int dataIdx, int spread)
        {
            while (spread > 0)
            {
                rangeToDataIndexCache.Add(dataIdx);
                spread--;
            }
        }

        public void SetIndex(int dataIndex, float value, bool ignoreDataCount = false)
        {
            if (!ignoreDataCount)
            {
                if (dataIndex >= ScrollPool.DataSource.ItemCount)
                {
                    while (heightCache.Count > dataIndex)
                        RemoveLast();
                    return;
                }
            }

            if (dataIndex >= heightCache.Count)
            {
                while (dataIndex > heightCache.Count)
                    Add(DefaultHeight);
                Add(value);
                return;
            }

            var cache = heightCache[dataIndex];
            var prevHeight = cache.height;

            var diff = value - prevHeight;
            if (diff != 0.0f)
            {
                // ExplorerCore.LogWarning("Height for data index " + dataIndex + " changed by " + diff);
                totalHeight += diff;
                cache.height = value;
            }

            // update our start position using the previous cell (if it exists)
            if (dataIndex > 0)
            {
                var prev = heightCache[dataIndex - 1];
                cache.startPosition = prev.startPosition + prev.height;
            }

            int rangeIndex = GetNormalizedHeight(cache.startPosition);
            var spread = GetNormalizedHeight(value);

            // If we are setting an index outside of our cached range we need to naively fill the gap
            if (rangeToDataIndexCache.Count <= rangeIndex)
            {
                if (rangeToDataIndexCache.Any())
                {
                    int lastDataIdx = rangeToDataIndexCache[rangeToDataIndexCache.Count - 1];
                    while (rangeToDataIndexCache.Count <= rangeIndex)
                    {
                        rangeToDataIndexCache.Add(lastDataIdx);
                        heightCache[lastDataIdx].normalizedSpread++;
                        if (lastDataIdx < dataIndex - 1)
                            lastDataIdx++;
                    }
                }

                AppendDataSpread(dataIndex, spread);
                cache.normalizedSpread = spread;
            }
            else if (spread != cache.normalizedSpread)
            {
                // The cell's height has changed by +/- DefaultCellHeight since we last set the range spread cache for it.
                // Need to add or remove accordingly.

                int spreadDiff = spread - cache.normalizedSpread;
                cache.normalizedSpread = spread;

                int rangeStart = -1;

                // the start will always be at LEAST (no less) PrototypeHeight * index, cells can never be smaller than that.
                int minStart = rangeToDataIndexCache[dataIndex];

                for (int i = minStart; i < rangeToDataIndexCache.Count; i++)
                {
                    if (rangeToDataIndexCache[i] == dataIndex)
                    {
                        rangeStart = i;
                        break;
                    }

                    // our index is further down. add the min difference and try again.
                    // the iterator will add 1 on the next loop so account for that.
                    int jmp = dataIndex - rangeToDataIndexCache[i] - 1;
                    i += jmp < 1 ? 0 : jmp;
                }

                if (rangeStart == -1)
                    rangeStart = rangeToDataIndexCache.Count - 1;

                if (spreadDiff > 0)
                {
                    // need to insert
                    for (int i = 0; i < spreadDiff; i++)
                        rangeToDataIndexCache.Insert(rangeStart, dataIndex);
                }
                else
                {
                    // need to remove
                    for (int i = 0; i < -spreadDiff; i++)
                        rangeToDataIndexCache.RemoveAt(rangeStart);
                }
            }

            // if sister cache is set, then update it too.
            if (sisterCache != null)
            {
                var realIdx = ScrollPool.DataSource.GetRealIndexOfTempIndex(dataIndex);
                if (realIdx >= 0)
                    sisterCache.SetIndex(realIdx, value, true);
            }
        }

        public int GetDataIndexAtPosition(float desiredHeight)
        {
            return GetDataIndexAtPosition(desiredHeight, out _);
        }

        public int GetDataIndexAtPosition(float desiredHeight, out DataViewInfo cache)
        {
            cache = null;
            int rangeIndex = GetNormalizedHeight(desiredHeight);

            if (rangeToDataIndexCache.Count <= rangeIndex)
                return -1;

            int dataIndex = rangeToDataIndexCache[rangeIndex];
            cache = heightCache[dataIndex];

            return dataIndex;
        }
    }
}
