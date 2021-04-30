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

        public int Depth { get; internal set; }

        public bool Expanded => Tree.IsCellExpanded(InstanceID);

        public CachedTransform(TransformTree tree, Transform transform, int depth, CachedTransform parent = null)
        {
            Tree = tree;
            Value = transform;
            Parent = parent;
            InstanceID = transform.GetInstanceID();
            Update(transform, depth);
        }

        public void Update(Transform transform, int depth)
        {
            Value = transform;
            Depth = depth;
        }
    }
}
