using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Runtime;
using UnityExplorer.CacheObject;
using UnityExplorer.CacheObject.Views;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Widgets;
using UnityExplorer.UI;

namespace UnityExplorer.Inspectors
{
    public class ReflectionInspector : InspectorBase, ICellPoolDataSource<CacheMemberCell>, ICacheObjectController
    {
        public CacheObjectBase ParentCacheObject { get; set; }
        public Type TargetType { get; private set; }
        public bool StaticOnly { get; internal set; }
        public bool CanWrite => true;

        private List<CacheMember> members = new List<CacheMember>();
        private readonly List<CacheMember> filteredMembers = new List<CacheMember>();

        public bool AutoUpdateWanted => autoUpdateToggle.isOn;

        private BindingFlags FlagsFilter;
        private string NameFilter;

        private MemberFlags MemberFilter = MemberFlags.All;
        private enum MemberFlags
        {
            None = 0,
            Property = 1,
            Field = 2,
            Method = 4,
            All = 7
        }

        // UI

        public ScrollPool<CacheMemberCell> MemberScrollPool { get; private set; }

        public Text NameText;
        public Text AssemblyText;
        private Toggle autoUpdateToggle;

        private string currentBaseTabText;

        private readonly Color disabledButtonColor = new Color(0.24f, 0.24f, 0.24f);
        private readonly Color enabledButtonColor = new Color(0.2f, 0.27f, 0.2f);

        private readonly Dictionary<BindingFlags, ButtonRef> scopeFilterButtons = new Dictionary<BindingFlags, ButtonRef>();
        private readonly List<Toggle> memberTypeToggles = new List<Toggle>();
        private InputFieldRef filterInputField;

        // Setup / return

        public override void OnBorrowedFromPool(object target)
        {
            base.OnBorrowedFromPool(target);
            CalculateLayouts();

            SetTarget(target);

            RuntimeProvider.Instance.StartCoroutine(InitCoroutine());
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

            UnityObjectRef = null;
            ComponentRef = null;
            TextureRef = null;
            CleanupTextureViewer();

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

            string asmText;
            if (TargetType.Assembly is AssemblyBuilder || string.IsNullOrEmpty(TargetType.Assembly.Location))
                asmText = $"{TargetType.Assembly.GetName().Name} <color=grey><i>(in memory)</i></color>";
            else
                asmText = Path.GetFileName(TargetType.Assembly.Location);
            AssemblyText.text = $"<color=grey>Assembly:</color> {asmText}";

            // unity helpers
            SetUnityTargets();

            // Get cache members

            this.members = CacheMember.GetCacheMembers(Target, TargetType, this);

            // reset filters

            this.filterInputField.Text = "";

            SetFilter("", StaticOnly ? BindingFlags.Static : BindingFlags.Instance);
            scopeFilterButtons[BindingFlags.Default].Component.gameObject.SetActive(!StaticOnly);
            scopeFilterButtons[BindingFlags.Instance].Component.gameObject.SetActive(!StaticOnly);

            foreach (var toggle in memberTypeToggles)
                toggle.isOn = true;

            refreshWanted = true;
        }

        // Updating

        private bool refreshWanted;
        private string lastNameFilter;
        private BindingFlags lastFlagsFilter;
        private MemberFlags lastMemberFilter = MemberFlags.All;
        private float timeOfLastAutoUpdate;

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
            if (refreshWanted || NameFilter != lastNameFilter || FlagsFilter != lastFlagsFilter || lastMemberFilter != MemberFilter)
            {
                lastNameFilter = NameFilter;
                lastFlagsFilter = FlagsFilter;
                lastMemberFilter = MemberFilter;

                FilterMembers();
                MemberScrollPool.Refresh(true, true);
                refreshWanted = false;
            }

