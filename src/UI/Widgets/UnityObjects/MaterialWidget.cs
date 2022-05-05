using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Config;
using UnityExplorer.Inspectors;
using UnityExplorer.UI.Panels;
using UniverseLib;
using UniverseLib.Runtime;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.ObjectPool;
using UniverseLib.Utility;

namespace UnityExplorer.UI.Widgets
{
    public class MaterialWidget : UnityObjectWidget
    {
        static MaterialWidget()
        {
            mi_GetTexturePropertyNames = typeof(Material).GetMethod("GetTexturePropertyNames", ArgumentUtility.EmptyTypes);
            MaterialWidgetSupported = mi_GetTexturePropertyNames != null;
        }

        internal static bool MaterialWidgetSupported { get; }
        static readonly MethodInfo mi_GetTexturePropertyNames;

        Material material;
        Texture2D activeTexture;
        readonly Dictionary<string, Texture> textures = new();
        readonly HashSet<Texture2D> texturesToDestroy = new();

        bool textureViewerWanted;
        ButtonRef toggleButton;

        GameObject textureViewerRoot;
        Dropdown textureDropdown;
        InputFieldRef savePathInput;
        Image image;
        LayoutElement imageLayout;

        public override void OnBorrowed(object target, Type targetType, ReflectionInspector inspector)
        {
            base.OnBorrowed(target, targetType, inspector);

            material = target.TryCast<Material>();

            if (material.mainTexture)
                SetActiveTexture(material.mainTexture);

            if (mi_GetTexturePropertyNames.Invoke(material, ArgumentUtility.EmptyArgs) is IEnumerable<string> propNames)
            {
                foreach (string property in propNames)
                {
                    if (material.GetTexture(property) is Texture texture)
                    {
                        if (texture.TryCast<Texture2D>() is null && texture.TryCast<Cubemap>() is null)
                            continue;

                        textures.Add(property, texture);

                        if (!activeTexture)
                            SetActiveTexture(texture);
                    }
                }
            }

            if (textureViewerRoot)
            {
                textureViewerRoot.transform.SetParent(inspector.UIRoot.transform);
                RefreshTextureDropdown();
            }

            InspectorPanel.Instance.Dragger.OnFinishResize += OnInspectorFinishResize;
        }

        void SetActiveTexture(Texture texture)
        {
            if (texture.TryCast<Texture2D>() is Texture2D tex2D)
                activeTexture = tex2D;
            else if (texture.TryCast<Cubemap>() is Cubemap cubemap)
            {
                activeTexture = TextureHelper.UnwrapCubemap(cubemap);
                texturesToDestroy.Add(activeTexture);
            }
        }

        public override void OnReturnToPool()
        {
            InspectorPanel.Instance.Dragger.OnFinishResize -= OnInspectorFinishResize;

            if (texturesToDestroy.Any())
            {
                foreach (Texture2D tex in texturesToDestroy)
                    UnityEngine.Object.Destroy(tex);
                texturesToDestroy.Clear();
            }

            material = null;
            activeTexture = null;
            textures.Clear();

            if (image.sprite)
                UnityEngine.Object.Destroy(image.sprite);

            if (textureViewerWanted)
                ToggleTextureViewer();

            if (textureViewerRoot)
                textureViewerRoot.transform.SetParent(Pool<Texture2DWidget>.Instance.InactiveHolder.transform);

            base.OnReturnToPool();
        }

        void ToggleTextureViewer()
        {
            if (textureViewerWanted)
            {
                // disable

                textureViewerWanted = false;
                textureViewerRoot.SetActive(false);
                toggleButton.ButtonText.text = "View Material";

                owner.ContentRoot.SetActive(true);
            }
            else
            {
                // enable

                if (!image.sprite)
                {
                    RefreshTextureViewer();
                    RefreshTextureDropdown();
                }

                SetImageSize();

                textureViewerWanted = true;
                textureViewerRoot.SetActive(true);
                toggleButton.ButtonText.text = "Hide Material";

                owner.ContentRoot.gameObject.SetActive(false);
            }
        }

        void RefreshTextureViewer()
        {
            if (!this.activeTexture)
            {
                ExplorerCore.LogWarning($"Material has no active textures!");
                savePathInput.Text = string.Empty;
                return;
            }

            if (image.sprite)
                UnityEngine.Object.Destroy(image.sprite);

            string name = activeTexture.name;
            if (string.IsNullOrEmpty(name))
                name = "untitled";
            savePathInput.Text = Path.Combine(ConfigManager.Default_Output_Path.Value, $"{name}.png");

            Sprite sprite = TextureHelper.CreateSprite(activeTexture);
            image.sprite = sprite;
        }

        void RefreshTextureDropdown()
        {
            if (!textureDropdown)
                return;

            textureDropdown.options.Clear();

            foreach (string key in textures.Keys)
                textureDropdown.options.Add(new(key));

            int i = 0;
            foreach (Texture value in textures.Values)
            {
                if (activeTexture.ReferenceEqual(value))
                {
                    textureDropdown.value = i;
                    break;
                }
                i++;
            }

            textureDropdown.RefreshShownValue();
        }

        void OnTextureDropdownChanged(int value)
        {
            Texture tex = textures.ElementAt(value).Value;
            if (activeTexture.ReferenceEqual(tex))
                return;
            SetActiveTexture(tex);
            RefreshTextureViewer();
        }

        void OnInspectorFinishResize()
        {
            SetImageSize();
        }

