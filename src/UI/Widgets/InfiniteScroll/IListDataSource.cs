using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityExplorer.UI.Widgets.InfiniteScroll
{
    public interface IListDataSource
    {
        int ItemCount { get; }

        void SetCell(ICell cell, int index);

        ICell CreateCell(RectTransform cellTransform);
    }
}
