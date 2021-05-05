using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI.CacheObject;
using UnityExplorer.UI.CacheObject.Views;
using UnityExplorer.UI.ObjectPool;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Inspectors
{
    public class ReflectionInspector : InspectorBase, IPoolDataSource<CacheMemberCell>, ICacheObjectController
    {
        public CacheObjectBase ParentCacheObject { get; set; }

        public Type TargetType { get; private set; }
        public bool CanWrite => true;

        // Instance state

        public bool StaticOnly { get; internal set; }

        public BindingFlags FlagsFilter { get; private set; }
        public string NameFilter { get; private set; }

        public bool AutoUpdateWanted { get; set; }

        private List<CacheMember> members = new List<CacheMember>();
        private readonly List<CacheMember> filteredMembers = new List<CacheMember>();


        // UI

        public ScrollPool<CacheMemberCell> MemberScrollPool { get; private set; }

        public Text NameText;
        public Text AssemblyText;

        // Unity object helpers
        private UnityEngine.Object ObjectRef;
        private Component ComponentRef;
        private Texture2D TextureRef;
        private bool TextureViewerWanted;
        private GameObject unityObjectRow;
        private ButtonRef gameObjectButton;
        private InputField nameInput;
        private InputField instanceIdInput;
        private ButtonRef textureButton;
        private GameObject textureViewer;

        private readonly Color disabledButtonColor = new Color(0.24f, 0.24f, 0.24f);
        private readonly Color enabledButtonColor = new Color(0.2f, 0.27f, 0.2f);
        private readonly Dictionary<BindingFlags, ButtonRef> scopeFilterButtons = new Dictionary<BindingFlags, ButtonRef>();
        private InputField filterInputField;

        //private LayoutElement memberTitleLayout;

        private Toggle autoUpdateToggle;

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

        protected override void OnCloseClicked()
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
            AutoUpdateWanted = false;

            ObjectRef = null;
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
            Tab.TabText.text = $"{prefix} {SignatureHighlighter.Parse(TargetType, false)}";
            NameText.text = SignatureHighlighter.Parse(TargetType, true);

            string asmText;
            if (TargetType.Assembly != null && !string.IsNullOrEmpty(TargetType.Assembly.Location))
                asmText = Path.GetFileName(TargetType.Assembly.Location);
            else
                asmText = $"{TargetType.Assembly.GetName().Name} <color=grey><i>(in memory)</i></color>";
            AssemblyText.text = $"<color=grey>Assembly:</color> {asmText}";

            // unity helpers
            SetUnityTargets();

            // Get cache members, and set filter to default 
            this.members = CacheMember.GetCacheMembers(Target, TargetType, this);
            this.filterInputField.text = "";
            SetFilter("", StaticOnly ? BindingFlags.Static : BindingFlags.Instance);
            refreshWanted = true;
        }

        // Updating

        private bool refreshWanted;
        private string lastNameFilter;
        private BindingFlags lastFlagsFilter;
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

            if (refreshWanted || NameFilter != lastNameFilter || FlagsFilter != lastFlagsFilter)
            {
                lastNameFilter = NameFilter;
                lastFlagsFilter = FlagsFilter;

                FilterMembers();
                MemberScrollPool.Refresh(true, true);
                refreshWanted = false;
            }

            if (timeOfLastAutoUpdate.OccuredEarlierThan(1))
            {
                timeOfLastAutoUpdate = Time.realtimeSinceStartup;

                if (AutoUpdateWanted)
                    UpdateDisplayedMembers();// true);
            }
        }

        // Filtering

        public void SetFilter(string filter) => SetFilter(filter, FlagsFilter);

        public void SetFilter(BindingFlags flagsFilter) => SetFilter(NameFilter, flagsFilter);

        public void SetFilter(string nameFilter, BindingFlags flagsFilter)
        {
            this.NameFilter = nameFilter;

            if (flagsFilter != FlagsFilter)
            {
                var btn = scopeFilterButtons[FlagsFilter].Button;
                RuntimeProvider.Instance.SetColorBlock(btn, disabledButtonColor, disabledButtonColor * 1.3f);

                this.FlagsFilter = flagsFilter;
                btn = scopeFilterButtons[FlagsFilter].Button;
                RuntimeProvider.Instance.SetColorBlock(btn, enabledButtonColor, enabledButtonColor * 1.3f);
            }
        }

        private void FilterMembers()
        {
            filteredMembers.Clear();

            for (int i = 0; i < members.Count; i++)
            {
                var member = members[i];

                if (!string.IsNullOrEmpty(NameFilter) && !member.NameForFiltering.ContainsIgnoreCase(NameFilter))
                    continue;

                if (FlagsFilter != BindingFlags.Default)
                {
                    if (FlagsFilter == BindingFlags.Instance && member.IsStatic
                        || FlagsFilter == BindingFlags.Static && !member.IsStatic)
                        continue;
                }

                filteredMembers.Add(member);
            }
        }

        private void UpdateDisplayedMembers()// bool onlyAutoUpdate)
        {
            bool shouldRefresh = false;
            foreach (var cell in MemberScrollPool.CellPool)
            {
                if (!cell.Enabled || cell.Occupant == null)
                    continue;
                var member = cell.MemberOccupant;
                if (member.ShouldAutoEvaluate) // && (!onlyAutoUpdate || member.AutoUpdateWanted))
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
            if (index < 0 || index >= filteredMembers.Count)
            {
                if (cell.Occupant != null)
                    cell.Occupant.UnlinkFromView();

                cell.Disable();
                return;
            }

            var member = filteredMembers[index];

            if (cell.Occupant != null && member != cell.Occupant)
                cell.Occupant.UnlinkFromView();

            member.SetView(cell);
            member.SetDataToCell(cell);

            SetCellLayout(cell);
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
            // Calculate sizes
            LeftGroupWidth = (int)Math.Max(200, (0.4f * InspectorManager.PanelWidth) - 5);// Math.Min(450f, 0.4f * InspectorManager.PanelWidth - 5));
            RightGroupWidth = (int)Math.Max(200, InspectorManager.PanelWidth - LeftGroupWidth - 65);

            //memberTitleLayout.minWidth = LeftGroupWidth;
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

            mainContentHolder = UIFactory.CreateVerticalGroup(UIRoot, "MemberHolder", false, false, true, true, 5, new Vector4(2,2,2,2),
                new Color(0.12f, 0.12f, 0.12f));
            UIFactory.SetLayoutElement(mainContentHolder, flexibleWidth: 9999, flexibleHeight: 9999);

            ConstructFilterRow(mainContentHolder);

            ConstructUpdateRow(mainContentHolder);

            // Member scroll pool

            var memberBorder = UIFactory.CreateVerticalGroup(mainContentHolder, "ScrollPoolHolder", false, false, true, true, padding: new Vector4(2,2,2,2),
                bgColor: new Color(0.05f, 0.05f, 0.05f));
            UIFactory.SetLayoutElement(memberBorder, flexibleWidth: 9999, flexibleHeight: 9999);

            MemberScrollPool = UIFactory.CreateScrollPool<CacheMemberCell>(memberBorder, "MemberList", out GameObject scrollObj,
                out GameObject _, new Color(0.09f, 0.09f, 0.09f));
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
            MemberScrollPool.Initialize(this);

            //InspectorPanel.Instance.UIRoot.GetComponent<Mask>().enabled = false;
            //MemberScrollPool.Viewport.GetComponent<Mask>().enabled = false;
            //MemberScrollPool.Viewport.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f);

            return UIRoot;
        }

        // Filter row

        private void ConstructFilterRow(GameObject parent)
        {
            var filterRow = UIFactory.CreateUIObject("FilterRow", parent);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(filterRow, true, true, true, true, 5, 2, 2, 2, 2);
            UIFactory.SetLayoutElement(filterRow, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            var nameLabel = UIFactory.CreateLabel(filterRow, "NameFilterLabel", "Filter names:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(nameLabel.gameObject, minHeight: 25, minWidth: 90, flexibleWidth: 0);
            var nameFilterObj = UIFactory.CreateInputField(filterRow, "NameFilterInput", "...", out filterInputField);
            UIFactory.SetLayoutElement(nameFilterObj, minHeight: 25, flexibleWidth: 300);
            filterInputField.onValueChanged.AddListener((string val) => { SetFilter(val); });

            var spacer = UIFactory.CreateUIObject("Spacer", filterRow);
            UIFactory.SetLayoutElement(spacer, minWidth: 25);

            var scopeLabel = UIFactory.CreateLabel(filterRow, "ScopeLabel", "Scope:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(scopeLabel.gameObject, minHeight: 25, minWidth: 60, flexibleWidth: 0);
            AddFilterButton(filterRow, BindingFlags.Default, true);
            AddFilterButton(filterRow, BindingFlags.Instance);
            AddFilterButton(filterRow, BindingFlags.Static);
        }

        private void AddFilterButton(GameObject parent, BindingFlags flags, bool setAsActive = false)
        {
            string lbl = flags == BindingFlags.Default ? "All" : flags.ToString();
            var color = setAsActive ? enabledButtonColor : disabledButtonColor;

            var button = UIFactory.CreateButton(parent, "Filter_" + flags, lbl, color);
            UIFactory.SetLayoutElement(button.Button.gameObject, minHeight: 25, flexibleHeight: 0, minWidth: 100, flexibleWidth: 0);
            scopeFilterButtons.Add(flags, button);

            button.OnClick += () => { SetFilter(flags); };
        }

        // Update row

        private void ConstructUpdateRow(GameObject parent)
        {
            var updateRow = UIFactory.CreateUIObject("UpdateRow", parent);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(updateRow, false, false, true, true, 4);
            UIFactory.SetLayoutElement(updateRow, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            var updateButton = UIFactory.CreateButton(updateRow, "UpdateButton", "Update displayed values", new Color(0.22f, 0.28f, 0.22f));
            UIFactory.SetLayoutElement(updateButton.Button.gameObject, minHeight: 25, minWidth: 175, flexibleWidth: 0);
            updateButton.OnClick += UpdateDisplayedMembers;

            var toggleObj = UIFactory.CreateToggle(updateRow, "AutoUpdateToggle", out autoUpdateToggle, out Text toggleText);
            //GameObject.DestroyImmediate(toggleText);
            UIFactory.SetLayoutElement(toggleObj, minWidth: 185, minHeight: 25);
            autoUpdateToggle.isOn = false;
            autoUpdateToggle.onValueChanged.AddListener((bool val) => { AutoUpdateWanted = val; });
            toggleText.text = "Auto-update displayed";
        }

        #region UNITY OBJECT SPECIFIC

        // Unity object helpers

        private void SetUnityTargets()
        {
            if (!typeof(UnityEngine.Object).IsAssignableFrom(TargetType))
            {
                unityObjectRow.SetActive(false);
                textureViewer.SetActive(false);
                return;
            }

            ObjectRef = (UnityEngine.Object)Target.TryCast(typeof(UnityEngine.Object));
            unityObjectRow.SetActive(true);

            nameInput.text = ObjectRef.name;
            instanceIdInput.text = ObjectRef.GetInstanceID().ToString();

            if (typeof(Component).IsAssignableFrom(TargetType))
            {
                ComponentRef = (Component)Target.TryCast(typeof(Component));
                gameObjectButton.Button.gameObject.SetActive(true);
            }
            else
                gameObjectButton.Button.gameObject.SetActive(false);

            if (typeof(Texture2D).IsAssignableFrom(TargetType))
            {
                TextureRef = (Texture2D)Target.TryCast(typeof(Texture2D));
                textureButton.Button.gameObject.SetActive(true);
            }
            else
                textureButton.Button.gameObject.SetActive(false);
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
                textureViewer.gameObject.SetActive(false);
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
                textureViewer.gameObject.SetActive(true);
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
            UIFactory.SetLayoutElement(textureButton.Button.gameObject, minHeight: 25, minWidth: 150);
            textureButton.OnClick += ToggleTextureViewer;

            gameObjectButton = UIFactory.CreateButton(unityObjectRow, "GameObjectButton", "Inspect GameObject", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(gameObjectButton.Button.gameObject, minHeight: 25, minWidth: 170);
            gameObjectButton.OnClick += OnGameObjectButtonClicked;

            var nameLabel = UIFactory.CreateLabel(unityObjectRow, "NameLabel", "Name:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(nameLabel.gameObject, minHeight: 25, minWidth: 45, flexibleWidth: 0);

            var nameInputObj = UIFactory.CreateInputField(unityObjectRow, "NameInput", "untitled", out nameInput);
            UIFactory.SetLayoutElement(nameInputObj, minHeight: 25, minWidth: 100, flexibleWidth: 1000);
            nameInput.readOnly = true;

            var instanceLabel = UIFactory.CreateLabel(unityObjectRow, "InstanceLabel", "Instance ID:", TextAnchor.MiddleRight, Color.grey);
            UIFactory.SetLayoutElement(instanceLabel.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 0);

            var instanceInputObj = UIFactory.CreateInputField(unityObjectRow, "InstanceIDInput", "ERROR", out instanceIdInput);
            UIFactory.SetLayoutElement(instanceInputObj, minHeight: 25, minWidth: 100, flexibleWidth: 0);
            instanceIdInput.readOnly = true;

            unityObjectRow.SetActive(false);

            ConstructTextureHelper();
        }

        // Texture viewer helper

        private InputField textureSavePathInput;
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
            UIFactory.SetLayoutElement(saveBtn.Button.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 0);
            saveBtn.OnClick += OnSaveTextureClicked;

            var inputObj = UIFactory.CreateInputField(saveRowObj, "SaveInput", "...", out textureSavePathInput);
            UIFactory.SetLayoutElement(inputObj, minHeight: 25, minWidth: 100, flexibleWidth: 9999);

            // Actual texture viewer

            var imageObj = UIFactory.CreateUIObject("TextureViewerImage", textureViewer);
            textureImage = imageObj.AddComponent<Image>();
            textureImageLayout = textureImage.gameObject.AddComponent<LayoutElement>();

            var fitter = imageObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            textureViewer.SetActive(false);
        }

        private void SetTextureViewer()
        {
            if (!this.TextureRef)
                return;

            var name = TextureRef.name;
            if (string.IsNullOrEmpty(name))
                name = "untitled";

            textureSavePathInput.text = Path.Combine(ConfigManager.Default_Output_Path.Value, $"{name}.png");

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

            if (string.IsNullOrEmpty(textureSavePathInput.text))
            {
                ExplorerCore.LogWarning("Save path cannot be empty!");
                return;
            }

            var path = textureSavePathInput.text;
            if (!path.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
            {
                ExplorerCore.LogWarning("Desired save path must end with '.png'!");
                return;
            }

            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

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
