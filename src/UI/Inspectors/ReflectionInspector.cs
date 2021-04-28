using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Inspectors.CacheObject;
using UnityExplorer.UI.Inspectors.CacheObject.Views;
using UnityExplorer.UI.ObjectPool;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Inspectors
{
    public class ReflectionInspector : InspectorBase, IPoolDataSource<CacheMemberCell>
    {
        public bool StaticOnly { get; internal set; }
        public bool AutoUpdate { get; internal set; }

        public object Target { get; private set; }
        public Type TargetType { get; private set; }

        public ScrollPool<CacheMemberCell> MemberScrollPool { get; private set; }

        private List<CacheMember> members = new List<CacheMember>();
        private readonly List<CacheMember> filteredMembers = new List<CacheMember>();

        public override GameObject UIRoot => uiRoot;
        private GameObject uiRoot;

        public Text NameText;
        public Text AssemblyText;

        private LayoutElement memberTitleLayout;

        public override void OnBorrowedFromPool(object target)
        {
            base.OnBorrowedFromPool(target);

            SetTitleLayouts();
            SetTarget(target);

            RuntimeProvider.Instance.StartCoroutine(InitCoroutine());
        }

        private IEnumerator InitCoroutine()
        {
            yield return null;

            LayoutRebuilder.ForceRebuildLayoutImmediate(InspectorPanel.Instance.ContentRect);

            MemberScrollPool.RecreateHeightCache();
            MemberScrollPool.Rebuild();
        }

        private void SetTarget(object target)
        {
            string prefix;
            if (StaticOnly)
            {
                Target = null;
                TargetType = target as Type;
                prefix = "[S]";
            }
            else
            {
                Target = target;
                TargetType = target.GetActualType();
                prefix = "[R]";
            }

            NameText.text = SignatureHighlighter.ParseFullSyntax(TargetType, true);

            string asmText;
            if (TargetType.Assembly != null && !string.IsNullOrEmpty(TargetType.Assembly.Location))
                asmText  = Path.GetFileName(TargetType.Assembly.Location);
            else
                asmText = $"{TargetType.Assembly.GetName().Name} <color=grey><i>(in memory)</i></color>";
            AssemblyText.text = $"<color=grey>Assembly:</color> {asmText}";

            Tab.TabText.text = $"{prefix} {SignatureHighlighter.HighlightTypeName(TargetType)}";

            this.members = CacheMember.GetCacheMembers(Target, TargetType, this);
            FilterMembers();
        }

        public void FilterMembers()
        {
            // todo
            for (int i = 0; i < members.Count; i++)
            {
                var member = members[i];
                filteredMembers.Add(member);
            }
        }

        public override void OnReturnToPool()
        {
            foreach (var member in members)
                member.OnDestroyed();

            members.Clear();
            filteredMembers.Clear();

            // release all cachememberviews
            MemberScrollPool.ReturnCells();
            MemberScrollPool.SetUninitialized();

            base.OnReturnToPool();
        }

        public override void OnSetActive()
        {
            base.OnSetActive();
        }

        public override void OnSetInactive()
        {
            base.OnSetInactive();
        }

        private float timeOfLastUpdate;

        public override void Update()
        {
            if (!this.IsActive)
                return;

            if (!StaticOnly && Target.IsNullOrDestroyed(false))
            {
                InspectorManager.ReleaseInspector(this);
                return;
            }

            if (AutoUpdate && Time.time - timeOfLastUpdate > 1f)
            {
                timeOfLastUpdate = Time.time;

                // Update displayed values (TODO)
            }
        }

        protected override void OnCloseClicked()
        {
            InspectorManager.ReleaseInspector(this);
        }


        #region IPoolDataSource

        public int ItemCount => filteredMembers.Count;

        public void OnCellBorrowed(CacheMemberCell cell)
        {
            cell.CurrentOwner = this;

            // todo add listeners
            cell.OnInspectClicked += OnCellInspect;
            cell.OnApplyClicked += OnCellApply;
            cell.OnSubContentClicked += OnCellSubContentToggle;
            cell.OnUpdateClicked += OnCellUpdateClicked;
            cell.OnEvaluateClicked += OnCellEvaluateClicked;
        }

        private void OnCellInspect(CacheMember occupant)
        {
            InspectorManager.Inspect(occupant.Value);
        }

        private void OnCellApply(CacheMember occupant)
        {
            ExplorerCore.Log($"TODO OnApply: {occupant.NameForFiltering}");
        }

        private void OnCellSubContentToggle(CacheMember occupant)
        {
            ExplorerCore.Log($"TODO SubContentToggle: {occupant.NameForFiltering}");
        }

        private void OnCellUpdateClicked(CacheMember occupant)
        {
            ExplorerCore.Log("TODO Update: " + occupant.NameForFiltering);
        }

        private void OnCellEvaluateClicked(CacheMember occupant)
        {
            ExplorerCore.Log("TODO Evaluate or toggle: " + occupant);
        }

        public void OnCellReturned(CacheMemberCell cell)
        {
            cell.OnReturnToPool();
        }

        public void SetCell(CacheMemberCell cell, int index)
        {
            if (cell.CurrentOccupant != null)
            {
                // TODO
            }

            if (index < 0 || index >= filteredMembers.Count)
            {
                cell.Disable();
                return;
            }

            var member = filteredMembers[index];
            cell.CurrentOccupant = member;
            member.SetCell(cell);

            SetCellLayout(cell);
        }

        public void DisableCell(CacheMemberCell cell, int index)
        {
            // need to do anything?
        }

        #endregion

        // Cell layout (fake table alignment)

        private static float MemLabelWidth { get; set; }
        private static float RightGroupWidth { get; set; }

        private void SetTitleLayouts()
        {
            // Calculate sizes
            MemLabelWidth = Math.Max(200, Math.Min(450f, 0.4f * InspectorManager.PanelWidth - 5));
            RightGroupWidth = Math.Max(200, InspectorManager.PanelWidth - MemLabelWidth - 55);

            memberTitleLayout.minWidth = MemLabelWidth;
        }

        private void SetCellLayout(CacheMemberCell cell)
        {
            cell.MemberLayout.minWidth = MemLabelWidth;
            cell.RightGroupLayout.minWidth = RightGroupWidth;
        }

        internal void SetLayouts()
        {
            SetTitleLayouts();

            foreach (var cell in MemberScrollPool.CellPool)
                SetCellLayout(cell);
        }

        public override GameObject CreateContent(GameObject parent)
        {
            uiRoot = UIFactory.CreateVerticalGroup(parent, "ReflectionInspector", true, true, true, true, 5, 
                new Vector4(4, 4, 4, 4), new Color(0.12f, 0.12f, 0.12f));

            NameText = UIFactory.CreateLabel(uiRoot, "Title", "not set", TextAnchor.MiddleLeft, fontSize: 20);
            UIFactory.SetLayoutElement(NameText.gameObject, minHeight: 25, flexibleHeight: 0);

            AssemblyText = UIFactory.CreateLabel(uiRoot, "AssemblyLabel", "not set", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(AssemblyText.gameObject, minHeight: 25, flexibleWidth: 9999);

            var listTitles = UIFactory.CreateUIObject("ListTitles", uiRoot);
            UIFactory.SetLayoutElement(listTitles, minHeight: 25);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(listTitles, true, true, true, true, 5, 1, 1, 1, 1);

            var memberTitle = UIFactory.CreateLabel(listTitles, "MemberTitle", "Member Name", TextAnchor.LowerLeft, Color.grey, fontSize: 15);
            memberTitleLayout = memberTitle.gameObject.AddComponent<LayoutElement>();

            //var typeTitle = UIFactory.CreateLabel(listTitles, "TypeTitle", "Type", TextAnchor.LowerLeft, Color.grey, fontSize: 15);
            //typeTitleLayout = typeTitle.gameObject.AddComponent<LayoutElement>();

            var valueTitle = UIFactory.CreateLabel(listTitles, "ValueTitle", "Value", TextAnchor.LowerLeft, Color.grey, fontSize: 15);
            UIFactory.SetLayoutElement(valueTitle.gameObject, minWidth: 150, flexibleWidth: 9999);

            MemberScrollPool = UIFactory.CreateScrollPool<CacheMemberCell>(uiRoot, "MemberList", out GameObject scrollObj,
                out GameObject scrollContent, new Color(0.09f, 0.09f, 0.09f));
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
            //UIFactory.SetLayoutElement(scrollContent, flexibleHeight: 9999);

            MemberScrollPool.Initialize(this);

            //InspectorPanel.Instance.UIRoot.GetComponent<Mask>().enabled = false;
            //MemberScrollPool.Viewport.GetComponent<Mask>().enabled = false;

            return uiRoot;
        }
    }
}
