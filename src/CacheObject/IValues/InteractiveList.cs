using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.CacheObject;
using UnityExplorer.CacheObject.Views;
using UnityExplorer.Inspectors;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Widgets;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Widgets;

namespace UnityExplorer.CacheObject.IValues
{
    public class InteractiveList : InteractiveValue, ICellPoolDataSource<CacheListEntryCell>, ICacheObjectController
    {
        CacheObjectBase ICacheObjectController.ParentCacheObject => this.CurrentOwner;
        object ICacheObjectController.Target => this.CurrentOwner.Value;
        public Type TargetType { get; private set; }

        public override bool CanWrite => base.CanWrite && ((RefIList != null && !RefIList.IsReadOnly) || IsWritableGenericIList);

        public Type EntryType;
        public IList RefIList;

        private bool IsWritableGenericIList;
        private PropertyInfo genericIndexer;

        public int ItemCount => cachedEntries.Count;
        private readonly List<CacheListEntry> cachedEntries = new();

        public ScrollPool<CacheListEntryCell> ListScrollPool { get; private set; }

        public Text TopLabel;
        private LayoutElement scrollLayout;
        private Text NotSupportedLabel;

        public override void OnBorrowed(CacheObjectBase owner)
        {
            base.OnBorrowed(owner);

            ListScrollPool.Refresh(true, true);
        }

        public override void ReleaseFromOwner()
        {
            base.ReleaseFromOwner();

            ClearAndRelease();
        }

        private void ClearAndRelease()
        {
            RefIList = null;

            foreach (var entry in cachedEntries)
            {
                entry.UnlinkFromView();
                entry.ReleasePooledObjects();
            }

            cachedEntries.Clear();
        }

        // List entry scroll pool

        public override void SetLayout()
        {
            var minHeight = 5f;

            foreach (var cell in ListScrollPool.CellPool)
            {
                if (cell.Enabled)
                    minHeight += cell.Rect.rect.height;
            }

            this.scrollLayout.minHeight = Math.Min(InspectorPanel.CurrentPanelHeight - 400f, minHeight);
        }

        public void OnCellBorrowed(CacheListEntryCell cell) { } // not needed

        public void SetCell(CacheListEntryCell cell, int index)
        {
            CacheObjectControllerHelper.SetCell(cell, index, cachedEntries, null);
        }

        // Setting the List value itself to this model
        public override void SetValue(object value)
        {
            if (value == null)
            {
                // should never be null
                if (cachedEntries.Any())
                    ClearAndRelease();
            }
            else
            {
                var type = value.GetActualType();
                ReflectionUtility.TryGetEntryType(type, out EntryType);

                CacheEntries(value);

                TopLabel.text = $"[{cachedEntries.Count}] {SignatureHighlighter.Parse(type, false)}";
            }

            //this.ScrollPoolLayout.minHeight = Math.Min(400f, 35f * values.Count);
            this.ListScrollPool.Refresh(true, false);
        }

        private void CacheEntries(object value)
        {
            RefIList = value as IList;

            // Check if the type implements IList<T> but not IList (ie. Il2CppArrayBase)
            if (RefIList == null)
                CheckGenericIList(value);
            else
                IsWritableGenericIList = false;

            int idx = 0;

            if (ReflectionUtility.TryGetEnumerator(value, out IEnumerator enumerator))
            {
                NotSupportedLabel.gameObject.SetActive(false);

                while (enumerator.MoveNext())
                {
                    var entry = enumerator.Current;

                    // If list count increased, create new cache entries
                    CacheListEntry cache;
                    if (idx >= cachedEntries.Count)
                    {
                        cache = new CacheListEntry();
                        cache.SetListOwner(this, idx);
                        cachedEntries.Add(cache);
                    }
                    else
                        cache = cachedEntries[idx];

                    cache.SetFallbackType(this.EntryType);
                    cache.SetValueFromSource(entry);
                    idx++;
                }

                // Remove excess cached entries if list count decreased
                if (cachedEntries.Count > idx)
                {
                    for (int i = cachedEntries.Count - 1; i >= idx; i--)
                    {
                        var cache = cachedEntries[i];
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

        private void CheckGenericIList(object value)
        {
            try
            {
                var type = value.GetType();
                if (type.GetInterfaces().Any(it => it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IList<>)))
                    IsWritableGenericIList = !(bool)type.GetProperty("IsReadOnly").GetValue(value, null);
                else
                    IsWritableGenericIList = false;

                if (IsWritableGenericIList)
                {
                    // Find the "this[int index]" property.
                    // It might be a private implementation.
                    foreach (var prop in type.GetProperties(ReflectionUtility.FLAGS))
                    {
                        if ((prop.Name == "Item"
                                || (prop.Name.StartsWith("System.Collections.Generic.IList<") && prop.Name.EndsWith(">.Item")))
                            && prop.GetIndexParameters() is ParameterInfo[] parameters
                            && parameters.Length == 1
                            && parameters[0].ParameterType == typeof(int))
                        {
                            genericIndexer = prop;
                            break;
                        }
                    }

                    if (genericIndexer == null)
                    {
                        ExplorerCore.LogWarning($"Failed to find indexer property for IList<T> type '{type.FullName}'!");
                        IsWritableGenericIList = false;
                    }
                }
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception processing IEnumerable for IList<T> check: {ex.ReflectionExToString()}");
                IsWritableGenericIList = false;
            }
        }

        // Setting the value of an index to the list

        public void TrySetValueToIndex(object value, int index)
        {
            try
            {
                if (!IsWritableGenericIList)
                {
                    RefIList[index] = value;
                }
                else
                {
                    genericIndexer.SetValue(CurrentOwner.Value, value, new object[] { index });
                }

                var entry = cachedEntries[index];
                entry.SetValueFromSource(value);

                if (entry.CellView != null)
                    entry.SetDataToCell(entry.CellView);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception setting IList value: {ex}");
            }
        }

        public override GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(parent, "InteractiveList", true, true, true, true, 6, new Vector4(10, 3, 15, 4),
                new Color(0.05f, 0.05f, 0.05f));
            UIFactory.SetLayoutElement(UIRoot, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 600);
            UIRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Entries label

            TopLabel = UIFactory.CreateLabel(UIRoot, "EntryLabel", "not set", TextAnchor.MiddleLeft, fontSize: 16);
            TopLabel.horizontalOverflow = HorizontalWrapMode.Overflow;

            // entry scroll pool

            ListScrollPool = UIFactory.CreateScrollPool<CacheListEntryCell>(UIRoot, "EntryList", out GameObject scrollObj,
                out GameObject _, new Color(0.09f, 0.09f, 0.09f));
            UIFactory.SetLayoutElement(scrollObj, minHeight: 400, flexibleHeight: 0);
            ListScrollPool.Initialize(this, SetLayout);
            scrollLayout = scrollObj.GetComponent<LayoutElement>();

            NotSupportedLabel = UIFactory.CreateLabel(ListScrollPool.Content.gameObject, "NotSupportedMessage",
                "The IEnumerable failed to enumerate. This is likely due to an issue with Unhollowed interfaces.",
                TextAnchor.MiddleLeft, Color.red);

            UIFactory.SetLayoutElement(NotSupportedLabel.gameObject, minHeight: 25, flexibleWidth: 9999);
            NotSupportedLabel.gameObject.SetActive(false);

            return UIRoot;
        }
    }
}