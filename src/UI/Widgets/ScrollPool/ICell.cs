using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExplorer.UI.ObjectPool;

namespace UnityExplorer.UI.Widgets
{
    public interface ICell : IPooledObject
    {
        bool Enabled { get; }

        RectTransform Rect { get; }

        void Enable();
        void Disable();
    }
}
