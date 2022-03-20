using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.CacheObject;
using UnityExplorer.CacheObject.Views;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Widgets;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.ObjectPool;
using UniverseLib.UI.Widgets.ScrollView;
using UniverseLib.Utility;

namespace UnityExplorer.Inspectors
{
    [Flags]
    public enum MemberFilter
    {
        None = 0,
        Property = 1,
        Field = 2,
        Constructor = 4,
        Method = 8,
        All = Property | Field | Method | Constructor,
    }

    public class ReflectionInspector : InspectorBase, ICellPoolDataSource<CacheMemberCell>, ICacheObjectController
    {
        public CacheObjectBase ParentCacheObject { get; set; }
        public Type TargetType { get; private set; }
        public bool StaticOnly { get; internal set; }
        public bool CanWrite => true;

        public bool AutoUpdateWanted => autoUpdateToggle.isOn;

        private List<CacheMember> members = new();
        private readonly List<CacheMember> filteredMembers = new();

        private BindingFlags scopeFlagsFilter;
        private string nameFilter;

        private MemberFilter MemberFilter = MemberFilter.All;

        // Updating

        private bool refreshWanted;
        private string lastNameFilter;
        private BindingFlags lastFlagsFilter;
        private MemberFilter lastMemberFilter = MemberFilter.All;
        private float timeOfLastAutoUpdate;

        // UI

        internal GameObject mainContentHolder;
        private static int LeftGroupWidth { get; set; }
        private static int RightGroupWidth { get; set; }

        public ScrollPool<CacheMemberCell> MemberScrollPool { get; private set; }
        public int ItemCount => filteredMembers.Count;

        public UnityObjectWidget UnityWidget;

        public InputFieldRef HiddenNameText;
        public Text NameText;
        public Text AssemblyText;
        private Toggle autoUpdateToggle;

        internal string currentBaseTabText;

        private readonly Dictionary<BindingFlags, ButtonRef> scopeFilterButtons = new();
        private readonly List<Toggle> memberTypeToggles = new();
        private InputFieldRef filterInputField;

        // const

        private readonly Color disabledButtonColor = new(0.24f, 0.24f, 0.24f);
        private readonly Color enabledButtonColor = new(0.2f, 0.27f, 0.2f);

        // Setup

        public override void OnBorrowedFromPool(object target)
        {
            base.OnBorrowedFromPool(target);
            CalculateLayouts();

            SetTarget(target);

            RuntimeHelper.StartCoroutine(InitCoroutine());
        }

        private IEnumerator InitCoroutine()
        {
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(InspectorPanel.Instance.ContentRect);
        }

        public override void CloseInspector()
        {
            InspectorManager.ReleaseInspector(this);
        }

        public override void OnReturnToPool()
        {
            foreach (var member in members)
            {
                member.UnlinkFromView();
                member.ReleasePooledObjects();
            }

            members.Clear();
            filteredMembers.Clear();

            autoUpdateToggle.isOn = false;

            if (UnityWidget != null)
            {
                UnityWidget.OnReturnToPool();
                Pool.Return(UnityWidget.GetType(), UnityWidget);
                this.UnityWidget = null;
            }

            base.OnReturnToPool();
        }

        // Setting target

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
                TargetType = target.GetActualType();
                prefix = "[R]";
            }

            // Setup main labels and tab text
            currentBaseTabText = $"{prefix} {SignatureHighlighter.Parse(TargetType, false)}";
            Tab.TabText.text = currentBaseTabText;
            NameText.text = SignatureHighlighter.Parse(TargetType, true);
            HiddenNameText.Text = SignatureHighlighter.RemoveHighlighting(NameText.text);

            string asmText;
            if (TargetType.Assembly is AssemblyBuilder || string.IsNullOrEmpty(TargetType.Assembly.Location))
                asmText = $"{TargetType.Assembly.GetName().Name} <color=grey><i>(in memory)</i></color>";
            else
                asmText = Path.GetFileName(TargetType.Assembly.Location);
            AssemblyText.text = $"<color=grey>Assembly:</color> {asmText}";

            // Unity object helper widget

            this.UnityWidget = UnityObjectWidget.GetUnityWidget(target, TargetType, this);

            // Get cache members

            this.members = CacheMemberFactory.GetCacheMembers(Target, TargetType, this);

            // reset filters

            this.filterInputField.Text = string.Empty;

            SetFilter(string.Empty, StaticOnly ? BindingFlags.Static : BindingFlags.Default);
            scopeFilterButtons[BindingFlags.Default].Component.gameObject.SetActive(!StaticOnly);
            scopeFilterButtons[BindingFlags.Instance].Component.gameObject.SetActive(!StaticOnly);

