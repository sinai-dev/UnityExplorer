using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityExplorer.UI.Widgets
{
    public interface ICell
    {
        bool Enabled { get; }

        RectTransform Rect { get; }

        void Enable();
        void Disable();
    }
}