        void SetImageSize()
        {
            if (!imageLayout)
                return;

            RuntimeHelper.StartCoroutine(SetImageSizeCoro());
        }

        IEnumerator SetImageSizeCoro()
        {
            if (!activeTexture)
                yield break;

            // let unity rebuild layout etc
            yield return null;

            RectTransform imageRect = InspectorPanel.Instance.Rect;

            float rectWidth = imageRect.rect.width - 25;
            float rectHeight = imageRect.rect.height - 196;

            // If our image is smaller than the viewport, just use 100% scaling
            if (activeTexture.width < rectWidth && activeTexture.height < rectHeight)
            {
                imageLayout.minWidth = activeTexture.width;
                imageLayout.minHeight = activeTexture.height;
            }
            else // we will need to scale down the image to fit
            {
                // get the ratio of our viewport dimensions to width and height
                float viewWidthRatio = (float)((decimal)rectWidth / (decimal)activeTexture.width);
                float viewHeightRatio = (float)((decimal)rectHeight / (decimal)activeTexture.height);

                // if width needs to be scaled more than height
                if (viewWidthRatio < viewHeightRatio)
                {
                    imageLayout.minWidth = activeTexture.width * viewWidthRatio;
                    imageLayout.minHeight = activeTexture.height * viewWidthRatio;
                }
                else // if height needs to be scaled more than width
                {
                    imageLayout.minWidth = activeTexture.width * viewHeightRatio;
                    imageLayout.minHeight = activeTexture.height * viewHeightRatio;
                }
            }
        }

        void OnSaveTextureClicked()
        {
            if (!activeTexture)
            {
                ExplorerCore.LogWarning("Texture is null, maybe it was destroyed?");
                return;
            }

            if (string.IsNullOrEmpty(savePathInput.Text))
            {
                ExplorerCore.LogWarning("Save path cannot be empty!");
                return;
            }

            string path = savePathInput.Text;
            if (!path.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
                path += ".png";

            path = IOUtility.EnsureValidFilePath(path);

            if (File.Exists(path))
                File.Delete(path);

            TextureHelper.SaveTextureAsPNG(activeTexture, path);
        }

        public override GameObject CreateContent(GameObject uiRoot)
        {
            GameObject ret = base.CreateContent(uiRoot);

            // Button

            toggleButton = UIFactory.CreateButton(UIRoot, "MaterialButton", "View Material", new Color(0.2f, 0.3f, 0.2f));
            toggleButton.Transform.SetSiblingIndex(0);
            UIFactory.SetLayoutElement(toggleButton.Component.gameObject, minHeight: 25, minWidth: 150);
            toggleButton.OnClick += ToggleTextureViewer;

            // Texture viewer

            textureViewerRoot = UIFactory.CreateVerticalGroup(uiRoot, "MaterialViewer", false, false, true, true, 2, new Vector4(5, 5, 5, 5),
                new Color(0.1f, 0.1f, 0.1f), childAlignment: TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(textureViewerRoot, flexibleWidth: 9999, flexibleHeight: 9999);

            // Buttons holder

            GameObject dropdownRow = UIFactory.CreateHorizontalGroup(textureViewerRoot, "DropdownRow", false, true, true, true, 5, new(3, 3, 3, 3));
            UIFactory.SetLayoutElement(dropdownRow, minHeight: 30, flexibleWidth: 9999);

            Text dropdownLabel = UIFactory.CreateLabel(dropdownRow, "DropdownLabel", "Texture:");
            UIFactory.SetLayoutElement(dropdownLabel.gameObject, minWidth: 75, minHeight: 25);

            GameObject dropdownObj = UIFactory.CreateDropdown(dropdownRow, "TextureDropdown", out textureDropdown, "NOT SET", 13, OnTextureDropdownChanged);
            UIFactory.SetLayoutElement(dropdownObj, minWidth: 350, minHeight: 25);

            // Save helper

            GameObject saveRowObj = UIFactory.CreateHorizontalGroup(textureViewerRoot, "SaveRow", false, false, true, true, 2, new Vector4(2, 2, 2, 2),
                new Color(0.1f, 0.1f, 0.1f));

            ButtonRef saveBtn = UIFactory.CreateButton(saveRowObj, "SaveButton", "Save .PNG", new Color(0.2f, 0.25f, 0.2f));
            UIFactory.SetLayoutElement(saveBtn.Component.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 0);
            saveBtn.OnClick += OnSaveTextureClicked;

            savePathInput = UIFactory.CreateInputField(saveRowObj, "SaveInput", "...");
            UIFactory.SetLayoutElement(savePathInput.UIRoot, minHeight: 25, minWidth: 100, flexibleWidth: 9999);

            // Actual texture viewer

            GameObject imageViewport = UIFactory.CreateVerticalGroup(textureViewerRoot, "ImageViewport", false, false, true, true,
                bgColor: new(1, 1, 1, 0), childAlignment: TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(imageViewport, flexibleWidth: 9999, flexibleHeight: 9999);

            GameObject imageHolder = UIFactory.CreateUIObject("ImageHolder", imageViewport);
            imageLayout = UIFactory.SetLayoutElement(imageHolder, 1, 1, 0, 0);

            GameObject actualImageObj = UIFactory.CreateUIObject("ActualImage", imageHolder);
            RectTransform actualRect = actualImageObj.GetComponent<RectTransform>();
            actualRect.anchorMin = new(0, 0);
            actualRect.anchorMax = new(1, 1);
            image = actualImageObj.AddComponent<Image>();

            textureViewerRoot.SetActive(false);

            return ret;
        }
    }
}
