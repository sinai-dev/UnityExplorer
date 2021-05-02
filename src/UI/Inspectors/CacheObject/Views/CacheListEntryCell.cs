using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Inspectors.IValues;

namespace UnityExplorer.UI.Inspectors.CacheObject.Views
{
    public class CacheListEntryCell : CacheObjectCell
    {
        public Image Image { get; private set; }
        public InteractiveList ListOwner => Occupant.Owner as InteractiveList;

        public static Color EvenColor = new Color(0.07f, 0.07f, 0.07f);
        public static Color OddColor = new Color(0.063f, 0.063f, 0.063f);

        public override GameObject CreateContent(GameObject parent)
        {
            var root = base.CreateContent(parent);

            Image = root.AddComponent<Image>();

            this.NameLayout.minWidth = 40;
            this.NameLayout.flexibleWidth = 50;
            this.NameLayout.minHeight = 30;
            this.NameLabel.alignment = TextAnchor.MiddleRight;

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
