using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Unity;
using UnityExplorer.UI;
using UnityExplorer.UI.CacheObject;
using UnityExplorer.UI.Utility;

namespace UnityExplorer.UI.InteractiveValues
{
    public class InteractiveEnumerable : InteractiveValue
    {
        public InteractiveEnumerable(object value, Type valueType) : base(value, valueType) 
        {
            if (valueType.IsGenericType)
                m_baseEntryType = valueType.GetGenericArguments()[0];
            else
                m_baseEntryType = typeof(object);
        }

        public override bool WantInspectBtn => false;
        public override bool HasSubContent => true;
        public override bool SubContentWanted
        {
            get
            {
                if (m_recacheWanted)
                    return true;
                else return m_entries.Count > 0;
            }
        }

        internal IEnumerable RefIEnumerable;
        internal IList RefIList;
//#if CPP
//        internal object CppICollection;
//#else
//        internal object CppICollection = null;
//#endif

        internal readonly Type m_baseEntryType;

        internal readonly List<CacheEnumerated> m_entries = new List<CacheEnumerated>();
        internal readonly CacheEnumerated[] m_displayedEntries = new CacheEnumerated[ConfigManager.Default_Page_Limit.Value];
        internal bool m_recacheWanted = true;

        public override void OnValueUpdated()
        {
            RefIEnumerable = Value as IEnumerable;
            RefIList = Value as IList;

//#if CPP
//            if (Value != null && RefIList == null)
//            {
//                try 
//                {
//                    var type = typeof(Il2CppSystem.Collections.ICollection).MakeGenericType(this.m_baseEntryType);
//                    CppICollection = (Value as Il2CppSystem.Object).Cast(type);
//                }
//                catch { }
//            }
//#endif

            if (m_subContentParent.activeSelf)
            {
                GetCacheEntries();
                RefreshDisplay();
            }
            else
                m_recacheWanted = true;

            base.OnValueUpdated();
        }

        public override void OnException(CacheMember member)
        {
            base.OnException(member);
        }

        private void OnPageTurned()
        {
            RefreshDisplay();
        }

        public override void RefreshUIForValue()
        {
            GetDefaultLabel();

            if (Value != null)
            {
                string count = "?";
                if (m_recacheWanted && RefIList != null)// || CppICollection != null))
                    count = RefIList.Count.ToString();// ?? CppICollection.Count.ToString();
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

                foreach (var entry in m_entries)
                    entry.Destroy();

                m_entries.Clear();
            }

#if CPP
            if (RefIEnumerable == null && Value != null)
                RefIEnumerable = EnumerateWithReflection();
#endif

            if (RefIEnumerable != null)
            {
                int index = 0;
                foreach (var entry in RefIEnumerable)
                {
                    var cache = new CacheEnumerated(index, this, RefIList, this.m_listContent);
                    cache.CreateIValue(entry, m_baseEntryType);
                    m_entries.Add(cache);

                    cache.Disable();

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
                if (entry != null)
                    entry.Disable();
                else
                    break;
            }

            if (entries.Count < 1)
                return;

            foreach (var itemIndex in m_pageHandler)
            {
                if (itemIndex >= entries.Count)
                    break;

                CacheEnumerated entry = entries[itemIndex];
                m_displayedEntries[itemIndex - m_pageHandler.StartIndex] = entry;
                entry.Enable();
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

#region CPP Helpers

#if CPP
        // some temp fixes for Il2Cpp IEnumerables until interfaces are fixed

        internal static readonly Dictionary<Type, MethodInfo> s_getEnumeratorMethods = new Dictionary<Type, MethodInfo>();

        internal static readonly Dictionary<Type, EnumeratorInfo> s_enumeratorInfos = new Dictionary<Type, EnumeratorInfo>();
        
        internal class EnumeratorInfo
        {
            internal MethodInfo moveNext;
            internal PropertyInfo current;
        }

        private IEnumerable EnumerateWithReflection()
        {
            if (Value == null) 
                return null;

            // new test
            var CppEnumerable = (Value as Il2CppSystem.Object)?.TryCast<Il2CppSystem.Collections.IEnumerable>();
            if (CppEnumerable != null)
            {
                var type = Value.GetType();
                if (!s_getEnumeratorMethods.ContainsKey(type))
                    s_getEnumeratorMethods.Add(type, type.GetMethod("GetEnumerator"));
                
                var enumerator = s_getEnumeratorMethods[type].Invoke(Value, null);
                var enumeratorType = enumerator.GetType();

                if (!s_enumeratorInfos.ContainsKey(enumeratorType))
                {
                    s_enumeratorInfos.Add(enumeratorType, new EnumeratorInfo
                    {
                        current = enumeratorType.GetProperty("Current"),
                        moveNext = enumeratorType.GetMethod("MoveNext"),
                    });
                }
                var info = s_enumeratorInfos[enumeratorType];

                // iterate
                var list = new List<object>();
                while ((bool)info.moveNext.Invoke(enumerator, null))
                    list.Add(info.current.GetValue(enumerator));

                return list;
            }

            return null;
        }
#endif

#endregion

#region UI CONSTRUCTION

        internal GameObject m_listContent;
        internal LayoutElement m_listLayout;

        internal PageHandler m_pageHandler;

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

            m_listContent = UIFactory.CreateVerticalGroup(this.m_subContentParent, "EnumerableContent", true, true, true, true, 2, new Vector4(5,5,5,5),
                new Color(0.08f, 0.08f, 0.08f));

            var scrollRect = m_listContent.GetComponent<RectTransform>();
            scrollRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);

            m_listLayout = Owner.m_mainContent.GetComponent<LayoutElement>();
            m_listLayout.minHeight = 25;
            m_listLayout.flexibleHeight = 0;
            Owner.m_mainRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 25);

            var contentFitter = m_listContent.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

#endregion
    }
}
