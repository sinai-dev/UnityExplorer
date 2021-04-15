using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityExplorer.UI.Widgets
{
    public class CachedTransform
    {
        public Transform RefTransform { get; }
        public CachedTransform Parent { get; internal set; }

        public string Name { get; internal set; }
        public int ChildCount { get; internal set; }
        public int Depth { get; internal set; }

        public bool Expanded { get; set; }

        public CachedTransform(Transform transform, CachedTransform parent = null)
        {
            RefTransform = transform;
            Expanded = false;
            Parent = parent;
            Update();
        }

        public void Update()
        {
            Name = RefTransform.name;
            ChildCount = RefTransform.childCount;
            Depth = Parent?.Depth + 1 ?? 0;
        }
    }
}
