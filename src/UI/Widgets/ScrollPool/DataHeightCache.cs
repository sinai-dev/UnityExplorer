using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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
        private DataHeightCache SisterCache { get; }

        public DataHeightCache(ScrollPool scrollPool)
        {
            ScrollPool = scrollPool;
        }

        public DataHeightCache(ScrollPool scrollPool, DataHeightCache sisterCache) : this(scrollPool)
        {
            this.SisterCache = sisterCache;

            for (int i = 0; i < scrollPool.DataSource.ItemCount; i++)
                Add(sisterCache[ScrollPool.DataSource.GetRealIndexOfTempIndex(i)]);
        }

        private readonly List<DataViewInfo> heightCache = new List<DataViewInfo>();

        public int Count => heightCache.Count;

        public float TotalHeight => totalHeight;
        private float totalHeight;

        public float DefaultHeight => m_defaultHeight ?? (float)(m_defaultHeight = ScrollPool.PrototypeCell.rect.height);
        private float? m_defaultHeight;

        private int GetRangeIndexOfPosition(float position) => (int)Math.Floor((decimal)position / (decimal)DefaultHeight);

        // for efficient lookup of "which data index is at this position"
        // list index: DefaultHeight * index from top of data
        // list value: the data index at this position
        private readonly List<int> rangeToDataIndexCache = new List<int>();

        public DataViewInfo this[int index]
        {
            get => heightCache[index];
            set => SetIndex(index, value);
        }

        private int GetSpread(float startPosition, float height)
        {
            float rem = startPosition % DefaultHeight;

            if (!Mathf.Approximately(rem, 0f))
                height -= (DefaultHeight - rem);

            return (int)Math.Ceiling((decimal)height / (decimal)DefaultHeight);
        }

        public void Add(float value)
        {
            int spread = GetSpread(totalHeight, value);

            heightCache.Add(new DataViewInfo()
            {
                height = value,
                startPosition = TotalHeight,
                normalizedSpread = spread,
            });

            int dataIdx = heightCache.Count - 1;
            for (int i = 0; i < spread; i++)
                rangeToDataIndexCache.Add(dataIdx);

            totalHeight += value;
        }

        public void RemoveLast()
        {
            if (!heightCache.Any())
                return;

            var val = heightCache[heightCache.Count - 1];
            totalHeight -= val;
            heightCache.RemoveAt(heightCache.Count - 1);
        }

        public void SetIndex(int dataIndex, float height, bool ignoreDataCount = false)
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
                Add(height);
                return;
            }

            var cache = heightCache[dataIndex];
            var prevHeight = cache.height;

            var diff = height - prevHeight;
            if (diff != 0.0f)
            {
                // ExplorerCore.LogWarning("Height for data index " + dataIndex + " changed by " + diff);
                totalHeight += diff;
                cache.height = height;
            }

            // update our start position using the previous cell (if it exists)
            if (dataIndex > 0)
            {
                var prev = heightCache[dataIndex - 1];
                cache.startPosition = prev.startPosition + prev.height;
            }

            int rangeIndex = GetRangeIndexOfPosition(cache.startPosition);
            // var spread = GetRangeIndexOfPosition(value);
            int spread = GetSpread(cache.startPosition, height);

            // setting range index beyond current count, need to append
            if (rangeToDataIndexCache.Count <= rangeIndex)
            {
                // if the gap is > 1, we need to fill that gap. This shouldn't really ever happen but just in case.
                if (rangeToDataIndexCache.Count < rangeIndex)
                {
                    int lastDataIdx = rangeToDataIndexCache[rangeToDataIndexCache.Count - 1];
                    while (rangeToDataIndexCache.Count < rangeIndex)
                    {
                        if (lastDataIdx < dataIndex - 1)
                            lastDataIdx++;
                        rangeToDataIndexCache.Add(lastDataIdx);
                        heightCache[lastDataIdx].normalizedSpread++;
                    }
                }

                // apend spread for this data
                for (int i = 0; i < spread; i++)
                    rangeToDataIndexCache.Add(dataIndex);

                cache.normalizedSpread = spread;
            }
            else if (spread != cache.normalizedSpread)
            {
                // The cell's spread has changed, need to update.

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
            if (SisterCache != null)
            {
                var realIdx = ScrollPool.DataSource.GetRealIndexOfTempIndex(dataIndex);
                if (realIdx >= 0)
                    SisterCache.SetIndex(realIdx, height, true);
            }
        }

        public int GetDataIndexAtPosition(float desiredHeight)
        {
            return GetDataIndexAtPosition(desiredHeight, out _);
        }

        public int GetDataIndexAtPosition(float desiredHeight, out DataViewInfo cache)
        {
            cache = null;
            int rangeIndex = GetRangeIndexOfPosition(desiredHeight);

            if (rangeToDataIndexCache.Count <= rangeIndex || rangeIndex < 0)
                return -1;

            int dataIndex = rangeToDataIndexCache[rangeIndex];
            cache = heightCache[dataIndex];

            return dataIndex;
        }
    }
}
