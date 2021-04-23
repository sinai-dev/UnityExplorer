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

        /// <summary>Get the first range (division of DefaultHeight) which the position appears in.</summary>
        private int GetRangeIndexOfPosition(float position) => (int)Math.Floor((decimal)position / (decimal)DefaultHeight);

        /// <summary>
        /// Lookup table for "which data index first appears at this position"<br/>
        /// Index: DefaultHeight * index from top of data<br/>
        /// Value: the first data index at this position<br/>
        /// </summary>
        private readonly List<int> rangeCache = new List<int>();

        public DataViewInfo this[int index]
        {
            get => heightCache[index];
            set => SetIndex(index, value);
        }

        /// <summary>
        /// Get the spread of the height, starting from the start position.<br/><br/>
        /// The "spread" begins at the start of the next interval of the DefaultHeight, then increases for
        /// every interval beyond that.
        /// </summary>
        private int GetRangeSpread(float startPosition, float height)
        {
            // get the remainder of the start position divided by min height
            float rem = startPosition % DefaultHeight;

            // if there is a remainder, this means the previous cell started in 
            // our first cell and they take priority, so reduce our height by
            // (minHeight - remainder) to account for that. We need to fill that
            // gap and reach the next cell before we take priority.
            if (!Mathf.Approximately(rem, 0f))
                height -= (DefaultHeight - rem);

            return (int)Math.Ceiling((decimal)height / (decimal)DefaultHeight);
        }

        /// <summary>Append a data index to the cache with the provided height value.</summary>
        public void Add(float value)
        {
            int spread = GetRangeSpread(totalHeight, value);

            heightCache.Add(new DataViewInfo()
            {
                height = value,
                startPosition = TotalHeight,
                normalizedSpread = spread,
            });

            int dataIdx = heightCache.Count - 1;
            for (int i = 0; i < spread; i++)
                rangeCache.Add(dataIdx);

            totalHeight += value;
        }

        /// <summary>Remove the last (highest count) index from the height cache.</summary>
        public void RemoveLast()
        {
            if (!heightCache.Any())
                return;

            var val = heightCache[heightCache.Count - 1];
            totalHeight -= val;
            heightCache.RemoveAt(heightCache.Count - 1);
        }

        /// <summary>Get the data index at the specific position of the total height cache.</summary>
        public int GetDataIndexAtPosition(float desiredHeight) => GetDataIndexAtPosition(desiredHeight, out _);

        /// <summary>Get the data index at the specific position of the total height cache.</summary>
        public int GetDataIndexAtPosition(float desiredHeight, out DataViewInfo cache)
        {
            cache = null;
            int rangeIndex = GetRangeIndexOfPosition(desiredHeight);

            if (rangeIndex <= 0)
                return 0;

            if (rangeCache.Count <= rangeIndex)
                return rangeCache[rangeCache.Count - 1];

            int dataIndex = rangeCache[rangeIndex];
            cache = heightCache[dataIndex];

            return dataIndex;
        }

        /// <summary>Set a given data index with the specified value.</summary>
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
            int spread = GetRangeSpread(cache.startPosition, height);

            if (rangeCache.Count <= rangeIndex)
            {
                // This should never happen, there is a gap in the previous data. Trigger rebuild?
                // I stress-tested the scroll pool and didn't seem to encounter this, leaving for now.
                ExplorerCore.LogWarning($"rangeToDataIndex.Count ({rangeCache.Count}) <= rangeIndex ({rangeIndex})?");
            }
            else if (spread != cache.normalizedSpread)
            {
                // The cell's spread has changed, need to update.

                int spreadDiff = spread - cache.normalizedSpread;
                cache.normalizedSpread = spread;

                int rangeStart = -1;

                // the start will always be at LEAST (no less) PrototypeHeight * index, cells can never be smaller than that.
                int minStart = rangeCache[dataIndex];

                for (int i = minStart; i < rangeCache.Count; i++)
                {
                    if (rangeCache[i] == dataIndex)
                    {
                        rangeStart = i;
                        break;
                    }

                    // our index is further down. add the min difference and try again.
                    // the iterator will add 1 on the next loop so account for that.
                    int jmp = dataIndex - rangeCache[i] - 1;
                    i += jmp < 1 ? 0 : jmp;
                }

                if (rangeStart == -1)
                {
                    ExplorerCore.LogWarning($"DataHeightCache corrupt? Couldn't find dataIndex {dataIndex} anywhere in range cache.");
                    return;
                }

                if (spreadDiff > 0)
                {
                    // need to insert
                    for (int i = 0; i < spreadDiff; i++)
                        rangeCache.Insert(rangeStart, dataIndex);
                }
                else
                {
                    // need to remove
                    for (int i = 0; i < -spreadDiff; i++)
                        rangeCache.RemoveAt(rangeStart);
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
    }
}
