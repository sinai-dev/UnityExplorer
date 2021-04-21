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

        public static implicit operator float(DataViewInfo ch) => ch.height;
    }

    public class DataHeightManager
    {
        private ScrollPool ScrollPool { get; }

        public DataHeightManager(ScrollPool scrollPool)
        {
            ScrollPool = scrollPool;
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

        public void RemoveLast()
        {
            if (!heightCache.Any())
                return;

            var val = heightCache[heightCache.Count - 1];
            totalHeight -= val;
            heightCache.RemoveAt(heightCache.Count - 1);

        }

        public void Clear()
        {
            heightCache.Clear();
            totalHeight = 0f;
        }

        private void AppendDataSpread(int dataIdx, int spread)
        {
            while (spread > 0)
            {
                rangeToDataIndexCache.Add(dataIdx);
                spread--;
            }
        }

        public void SetIndex(int dataIndex, float value)
        {
            if (dataIndex >= ScrollPool.DataSource.ItemCount)
                return;

            if (dataIndex >= heightCache.Count)
            {
                while (dataIndex > heightCache.Count)
                    Add(DefaultHeight);
                Add(value);
                return;
            }

            var curr = heightCache[dataIndex].height;
            //if (curr.Equals(value))
            //    return;

            // ExplorerCore.LogWarning("Updating height for data index " + dataIndex + " to " + value);
            var cache = heightCache[dataIndex];

            var diff = value - curr;
            totalHeight += diff;

            if (diff != 0.0f)
            {
                ExplorerCore.LogWarning("Height for data index " + dataIndex + " changed by " + diff);
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

                //ExplorerCore.Log("Spread changed to " + spread);

                // TODO improve this.
                // this could be expensive with large lists as we need to iterate until we find our data index.
                // could possibly maintain the 'range start index' of each data index, as long as maintaining that is 
                // not more expensive than this.

                int rangeStart = -1;
                for (int i = 0; i < rangeToDataIndexCache.Count; i++)
                {
                    if (rangeToDataIndexCache[i] == dataIndex)
                    {
                        rangeStart = i;
                        break;
                    }
                }

                if (rangeStart == -1)
                    throw new Exception($"Couldn't find range start index, rangeStart is -1.");

                if (spreadDiff > 0)
                {
                    // ExplorerCore.Log("Inserting " + spreadDiff + " at " + rangeStart);
                    // need to insert
                    for (int i = 0; i < spreadDiff; i++)
                        rangeToDataIndexCache.Insert(rangeStart, dataIndex);
                }
                else
                {
                    // ExplorerCore.Log("Removing " + -spreadDiff + " at " + rangeStart);
                    // need to remove
                    for (int i = 0; i < -spreadDiff; i++)
                        rangeToDataIndexCache.RemoveAt(rangeStart);
                }
            }
        }

        public int GetDataIndexAtPosition(float desiredHeight)
            => GetDataIndexAtPosition(desiredHeight, out _);

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
