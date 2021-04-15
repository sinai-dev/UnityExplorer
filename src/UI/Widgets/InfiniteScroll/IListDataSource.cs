using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityExplorer.UI.Widgets.InfiniteScroll
{
    public interface IListDataSource
    {
        int ItemCount { get; }

        void SetCell(ICell cell, int index);
    }
}
