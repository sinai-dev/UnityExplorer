using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityExplorer.UI.Widgets
{
    public interface IPoolDataSource<T> where T : ICell
    {
        int ItemCount { get; }

        void OnCellBorrowed(T cell);
        //void ReleaseCell(T cell);

        void SetCell(T cell, int index);
        //void DisableCell(T cell, int index);
    }
}
