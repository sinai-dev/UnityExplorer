using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Config;
using UnityExplorer.Helpers;
using UnityExplorer.UI;
using UnityExplorer.UI.Shared;

namespace UnityExplorer.Inspectors.Reflection
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

        internal readonly Type m_baseEntryType;

        internal readonly List<CacheEnumerated> m_entries = new List<CacheEnumerated>();
        internal readonly CacheEnumerated[] m_displayedEntries = new CacheEnumerated[ModConfig.Instance.Default_Page_Limit];
        internal bool m_recacheWanted = true;

        public override void OnValueUpdated()
        {
            RefIEnumerable = Value as IEnumerable;
            RefIList = Value as IList;

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
                if (m_recacheWanted && RefIList != null)
                    count = RefIList.Count.ToString();
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

        private IEnumerable EnumerateWithReflection()
        {
            if (Value.IsNullOrDestroyed()) 
                return null;

            var genericDef = Value.GetType().GetGenericTypeDefinition();

            if (genericDef == typeof(Il2CppSystem.Collections.Generic.List<>))
                return CppListToMono(genericDef);
            else if (genericDef == typeof(Il2CppSystem.Collections.Generic.HashSet<>))
                return CppHashSetToMono();
            else
                return CppIListToMono();
        }

        // List<T>.ToArray()
        private IEnumerable CppListToMono(Type genericTypeDef)
        {
            if (genericTypeDef == null) return null;

            return genericTypeDef
                    .MakeGenericType(new Type[] { this.m_baseEntryType })
                    .GetMethod("ToArray")
                    .Invoke(Value, new object[0]) as IEnumerable;
        }

        // HashSet.GetEnumerator
        private IEnumerable CppHashSetToMono()
        {
            var set = new HashSet<object>();

            // invoke GetEnumerator
            var enumerator = Value.GetType().GetMethod("GetEnumerator").Invoke(Value, null);
            // get the type of it
            var enumeratorType = enumerator.GetType();
            // reflect MoveNext and Current
            var moveNext = enumeratorType.GetMethod("MoveNext");
            var current = enumeratorType.GetProperty("Current");
            // iterate
            while ((bool)moveNext.Invoke(enumerator, null))
                set.Add(current.GetValue(enumerator));

            return set;
        }

        // IList.Item
        private IList CppIListToMono()
        {
            try
            {
                var genericType = typeof(List<>).MakeGenericType(new Type[] { this.m_baseEntryType });
                var list = (IList)Activator.CreateInstance(genericType);

                for (int i = 0; ; i++)
                {
                    try
                    {
                        var itm = Value?.GetType()
                            .GetProperty("Item")
                            .GetValue(Value, new object[] { i });
                        list.Add(itm);
                    }
                    catch { break; }
                }

                return list;
            }
            catch (Exception e)
            {
                ExplorerCore.Log("Exception converting Il2Cpp IList to Mono IList: " + e.GetType() + ", " + e.Message);
                return null;
            }
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

        #endregion
    }
}