            foreach (var toggle in memberTypeToggles)
                toggle.isOn = true;

            refreshWanted = true;
        }

        // Updating

        public override void Update()
        {
            if (!this.IsActive)
                return;

            if (!StaticOnly && Target.IsNullOrDestroyed(false))
            {
                InspectorManager.ReleaseInspector(this);
                return;
            }

            // check filter changes or force-refresh
            if (refreshWanted || nameFilter != lastNameFilter || scopeFlagsFilter != lastFlagsFilter || lastMemberFilter != MemberFilter)
            {
                lastNameFilter = nameFilter;
                lastFlagsFilter = scopeFlagsFilter;
                lastMemberFilter = MemberFilter;

                FilterMembers();
                MemberScrollPool.Refresh(true, true);
                refreshWanted = false;
            }

            // once-per-second updates
            if (timeOfLastAutoUpdate.OccuredEarlierThan(1))
            {
                timeOfLastAutoUpdate = Time.realtimeSinceStartup;

                if (this.UnityWidget != null)
                    UnityWidget.Update();

                if (AutoUpdateWanted)
                    UpdateDisplayedMembers();
            }
        }

        public void UpdateClicked()
        {
            UpdateDisplayedMembers();
        }

        // Filtering

        public void SetFilter(string name) => SetFilter(name, scopeFlagsFilter);

        public void SetFilter(BindingFlags flags) => SetFilter(nameFilter, flags);

        public void SetFilter(string name, BindingFlags flags)
        {
            this.nameFilter = name;

            if (flags != scopeFlagsFilter)
            {
                var btn = scopeFilterButtons[scopeFlagsFilter].Component;
                RuntimeHelper.SetColorBlock(btn, disabledButtonColor, disabledButtonColor * 1.3f);

                this.scopeFlagsFilter = flags;
                btn = scopeFilterButtons[scopeFlagsFilter].Component;
                RuntimeHelper.SetColorBlock(btn, enabledButtonColor, enabledButtonColor * 1.3f);
            }
        }

        private void OnMemberTypeToggled(MemberFilter flag, bool val)
        {
            if (!val)
                MemberFilter &= ~flag;
            else
                MemberFilter |= flag;
        }

        private void FilterMembers()
        {
            filteredMembers.Clear();

            for (int i = 0; i < members.Count; i++)
            {
                var member = members[i];

                if (scopeFlagsFilter != BindingFlags.Default)
                {
                    if (scopeFlagsFilter == BindingFlags.Instance && member.IsStatic
                        || scopeFlagsFilter == BindingFlags.Static && !member.IsStatic)
                        continue;
                }

                if ((member is CacheMethod && !MemberFilter.HasFlag(MemberFilter.Method))
                    || (member is CacheField && !MemberFilter.HasFlag(MemberFilter.Field))
                    || (member is CacheProperty && !MemberFilter.HasFlag(MemberFilter.Property))
                    || (member is CacheConstructor && !MemberFilter.HasFlag(MemberFilter.Constructor)))
                    continue;

                if (!string.IsNullOrEmpty(nameFilter) && !member.NameForFiltering.ContainsIgnoreCase(nameFilter))
                    continue;

                filteredMembers.Add(member);
            }
        }

        private void UpdateDisplayedMembers()
        {
            bool shouldRefresh = false;
            foreach (var cell in MemberScrollPool.CellPool)
            {
                if (!cell.Enabled || cell.Occupant == null)
                    continue;
                var member = cell.MemberOccupant;
                if (member.ShouldAutoEvaluate)
                {
                    shouldRefresh = true;
                    member.Evaluate();
                    member.SetDataToCell(member.CellView);
                }
            }

            if (shouldRefresh)
                MemberScrollPool.Refresh(false);
        }

        // Member cells

        public void OnCellBorrowed(CacheMemberCell cell) { } // not needed

        public void SetCell(CacheMemberCell cell, int index)
        {
            CacheObjectControllerHelper.SetCell(cell, index, filteredMembers, SetCellLayout);
        }

        // Cell layout (fake table alignment)

        internal void SetLayouts()
        {
            CalculateLayouts();

            foreach (var cell in MemberScrollPool.CellPool)
                SetCellLayout(cell);
        }

        private void CalculateLayouts()
        {
            LeftGroupWidth = (int)Math.Max(200, (0.4f * InspectorManager.PanelWidth) - 5);
            RightGroupWidth = (int)Math.Max(200, InspectorManager.PanelWidth - LeftGroupWidth - 65);
        }

        private void SetCellLayout(CacheObjectCell cell)
        {
            cell.NameLayout.minWidth = LeftGroupWidth;
            cell.RightGroupLayout.minWidth = RightGroupWidth;

            if (cell.Occupant?.IValue != null)
                cell.Occupant.IValue.SetLayout();
        }

        private void OnCopyClicked()
        {
            ClipboardPanel.Copy(this.Target ?? this.TargetType);
        }

        // UI Construction

        public override GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(parent, "ReflectionInspector", true, true, true, true, 5,
                new Vector4(4, 4, 4, 4), new Color(0.065f, 0.065f, 0.065f));

            // Class name, assembly

            var topRow = UIFactory.CreateHorizontalGroup(UIRoot, "TopRow", false, false, true, true, 4, default, new(1, 1, 1, 0), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(topRow, minHeight: 25, flexibleWidth: 9999);

            var titleHolder = UIFactory.CreateUIObject("TitleHolder", topRow);
            UIFactory.SetLayoutElement(titleHolder, minHeight: 35, flexibleHeight: 0, flexibleWidth: 9999);

            NameText = UIFactory.CreateLabel(titleHolder, "VisibleTitle", "NotSet", TextAnchor.MiddleLeft);
            var namerect = NameText.GetComponent<RectTransform>();
            namerect.anchorMin = new Vector2(0, 0);
            namerect.anchorMax = new Vector2(1, 1);
            NameText.fontSize = 17;
            UIFactory.SetLayoutElement(NameText.gameObject, minHeight: 35, flexibleHeight: 0, minWidth: 300, flexibleWidth: 9999);

            HiddenNameText = UIFactory.CreateInputField(titleHolder, "Title", "not set");
            var hiddenrect = HiddenNameText.Component.gameObject.GetComponent<RectTransform>();
            hiddenrect.anchorMin = new Vector2(0, 0);
            hiddenrect.anchorMax = new Vector2(1, 1);
            HiddenNameText.Component.readOnly = true;
            HiddenNameText.Component.lineType = InputField.LineType.MultiLineNewline;
            HiddenNameText.Component.gameObject.GetComponent<Image>().color = Color.clear;
            HiddenNameText.Component.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            HiddenNameText.Component.textComponent.fontSize = 17;
            HiddenNameText.Component.textComponent.color = Color.clear;
            UIFactory.SetLayoutElement(HiddenNameText.Component.gameObject, minHeight: 35, flexibleHeight: 0, flexibleWidth: 9999);

            var copyButton = UIFactory.CreateButton(topRow, "CopyButton", "Copy to Clipboard", new Color(0.2f, 0.2f, 0.2f, 1));
            copyButton.ButtonText.color = Color.yellow;
            UIFactory.SetLayoutElement(copyButton.Component.gameObject, minHeight: 25, minWidth: 120, flexibleWidth: 0);
            copyButton.OnClick += OnCopyClicked;

            AssemblyText = UIFactory.CreateLabel(UIRoot, "AssemblyLabel", "not set", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(AssemblyText.gameObject, minHeight: 25, flexibleWidth: 9999);

            mainContentHolder = UIFactory.CreateVerticalGroup(UIRoot, "MemberHolder", false, false, true, true, 5, new Vector4(2, 2, 2, 2),
                new Color(0.12f, 0.12f, 0.12f));
            UIFactory.SetLayoutElement(mainContentHolder, flexibleWidth: 9999, flexibleHeight: 9999);

            ConstructFirstRow(mainContentHolder);

            ConstructSecondRow(mainContentHolder);

            // Member scroll pool

            var memberBorder = UIFactory.CreateVerticalGroup(mainContentHolder, "ScrollPoolHolder", false, false, true, true, padding: new Vector4(2, 2, 2, 2),
                bgColor: new Color(0.05f, 0.05f, 0.05f));
            UIFactory.SetLayoutElement(memberBorder, flexibleWidth: 9999, flexibleHeight: 9999);

            MemberScrollPool = UIFactory.CreateScrollPool<CacheMemberCell>(memberBorder, "MemberList", out GameObject scrollObj,
                out GameObject _, new Color(0.09f, 0.09f, 0.09f));
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
            MemberScrollPool.Initialize(this);

            // For debugging scroll pool
            //InspectorPanel.Instance.UIRoot.GetComponent<Mask>().enabled = false;
            //MemberScrollPool.Viewport.GetComponent<Mask>().enabled = false;
            //MemberScrollPool.Viewport.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f);

            return UIRoot;
        }

        // First row

        private void ConstructFirstRow(GameObject parent)
        {
            var rowObj = UIFactory.CreateUIObject("FirstRow", parent);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(rowObj, true, true, true, true, 5, 2, 2, 2, 2);
            UIFactory.SetLayoutElement(rowObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            var nameLabel = UIFactory.CreateLabel(rowObj, "NameFilterLabel", "Filter names:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(nameLabel.gameObject, minHeight: 25, minWidth: 90, flexibleWidth: 0);

            filterInputField = UIFactory.CreateInputField(rowObj, "NameFilterInput", "...");
            UIFactory.SetLayoutElement(filterInputField.UIRoot, minHeight: 25, flexibleWidth: 300);
            filterInputField.OnValueChanged += (string val) => { SetFilter(val); };

            var spacer = UIFactory.CreateUIObject("Spacer", rowObj);
            UIFactory.SetLayoutElement(spacer, minWidth: 25);

            // Update button and toggle

            var updateButton = UIFactory.CreateButton(rowObj, "UpdateButton", "Update displayed values", new Color(0.22f, 0.28f, 0.22f));
            UIFactory.SetLayoutElement(updateButton.Component.gameObject, minHeight: 25, minWidth: 175, flexibleWidth: 0);
            updateButton.OnClick += UpdateClicked;

            var toggleObj = UIFactory.CreateToggle(rowObj, "AutoUpdateToggle", out autoUpdateToggle, out Text toggleText);
            UIFactory.SetLayoutElement(toggleObj, minWidth: 125, minHeight: 25);
            autoUpdateToggle.isOn = false;
            toggleText.text = "Auto-update";
        }

        // Second row

        private void ConstructSecondRow(GameObject parent)
        {
            var rowObj = UIFactory.CreateUIObject("SecondRow", parent);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(rowObj, false, false, true, true, 5, 2, 2, 2, 2);
            UIFactory.SetLayoutElement(rowObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            // Scope buttons

            var scopeLabel = UIFactory.CreateLabel(rowObj, "ScopeLabel", "Scope:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(scopeLabel.gameObject, minHeight: 25, minWidth: 60, flexibleWidth: 0);
            AddScopeFilterButton(rowObj, BindingFlags.Default, true);
            AddScopeFilterButton(rowObj, BindingFlags.Instance);
            AddScopeFilterButton(rowObj, BindingFlags.Static);

            var spacer = UIFactory.CreateUIObject("Spacer", rowObj);
            UIFactory.SetLayoutElement(spacer, minWidth: 15);

            // Member type toggles

            AddMemberTypeToggle(rowObj, MemberTypes.Property, 90);
            AddMemberTypeToggle(rowObj, MemberTypes.Field, 70);
            AddMemberTypeToggle(rowObj, MemberTypes.Method, 90);
            AddMemberTypeToggle(rowObj, MemberTypes.Constructor, 110);
        }

        private void AddScopeFilterButton(GameObject parent, BindingFlags flags, bool setAsActive = false)
        {
            string lbl = flags == BindingFlags.Default ? "All" : flags.ToString();
            var color = setAsActive ? enabledButtonColor : disabledButtonColor;

            var button = UIFactory.CreateButton(parent, "Filter_" + flags, lbl, color);
            UIFactory.SetLayoutElement(button.Component.gameObject, minHeight: 25, flexibleHeight: 0, minWidth: 70, flexibleWidth: 0);
            scopeFilterButtons.Add(flags, button);

            button.OnClick += () => { SetFilter(flags); };
        }

        private void AddMemberTypeToggle(GameObject parent, MemberTypes type, int width)
        {
            var toggleObj = UIFactory.CreateToggle(parent, "Toggle_" + type, out Toggle toggle, out Text toggleText);
            UIFactory.SetLayoutElement(toggleObj, minHeight: 25, minWidth: width);
            string color = type switch
            {
                MemberTypes.Method => SignatureHighlighter.METHOD_INSTANCE,
                MemberTypes.Field => SignatureHighlighter.FIELD_INSTANCE,
                MemberTypes.Property => SignatureHighlighter.PROP_INSTANCE,
                MemberTypes.Constructor => SignatureHighlighter.CLASS_INSTANCE,
                _ => throw new NotImplementedException()
            };
            toggleText.text = $"<color={color}>{type}</color>";

            toggle.graphic.TryCast<Image>().color = color.ToColor() * 0.65f;

            MemberFilter flag = type switch
            {
                MemberTypes.Method => MemberFilter.Method,
                MemberTypes.Property => MemberFilter.Property,
                MemberTypes.Field => MemberFilter.Field,
                MemberTypes.Constructor => MemberFilter.Constructor,
                _ => throw new NotImplementedException()
            };

            toggle.onValueChanged.AddListener((bool val) => { OnMemberTypeToggled(flag, val); });

            memberTypeToggles.Add(toggle);
        }
    }
}
