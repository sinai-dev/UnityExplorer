using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityExplorer.UI.Widgets
{
    public class CachedTransform
    {
        public TransformTree Tree { get; }
        public Transform Value { get; private set; }
        public int InstanceID { get; private set; }
        public CachedTransform Parent { get; internal set; }

        //public string Name { get; internal set; }
        //public int ChildCount { get; internal set; }
        public int Depth { get; internal set; }

        public bool Expanded => Tree.IsCellExpanded(InstanceID);

        public CachedTransform(TransformTree tree, Transform transform, CachedTransform parent = null)
        {
            Tree = tree;
            Value = transform;
            Parent = parent;
            Update(transform);
        }

        public void Update(Transform transform)
        {
            Value = transform;
            InstanceID = transform.GetInstanceID();
            //Name = Value.name;
            //ChildCount = Value.childCount;
            Depth = Parent?.Depth + 1 ?? 0;
        }
    }
}
