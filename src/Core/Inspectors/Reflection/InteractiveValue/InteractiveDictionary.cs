using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Unity;
using UnityExplorer.UI;
using UnityExplorer.UI.Reusable;
using System.Reflection;
#if CPP
using CppDictionary = Il2CppSystem.Collections.IDictionary;
#endif

namespace UnityExplorer.Core.Inspectors.Reflection
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

        public override bool WantInspectBtn => false;
        public override bool HasSubContent => true;
        // todo fix for il2cpp
        public override bool SubContentWanted
        {
            get
            {
                if (m_recacheWanted)
                    return true;
                else return m_entries.Count > 0;
            }
        }

        internal IDictionary RefIDictionary;
#if CPP
        internal CppDictionary RefCppDictionary;
#else
        internal IDictionary RefCppDictionary = null;
#endif
        internal Type m_typeOfKeys;
        internal Type m_typeofValues;

        internal readonly List<KeyValuePair<CachePaired, CachePaired>> m_entries 
            = new List<KeyValuePair<CachePaired, CachePaired>>();

        internal readonly KeyValuePair<CachePaired, CachePaired>[] m_displayedEntries
            = new KeyValuePair<CachePaired, CachePaired>[ExplorerConfig.Instance.Default_Page_Limit];

        internal bool m_recacheWanted = true;

        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        public override void OnValueUpdated()
        {
            RefIDictionary = Value as IDictionary;

#if CPP
            try { RefCppDictionary = (Value as Il2CppSystem.Object).TryCast<CppDictionary>(); }
            catch { }
#endif

            if (m_subContentParent.activeSelf)
            {
                GetCacheEntries();
                RefreshDisplay();
            }
            else
                m_recacheWanted = true;

            base.OnValueUpdated();
        }

        internal void OnPageTurned()
        {
            RefreshDisplay();
        }

        public override void RefreshUIForValue()
        {
            GetDefaultLabel();

            if (Value != null)
            {
                string count = "?";
                if (m_recacheWanted && RefIDictionary != null)
                    count = RefIDictionary.Count.ToString();
                else if (!m_recacheWanted)
                    count = m_entries.Count.ToString();

                m_baseLabel.text = $"[{count}] {m_richValueType}";
            }
            else
            {
                m_baseLabel.text = DefaultLabel;
            }
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

#if CPP
            if (RefIDictionary == null && Value != null)
                RefIDictionary = EnumerateWithReflection();
#endif

            if (RefIDictionary != null)
            {
                int index = 0;

                foreach (var key in RefIDictionary.Keys)
                {
                    var value = RefIDictionary[key];

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
                RefreshUIForValue();
            }

            RefreshDisplay();
        }

#region CPP fixes
#if CPP
        // temp fix for Il2Cpp IDictionary until interfaces are fixed

        private IDictionary EnumerateWithReflection()
        {
            var valueType = Value?.GetType() ?? FallbackType;

            // get keys and values
            var keys = valueType.GetProperty("Keys").GetValue(Value, null);
            var values = valueType.GetProperty("Values").GetValue(Value, null);

            // create lists to hold them
            var keyList = new List<object>();
            var valueList = new List<object>();

            // store entries with reflection
            EnumerateCollection(keys, keyList);
            EnumerateCollection(values, valueList);

            // make actual mono dictionary
            var dict = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>)
                                             .MakeGenericType(m_typeOfKeys, m_typeofValues));

            // finally iterate into mono dictionary
            for (int i = 0; i < keyList.Count; i++)
                dict.Add(keyList[i], valueList[i]);

            return dict;
        }

        private void EnumerateCollection(object collection, List<object> list)
        {
            // invoke GetEnumerator
            var enumerator = collection.GetType().GetMethod("GetEnumerator").Invoke(collection, null);
            // get the type of it
            var enumeratorType = enumerator.GetType();
            // reflect MoveNext and Current
            var moveNext = enumeratorType.GetMethod("MoveNext");
            var current = enumeratorType.GetProperty("Current");
            // iterate
            while ((bool)moveNext.Invoke(enumerator, null))
            {
                list.Add(current.GetValue(enumerator, null));
            }
        }
#endif

#endregion

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

            m_listLayout = Owner.m_mainContent.GetComponent<LayoutElement>();
            m_listLayout.minHeight = 25;
            m_listLayout.flexibleHeight = 0;
            Owner.m_mainRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 25);

            var scrollGroup = m_listContent.GetComponent<VerticalLayoutGroup>();
            scrollGroup.childForceExpandHeight = true;
            scrollGroup.SetChildControlHeight(true);
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
