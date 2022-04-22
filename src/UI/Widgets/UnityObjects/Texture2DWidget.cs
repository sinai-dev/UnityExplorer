using System;
using System.Collections;
using System.IO;
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
    public class Texture2DWidget : UnityObjectWidget
    {
        private Texture2D TextureRef;
        private float realWidth;
        private float realHeight;

        private bool textureViewerWanted;
        private ButtonRef toggleButton;

        private GameObject textureViewerRoot;
        private InputFieldRef savePathInput;
        private Image image;
        private LayoutElement imageLayout;

        public override void OnBorrowed(object target, Type targetType, ReflectionInspector inspector)
        {
            base.OnBorrowed(target, targetType, inspector);

            TextureRef = target.TryCast<Texture2D>();

            realWidth = TextureRef.width;
            realHeight = TextureRef.height;

            if (this.textureViewerRoot)
                this.textureViewerRoot.transform.SetParent(inspector.UIRoot.transform);

            InspectorPanel.Instance.Dragger.OnFinishResize += OnInspectorFinishResize;
        }

        public override void OnReturnToPool()
        {
            InspectorPanel.Instance.Dragger.OnFinishResize -= OnInspectorFinishResize;

            TextureRef = null;

            if (image.sprite)
                GameObject.Destroy(image.sprite);

            if (textureViewerWanted)
                ToggleTextureViewer();

            if (this.textureViewerRoot)
                this.textureViewerRoot.transform.SetParent(Pool<Texture2DWidget>.Instance.InactiveHolder.transform);

            base.OnReturnToPool();
        }

        private void ToggleTextureViewer()
        {
            if (textureViewerWanted)
            {
                // disable
                textureViewerWanted = false;
                textureViewerRoot.SetActive(false);
                toggleButton.ButtonText.text = "View Texture";

                ParentInspector.ContentRoot.SetActive(true);
            }
            else
            {
                // enable
                if (!image.sprite)
                    SetupTextureViewer();

                SetImageSize();

                textureViewerWanted = true;
                textureViewerRoot.SetActive(true);
                toggleButton.ButtonText.text = "Hide Texture";

                ParentInspector.ContentRoot.gameObject.SetActive(false);
            }
        }

        private void SetupTextureViewer()
        {
            if (!this.TextureRef)
                return;

            string name = TextureRef.name;
            if (string.IsNullOrEmpty(name))
                name = "untitled";
            savePathInput.Text = Path.Combine(ConfigManager.Default_Output_Path.Value, $"{name}.png");

            Sprite sprite = TextureHelper.CreateSprite(TextureRef);
            image.sprite = sprite;
        }

        private void OnInspectorFinishResize()
        {
            SetImageSize();
        }

        private void SetImageSize()
        {
            if (!imageLayout)
                return;

            RuntimeHelper.StartCoroutine(SetImageSizeCoro());
        }

        IEnumerator SetImageSizeCoro()
        {
            // let unity rebuild layout etc
            yield return null;

            RectTransform imageRect = InspectorPanel.Instance.Rect;

            float rectWidth = imageRect.rect.width - 25;
            float rectHeight = imageRect.rect.height - 196;

            // If our image is smaller than the viewport, just use 100% scaling
            if (realWidth < rectWidth && realHeight < rectHeight)
            {
                imageLayout.minWidth = realWidth;
                imageLayout.minHeight = realHeight;
            }
            else // we will need to scale down the image to fit
            {
                // get the ratio of our viewport dimensions to width and height
                float viewWidthRatio = (float)((decimal)rectWidth / (decimal)realWidth);
                float viewHeightRatio = (float)((decimal)rectHeight / (decimal)realHeight);

                // if width needs to be scaled more than height
                if (viewWidthRatio < viewHeightRatio)
                {
                    imageLayout.minWidth = realWidth * viewWidthRatio;
                    imageLayout.minHeight = realHeight * viewWidthRatio;
                }
                else // if height needs to be scaled more than width
                {
                    imageLayout.minWidth = realWidth * viewHeightRatio;
                    imageLayout.minHeight = realHeight * viewHeightRatio;
                }
            }
        }

        private void OnSaveTextureClicked()
        {
            if (!TextureRef)
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

            Texture2D tex = TextureRef;
            if (!TextureHelper.IsReadable(tex))
                tex = TextureHelper.ForceReadTexture(tex);

            byte[] data = TextureHelper.EncodeToPNG(tex);
            File.WriteAllBytes(path, data);

            if (tex != TextureRef)
            {
                // cleanup temp texture if we had to force-read it.
                GameObject.Destroy(tex);
            }
        }

        public override GameObject CreateContent(GameObject uiRoot)
        {
            GameObject ret = base.CreateContent(uiRoot);

            // Button

            toggleButton = UIFactory.CreateButton(UIRoot, "TextureButton", "View Texture", new Color(0.2f, 0.3f, 0.2f));
            toggleButton.Transform.SetSiblingIndex(0);
            UIFactory.SetLayoutElement(toggleButton.Component.gameObject, minHeight: 25, minWidth: 150);
            toggleButton.OnClick += ToggleTextureViewer;

            // Texture viewer

            textureViewerRoot = UIFactory.CreateVerticalGroup(uiRoot, "TextureViewer", false, false, true, true, 2, new Vector4(5, 5, 5, 5),
                new Color(0.1f, 0.1f, 0.1f), childAlignment: TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(textureViewerRoot, flexibleWidth: 9999, flexibleHeight: 9999);

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
