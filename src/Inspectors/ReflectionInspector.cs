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
        //public Type TargetType { get; private set; }
        public bool StaticOnly { get; internal set; }
        public bool CanWrite => true;

        public bool AutoUpdateWanted => autoUpdateToggle.isOn;

        List<CacheMember> members = new();
        readonly List<CacheMember> filteredMembers = new();

        string nameFilter;
        BindingFlags scopeFlagsFilter;
        MemberFilter memberFilter = MemberFilter.All;

        // Updating

        bool refreshWanted;
        string lastNameFilter;
        BindingFlags lastFlagsFilter;
        MemberFilter lastMemberFilter = MemberFilter.All;
        float timeOfLastAutoUpdate;

        // UI

        static int LeftGroupWidth { get; set; }
        static int RightGroupWidth { get; set; }

        static readonly Color disabledButtonColor = new(0.24f, 0.24f, 0.24f);
        static readonly Color enabledButtonColor = new(0.2f, 0.27f, 0.2f);

        public GameObject ContentRoot { get; private set; }
        public ScrollPool<CacheMemberCell> MemberScrollPool { get; private set; }
        public int ItemCount => filteredMembers.Count;
        public UnityObjectWidget UnityWidget { get; private set; }
        public string TabButtonText { get; set; }

        InputFieldRef hiddenNameText;
        Text nameText;
        Text assemblyText;
        Toggle autoUpdateToggle;

        ButtonRef makeGenericButton;
        GenericConstructorWidget genericConstructor;

        InputFieldRef filterInputField;
        readonly List<Toggle> memberTypeToggles = new();
        readonly Dictionary<BindingFlags, ButtonRef> scopeFilterButtons = new();

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
            foreach (CacheMember member in members)
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

            genericConstructor?.Cancel();

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

                makeGenericButton.GameObject.SetActive(TargetType.IsGenericTypeDefinition);
            }
            else
            {
                TargetType = target.GetActualType();
                prefix = "[R]";
            }

            // Setup main labels and tab text
            TabButtonText = $"{prefix} {SignatureHighlighter.Parse(TargetType, false)}";
            Tab.TabText.text = TabButtonText;
            nameText.text = SignatureHighlighter.Parse(TargetType, true);
            hiddenNameText.Text = SignatureHighlighter.RemoveHighlighting(nameText.text);

            string asmText;
            if (TargetType.Assembly is AssemblyBuilder || string.IsNullOrEmpty(TargetType.Assembly.Location))
                asmText = $"{TargetType.Assembly.GetName().Name} <color=grey><i>(in memory)</i></color>";
            else
                asmText = Path.GetFileName(TargetType.Assembly.Location);
            assemblyText.text = $"<color=grey>Assembly:</color> {asmText}";

            // Unity object helper widget

            if (!StaticOnly)
                this.UnityWidget = UnityObjectWidget.GetUnityWidget(target, TargetType, this);

            // Get cache members

            this.members = CacheMemberFactory.GetCacheMembers(TargetType, this);

            // reset filters

            this.filterInputField.Text = string.Empty;

            SetFilter(string.Empty, StaticOnly ? BindingFlags.Static : BindingFlags.Default);
            scopeFilterButtons[BindingFlags.Default].Component.gameObject.SetActive(!StaticOnly);
            scopeFilterButtons[BindingFlags.Instance].Component.gameObject.SetActive(!StaticOnly);

            foreach (Toggle toggle in memberTypeToggles)
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
            if (refreshWanted || nameFilter != lastNameFilter || scopeFlagsFilter != lastFlagsFilter || lastMemberFilter != memberFilter)
            {
                lastNameFilter = nameFilter;
                lastFlagsFilter = scopeFlagsFilter;
                lastMemberFilter = memberFilter;

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

        // Filtering

        public void SetFilter(string name, BindingFlags flags)
        {
            this.nameFilter = name;

            if (flags != scopeFlagsFilter)
            {
                Button btn = scopeFilterButtons[scopeFlagsFilter].Component;
                RuntimeHelper.SetColorBlock(btn, disabledButtonColor, disabledButtonColor * 1.3f);

                this.scopeFlagsFilter = flags;
                btn = scopeFilterButtons[scopeFlagsFilter].Component;
                RuntimeHelper.SetColorBlock(btn, enabledButtonColor, enabledButtonColor * 1.3f);
            }
        }

        void FilterMembers()
        {
            filteredMembers.Clear();

            for (int i = 0; i < members.Count; i++)
            {
                CacheMember member = members[i];

                if (scopeFlagsFilter != BindingFlags.Default)
                {
                    if (scopeFlagsFilter == BindingFlags.Instance && member.IsStatic
                        || scopeFlagsFilter == BindingFlags.Static && !member.IsStatic)
                        continue;
                }

                if ((member is CacheMethod && !memberFilter.HasFlag(MemberFilter.Method))
                    || (member is CacheField && !memberFilter.HasFlag(MemberFilter.Field))
                    || (member is CacheProperty && !memberFilter.HasFlag(MemberFilter.Property))
                    || (member is CacheConstructor && !memberFilter.HasFlag(MemberFilter.Constructor)))
                    continue;

                if (!string.IsNullOrEmpty(nameFilter) && !member.NameForFiltering.ContainsIgnoreCase(nameFilter))
                    continue;

                filteredMembers.Add(member);
            }
        }

        void UpdateDisplayedMembers()
        {
            bool shouldRefresh = false;
            foreach (CacheMemberCell cell in MemberScrollPool.CellPool)
            {
                if (!cell.Enabled || cell.Occupant == null)
                    continue;
                CacheMember member = cell.MemberOccupant;
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

            foreach (CacheMemberCell cell in MemberScrollPool.CellPool)
                SetCellLayout(cell);
        }

        void CalculateLayouts()
        {
            LeftGroupWidth = (int)Math.Max(200, (0.4f * InspectorManager.PanelWidth) - 5);
            RightGroupWidth = (int)Math.Max(200, InspectorManager.PanelWidth - LeftGroupWidth - 65);
        }

        void SetCellLayout(CacheObjectCell cell)
        {
            cell.NameLayout.minWidth = LeftGroupWidth;
            cell.RightGroupLayout.minWidth = RightGroupWidth;

            if (cell.Occupant?.IValue != null)
                cell.Occupant.IValue.SetLayout();
        }

        // UI listeners

        void OnUpdateClicked()
        {
            UpdateDisplayedMembers();
        }

        public void OnSetNameFilter(string name)
        {
            SetFilter(name, scopeFlagsFilter);
        }

        public void OnSetFlags(BindingFlags flags)
        {
            SetFilter(nameFilter, flags);
        }

        void OnMemberTypeToggled(MemberFilter flag, bool val)
        {
            if (!val)
                memberFilter &= ~flag;
            else
                memberFilter |= flag;
        }

        void OnCopyClicked()
        {
            ClipboardPanel.Copy(this.Target ?? this.TargetType);
        }

        void OnMakeGenericClicked()
        {
            ContentRoot.SetActive(false);

            if (genericConstructor == null)
            {
                genericConstructor = new();
                genericConstructor.ConstructUI(UIRoot);
            }

            genericConstructor.UIRoot.SetActive(true);
            genericConstructor.Show(OnGenericSubmit, OnGenericCancel, TargetType);
        }

        void OnGenericSubmit(Type[] args)
        {
            ContentRoot.SetActive(true);
            genericConstructor.UIRoot.SetActive(false);

            Type newType = TargetType.MakeGenericType(args);
            InspectorManager.Inspect(newType);
            //InspectorManager.ReleaseInspector(this);
        }

        void OnGenericCancel()
        {
            ContentRoot.SetActive(true);
            genericConstructor.UIRoot.SetActive(false);
        }

        // UI Construction

        public override GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(parent, "ReflectionInspector", true, true, true, true, 5,
                new Vector4(4, 4, 4, 4), new Color(0.065f, 0.065f, 0.065f));

            // Class name, assembly

            GameObject topRow = UIFactory.CreateHorizontalGroup(UIRoot, "TopRow", false, false, true, true, 4, default, 
                new(0.1f, 0.1f, 0.1f), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(topRow, minHeight: 25, flexibleWidth: 9999);

            GameObject titleHolder = UIFactory.CreateUIObject("TitleHolder", topRow);
            UIFactory.SetLayoutElement(titleHolder, minHeight: 35, flexibleHeight: 0, flexibleWidth: 9999);

            nameText = UIFactory.CreateLabel(titleHolder, "VisibleTitle", "NotSet", TextAnchor.MiddleLeft);
            RectTransform namerect = nameText.GetComponent<RectTransform>();
            namerect.anchorMin = new Vector2(0, 0);
            namerect.anchorMax = new Vector2(1, 1);
            nameText.fontSize = 17;
            UIFactory.SetLayoutElement(nameText.gameObject, minHeight: 35, flexibleHeight: 0, minWidth: 300, flexibleWidth: 9999);

            hiddenNameText = UIFactory.CreateInputField(titleHolder, "Title", "not set");
            RectTransform hiddenrect = hiddenNameText.Component.gameObject.GetComponent<RectTransform>();
            hiddenrect.anchorMin = new Vector2(0, 0);
            hiddenrect.anchorMax = new Vector2(1, 1);
            hiddenNameText.Component.readOnly = true;
            hiddenNameText.Component.lineType = InputField.LineType.MultiLineNewline;
            hiddenNameText.Component.gameObject.GetComponent<Image>().color = Color.clear;
            hiddenNameText.Component.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            hiddenNameText.Component.textComponent.fontSize = 17;
            hiddenNameText.Component.textComponent.color = Color.clear;
            UIFactory.SetLayoutElement(hiddenNameText.Component.gameObject, minHeight: 35, flexibleHeight: 0, flexibleWidth: 9999);

            makeGenericButton = UIFactory.CreateButton(topRow, "MakeGenericButton", "Construct Generic", new Color(0.2f, 0.3f, 0.2f));
            UIFactory.SetLayoutElement(makeGenericButton.GameObject, minWidth: 140, minHeight: 25);
            makeGenericButton.OnClick += OnMakeGenericClicked;
            makeGenericButton.GameObject.SetActive(false);

            ButtonRef copyButton = UIFactory.CreateButton(topRow, "CopyButton", "Copy to Clipboard", new Color(0.2f, 0.2f, 0.2f, 1));
            copyButton.ButtonText.color = Color.yellow;
            UIFactory.SetLayoutElement(copyButton.Component.gameObject, minHeight: 25, minWidth: 120, flexibleWidth: 0);
            copyButton.OnClick += OnCopyClicked;

            assemblyText = UIFactory.CreateLabel(UIRoot, "AssemblyLabel", "not set", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(assemblyText.gameObject, minHeight: 25, flexibleWidth: 9999);

            ContentRoot = UIFactory.CreateVerticalGroup(UIRoot, "MemberHolder", false, false, true, true, 5, new Vector4(2, 2, 2, 2),
                new Color(0.12f, 0.12f, 0.12f));
            UIFactory.SetLayoutElement(ContentRoot, flexibleWidth: 9999, flexibleHeight: 9999);

            ConstructFirstRow(ContentRoot);

            ConstructSecondRow(ContentRoot);

            // Member scroll pool

            GameObject memberBorder = UIFactory.CreateVerticalGroup(ContentRoot, "ScrollPoolHolder", false, false, true, true,
                padding: new Vector4(2, 2, 2, 2), bgColor: new Color(0.05f, 0.05f, 0.05f));
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

        void ConstructFirstRow(GameObject parent)
        {
            GameObject rowObj = UIFactory.CreateUIObject("FirstRow", parent);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(rowObj, true, true, true, true, 5, 2, 2, 2, 2);
            UIFactory.SetLayoutElement(rowObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            Text nameLabel = UIFactory.CreateLabel(rowObj, "NameFilterLabel", "Filter names:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(nameLabel.gameObject, minHeight: 25, minWidth: 90, flexibleWidth: 0);

            filterInputField = UIFactory.CreateInputField(rowObj, "NameFilterInput", "...");
            UIFactory.SetLayoutElement(filterInputField.UIRoot, minHeight: 25, flexibleWidth: 300);
            filterInputField.OnValueChanged += (string val) => { OnSetNameFilter(val); };

            GameObject spacer = UIFactory.CreateUIObject("Spacer", rowObj);
            UIFactory.SetLayoutElement(spacer, minWidth: 25);

            // Update button and toggle

            ButtonRef updateButton = UIFactory.CreateButton(rowObj, "UpdateButton", "Update displayed values", new Color(0.22f, 0.28f, 0.22f));
            UIFactory.SetLayoutElement(updateButton.Component.gameObject, minHeight: 25, minWidth: 175, flexibleWidth: 0);
            updateButton.OnClick += OnUpdateClicked;

            GameObject toggleObj = UIFactory.CreateToggle(rowObj, "AutoUpdateToggle", out autoUpdateToggle, out Text toggleText);
            UIFactory.SetLayoutElement(toggleObj, minWidth: 125, minHeight: 25);
            autoUpdateToggle.isOn = false;
            toggleText.text = "Auto-update";
        }

        // Second row

        void ConstructSecondRow(GameObject parent)
        {
            GameObject rowObj = UIFactory.CreateUIObject("SecondRow", parent);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(rowObj, false, false, true, true, 5, 2, 2, 2, 2);
            UIFactory.SetLayoutElement(rowObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            // Scope buttons

            Text scopeLabel = UIFactory.CreateLabel(rowObj, "ScopeLabel", "Scope:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(scopeLabel.gameObject, minHeight: 25, minWidth: 60, flexibleWidth: 0);
            AddScopeFilterButton(rowObj, BindingFlags.Default, true);
            AddScopeFilterButton(rowObj, BindingFlags.Instance);
            AddScopeFilterButton(rowObj, BindingFlags.Static);

            GameObject spacer = UIFactory.CreateUIObject("Spacer", rowObj);
            UIFactory.SetLayoutElement(spacer, minWidth: 15);

            // Member type toggles

            AddMemberTypeToggle(rowObj, MemberTypes.Property, 90);
            AddMemberTypeToggle(rowObj, MemberTypes.Field, 70);
            AddMemberTypeToggle(rowObj, MemberTypes.Method, 90);
            AddMemberTypeToggle(rowObj, MemberTypes.Constructor, 110);
        }

        void AddScopeFilterButton(GameObject parent, BindingFlags flags, bool setAsActive = false)
        {
            string lbl = flags == BindingFlags.Default ? "All" : flags.ToString();
            Color color = setAsActive ? enabledButtonColor : disabledButtonColor;

            ButtonRef button = UIFactory.CreateButton(parent, "Filter_" + flags, lbl, color);
            UIFactory.SetLayoutElement(button.Component.gameObject, minHeight: 25, flexibleHeight: 0, minWidth: 70, flexibleWidth: 0);
            scopeFilterButtons.Add(flags, button);

            button.OnClick += () => { OnSetFlags(flags); };
        }

        void AddMemberTypeToggle(GameObject parent, MemberTypes type, int width)
        {
            GameObject toggleObj = UIFactory.CreateToggle(parent, "Toggle_" + type, out Toggle toggle, out Text toggleText);
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
