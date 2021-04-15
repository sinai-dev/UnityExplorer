using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core;
using UnityExplorer.UI.Widgets.InfiniteScroll;

namespace UnityExplorer.UI.Widgets
{
    public class TransformTree : MonoBehaviour, IListDataSource
    {
        public Func<IEnumerable<GameObject>> GetRootEntriesMethod;

        public string CurrentFilter
        {
            get => currentFilter;
            set => currentFilter = value?.ToLower() ?? "";
        }
        private string currentFilter;

        internal InfiniteScrollRect infiniteScroll;

        //internal readonly List<CachedTransform> objectTree = new List<CachedTransform>();

        internal readonly Dictionary<IntPtr, CachedTransform> objectCache = new Dictionary<IntPtr, CachedTransform>();
        internal Dictionary<IntPtr, CachedTransform> tempObjectCache;

        public int ItemCount => objectCache.Count;

        internal void Awake()
        {
            RuntimeProvider.Instance.StartCoroutine(InitCoroutine());
        }

        private IEnumerator InitCoroutine()
        {
            yield return null;

            RefreshData();
            infiniteScroll.DataSource = this;
            infiniteScroll.Initialize(this);
        }

        public void RefreshData(bool andReload = false)
        {
            tempObjectCache = objectCache.ToDictionary(it => it.Key, it => it.Value);
            objectCache.Clear();

            var objects = GetRootEntriesMethod.Invoke();

            foreach (var obj in objects)
                Traverse(obj.transform);

            if (andReload)
                infiniteScroll.Refresh();
        }

        private void Traverse(Transform transform, CachedTransform parent = null)
        {
            CachedTransform cached;
            if (tempObjectCache.ContainsKey(transform.m_CachedPtr))
            {
                cached = tempObjectCache[transform.m_CachedPtr];
                cached.Update();
            }
            else
                cached = new CachedTransform(transform, parent);

            if (!string.IsNullOrEmpty(CurrentFilter))
            {
                if (!FilterHierarchy(transform))
                    return;

                // auto-expand to show results: works, but then we need to collapse after the search ends.

                //if (FilterHierarchy(transform))
                //    cached.Expanded = true;
                //else
                //    return;
            }

            objectCache.Add(transform.m_CachedPtr, cached);

            if (cached.Expanded && cached.ChildCount > 0)
            {
                for (int i = 0; i < transform.childCount; i++)
                    Traverse(transform.GetChild(i), cached);
            }
        }

        private bool FilterHierarchy(Transform obj)
        {
            if (obj.name.ToLower().Contains(currentFilter))
                return true;

            if (obj.childCount <= 0)
                return false;

            for (int i = 0; i < obj.childCount; i++)
                if (FilterHierarchy(obj.GetChild(i)))
                    return true;

            return false;
        }

        public void SetCell(ICell iCell, int index)
        {
            var cell = iCell as TransformCell;

            if (index < objectCache.Count)
                cell.ConfigureCell(objectCache.ElementAt(index).Value, index);
            else
                cell.Disable();
        }

        public void ToggleExpandCell(TransformCell cell)
        {
            cell.cachedTransform.Expanded = !cell.cachedTransform.Expanded;
            RefreshData(true);
        }

        public GameObject CreatePrototypeCell(GameObject parent, TransformTree tree)
        {
            var prototype = UIFactory.CreateHorizontalGroup(parent, "PrototypeCell", true, true, true, true, 2, default,
                new Color(0.15f, 0.15f, 0.15f), TextAnchor.MiddleCenter);
            var cell = prototype.AddComponent<TransformCell>();
            var rect = prototype.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(25, 25);
            UIFactory.SetLayoutElement(prototype, minWidth: 100, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 0);

            var spacer = UIFactory.CreateUIObject("Spacer", prototype, new Vector2(0,0));
            UIFactory.SetLayoutElement(spacer, minWidth: 0, flexibleWidth: 0, minHeight: 0, flexibleHeight: 0);

            var expandButton = UIFactory.CreateButton(prototype, "ExpandButton", "►", null);
            UIFactory.SetLayoutElement(expandButton.gameObject, minWidth: 15, flexibleWidth: 0, minHeight: 25, flexibleHeight: 0);

            var nameButton = UIFactory.CreateButton(prototype, "NameButton", "Name", null);
            UIFactory.SetLayoutElement(nameButton.gameObject, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 0);
            nameButton.GetComponentInChildren<Text>().horizontalOverflow = HorizontalWrapMode.Overflow;

            Color normal = new Color(0.15f, 0.15f, 0.15f);
            Color highlight = new Color(0.25f, 0.25f, 0.25f);
            Color pressed = new Color(0.05f, 0.05f, 0.05f);
            Color disabled = new Color(1, 1, 1, 0);
            RuntimeProvider.Instance.SetColorBlock(expandButton, normal, highlight, pressed, disabled);
            RuntimeProvider.Instance.SetColorBlock(nameButton, normal, highlight, pressed, disabled);

            cell.tree = tree;
            cell.nameButton = nameButton;
            cell.nameLabel = nameButton.GetComponentInChildren<Text>();
            cell.nameLabel.alignment = TextAnchor.MiddleLeft;
            cell.expandButton = expandButton;
            cell.expandLabel = expandButton.GetComponentInChildren<Text>();
            cell.spacer = spacer.GetComponent<LayoutElement>();

            prototype.SetActive(false);

            return prototype;
        }
    }
}
