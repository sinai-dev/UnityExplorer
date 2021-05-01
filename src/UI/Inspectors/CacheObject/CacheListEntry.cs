using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityExplorer.UI.Inspectors.CacheObject.Views;
using UnityExplorer.UI.Inspectors.IValues;

namespace UnityExplorer.UI.Inspectors.CacheObject
{
    public class CacheListEntry : CacheObjectBase
    {
        public InteractiveList CurrentList { get; set; }

        public int ListIndex;

        public override bool ShouldAutoEvaluate => true;
        public override bool HasArguments => false;

        public void SetListOwner(InteractiveList iList, int listIndex)
        {
            this.CurrentList = iList;
            this.ListIndex = listIndex;
        }

        public override void SetCell(CacheObjectCell cell)
        {
            base.SetCell(cell);

            var listCell = cell as CacheListEntryCell;

            listCell.NameLabel.text = $"{ListIndex}:";
        }

        public override void SetUserValue(object value)
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
