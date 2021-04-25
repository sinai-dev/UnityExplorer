using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Runtime;

namespace UnityExplorer.UI.Inspectors.Reflection
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

        internal void OnScopeFilterClicked(MemberScopes type, Button button)
        {
            if (m_lastActiveScopeButton)
                RuntimeProvider.Instance.SetColorBlock(m_lastActiveScopeButton, new Color(0.2f, 0.2f, 0.2f));

            m_scopeFilter = type;
            m_lastActiveScopeButton = button;

            RuntimeProvider.Instance.SetColorBlock(m_lastActiveScopeButton, new Color(0.2f, 0.6f, 0.2f));

            FilterMembers(null, true);

            ScrollPool.RecreateHeightCache();
            ScrollPool.RefreshAndJumpToTop();
            //RefreshDisplay();
            //m_sliderScroller.m_slider.value = 1f;
        }

        public void ConstructInstanceScopeFilters(GameObject parent)
        {
            var memberFilterRowObj = UIFactory.CreateHorizontalGroup(parent, "InstanceFilterRow", false, false, true, true, 5, default,
                new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(memberFilterRowObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 5000);

            var memLabel = UIFactory.CreateLabel(memberFilterRowObj, "MemberLabel", "Filter scope:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(memLabel.gameObject, minWidth: 100, minHeight: 25, flexibleWidth: 0);

            AddFilterButton(memberFilterRowObj, MemberScopes.All, true);
            AddFilterButton(memberFilterRowObj, MemberScopes.Instance);
            AddFilterButton(memberFilterRowObj, MemberScopes.Static);
        }

        private void AddFilterButton(GameObject parent, MemberScopes type, bool setEnabled = false)
        {
            var btn = UIFactory.CreateButton(parent,
                "ScopeFilterButton_" + type,
                type.ToString(),
                null,
                new Color(0.2f, 0.2f, 0.2f));

            UIFactory.SetLayoutElement(btn.gameObject, minHeight: 25, minWidth: 70);

            btn.onClick.AddListener(() => { OnScopeFilterClicked(type, btn); });

            RuntimeProvider.Instance.SetColorBlock(btn, highlighted: new Color(0.3f, 0.7f, 0.3f));

            if (setEnabled)
            {
                RuntimeProvider.Instance.SetColorBlock(btn, new Color(0.2f, 0.6f, 0.2f));
                m_scopeFilter = type;
                m_lastActiveScopeButton = btn;
            }
        }

        public void ConstructUnityInstanceHelpers()
        {
            if (!typeof(UnityEngine.Object).IsAssignableFrom(m_targetType))
                return;

            var rowObj = UIFactory.CreateHorizontalGroup(Content, "InstanceHelperRow", true, true, true, true, 5, new Vector4(2, 2, 2, 2),
                new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(rowObj, minHeight: 25, flexibleWidth: 5000);

            if (typeof(Component).IsAssignableFrom(m_targetType))
                ConstructCompHelper(rowObj);

            ConstructUnityObjHelper(rowObj);

            if (m_targetType == typeof(Texture2D))
                ConstructTextureHelper();
        }

        internal void ConstructCompHelper(GameObject rowObj)
        {
            //var gameObjectLabel = UIFactory.CreateLabel(rowObj, "GameObjectLabel", "GameObject:", TextAnchor.MiddleLeft);
            //UIFactory.SetLayoutElement(gameObjectLabel.gameObject, minWidth: 90, minHeight: 25, flexibleWidth: 0);

            var comp = Target.TryCast(typeof(Component)) as Component;

            var btn = UIFactory.CreateButton(rowObj,
                "GameObjectButton",
                "View GameObject",
                () => { InspectorManager.Inspect(comp.gameObject); },
                new Color(0.2f, 0.5f, 0.2f));
            UIFactory.SetLayoutElement(btn.gameObject, minHeight: 25, minWidth: 120, flexibleWidth: 0);
        }

        internal void ConstructUnityObjHelper(GameObject rowObj)
        {
            var label = UIFactory.CreateLabel(rowObj, "NameLabel", "Name:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(label.gameObject, minWidth: 60, minHeight: 25, flexibleWidth: 0);

            var uObj = Target.TryCast(typeof(UnityEngine.Object)) as UnityEngine.Object;

            var inputObj = UIFactory.CreateInputField(rowObj, "NameInput", "...", out InputField inputField, 14, 3, 1);
            UIFactory.SetLayoutElement(inputObj, minHeight: 25, flexibleWidth: 2000);
            inputField.readOnly = true;
            inputField.text = uObj.name;
        }

        internal bool showingTextureHelper;
        internal bool constructedTextureViewer;

        internal GameObject m_textureViewerObj;

        internal void ConstructTextureHelper()
        {
            var rowObj = UIFactory.CreateHorizontalGroup(Content, "TextureHelper", false, false, true, true, 5, new Vector4(3, 3, 3, 3),
                new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(rowObj, minHeight: 25, flexibleHeight: 0);

            var showBtn = UIFactory.CreateButton(rowObj, "ShowButton", "Show", null, new Color(0.2f, 0.3f, 0.2f));
            UIFactory.SetLayoutElement(showBtn.gameObject, minWidth: 50, flexibleWidth: 0, minHeight: 20);

            UIFactory.CreateLabel(rowObj, "TextureViewerLabel", "Texture Viewer", TextAnchor.MiddleLeft);

            m_textureViewerObj = UIFactory.CreateScrollView(Content, "TextureViewerContent", out GameObject scrollContent, out _,
                new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(scrollContent, false, false, true, true);
            UIFactory.SetLayoutElement(m_textureViewerObj, minHeight: 100, flexibleHeight: 9999, flexibleWidth: 9999);

            m_textureViewerObj.SetActive(false);

            var showText = showBtn.GetComponentInChildren<Text>();
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

            var tex = Target.TryCast(typeof(Texture2D)) as Texture2D;

            if (!tex)
            {
                ExplorerCore.LogWarning("Could not cast the target instance to Texture2D! Maybe its null or destroyed?");
                return;
            }

            // Save helper

            var saveRowObj = UIFactory.CreateHorizontalGroup(parent, "SaveRow", true, true, true, true, 2, new Vector4(2, 2, 2, 2),
                new Color(0.1f, 0.1f, 0.1f));

            var saveBtn = UIFactory.CreateButton(saveRowObj, "SaveButton", "Save .PNG", null, new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(saveBtn.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 0);

            var inputObj = UIFactory.CreateInputField(saveRowObj, "SaveInput", "...", out InputField inputField);
            UIFactory.SetLayoutElement(inputObj, minHeight: 25, minWidth: 100, flexibleWidth: 9999);

            var name = tex.name;
            if (string.IsNullOrEmpty(name))
                name = "untitled";

            inputField.text = Path.Combine(ConfigManager.Default_Output_Path.Value, $"{name}.png");

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

                    if (!TextureUtilProvider.IsReadable(tex))
                        tex = TextureUtilProvider.ForceReadTexture(tex);

                    byte[] data = TextureUtilProvider.Instance.EncodeToPNG(tex);

                    File.WriteAllBytes(path, data);
                }
            });

            // Actual texture viewer

            var imageObj = UIFactory.CreateUIObject("TextureViewerImage", parent);
            var image = imageObj.AddComponent<Image>();
            var sprite = TextureUtilProvider.Instance.CreateSprite(tex);
            image.sprite = sprite;

            var fitter = imageObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var imageLayout = imageObj.AddComponent<LayoutElement>();
            imageLayout.preferredHeight = sprite.rect.height;
            imageLayout.preferredWidth = sprite.rect.width;
        }

        internal void ToggleTextureViewer(bool enabled)
        {
            m_textureViewerObj.SetActive(enabled);

            m_filterAreaObj.SetActive(!enabled);
            this.ScrollPool.UIRoot.SetActive(!enabled);
            m_updateRowObj.SetActive(!enabled);
        }
    }
}
