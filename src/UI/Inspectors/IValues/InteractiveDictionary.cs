using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Inspectors.CacheObject;
using UnityExplorer.UI.Inspectors.CacheObject.Views;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Inspectors.IValues
{
    public class InteractiveDictionary : InteractiveValue, IPoolDataSource<CacheKeyValuePairCell>, ICacheObjectController
    {
        CacheObjectBase ICacheObjectController.ParentCacheObject => this.CurrentOwner;
        object ICacheObjectController.Target => this.CurrentOwner.Value;
        public Type TargetType { get; private set; }

        public override bool CanWrite => false;// TODO RefIDictionary != null && !RefIDictionary.IsReadOnly;

        public Type KeyType;
        public Type ValueType;
        public IDictionary RefIDictionary;

        public int ItemCount => values.Count;
        private readonly List<object> keys = new List<object>();
        private readonly List<object> values = new List<object>();
        private readonly List<CacheKeyValuePair> cachedEntries = new List<CacheKeyValuePair>();

        public ScrollPool<CacheKeyValuePairCell> DictScrollPool { get; private set; }

        public Text TopLabel;

        public LayoutElement KeyTitleLayout;
        public LayoutElement ValueTitleLayout;

        public override void OnBorrowed(CacheObjectBase owner)
        {
            base.OnBorrowed(owner);

            DictScrollPool.Refresh(true, true);
        }

        public override void ReleaseFromOwner()
        {
            base.ReleaseFromOwner();

            ClearAndRelease();
        }

        private void ClearAndRelease()
        {
            keys.Clear();
            values.Clear();

            foreach (var entry in cachedEntries)
                entry.ReleasePooledObjects();

            cachedEntries.Clear();
        }

        public override void SetValue(object value)
        {
            if (value == null)
            {
                // should never be null
                if (keys.Any())
                    ClearAndRelease();
            }
            else
            {
                var type = value.GetActualType();
                if (type.IsGenericType && type.GetGenericArguments().Length == 2)
                { 
                    KeyType = type.GetGenericArguments()[0];
                    ValueType = type.GetGenericArguments()[1];
                }
                else
                { 
                    KeyType = typeof(object);
                    ValueType = typeof(object);
                }

                CacheEntries(value);

                TopLabel.text = $"[{cachedEntries.Count}] {SignatureHighlighter.ParseType(type, false)}";
            }


            this.DictScrollPool.Refresh(true, false);
        }

        private void CacheEntries(object value)
        {
            RefIDictionary = value as IDictionary;

            if (RefIDictionary == null)
            {
                // todo il2cpp
                return;
            }

            keys.Clear();
            foreach (var k in RefIDictionary.Keys)
                keys.Add(k);

            values.Clear();
            foreach (var v in RefIDictionary.Values)
                values.Add(v);

            int idx = 0;
            for (int i = 0; i < keys.Count; i++)
            {
                CacheKeyValuePair cache;
                if (idx >= cachedEntries.Count)
                {
                    cache = new CacheKeyValuePair();
                    cache.SetDictOwner(this, i);
                    cachedEntries.Add(cache);
                }
                else
                    cache = cachedEntries[i];

                cache.SetFallbackType(ValueType);
                cache.SetKey(keys[i]);
                cache.SetValueFromSource(values[i]);

                idx++;
            }

            // Remove excess cached entries if dict count decreased
            if (cachedEntries.Count > values.Count)
            {
                for (int i = cachedEntries.Count - 1; i >= values.Count; i--)
                {
                    var cache = cachedEntries[i];
                    if (cache.CellView != null)
                    {
                        cache.CellView.Occupant = null;
                        cache.CellView = null;
                    }
                    cache.ReleasePooledObjects();
                    cachedEntries.RemoveAt(i);
                }
            }
        }

        // KVP entry scroll pool

        public void OnCellBorrowed(CacheKeyValuePairCell cell)
        {

        }

        public void SetCell(CacheKeyValuePairCell cell, int index)
        {
            if (index < 0 || index >= cachedEntries.Count)
            {
                if (cell.Occupant != null)
                {
                    cell.Occupant.CellView = null;
                    cell.Occupant = null;
                }

                cell.Disable();
                return;
            }

            var entry = cachedEntries[index];

            if (entry != cell.Occupant)
            {
                if (cell.Occupant != null)
                {
                    cell.Occupant.HidePooledObjects();
                    cell.Occupant.CellView = null;
                    cell.Occupant = null;
                }

                cell.Occupant = entry;
                entry.CellView = cell;
            }

            entry.SetCell(cell);

            SetCellLayout(cell);
        }

        public override void SetLayout()
        {
            var minHeight = 5f;

            foreach (var cell in DictScrollPool.CellPool)
            {
                SetCellLayout(cell);
                if (cell.Enabled)
                    minHeight += cell.Rect.rect.height;
            }

            this.scrollLayout.minHeight = Math.Min(InspectorPanel.CurrentPanelHeight - 400f, minHeight);
        }

        private void SetCellLayout(CacheKeyValuePairCell cell)
        {
            cell.KeyGroupLayout.minWidth = cell.AdjustedKeyWidth;
            cell.RightGroupLayout.minWidth = cell.HalfWidth;

            if (cell.Occupant?.IValue != null)
                cell.Occupant.IValue.SetLayout();
        }

        private LayoutElement scrollLayout;

        public override GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(parent, "InteractiveDict", true, true, true, true, 6, new Vector4(10, 3, 15, 4),
                new Color(0.05f, 0.05f, 0.05f));
            UIFactory.SetLayoutElement(UIRoot, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 475);
            UIRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Entries label

            TopLabel = UIFactory.CreateLabel(UIRoot, "EntryLabel", "not set", TextAnchor.MiddleLeft, fontSize: 16);
            TopLabel.horizontalOverflow = HorizontalWrapMode.Overflow;

            // key / value titles

            var titleGroup = UIFactory.CreateUIObject("TitleGroup", UIRoot);
            UIFactory.SetLayoutElement(titleGroup, minHeight: 25, flexibleWidth: 9999, flexibleHeight: 0);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(titleGroup, true, true, true, true, padLeft: 50, padRight: 65);

            var keyTitle = UIFactory.CreateLabel(titleGroup, "KeyTitle", "Keys", TextAnchor.MiddleLeft);
            //UIFactory.SetLayoutElement(keyTitle.gameObject, minWidth: ReflectionInspector.LeftGroupWidth);
            KeyTitleLayout = keyTitle.GetComponent<LayoutElement>();

            var valueTitle = UIFactory.CreateLabel(titleGroup, "ValueTitle", "Values", TextAnchor.MiddleLeft);
            //UIFactory.SetLayoutElement(valueTitle.gameObject, minWidth: ReflectionInspector.RightGroupWidth);
            ValueTitleLayout = valueTitle.GetComponent<LayoutElement>();

            // entry scroll pool

            DictScrollPool = UIFactory.CreateScrollPool<CacheKeyValuePairCell>(UIRoot, "EntryList", out GameObject scrollObj,
                out GameObject _, new Color(0.09f, 0.09f, 0.09f));
            UIFactory.SetLayoutElement(scrollObj, minHeight: 150, flexibleHeight: 0);
            DictScrollPool.Initialize(this, SetLayout);
            scrollLayout = scrollObj.GetComponent<LayoutElement>();

            return UIRoot;
        }
    }
}