using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.CacheObject.Views;
using UnityExplorer.UI.Panels;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Widgets.ScrollView;
using UniverseLib.Utility;

namespace UnityExplorer.CacheObject.IValues
{
    public class InteractiveDictionary : InteractiveValue, ICellPoolDataSource<CacheKeyValuePairCell>, ICacheObjectController
    {
        CacheObjectBase ICacheObjectController.ParentCacheObject => this.CurrentOwner;
        object ICacheObjectController.Target => this.CurrentOwner.Value;
        public Type TargetType { get; private set; }

        public override bool CanWrite => base.CanWrite && RefIDictionary != null && !RefIDictionary.IsReadOnly;

        public Type KeysType;
        public Type ValuesType;
        public IDictionary RefIDictionary;

        public int ItemCount => cachedEntries.Count;
        private readonly List<CacheKeyValuePair> cachedEntries = new();

        public ScrollPool<CacheKeyValuePairCell> DictScrollPool { get; private set; }

        private Text NotSupportedLabel;

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
            RefIDictionary = null;

            foreach (CacheKeyValuePair entry in cachedEntries)
            {
                entry.UnlinkFromView();
                entry.ReleasePooledObjects();
            }

            cachedEntries.Clear();
        }

        public override void SetValue(object value)
        {
            if (value == null)
            {
                // should never be null
                ClearAndRelease();
                return;
            }
            else
            {
                Type type = value.GetActualType();
                ReflectionUtility.TryGetEntryTypes(type, out KeysType, out ValuesType);

                CacheEntries(value);

                TopLabel.text = $"[{cachedEntries.Count}] {SignatureHighlighter.Parse(type, false)}";
            }

            this.DictScrollPool.Refresh(true, false);
        }

        private void CacheEntries(object value)
        {
            RefIDictionary = value as IDictionary;

            if (ReflectionUtility.TryGetDictEnumerator(value, out IEnumerator<DictionaryEntry> dictEnumerator))
            {
                NotSupportedLabel.gameObject.SetActive(false);

                int idx = 0;
                while (dictEnumerator.MoveNext())
                {
                    CacheKeyValuePair cache;
                    if (idx >= cachedEntries.Count)
                    {
                        cache = new CacheKeyValuePair();
                        cache.SetDictOwner(this, idx);
                        cachedEntries.Add(cache);
                    }
                    else
                        cache = cachedEntries[idx];

                    cache.SetFallbackType(ValuesType);
                    cache.SetKey(dictEnumerator.Current.Key);
                    cache.SetValueFromSource(dictEnumerator.Current.Value);

                    idx++;
                }

                // Remove excess cached entries if dict count decreased
                if (cachedEntries.Count > idx)
                {
                    for (int i = cachedEntries.Count - 1; i >= idx; i--)
                    {
                        CacheKeyValuePair cache = cachedEntries[i];
                        if (cache.CellView != null)
                            cache.UnlinkFromView();

                        cache.ReleasePooledObjects();
                        cachedEntries.RemoveAt(i);
                    }
                }
            }
            else
            {
                NotSupportedLabel.gameObject.SetActive(true);
            }
        }

        // Setting value to dictionary

        public void TrySetValueToKey(object key, object value, int keyIndex)
        {
            try
            {
                if (!RefIDictionary.Contains(key))
                {
                    ExplorerCore.LogWarning("Unable to set key! Key may have been boxed to/from Il2Cpp Object.");
                    return;
                }

                RefIDictionary[key] = value;

                CacheKeyValuePair entry = cachedEntries[keyIndex];
                entry.SetValueFromSource(value);
                if (entry.CellView != null)
                    entry.SetDataToCell(entry.CellView);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception setting IDictionary key! {ex}");
            }
        }

        // KVP entry scroll pool

        public void OnCellBorrowed(CacheKeyValuePairCell cell) { }

        public void SetCell(CacheKeyValuePairCell cell, int index)
        {
            CacheObjectControllerHelper.SetCell(cell, index, cachedEntries, SetCellLayout);
        }

        public int AdjustedWidth => (int)UIRect.rect.width - 80;

        public override void SetLayout()
        {
            float minHeight = 5f;

            KeyTitleLayout.minWidth = AdjustedWidth * 0.44f;
            ValueTitleLayout.minWidth = AdjustedWidth * 0.55f;

            foreach (CacheKeyValuePairCell cell in DictScrollPool.CellPool)
            {
                SetCellLayout(cell);
                if (cell.Enabled)
                    minHeight += cell.Rect.rect.height;
            }

            this.scrollLayout.minHeight = Math.Min(InspectorPanel.CurrentPanelHeight - 400f, minHeight);
        }

        private void SetCellLayout(CacheObjectCell objcell)
        {
            CacheKeyValuePairCell cell = objcell as CacheKeyValuePairCell;
            cell.KeyGroupLayout.minWidth = cell.AdjustedWidth * 0.44f;
            cell.RightGroupLayout.minWidth = cell.AdjustedWidth * 0.55f;

            if (cell.Occupant?.IValue != null)
                cell.Occupant.IValue.SetLayout();
        }

        private LayoutElement scrollLayout;
        private RectTransform UIRect;

        public override GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(parent, "InteractiveDict", true, true, true, true, 6, new Vector4(10, 3, 15, 4),
                new Color(0.05f, 0.05f, 0.05f));
            UIFactory.SetLayoutElement(UIRoot, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 475);
            UIRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            UIRect = UIRoot.GetComponent<RectTransform>();

            // Entries label

            TopLabel = UIFactory.CreateLabel(UIRoot, "EntryLabel", "not set", TextAnchor.MiddleLeft, fontSize: 16);
            TopLabel.horizontalOverflow = HorizontalWrapMode.Overflow;

            // key / value titles

            GameObject titleGroup = UIFactory.CreateUIObject("TitleGroup", UIRoot);
            UIFactory.SetLayoutElement(titleGroup, minHeight: 25, flexibleWidth: 9999, flexibleHeight: 0);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(titleGroup, false, true, true, true, padLeft: 65, padRight: 0, childAlignment: TextAnchor.LowerLeft);

            Text keyTitle = UIFactory.CreateLabel(titleGroup, "KeyTitle", "Keys", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(keyTitle.gameObject, minWidth: 100, flexibleWidth: 0);
            KeyTitleLayout = keyTitle.GetComponent<LayoutElement>();

            Text valueTitle = UIFactory.CreateLabel(titleGroup, "ValueTitle", "Values", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(valueTitle.gameObject, minWidth: 100, flexibleWidth: 0);
            ValueTitleLayout = valueTitle.GetComponent<LayoutElement>();

            // entry scroll pool

            DictScrollPool = UIFactory.CreateScrollPool<CacheKeyValuePairCell>(UIRoot, "EntryList", out GameObject scrollObj,
                out GameObject _, new Color(0.09f, 0.09f, 0.09f));
            UIFactory.SetLayoutElement(scrollObj, minHeight: 150, flexibleHeight: 0);
            DictScrollPool.Initialize(this, SetLayout);
            scrollLayout = scrollObj.GetComponent<LayoutElement>();

            NotSupportedLabel = UIFactory.CreateLabel(DictScrollPool.Content.gameObject, "NotSupportedMessage",
                "The IDictionary failed to enumerate. This is likely due to an issue with Unhollowed interfaces.",
                TextAnchor.MiddleLeft, Color.red);

            UIFactory.SetLayoutElement(NotSupportedLabel.gameObject, minHeight: 25, flexibleWidth: 9999);
            NotSupportedLabel.gameObject.SetActive(false);

            return UIRoot;
        }
    }
}