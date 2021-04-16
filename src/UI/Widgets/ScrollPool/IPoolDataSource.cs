using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityExplorer.UI.Widgets
{
    public interface IPoolDataSource
    {
        int ItemCount { get; }

        void SetCell(ICell cell, int index);

        ICell CreateCell(RectTransform cellTransform);
    }
}
