using System;
using System.Reflection;
using UnityEngine;
using UnityExplorer.Helpers;
using UnityExplorer.UI;
using UnityEngine.UI;

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

            //if (m_targetType == typeof(Texture2D))
            //    ConstructTextureHelper();
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

        //internal bool showingTextureHelper;
        //internal bool constructedTextureViewer;

        //internal void ConstructTextureHelper()
        //{
        //    var rowObj = UIFactory.CreateHorizontalGroup(Content, new Color(0.1f, 0.1f, 0.1f));
        //    var rowLayout = rowObj.AddComponent<LayoutElement>();
        //    rowLayout.minHeight = 25;
        //    rowLayout.flexibleHeight = 0;
        //    var rowGroup = rowObj.GetComponent<HorizontalLayoutGroup>();
        //    rowGroup.childForceExpandHeight = true;
        //    rowGroup.childForceExpandWidth = false;
        //    rowGroup.padding.top = 3;
        //    rowGroup.padding.left = 3;
        //    rowGroup.padding.bottom = 3;
        //    rowGroup.padding.right = 3;
        //    rowGroup.spacing = 5;

        //    var showBtnObj = UIFactory.CreateButton(rowObj, new Color(0.2f, 0.2f, 0.2f));
        //    var showBtnLayout = showBtnObj.AddComponent<LayoutElement>();
        //    showBtnLayout.minWidth = 50;
        //    showBtnLayout.flexibleWidth = 0;
        //    var showText = showBtnObj.GetComponentInChildren<Text>();
        //    showText.text = "Show";
        //    var showBtn = showBtnObj.GetComponent<Button>();

        //    var labelObj = UIFactory.CreateLabel(rowObj, TextAnchor.MiddleLeft);
        //    var labelText = labelObj.GetComponent<Text>();
        //    labelText.text = "Texture Viewer";

        //    var textureViewerObj = UIFactory.CreateScrollView(Content, out GameObject scrollContent, out _, new Color(0.1f, 0.1f, 0.1f));
        //    var viewerGroup = scrollContent.GetComponent<VerticalLayoutGroup>();
        //    viewerGroup.childForceExpandHeight = false;
        //    viewerGroup.childForceExpandWidth = false;
        //    viewerGroup.childControlHeight = true;
        //    viewerGroup.childControlWidth = true;
        //    var mainLayout = textureViewerObj.GetComponent<LayoutElement>();
        //    mainLayout.flexibleHeight = -1;
        //    mainLayout.flexibleWidth = 2000;
        //    mainLayout.minHeight = 25;

        //    textureViewerObj.SetActive(false);

        //    showBtn.onClick.AddListener(() =>
        //    {
        //        showingTextureHelper = !showingTextureHelper;

        //        if (showingTextureHelper)
        //        {
        //            if (!constructedTextureViewer)
        //                ConstructTextureViewerArea(scrollContent);

        //            showText.text = "Hide";
        //            textureViewerObj.SetActive(true);
        //        }
        //        else
        //        {
        //            showText.text = "Show";
        //            textureViewerObj.SetActive(false);
        //        }
        //    });
        //}

        //internal void ConstructTextureViewerArea(GameObject parent)
        //{
        //    constructedTextureViewer = true;

        //    var tex = Target as Texture2D;

        //    if (!tex)
        //    {
        //        ExplorerCore.LogWarning("Could not cast the target instance to Texture2D!");
        //        return;
        //    }

        //    var imageObj = UIFactory.CreateUIObject("TextureViewerImage", parent, new Vector2(1, 1));
        //    var image = imageObj.AddComponent<Image>();
        //    var sprite = UIManager.CreateSprite(tex);
        //    image.sprite = sprite;

        //    var fitter = imageObj.AddComponent<ContentSizeFitter>();
        //    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        //    //fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        //    var imageLayout = imageObj.AddComponent<LayoutElement>();
        //    imageLayout.preferredHeight = sprite.rect.height;
        //    imageLayout.preferredWidth = sprite.rect.width;
        //}

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
