using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.IValues;

namespace UnityExplorer.UI.CacheObject.Views
{
    public class CacheListEntryCell : CacheObjectCell
    {
        public Image Image { get; private set; }
        public InteractiveList ListOwner => Occupant.Owner as InteractiveList;

        public static Color EvenColor = new Color(0.12f, 0.12f, 0.12f);
        public static Color OddColor = new Color(0.1f, 0.1f, 0.1f);

        public override GameObject CreateContent(GameObject parent)
        {
            var root = base.CreateContent(parent);

            Image = root.AddComponent<Image>();

            this.NameLayout.minWidth = 40;
            this.NameLayout.flexibleWidth = 50;
            this.NameLayout.minHeight = 25;
            this.NameLayout.flexibleHeight = 0;
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
