using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExplorer.UI.Inspectors.IValues;

namespace UnityExplorer.UI.Inspectors.CacheObject.Views
{
    public class CacheListEntryCell : CacheObjectCell
    {
        public InteractiveList ListOwner { get; set; }

        public override GameObject CreateContent(GameObject parent)
        {
            var root = base.CreateContent(parent);

            this.NameLayout.minWidth = 50;
            this.NameLayout.flexibleWidth = 50f;

            return root;
        }

        protected override void ConstructEvaluateHolder(GameObject parent)
        {
            // not used
        }

        //protected override void ConstructUpdateToggle(GameObject parent)
        //{
        //    // not used
        //}
    }
}
