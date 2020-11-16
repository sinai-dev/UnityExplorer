using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityExplorer.UI;
using UnityEngine;
using UnityEngine.UI;

namespace UnityExplorer.Inspectors.Reflection
{
    public class CacheEnumerated : CacheObjectBase
    {
        public override Type FallbackType => ParentEnumeration.m_baseEntryType;
        public override bool CanWrite => RefIList != null && ParentEnumeration.Owner.CanWrite;

        public int Index { get; set; }
        public IList RefIList { get; set; }
        public InteractiveEnumerable ParentEnumeration { get; set; }

        public CacheEnumerated(int index, InteractiveEnumerable parentEnumeration, IList refIList, GameObject parentContent)
        {
            this.ParentEnumeration = parentEnumeration;
            this.Index = index;
            this.RefIList = refIList;
            this.m_parentContent = parentContent;
        }

        public override void CreateIValue(object value, Type fallbackType)
        {
            IValue = InteractiveValue.Create(value, fallbackType);
            IValue.Owner = this;
        }

        public override void SetValue()
        {
            RefIList[Index] = IValue.Value;
            ParentEnumeration.Value = RefIList;

            ParentEnumeration.Owner.SetValue();
        }

        internal override void ConstructUI()
        {
            base.ConstructUI();

            var rowObj = UIFactory.CreateHorizontalGroup(m_mainContent, new Color(1, 1, 1, 0));
            var rowGroup = rowObj.GetComponent<HorizontalLayoutGroup>();
            rowGroup.padding.left = 5;
            rowGroup.padding.right = 2;

            var indexLabelObj = UIFactory.CreateLabel(rowObj, TextAnchor.MiddleLeft);
            var indexLayout = indexLabelObj.AddComponent<LayoutElement>();
            indexLayout.minWidth = 20;
            indexLayout.flexibleWidth = 30;
            indexLayout.minHeight = 25;
            var indexText = indexLabelObj.GetComponent<Text>();
            indexText.text = this.Index + ":";

            IValue.m_mainContentParent = rowObj;
        }
    }
}
