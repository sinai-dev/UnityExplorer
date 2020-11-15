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
    public enum PairTypes
    {
        Key,
        Value
    }

    public class CachePaired : CacheObjectBase, INestedValue
    {
        public override Type FallbackType => PairType == PairTypes.Key
            ? ParentDictionary.m_typeOfKeys
            : ParentDictionary.m_typeofValues;

        public override bool CanWrite => false; // todo?

        public PairTypes PairType;
        public int Index { get; private set; }
        public InteractiveDictionary ParentDictionary { get; private set; }
        internal IDictionary RefIDIct;

        public CachePaired(int index, InteractiveDictionary parentDict, IDictionary refIDict, PairTypes pairType, GameObject parentContent)
        {
            Index = index;
            ParentDictionary = parentDict;
            RefIDIct = refIDict;
            this.PairType = pairType;
            this.m_parentContent = parentContent;
        }

        public override void CreateIValue(object value, Type fallbackType)
        {
            IValue = InteractiveValue.Create(value, fallbackType);
            IValue.OwnerCacheObject = this;
        }

        public void UpdateSubcontentHeight()
        {
            ParentDictionary.UpdateSubcontentHeight();
        }

        #region UI CONSTRUCTION

        internal override void ConstructUI()
        {
            base.ConstructUI();

            var rowObj = UIFactory.CreateHorizontalGroup(m_mainContent, new Color(1, 1, 1, 0));
            var rowGroup = rowObj.GetComponent<HorizontalLayoutGroup>();
            rowGroup.padding.left = 5;
            rowGroup.padding.right = 2;

            var indexLabelObj = UIFactory.CreateLabel(rowObj, TextAnchor.MiddleLeft);
            var indexLayout = indexLabelObj.AddComponent<LayoutElement>();
            indexLayout.minWidth = 80;
            indexLayout.flexibleWidth = 30;
            indexLayout.minHeight = 25;
            var indexText = indexLabelObj.GetComponent<Text>();
            indexText.text = $"{this.PairType} {this.Index}:";

            IValue.m_mainContentParent = rowObj;
        }

        #endregion
    }
}
