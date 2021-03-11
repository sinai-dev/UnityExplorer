using System;
using System.Reflection;
using UnityEngine;
using UnityExplorer.Helpers;
using UnityExplorer.UI;
using UnityEngine.UI;
using UnityExplorer.Unstrip;
using System.IO;

namespace UnityExplorer.Inspectors.Reflection
{
    public enum MemberScopes
    {
        All,
        Instance,
        Static
    }

    public class InstanceInspector : ReflectionInspector
    {
        public override string TabLabel => $" <color=cyan>[R]</color> {base.TabLabel}";

        internal MemberScopes m_scopeFilter;
        internal Button m_lastActiveScopeButton;

        public InstanceInspector(object target) : base(target) { }

        private void OnScopeFilterClicked(MemberScopes type, Button button)
        {
            if (m_lastActiveScopeButton)
            {
                var lastColors = m_lastActiveScopeButton.colors;
                lastColors.normalColor = new Color(0.2f, 0.2f, 0.2f);
                m_lastActiveScopeButton.colors = lastColors;
            }

            m_scopeFilter = type;
            m_lastActiveScopeButton = button;

            var colors = m_lastActiveScopeButton.colors;
            colors.normalColor = new Color(0.2f, 0.6f, 0.2f);
            m_lastActiveScopeButton.colors = colors;

            FilterMembers(null, true);
            m_sliderScroller.m_slider.value = 1f;
        }

        public void ConstructInstanceHelpers()
        {
            if (!typeof(Component).IsAssignableFrom(m_targetType) && !typeof(UnityEngine.Object).IsAssignableFrom(m_targetType))
                return;

            var rowObj = UIFactory.CreateHorizontalGroup(Content, new Color(0.1f, 0.1f, 0.1f));
            var rowGroup = rowObj.GetComponent<HorizontalLayoutGroup>();
            rowGroup.childForceExpandWidth = true;
            rowGroup.childControlWidth = true;
            rowGroup.spacing = 5;
            rowGroup.padding.top = 2;
            rowGroup.padding.bottom = 2;
            rowGroup.padding.right = 2;
            rowGroup.padding.left = 2;
            var rowLayout = rowObj.AddComponent<LayoutElement>();
            rowLayout.minHeight = 25;
            rowLayout.flexibleWidth = 5000;

            if (typeof(Component).IsAssignableFrom(m_targetType))
            {
                ConstructCompHelper(rowObj);
            }

            ConstructUObjHelper(rowObj);

            // WIP

            if (m_targetType == typeof(Texture2D))
                ConstructTextureHelper();
        }

        internal void ConstructCompHelper(GameObject rowObj)
        {
            var labelObj = UIFactory.CreateLabel(rowObj, TextAnchor.MiddleLeft);
            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.minWidth = 90;
            labelLayout.minHeight = 25;
            labelLayout.flexibleWidth = 0;
            var labelText = labelObj.GetComponent<Text>();
            labelText.text = "GameObject:";

#if MONO
                var comp = Target as Component;
#else
            var comp = (Target as Il2CppSystem.Object).TryCast<Component>();
#endif

            var goBtnObj = UIFactory.CreateButton(rowObj, new Color(0.2f, 0.5f, 0.2f));
            var goBtnLayout = goBtnObj.AddComponent<LayoutElement>();
            goBtnLayout.minHeight = 25;
            goBtnLayout.minWidth = 200;
            goBtnLayout.flexibleWidth = 0;
            var text = goBtnObj.GetComponentInChildren<Text>();
            text.text = comp.name;
            var btn = goBtnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => { InspectorManager.Instance.Inspect(comp.gameObject); });
        }

        internal void ConstructUObjHelper(GameObject rowObj)
        {
            var labelObj = UIFactory.CreateLabel(rowObj, TextAnchor.MiddleLeft);
            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.minWidth = 60;
            labelLayout.minHeight = 25;
            labelLayout.flexibleWidth = 0;
            var labelText = labelObj.GetComponent<Text>();
            labelText.text = "Name:";

#if MONO
            var uObj = Target as UnityEngine.Object;
#else
            var uObj = (Target as Il2CppSystem.Object).TryCast<UnityEngine.Object>();
#endif

            var inputObj = UIFactory.CreateInputField(rowObj, 14, 3, 1);
            var inputLayout = inputObj.AddComponent<LayoutElement>();
            inputLayout.minHeight = 25;
            inputLayout.flexibleWidth = 2000;
            var inputField = inputObj.GetComponent<InputField>();
            inputField.readOnly = true;
            inputField.text = uObj.name;

            //var goBtnObj = UIFactory.CreateButton(rowObj, new Color(0.2f, 0.5f, 0.2f));
            //var goBtnLayout = goBtnObj.AddComponent<LayoutElement>();
            //goBtnLayout.minHeight = 25;
            //goBtnLayout.minWidth = 200;
            //goBtnLayout.flexibleWidth = 0;
            //var text = goBtnObj.GetComponentInChildren<Text>();
            //text.text = comp.name;
            //var btn = goBtnObj.GetComponent<Button>();
            //btn.onClick.AddListener(() => { InspectorManager.Instance.Inspect(comp.gameObject); });
        }

