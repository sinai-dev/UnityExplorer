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

        public object Target { get; private set; }
        public Type TargetType { get; private set; }

        public ScrollPool<CacheMemberCell> MemberScrollPool { get; private set; }

        private List<CacheMember> members = new List<CacheMember>();
        private readonly List<CacheMember> filteredMembers = new List<CacheMember>();
        private readonly HashSet<CacheMember> displayedMembers = new HashSet<CacheMember>();

        public override GameObject UIRoot => uiRoot;
        private GameObject uiRoot;

        public Text NameText;
        public Text AssemblyText;

        private LayoutElement memberTitleLayout;

        private Toggle autoUpdateToggle;

        public override void OnBorrowedFromPool(object target)
        {
            base.OnBorrowedFromPool(target);

            SetTitleLayouts();
            SetTarget(target);

            // MemberScrollPool.SetDataSource(this);
            MemberScrollPool.Refresh(true, true);
            RuntimeProvider.Instance.StartCoroutine(InitCoroutine());
        }

        private IEnumerator InitCoroutine()
        {
            yield return null;

            LayoutRebuilder.ForceRebuildLayoutImmediate(InspectorPanel.Instance.ContentRect);
        }

        public override void OnReturnToPool()
        {
            foreach (var member in members)
                member.ReleasePooledObjects();

            members.Clear();
            filteredMembers.Clear();
            displayedMembers.Clear();

            autoUpdateToggle.isOn = false;

            base.OnReturnToPool();
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

            Tab.TabText.text = $"{prefix} {SignatureHighlighter.ParseFullType(TargetType)}";

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

            //MemberScrollPool.RecreateHeightCache();
        }

        public override void OnSetActive()
        {
            base.OnSetActive();
        }

        public override void OnSetInactive()
        {
            base.OnSetInactive();
        }

        protected override void OnCloseClicked()
        {
            InspectorManager.ReleaseInspector(this);
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

            if (Time.time - timeOfLastUpdate > 1f)
            {
                timeOfLastUpdate = Time.time;

                UpdateDisplayedMembers(true);
            }
        }

        private void UpdateDisplayedMembers()
        {
            UpdateDisplayedMembers(false);
        }

        private void UpdateDisplayedMembers(bool onlyAutoUpdate)
        {
            bool shouldRefresh = false;
            foreach (var member in displayedMembers)
            {
                if (member.ShouldAutoEvaluate && (!onlyAutoUpdate || member.AutoUpdateWanted))
                {
                    shouldRefresh = true;
                    member.Evaluate();
                    member.SetCell(member.CellView);
                }
            }

            if (shouldRefresh)
                MemberScrollPool.Refresh(false);
        }

        // Member cells

        public int ItemCount => filteredMembers.Count;

        public void OnCellBorrowed(CacheMemberCell cell)
        {
            cell.Owner = this;
        }

        public void SetCell(CacheMemberCell cell, int index)
        {
            if (index < 0 || index >= filteredMembers.Count)
            {
                if (cell.Occupant != null)
                {
                    if (displayedMembers.Contains(cell.MemberOccupant))
                        displayedMembers.Remove(cell.MemberOccupant);

                    cell.Occupant.CellView = null;
                }

                cell.Disable();
                return;
            }

            var member = filteredMembers[index];

            if (member != cell.Occupant)
            {
                if (cell.Occupant != null)
                {
                    cell.Occupant.HideIValue();
                    displayedMembers.Remove(cell.MemberOccupant);
                    cell.Occupant.CellView = null;
                }

                cell.Occupant = member;
                member.CellView = cell;
                displayedMembers.Add(member);
            }
            
            member.SetCell(cell);

            SetCellLayout(cell);
        }

        private void ToggleAllAutoUpdateStates(bool state)
        {
            if (members == null || !members.Any())
                return;

            foreach (var member in members)
                member.AutoUpdateWanted = state;

            foreach (var cell in MemberScrollPool.CellPool)
                cell.UpdateToggle.isOn = state;
        }

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

        private void SetCellLayout(CacheObjectCell cell)
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

            // Class name, assembly. TODO more details

            NameText = UIFactory.CreateLabel(uiRoot, "Title", "not set", TextAnchor.MiddleLeft, fontSize: 20);
            UIFactory.SetLayoutElement(NameText.gameObject, minHeight: 25, flexibleHeight: 0);

            AssemblyText = UIFactory.CreateLabel(uiRoot, "AssemblyLabel", "not set", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(AssemblyText.gameObject, minHeight: 25, flexibleWidth: 9999);

            // TODO filter row

            // Member list

            var listTitles = UIFactory.CreateUIObject("ListTitles", uiRoot);
            UIFactory.SetLayoutElement(listTitles, minHeight: 25);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(listTitles, true, true, true, true, 5, 1, 1, 1, 1);

            var memberTitle = UIFactory.CreateLabel(listTitles, "MemberTitle", "Member Name", TextAnchor.LowerLeft, Color.grey, fontSize: 15);
            memberTitleLayout = memberTitle.gameObject.AddComponent<LayoutElement>();

            var valueTitle = UIFactory.CreateLabel(listTitles, "ValueTitle", "Value", TextAnchor.LowerLeft, Color.grey, fontSize: 15);
            UIFactory.SetLayoutElement(valueTitle.gameObject, minWidth: 50, flexibleWidth: 9999);

            var updateButton = UIFactory.CreateButton(listTitles, "UpdateButton", "Update values", new Color(0.22f, 0.28f, 0.22f));
            UIFactory.SetLayoutElement(updateButton.Button.gameObject, minHeight: 25, minWidth: 130, flexibleWidth: 0);
            updateButton.OnClick += UpdateDisplayedMembers;

            var updateText = UIFactory.CreateLabel(listTitles, "AutoUpdateLabel", "Auto-update", TextAnchor.MiddleRight, Color.grey);
            UIFactory.SetLayoutElement(updateText.gameObject, minHeight: 25, minWidth: 80, flexibleWidth: 0);

            var toggleObj = UIFactory.CreateToggle(listTitles, "AutoUpdateToggle", out autoUpdateToggle, out Text toggleText);
            GameObject.DestroyImmediate(toggleText);
            UIFactory.SetLayoutElement(toggleObj, minHeight: 25, minWidth: 25);
            autoUpdateToggle.isOn = false;
            autoUpdateToggle.onValueChanged.AddListener((bool val) => { ToggleAllAutoUpdateStates(val); });

            var spacer = UIFactory.CreateUIObject("spacer", listTitles);
            UIFactory.SetLayoutElement(spacer, minWidth: 25, flexibleWidth: 0);

            // Member scroll pool

            MemberScrollPool = UIFactory.CreateScrollPool<CacheMemberCell>(uiRoot, "MemberList", out GameObject scrollObj,
                out GameObject _, new Color(0.09f, 0.09f, 0.09f));
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
            MemberScrollPool.Initialize(this);

            //InspectorPanel.Instance.UIRoot.GetComponent<Mask>().enabled = false;
            //MemberScrollPool.Viewport.GetComponent<Mask>().enabled = false;
            //MemberScrollPool.Viewport.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f);

            return uiRoot;
        }
    }
}
