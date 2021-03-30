using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityExplorer.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.InteractiveValues;

namespace UnityExplorer.UI.CacheObject
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

            var rowObj = UIFactory.CreateHorizontalGroup(m_mainContent, "CacheEnumeratedGroup", false, true, true, true, 0, new Vector4(0,0,5,2),
                new Color(1, 1, 1, 0));

            var indexLabel = UIFactory.CreateLabel(rowObj, "IndexLabel", $"{this.Index}:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(indexLabel.gameObject, minWidth: 20, flexibleWidth: 30, minHeight: 25);

            IValue.m_mainContentParent = rowObj;
        }
    }
}