        internal bool showingTextureHelper;
        internal bool constructedTextureViewer;

        internal GameObject m_textureViewerObj;

        internal void ConstructTextureHelper()
        {
            var rowObj = UIFactory.CreateHorizontalGroup(Content, new Color(0.1f, 0.1f, 0.1f));
            var rowLayout = rowObj.AddComponent<LayoutElement>();
            rowLayout.minHeight = 25;
            rowLayout.flexibleHeight = 0;
            var rowGroup = rowObj.GetComponent<HorizontalLayoutGroup>();
            rowGroup.childForceExpandHeight = true;
            rowGroup.childForceExpandWidth = false;
            rowGroup.padding.top = 3;
            rowGroup.padding.left = 3;
            rowGroup.padding.bottom = 3;
            rowGroup.padding.right = 3;
            rowGroup.spacing = 5;

            var showBtnObj = UIFactory.CreateButton(rowObj, new Color(0.2f, 0.6f, 0.2f));
            var showBtnLayout = showBtnObj.AddComponent<LayoutElement>();
            showBtnLayout.minWidth = 50;
            showBtnLayout.flexibleWidth = 0;
            var showText = showBtnObj.GetComponentInChildren<Text>();
            showText.text = "Show";
            var showBtn = showBtnObj.GetComponent<Button>();

            var labelObj = UIFactory.CreateLabel(rowObj, TextAnchor.MiddleLeft);
            var labelText = labelObj.GetComponent<Text>();
            labelText.text = "Texture Viewer";

            var textureViewerObj = UIFactory.CreateScrollView(Content, out GameObject scrollContent, out _, new Color(0.1f, 0.1f, 0.1f));
            var viewerGroup = scrollContent.GetComponent<VerticalLayoutGroup>();
            viewerGroup.childForceExpandHeight = false;
            viewerGroup.childForceExpandWidth = false;
            viewerGroup.childControlHeight = true;
            viewerGroup.childControlWidth = true;
            var mainLayout = textureViewerObj.GetComponent<LayoutElement>();
            mainLayout.flexibleHeight = 9999;
            mainLayout.flexibleWidth = 9999;
            mainLayout.minHeight = 100;

            textureViewerObj.SetActive(false);

            m_textureViewerObj = textureViewerObj;

            showBtn.onClick.AddListener(() =>
            {
                showingTextureHelper = !showingTextureHelper;

                if (showingTextureHelper)
                {
                    if (!constructedTextureViewer)
                        ConstructTextureViewerArea(scrollContent);

                    showText.text = "Hide";
                    ToggleTextureViewer(true);
                }
                else
                {
                    showText.text = "Show";
                    ToggleTextureViewer(false);
                }
            });
        }

