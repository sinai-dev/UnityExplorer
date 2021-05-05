using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityExplorer.UI.CacheObject.Views;
using UnityExplorer.UI.IValues;

namespace UnityExplorer.UI.CacheObject
{
    public class CacheListEntry : CacheObjectBase
    {
        public int ListIndex;

        public override bool ShouldAutoEvaluate => true;
        public override bool HasArguments => false;
        public override bool CanWrite => Owner.CanWrite;

        public void SetListOwner(InteractiveList list, int listIndex)
        {
            this.Owner = list;
            this.ListIndex = listIndex;
        }

        public override void SetDataToCell(CacheObjectCell cell)
        {
            base.SetDataToCell(cell);

            var listCell = cell as CacheListEntryCell;

            listCell.NameLabel.text = $"{ListIndex}:";
            listCell.Image.color = ListIndex % 2 == 0 ? CacheListEntryCell.EvenColor : CacheListEntryCell.OddColor;
        }

        public override void TrySetUserValue(object value)
        {
            throw new NotImplementedException("TODO");
        }


        protected override bool SetCellEvaluateState(CacheObjectCell cell)
        {
            // not needed
            return false;
        }
    }
}
