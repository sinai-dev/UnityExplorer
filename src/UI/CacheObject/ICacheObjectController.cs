using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityExplorer.UI.CacheObject;
using UnityExplorer.UI.CacheObject.Views;

namespace UnityExplorer.UI.CacheObject
{
    public interface ICacheObjectController
    {
        CacheObjectBase ParentCacheObject { get; }

        object Target { get; }
        Type TargetType { get; }

        bool CanWrite { get; }
    }

    public static class CacheObjectControllerHelper
    {
        // Helper so that this doesn't need to be copy+pasted between each implementation of the interface

        public static void SetCell(CacheObjectCell cell, int index, IList cachedEntries, Action<CacheObjectCell> onDataSetToCell)
        {
            if (index < 0 || index >= cachedEntries.Count)
            {
                if (cell.Occupant != null)
                    cell.Occupant.UnlinkFromView();

                cell.Disable();
                return;
            }

            var entry = (CacheObjectBase)cachedEntries[index];

            if (entry.CellView != null && entry.CellView != cell)
                entry.UnlinkFromView();

            if (cell.Occupant != null && cell.Occupant != entry)
                cell.Occupant.UnlinkFromView();

            if (entry.CellView != cell)
                entry.SetView(cell);

            entry.SetDataToCell(cell);

            onDataSetToCell?.Invoke(cell);
        }
    }
}
