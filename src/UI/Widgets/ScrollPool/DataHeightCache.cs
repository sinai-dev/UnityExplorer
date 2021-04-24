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
        //private DataHeightCache SisterCache { get; }

        public DataHeightCache(ScrollPool scrollPool)
        {
            ScrollPool = scrollPool;
        }

        public DataHeightCache(ScrollPool scrollPool, DataHeightCache sisterCache) : this(scrollPool)
        {
            //this.SisterCache = sisterCache;

            //for (int i = 0; i < scrollPool.DataSource.ItemCount; i++)
            //    Add(sisterCache[ScrollPool.DataSource.GetRealIndexOfTempIndex(i)]);
        }

        private readonly List<DataViewInfo> heightCache = new List<DataViewInfo>();

        public DataViewInfo this[int index]
        {
            get => heightCache[index];
            set => SetIndex(index, value);
        }

        public int Count => heightCache.Count;

        public float TotalHeight => totalHeight;
        private float totalHeight;

        public float DefaultHeight => m_defaultHeight ?? (float)(m_defaultHeight = ScrollPool.PrototypeCell.rect.height);
        private float? m_defaultHeight;

        /// <summary>
        /// Lookup table for "which data index first appears at this position"<br/>
        /// Index: DefaultHeight * index from top of data<br/>
        /// Value: the first data index at this position<br/>
        /// </summary>
        private readonly List<int> rangeCache = new List<int>();

        /// <summary>Get the first range (division of DefaultHeight) which the position appears in.</summary>
        private int GetRangeIndexOfPosition(float position) => (int)Math.Floor((decimal)position / (decimal)DefaultHeight);

        /// <summary>Same as GetRangeIndexOfPosition, except this rounds up to the next division if there was remainder from the previous cell.</summary>
        private int GetRangeCeilingOfPosition(float position) => (int)Math.Ceiling((decimal)position / (decimal)DefaultHeight);

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

            if (rangeIndex < 0)
                throw new Exception("Range index (" + rangeIndex + ") is below 0");

            if (rangeCache.Count <= rangeIndex)
                throw new Exception("Range index (" + rangeIndex + ") exceeded rangeCache count (" + rangeCache.Count + ")");

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
                // LogWarning("Height for data index " + dataIndex + " changed by " + diff);
                totalHeight += diff;
                cache.height = height;
            }

            // update our start position using the previous cell (if it exists)
            if (dataIndex > 0)
            {
                var prev = heightCache[dataIndex - 1];
                cache.startPosition = prev.startPosition + prev.height;
            }

            int rangeIndex = GetRangeCeilingOfPosition(cache.startPosition);
            int spread = GetRangeSpread(cache.startPosition, height);

            // check if our range cache is "corrupt" or not. If so we need to do a quick rebuild,
            // so that each cell's start position is correct again.
            if (rangeCache.Count <= rangeIndex || rangeCache[rangeIndex] != dataIndex)
            {
                RebuildStartPositions(ignoreDataCount);
                // get these values again after rebuilding
                rangeIndex = GetRangeCeilingOfPosition(cache.startPosition);
                spread = GetRangeSpread(cache.startPosition, height);
            }

            if (spread != cache.normalizedSpread)
            {
                // The cell's spread has changed, need to update.

                int spreadDiff = spread - cache.normalizedSpread;
                cache.normalizedSpread = spread;

                if (rangeCache[rangeIndex] != dataIndex)
                {
                    // In some rare cases we may not find our data index at the expected range index.
                    // We can make some educated guesses and find the real index pretty quickly.
                    int minStart = rangeCache[dataIndex];
                    for (int i = minStart; i < rangeCache.Count; i++)
                    {
                        if (rangeCache[i] == dataIndex)
                        {
                            rangeIndex = i;
                            break;
                        }

                        // If we somehow reached the end and didn't find the data index...
                        if (i == rangeCache.Count - 1)
                        {
                            // This should never happen. We might be in a rebuild right now so don't
                            // rebuild again, we could overflow the stack. Just log it.
                            ExplorerCore.LogWarning($"DataHeightCache: Looking for range index of data {dataIndex} but reached the end and didn't find it.");
                            ExplorerCore.Log($"startPos: {cache.startPosition}, rangeIndex: {rangeIndex}, total height: {TotalHeight}");
                            return;
                        }

                        // our data index is further down. add the min difference and try again.
                        // the iterator will add 1 on the next loop so account for that.
                        // also, add the (spread - 1) of the cell we found at this index to skip it.
                        int jmp = dataIndex - rangeCache[i] - 1;
                        jmp += heightCache[rangeCache[i]].normalizedSpread - 1;
                        i += jmp < 1 ? 0 : jmp;
                    }
                }

                if (spreadDiff > 0)
                {
                    // need to insert
                    for (int i = 0; i < spreadDiff; i++)
                    {
                        if (rangeCache[rangeIndex] == dataIndex)
                            rangeCache.Insert(rangeIndex, dataIndex);
                        else
                            ExplorerCore.LogWarning($"DataHeightCache error increasing spread of data {dataIndex}, " +
                                $"the value at range {rangeIndex} is {rangeCache[rangeIndex]}!");
                    }
                }
                else
                {
                    // need to remove
                    for (int i = 0; i < -spreadDiff; i++)
                        if (rangeCache[rangeIndex] == dataIndex)
                            rangeCache.RemoveAt(rangeIndex);
                        else
                            ExplorerCore.LogWarning($"DataHeightCache error decreasing spread of data {dataIndex}, " +
                                $"the value at range {rangeIndex} is {rangeCache[rangeIndex]}!");
                }
            }

            //// if sister cache is set, then update it too.
            //if (SisterCache != null)
            //{
            //    var realIdx = ScrollPool.DataSource.GetRealIndexOfTempIndex(dataIndex);
            //    if (realIdx >= 0)
            //        SisterCache.SetIndex(realIdx, height, true);
            //}
        }

        private void RebuildStartPositions(bool ignoreDataCount)
        {
            //start at 1 because 0's start pos is always 0
            for (int i = 1; i < heightCache.Count; i++)
                SetIndex(i, heightCache[i].height, ignoreDataCount);
        }
    }
}
