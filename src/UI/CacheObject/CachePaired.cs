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
    public enum PairTypes
    {
        Key,
        Value
    }

    public class CachePaired : CacheObjectBase
    {
        public override Type FallbackType => PairType == PairTypes.Key
            ? ParentDictionary.m_typeOfKeys
            : ParentDictionary.m_typeofValues;

        public override bool CanWrite => false; // todo?

        public PairTypes PairType;
        public int Index { get; private set; }
        public InteractiveDictionary ParentDictionary { get; private set; }
        internal IDictionary RefIDict;

        public CachePaired(int index, InteractiveDictionary parentDict, IDictionary refIDict, PairTypes pairType, GameObject parentContent)
        {
            Index = index;
            ParentDictionary = parentDict;
            RefIDict = refIDict;
            this.PairType = pairType;
            this.m_parentContent = parentContent;
        }

        public override void CreateIValue(object value, Type fallbackType)
        {
            IValue = InteractiveValue.Create(value, fallbackType);
            IValue.Owner = this;
        }

        #region UI CONSTRUCTION

        internal override void ConstructUI()
        {
            base.ConstructUI();

            var rowObj = UIFactory.CreateHorizontalGroup(m_mainContent, "PairedGroup", false, false, true, true, 0, new Vector4(0,0,5,2),
                new Color(1, 1, 1, 0));
            
            var indexLabel = UIFactory.CreateLabel(rowObj, "IndexLabel", $"{this.PairType} {this.Index}:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(indexLabel.gameObject, minWidth: 80, flexibleWidth: 30, minHeight: 25);

            IValue.m_mainContentParent = rowObj;
        }

        #endregion
    }
}
