using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Inspectors.Reflection
{
    public class CacheMemberList : IPoolDataSource
    {
        public ScrollPool ScrollPool { get; }
        public ReflectionInspector Inspector { get; }

        public CacheMemberList(ScrollPool scrollPool, ReflectionInspector inspector)
        {
            this.ScrollPool = scrollPool;
            this.Inspector = inspector;
        }

        public int ItemCount => Inspector.filteredMembers.Count;

        public int GetRealIndexOfTempIndex(int index)
        {
            if (index < 0 || index >= Inspector.filteredToRealIndices.Count)
                return -1;
            return Inspector.filteredToRealIndices[index];
        }

        public ICell CreateCell(RectTransform cellTransform) => new CellViewHolder(cellTransform.gameObject);

        public void DisableCell(ICell cell, int index)
        {
            var root = (cell as CellViewHolder).UIRoot;
            DisableContent(root);
            cell.Disable();
        }

        public void SetCell(ICell icell, int index)
        {
            var root = (icell as CellViewHolder).UIRoot;

            if (index < 0 || index >= ItemCount)
            {
                DisableContent(root);
                icell.Disable();
                return;
            }

            float start = Time.realtimeSinceStartup;
            index = GetRealIndexOfTempIndex(index);

            var cache = Inspector.allMembers[index];
            cache.Enable();

            var content = cache.UIRoot;

            if (content.transform.parent.ReferenceEqual(root.transform))
                return;

            var orig = content.transform.parent;

            DisableContent(root);

            content.transform.SetParent(root.transform, false);
            //ExplorerCore.Log("Set cell " + index + ", took " + (Time.realtimeSinceStartup - start) + " secs");
            //ExplorerCore.Log("orig parent was " + (orig?.name ?? " <null>"));
        }

        private void DisableContent(GameObject cellRoot)
        {
            if (cellRoot.transform.childCount > 0 && cellRoot.transform.GetChild(0) is Transform existing)
                existing.transform.SetParent(Inspector.InactiveHolder.transform, false);
        }
    }
}