            // once-per-second updates
            if (timeOfLastAutoUpdate.OccuredEarlierThan(1))
            {
                timeOfLastAutoUpdate = Time.realtimeSinceStartup;

                if (this.UnityObjectRef)
                {
                    nameInput.Text = UnityObjectRef.name;
                    this.Tab.TabText.text = $"{currentBaseTabText} \"{UnityObjectRef.name}\"";
                }

                if (AutoUpdateWanted)
                    UpdateDisplayedMembers();
            }
        }

        public void UpdateClicked()
        {
            UpdateDisplayedMembers();
        }

        // Filtering

        public void SetFilter(string filter) => SetFilter(filter, FlagsFilter);

        public void SetFilter(BindingFlags flagsFilter) => SetFilter(NameFilter, flagsFilter);

        public void SetFilter(string nameFilter, BindingFlags flagsFilter)
        {
            this.NameFilter = nameFilter;

            if (flagsFilter != FlagsFilter)
            {
                var btn = scopeFilterButtons[FlagsFilter].Component;
                RuntimeProvider.Instance.SetColorBlock(btn, disabledButtonColor, disabledButtonColor * 1.3f);

                this.FlagsFilter = flagsFilter;
                btn = scopeFilterButtons[FlagsFilter].Component;
                RuntimeProvider.Instance.SetColorBlock(btn, enabledButtonColor, enabledButtonColor * 1.3f);
            }
        }

        private void OnMemberTypeToggled(MemberFlags flag, bool val)
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

                if (FlagsFilter != BindingFlags.Default)
                {
                    if (FlagsFilter == BindingFlags.Instance && member.IsStatic
                        || FlagsFilter == BindingFlags.Static && !member.IsStatic)
                        continue;
                }

                if ((member is CacheMethod && !MemberFilter.HasFlag(MemberFlags.Method))
                    || (member is CacheField && !MemberFilter.HasFlag(MemberFlags.Field))
                    || (member is CacheProperty && !MemberFilter.HasFlag(MemberFlags.Property)))
                    continue;

                if (!string.IsNullOrEmpty(NameFilter) && !member.NameForFiltering.ContainsIgnoreCase(NameFilter))
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

        public int ItemCount => filteredMembers.Count;

        public void OnCellBorrowed(CacheMemberCell cell) { } // not needed

        public void SetCell(CacheMemberCell cell, int index)
        {
            CacheObjectControllerHelper.SetCell(cell, index, filteredMembers, SetCellLayout);
        }

        // Cell layout (fake table alignment)

        private static int LeftGroupWidth { get; set; }
        private static int RightGroupWidth { get; set; }

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

        // UI Construction

        private GameObject mainContentHolder;

        public override GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(parent, "ReflectionInspector", true, true, true, true, 5,
                new Vector4(4, 4, 4, 4), new Color(0.065f, 0.065f, 0.065f));

            // Class name, assembly

            NameText = UIFactory.CreateLabel(UIRoot, "Title", "not set", TextAnchor.MiddleLeft, fontSize: 17);
            UIFactory.SetLayoutElement(NameText.gameObject, minHeight: 25, flexibleHeight: 0);

            AssemblyText = UIFactory.CreateLabel(UIRoot, "AssemblyLabel", "not set", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(AssemblyText.gameObject, minHeight: 25, flexibleWidth: 9999);

            ConstructUnityObjectRow();

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
            var color = SignatureHighlighter.GetMemberInfoColor(type);
            toggleText.text = $"<color={color}>{type}</color>";

            toggle.graphic.TryCast<Image>().color = color.ToColor() * 0.65f;

            MemberFlags flag;
            switch (type)
            {
                case MemberTypes.Method: flag = MemberFlags.Method; break;
                case MemberTypes.Property: flag = MemberFlags.Property; break;
                case MemberTypes.Field: flag = MemberFlags.Field; break;
                default: return;
            }

            toggle.onValueChanged.AddListener((bool val) => { OnMemberTypeToggled(flag, val); });

            memberTypeToggles.Add(toggle);
        }


        // Todo should probably put this in a separate class or maybe as a widget

        #region UNITY OBJECT SPECIFIC

        // Unity object helpers

