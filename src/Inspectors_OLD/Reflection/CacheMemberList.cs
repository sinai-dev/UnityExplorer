//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using UnityEngine;
//using UnityExplorer.UI.Widgets;

//namespace UnityExplorer.Inspectors.Reflection
//{
//    public class CacheMemberList : IPoolDataSource
//    {
//        public ScrollPool ScrollPool { get; }
//        public ReflectionInspector Inspector { get; }

//        public CacheMemberList(ScrollPool scrollPool, ReflectionInspector inspector)
//        {
//            this.ScrollPool = scrollPool;
//            this.Inspector = inspector;
//        }

//        public int ItemCount => Inspector.filteredMembers.Count;

//        public int GetRealIndexOfTempIndex(int index)
//        {
//            if (index < 0 || index >= Inspector.filteredToRealIndices.Count)
//                return -1;
//            return Inspector.filteredToRealIndices[index];
//        }

//        public ICell CreateCell(RectTransform cellTransform) => new CellViewHolder(cellTransform.gameObject);

//        public void SetCell(ICell icell, int index)
//        {
//            var cell = icell as CellViewHolder;

//            if (index < 0 || index >= ItemCount)
//            {
//                var existing = cell.DisableContent();
//                if (existing)
//                    existing.transform.SetParent(Inspector.InactiveHolder.transform, false);
//                return;
//            }

//            index = GetRealIndexOfTempIndex(index);

//            var cache = Inspector.allMembers[index];
//            cache.Enable();

//            var prev = cell.SetContent(cache.UIRoot);
//            if (prev)
//                prev.transform.SetParent(Inspector.InactiveHolder.transform, false);
//        }

//        public void DisableCell(ICell cell, int index)
//        {
//            var content = (cell as CellViewHolder).DisableContent();
//            if (content)
//                content.transform.SetParent(Inspector.InactiveHolder.transform, false);
//        }
//    }
//}