        internal void ConstructTextureViewerArea(GameObject parent)
        {
            constructedTextureViewer = true;

            var tex = Target as Texture2D;
#if CPP
            if (!tex)
                tex = (Target as Il2CppSystem.Object).TryCast<Texture2D>();
#endif

            if (!tex)
            {
                ExplorerCore.LogWarning("Could not cast the target instance to Texture2D! Maybe its null or destroyed?");
                return;
            }

            // Save helper

            var saveRowObj = UIFactory.CreateHorizontalGroup(parent, new Color(0.1f, 0.1f, 0.1f));
            var saveRow = saveRowObj.GetComponent<HorizontalLayoutGroup>();
            saveRow.childForceExpandHeight = true;
            saveRow.childForceExpandWidth = true;
            saveRow.padding = new RectOffset() { left = 2, bottom = 2, right = 2, top = 2 };
            saveRow.spacing = 2;

            var btnObj = UIFactory.CreateButton(saveRowObj, new Color(0.2f, 0.2f, 0.2f));
            var btnLayout = btnObj.AddComponent<LayoutElement>();
            btnLayout.minHeight = 25;
            btnLayout.minWidth = 100;
            btnLayout.flexibleWidth = 0;
            var saveBtn = btnObj.GetComponent<Button>();

            var saveBtnText = btnObj.GetComponentInChildren<Text>();
            saveBtnText.text = "Save .PNG";

            var inputObj = UIFactory.CreateInputField(saveRowObj);
            var inputLayout = inputObj.AddComponent<LayoutElement>();
            inputLayout.minHeight = 25;
            inputLayout.minWidth = 100;
            inputLayout.flexibleWidth = 9999;
            var inputField = inputObj.GetComponent<InputField>();

            var name = tex.name;
            if (string.IsNullOrEmpty(name))
                name = "untitled";

            var savePath = $@"{Config.ExplorerConfig.Instance.Default_Output_Path}\{name}.png";
            inputField.text = savePath;

            saveBtn.onClick.AddListener(() => 
            {
                if (tex && !string.IsNullOrEmpty(inputField.text))
                {
                    var path = inputField.text;
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

                    if (!tex.IsReadable())
                        tex = Texture2DHelpers.ForceReadTexture(tex);
#if CPP
                    byte[] data = tex.EncodeToPNG();
#else
                    byte[] data = tex.EncodeToPNGSafe();
#endif

                    File.WriteAllBytes(path, data);
                }
            });

            // Actual texture viewer

            var imageObj = UIFactory.CreateUIObject("TextureViewerImage", parent);
            var image = imageObj.AddComponent<Image>();
            var sprite = ImageConversionUnstrip.CreateSprite(tex);
            image.sprite = sprite;

            var fitter = imageObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            //fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var imageLayout = imageObj.AddComponent<LayoutElement>();
            imageLayout.preferredHeight = sprite.rect.height;
            imageLayout.preferredWidth = sprite.rect.width;
        }

        internal void ToggleTextureViewer(bool enabled)
        {
            m_textureViewerObj.SetActive(enabled);

            m_filterAreaObj.SetActive(!enabled);
            m_memberListObj.SetActive(!enabled);
            m_updateRowObj.SetActive(!enabled);
        }

        public void ConstructInstanceFilters(GameObject parent)
        {
            var memberFilterRowObj = UIFactory.CreateHorizontalGroup(parent, new Color(1, 1, 1, 0));
            var memFilterGroup = memberFilterRowObj.GetComponent<HorizontalLayoutGroup>();
            memFilterGroup.childForceExpandHeight = false;
            memFilterGroup.childForceExpandWidth = false;
            memFilterGroup.childControlWidth = true;
            memFilterGroup.childControlHeight = true;
            memFilterGroup.spacing = 5;
            var memFilterLayout = memberFilterRowObj.AddComponent<LayoutElement>();
            memFilterLayout.minHeight = 25;
            memFilterLayout.flexibleHeight = 0;
            memFilterLayout.flexibleWidth = 5000;

            var memLabelObj = UIFactory.CreateLabel(memberFilterRowObj, TextAnchor.MiddleLeft);
            var memLabelLayout = memLabelObj.AddComponent<LayoutElement>();
            memLabelLayout.minWidth = 100;
            memLabelLayout.minHeight = 25;
            memLabelLayout.flexibleWidth = 0;
            var memLabelText = memLabelObj.GetComponent<Text>();
            memLabelText.text = "Filter scope:";
            memLabelText.color = Color.grey;

            AddFilterButton(memberFilterRowObj, MemberScopes.All, true);
            AddFilterButton(memberFilterRowObj, MemberScopes.Instance);
            AddFilterButton(memberFilterRowObj, MemberScopes.Static);
        }

        private void AddFilterButton(GameObject parent, MemberScopes type, bool setEnabled = false)
        {
            var btnObj = UIFactory.CreateButton(parent, new Color(0.2f, 0.2f, 0.2f));

            var btnLayout = btnObj.AddComponent<LayoutElement>();
            btnLayout.minHeight = 25;
            btnLayout.minWidth = 70;

            var text = btnObj.GetComponentInChildren<Text>();
            text.text = type.ToString();

            var btn = btnObj.GetComponent<Button>();

            btn.onClick.AddListener(() => { OnScopeFilterClicked(type, btn); });

            var colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.7f, 0.3f);

            if (setEnabled)
            {
                colors.normalColor = new Color(0.2f, 0.6f, 0.2f);
                m_scopeFilter = type;
                m_lastActiveScopeButton = btn;
            }

            btn.colors = colors;
        }
    }
}