        private UnityEngine.Object UnityObjectRef;
        private Component ComponentRef;
        private Texture2D TextureRef;
        private bool TextureViewerWanted;
        private GameObject unityObjectRow;
        private ButtonRef gameObjectButton;
        private InputFieldRef nameInput;
        private InputFieldRef instanceIdInput;
        private ButtonRef textureButton;
        private GameObject textureViewer;

        private void SetUnityTargets()
        {
            if (StaticOnly || !typeof(UnityEngine.Object).IsAssignableFrom(TargetType))
            {
                unityObjectRow.SetActive(false);
                textureViewer.SetActive(false);
                return;
            }

            UnityObjectRef = (UnityEngine.Object)Target.TryCast(typeof(UnityEngine.Object));
            unityObjectRow.SetActive(true);

            nameInput.Text = UnityObjectRef.name;
            instanceIdInput.Text = UnityObjectRef.GetInstanceID().ToString();

            if (typeof(Component).IsAssignableFrom(TargetType))
            {
                ComponentRef = (Component)Target.TryCast(typeof(Component));
                gameObjectButton.Component.gameObject.SetActive(true);
            }
            else
                gameObjectButton.Component.gameObject.SetActive(false);

            if (typeof(Texture2D).IsAssignableFrom(TargetType))
            {
                TextureRef = (Texture2D)Target.TryCast(typeof(Texture2D));
                textureButton.Component.gameObject.SetActive(true);
            }
            else
                textureButton.Component.gameObject.SetActive(false);
        }

        private void OnGameObjectButtonClicked()
        {
            if (!ComponentRef)
            {
                ExplorerCore.LogWarning("Component reference is null or destroyed!");
                return;
            }

            InspectorManager.Inspect(ComponentRef.gameObject);
        }

        private void ToggleTextureViewer()
        {
            if (TextureViewerWanted)
            {
                // disable
                TextureViewerWanted = false;
                textureViewer.SetActive(false);
                mainContentHolder.SetActive(true);
                textureButton.ButtonText.text = "View Texture";
            }
            else
            {
                if (!textureImage.sprite)
                {
                    // First show, need to create sprite for displaying texture
                    SetTextureViewer();
                }

                // enable
                TextureViewerWanted = true;
                textureViewer.SetActive(true);
                mainContentHolder.gameObject.SetActive(false);
                textureButton.ButtonText.text = "Hide Texture";
            }
        }

        // UI construction

