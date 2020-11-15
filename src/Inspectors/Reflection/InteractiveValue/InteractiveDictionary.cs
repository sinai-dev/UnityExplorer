using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Config;
using UnityExplorer.Helpers;
using UnityExplorer.UI;
using UnityExplorer.UI.Shared;

namespace UnityExplorer.Inspectors.Reflection
{
    public class InteractiveDictionary : InteractiveValue
    {
        public InteractiveDictionary(object value, Type valueType) : base(value, valueType)
        {
            if (valueType.IsGenericType)
            {
                var gArgs = valueType.GetGenericArguments();
                m_typeOfKeys = gArgs[0];
                m_typeofValues = gArgs[1];
            }
            else
            {
                m_typeOfKeys = typeof(object);
                m_typeofValues = typeof(object);
            }
        }

        public override IValueTypes IValueType => IValueTypes.Dictionary;
        public override bool HasSubContent => true;
        public override bool SubContentWanted => (RefIDictionary?.Count ?? 1) > 0;
        public override bool WantInspectBtn => false;

        internal IDictionary RefIDictionary;

        internal Type m_typeOfKeys;
        internal Type m_typeofValues;

        internal readonly List<KeyValuePair<CachePaired, CachePaired>> m_entries 
            = new List<KeyValuePair<CachePaired, CachePaired>>();

        internal readonly KeyValuePair<CachePaired, CachePaired>[] m_displayedEntries
            = new KeyValuePair<CachePaired, CachePaired>[ModConfig.Instance.Default_Page_Limit];

        internal bool m_recacheWanted = true;

        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        public override void OnValueUpdated()
        {
            base.OnValueUpdated();

            if (!Value.IsNullOrDestroyed())
            {
                RefIDictionary = Value as IDictionary;
                UpdateLabel();
            }
            else
            {
                m_baseLabel.text = base.GetLabelForValue();
                RefIDictionary = null;
            }

            if (m_subContentParent.activeSelf)
            {
                GetCacheEntries();
                RefreshDisplay();
            }
            else
                m_recacheWanted = true;
        }

        internal void OnPageTurned()
        {
            RefreshDisplay();
        }

        internal void UpdateLabel()
        {
            string count = "?";
            if (m_recacheWanted && RefIDictionary != null)
                count = RefIDictionary.Count.ToString();
            else if (!m_recacheWanted)
                count = m_entries.Count.ToString();

            m_baseLabel.text = $"[{count}] {m_richValueType}";
        }

        public void GetCacheEntries()
        {
            if (m_entries.Any())
            {
                // maybe improve this, probably could be more efficient i guess

                foreach (var pair in m_entries)
                {
                    pair.Key.Destroy();
                    pair.Value.Destroy();
                }

                m_entries.Clear();
            }

            if (RefIDictionary != null)
            {
                int index = 0;

                foreach (var key in RefIDictionary.Keys)
                {
                    var value = RefIDictionary[key];

                    //if (index >= m_rowHolders.Count)
                    //{
                    //    AddRowHolder();
                    //}

                    //var holder = m_rowHolders[index];

                    var cacheKey = new CachePaired(index, this, this.RefIDictionary, PairTypes.Key, m_listContent);
                    cacheKey.CreateIValue(key, this.m_typeOfKeys);
                    cacheKey.Disable();

                    var cacheValue = new CachePaired(index, this, this.RefIDictionary, PairTypes.Value, m_listContent);
                    cacheValue.CreateIValue(value, this.m_typeofValues);
                    cacheValue.Disable();

                    //holder.SetActive(false);

                    m_entries.Add(new KeyValuePair<CachePaired, CachePaired>(cacheKey, cacheValue));

                    index++;
                }
            }

            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            var entries = m_entries;
            m_pageHandler.ListCount = entries.Count;

            for (int i = 0; i < m_displayedEntries.Length; i++)
            {
                var entry = m_displayedEntries[i];
                if (entry.Key != null && entry.Value != null)
                {
                    //m_rowHolders[i].SetActive(false);
                    entry.Key.Disable();
                    entry.Value.Disable();
                }
                else
                    break;
            }

            if (entries.Count < 1)
                return;

            foreach (var itemIndex in m_pageHandler)
            {
                if (itemIndex >= entries.Count)
                    break;

                var entry = entries[itemIndex];
                m_displayedEntries[itemIndex - m_pageHandler.StartIndex] = entry;

                //m_rowHolders[itemIndex].SetActive(true);
                entry.Key.Enable();
                entry.Value.Enable();
            }

            //UpdateSubcontentHeight();
        }

        internal override void OnToggleSubcontent(bool active)
        {
            base.OnToggleSubcontent(active);

            if (active && m_recacheWanted)
            {
                m_recacheWanted = false;
                GetCacheEntries();
                UpdateLabel();
            }

            RefreshDisplay();
        }

        #region UI CONSTRUCTION

        internal GameObject m_listContent;
        internal LayoutElement m_listLayout;

        internal PageHandler m_pageHandler;

        //internal List<GameObject> m_rowHolders = new List<GameObject>();

        public override void ConstructUI(GameObject parent, GameObject subGroup)
        {
            base.ConstructUI(parent, subGroup);
        }

        public override void ConstructSubcontent()
        {
            base.ConstructSubcontent();

            m_pageHandler = new PageHandler(null);
            m_pageHandler.ConstructUI(m_subContentParent);
            m_pageHandler.OnPageChanged += OnPageTurned;

            var scrollObj = UIFactory.CreateVerticalGroup(this.m_subContentParent, new Color(0.08f, 0.08f, 0.08f));
            m_listContent = scrollObj;

            var scrollRect = scrollObj.GetComponent<RectTransform>();
            scrollRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);

            m_listLayout = OwnerCacheObject.m_mainContent.GetComponent<LayoutElement>();
            m_listLayout.minHeight = 25;
            m_listLayout.flexibleHeight = 0;
            OwnerCacheObject.m_mainRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 25);

            var scrollGroup = m_listContent.GetComponent<VerticalLayoutGroup>();
            scrollGroup.childForceExpandHeight = true;
            scrollGroup.childControlHeight = true;
            scrollGroup.spacing = 2;
            scrollGroup.padding.top = 5;
            scrollGroup.padding.left = 5;
            scrollGroup.padding.right = 5;
            scrollGroup.padding.bottom = 5;

            var contentFitter = scrollObj.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        //internal void AddRowHolder()
        //{
        //    var obj = UIFactory.CreateHorizontalGroup(m_listContent, new Color(0.15f, 0.15f, 0.15f));

        //    m_rowHolders.Add(obj);
        //}

        #endregion
    }
}
