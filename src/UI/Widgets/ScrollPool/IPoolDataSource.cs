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
        void DisableCell(ICell cell, int index);

        int GetRealIndexOfTempIndex(int tempIndex);

        ICell CreateCell(RectTransform cellTransform);
    }
}