        private void ConstructUnityObjectRow()
        {
            unityObjectRow = UIFactory.CreateUIObject("UnityObjectRow", UIRoot);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(unityObjectRow, false, false, true, true, 5);
            UIFactory.SetLayoutElement(unityObjectRow, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            textureButton = UIFactory.CreateButton(unityObjectRow, "TextureButton", "View Texture", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(textureButton.Component.gameObject, minHeight: 25, minWidth: 150);
            textureButton.OnClick += ToggleTextureViewer;

            var nameLabel = UIFactory.CreateLabel(unityObjectRow, "NameLabel", "Name:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(nameLabel.gameObject, minHeight: 25, minWidth: 45, flexibleWidth: 0);

            nameInput = UIFactory.CreateInputField(unityObjectRow, "NameInput", "untitled");
            UIFactory.SetLayoutElement(nameInput.UIRoot, minHeight: 25, minWidth: 100, flexibleWidth: 1000);
            nameInput.Component.readOnly = true;

            gameObjectButton = UIFactory.CreateButton(unityObjectRow, "GameObjectButton", "Inspect GameObject", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(gameObjectButton.Component.gameObject, minHeight: 25, minWidth: 160);
            gameObjectButton.OnClick += OnGameObjectButtonClicked;

            var instanceLabel = UIFactory.CreateLabel(unityObjectRow, "InstanceLabel", "Instance ID:", TextAnchor.MiddleRight, Color.grey);
            UIFactory.SetLayoutElement(instanceLabel.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 0);

            instanceIdInput = UIFactory.CreateInputField(unityObjectRow, "InstanceIDInput", "ERROR");
            UIFactory.SetLayoutElement(instanceIdInput.UIRoot, minHeight: 25, minWidth: 100, flexibleWidth: 0);
            instanceIdInput.Component.readOnly = true;

            unityObjectRow.SetActive(false);

            ConstructTextureHelper();
        }

        // Texture viewer helper

        private InputFieldRef textureSavePathInput;
        private Image textureImage;
        private LayoutElement textureImageLayout;

        private void CleanupTextureViewer()
        {
            if (textureImage.sprite)
                GameObject.Destroy(textureImage.sprite);

            if (TextureViewerWanted)
                ToggleTextureViewer();
        }

        private void ConstructTextureHelper()
        {
            textureViewer = UIFactory.CreateVerticalGroup(UIRoot, "TextureViewer", false, false, true, true, 2, new Vector4(5, 5, 5, 5),
                new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(textureViewer, flexibleWidth: 9999, flexibleHeight: 9999);

            // Save helper

            var saveRowObj = UIFactory.CreateHorizontalGroup(textureViewer, "SaveRow", false, false, true, true, 2, new Vector4(2, 2, 2, 2),
                new Color(0.1f, 0.1f, 0.1f));

            var saveBtn = UIFactory.CreateButton(saveRowObj, "SaveButton", "Save .PNG", new Color(0.2f, 0.25f, 0.2f));
            UIFactory.SetLayoutElement(saveBtn.Component.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 0);
            saveBtn.OnClick += OnSaveTextureClicked;

            textureSavePathInput = UIFactory.CreateInputField(saveRowObj, "SaveInput", "...");
            UIFactory.SetLayoutElement(textureSavePathInput.UIRoot, minHeight: 25, minWidth: 100, flexibleWidth: 9999);

            // Actual texture viewer

            var imageViewport = UIFactory.CreateVerticalGroup(textureViewer, "Viewport", false, false, true, true);
            imageViewport.GetComponent<Image>().color = Color.white;
            imageViewport.AddComponent<Mask>().showMaskGraphic = false;

            var imageObj = UIFactory.CreateUIObject("Image", imageViewport);
            var fitter = imageObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            textureImage = imageObj.AddComponent<Image>();
            textureImageLayout = UIFactory.SetLayoutElement(imageObj, flexibleWidth: 9999, flexibleHeight: 9999);

            textureViewer.SetActive(false);
        }

        private void SetTextureViewer()
        {
            if (!this.TextureRef)
                return;

            var name = TextureRef.name;
            if (string.IsNullOrEmpty(name))
                name = "untitled";

            textureSavePathInput.Text = Path.Combine(ConfigManager.Default_Output_Path.Value, $"{name}.png");

            var sprite = TextureUtilProvider.Instance.CreateSprite(TextureRef);
            textureImage.sprite = sprite;

            textureImageLayout.preferredHeight = sprite.rect.height;
            textureImageLayout.preferredWidth = sprite.rect.width;
        }

        private void OnSaveTextureClicked()
        {
            if (!TextureRef)
            {
                ExplorerCore.LogWarning("Ref Texture is null, maybe it was destroyed?");
                return;
            }

            if (string.IsNullOrEmpty(textureSavePathInput.Text))
            {
                ExplorerCore.LogWarning("Save path cannot be empty!");
                return;
            }

            var path = textureSavePathInput.Text;
            if (!path.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
            {
                ExplorerCore.LogWarning("Desired save path must end with '.png'!");
                return;
            }

            path = IOUtility.EnsureValidDirectory(path);

            if (File.Exists(path))
                File.Delete(path);

            var tex = TextureRef;

            if (!TextureUtilProvider.IsReadable(tex))
                tex = TextureUtilProvider.ForceReadTexture(tex);

            byte[] data = TextureUtilProvider.Instance.EncodeToPNG(tex);

            File.WriteAllBytes(path, data);

            if (tex != TextureRef)
            {
                // cleanup temp texture if we had to force-read it.
                GameObject.Destroy(tex);
            }
        }

        #endregion
    }
}
