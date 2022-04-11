using UnityEngine;

namespace UnityExplorer.UI.Widgets
{
    public class CachedTransform
    {
        public TransformTree Tree { get; }
        public Transform Value { get; private set; }
        public int InstanceID { get; }
        public CachedTransform Parent { get; internal set; }

        public int Depth { get; internal set; }
        public int ChildCount { get; internal set; }
        public string Name { get; internal set; }
        public bool Enabled { get; internal set; }
        public int SiblingIndex { get; internal set; }

        public bool Expanded => Tree.IsTransformExpanded(InstanceID);

        public CachedTransform(TransformTree tree, Transform transform, int depth, CachedTransform parent = null)
        {
            InstanceID = transform.GetInstanceID();

            Tree = tree;
            Value = transform;
            Parent = parent;
            SiblingIndex = transform.GetSiblingIndex();
            Update(transform, depth);
        }

        public bool Update(Transform transform, int depth)
        {
            bool changed = false;

            if (Value != transform
                || depth != Depth
                || ChildCount != transform.childCount
                || Name != transform.name
                || Enabled != transform.gameObject.activeSelf
                || SiblingIndex != transform.GetSiblingIndex())
            {
                changed = true;

                Value = transform;
                Depth = depth;
                ChildCount = transform.childCount;
                Name = transform.name;
                Enabled = transform.gameObject.activeSelf;
                SiblingIndex = transform.GetSiblingIndex();
            }

            return changed;
        }
    }
}
