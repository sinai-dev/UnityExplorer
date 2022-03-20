using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public class Texture2DWidget : UnityObjectWidget
    {
        private Texture2D TextureRef;

        private bool textureViewerWanted;

        private InputFieldRef textureSavePathInput;
        private Image textureImage;
        private LayoutElement textureImageLayout;

        private ButtonRef textureButton;
        private GameObject textureViewer;

        private float realWidth;
        private float realHeight;

        public override void OnBorrowed(object target, Type targetType, ReflectionInspector inspector)
        {
            base.OnBorrowed(target, targetType, inspector);

            TextureRef = (Texture2D)target.TryCast(typeof(Texture2D));
            textureButton.Component.gameObject.SetActive(true);

            realWidth = TextureRef.width;
            realHeight = TextureRef.height;

            if (this.textureViewer)
                this.textureViewer.transform.SetParent(inspector.UIRoot.transform);

            InspectorPanel.Instance.Dragger.OnFinishResize += OnInspectorFinishResize;
        }

        public override void OnReturnToPool()
        {
            InspectorPanel.Instance.Dragger.OnFinishResize -= OnInspectorFinishResize;

            TextureRef = null;

            if (textureImage.sprite)
                GameObject.Destroy(textureImage.sprite);

            if (textureViewerWanted)
                ToggleTextureViewer();

            this.textureViewer.transform.SetParent(Pool<Texture2DWidget>.Instance.InactiveHolder.transform);

            base.OnReturnToPool();
        }

        private void ToggleTextureViewer()
        {
            if (textureViewerWanted)
            {
                // disable
                textureViewerWanted = false;
                textureViewer.SetActive(false);
                textureButton.ButtonText.text = "View Texture";

                ParentInspector.mainContentHolder.SetActive(true);
            }
            else
            {
                // enable
                if (!textureImage.sprite)
                    SetupTextureViewer();

                SetImageSize();

                textureViewerWanted = true;
                textureViewer.SetActive(true);
                textureButton.ButtonText.text = "Hide Texture";

                ParentInspector.mainContentHolder.gameObject.SetActive(false);
            }
        }

        private void SetupTextureViewer()
        {
            if (!this.TextureRef)
                return;

            string name = TextureRef.name;
            if (string.IsNullOrEmpty(name))
                name = "untitled";

            textureSavePathInput.Text = Path.Combine(ConfigManager.Default_Output_Path.Value, $"{name}.png");

            Sprite sprite = TextureHelper.CreateSprite(TextureRef);
            textureImage.sprite = sprite;
        }

        private void OnInspectorFinishResize(RectTransform _)
        {
            SetImageSize();
        }

        private void SetImageSize()
        {
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
                textureImageLayout.minWidth = realWidth;
                textureImageLayout.minHeight = realHeight;
            }
            else // we will need to scale down the image to fit
            {
                // get the ratio of our viewport dimensions to width and height
                float viewWidthRatio = (float)((decimal)rectWidth / (decimal)realWidth);
                float viewHeightRatio = (float)((decimal)rectHeight / (decimal)realHeight);

                // if width needs to be scaled more than height
                if (viewWidthRatio < viewHeightRatio)
                {
                    textureImageLayout.minWidth = realWidth * viewWidthRatio;
                    textureImageLayout.minHeight = realHeight * viewWidthRatio;
                }
                else // if height needs to be scaled more than width
                {
                    textureImageLayout.minWidth = realWidth * viewHeightRatio;
                    textureImageLayout.minHeight = realHeight * viewHeightRatio;
                }
            }
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

            string path = textureSavePathInput.Text;
            if (!path.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
            {
                ExplorerCore.LogWarning("Desired save path must end with '.png'!");
                return;
            }

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

            textureButton = UIFactory.CreateButton(unityObjectRow, "TextureButton", "View Texture", new Color(0.2f, 0.3f, 0.2f));
            textureButton.Transform.SetSiblingIndex(0);
            UIFactory.SetLayoutElement(textureButton.Component.gameObject, minHeight: 25, minWidth: 150);
            textureButton.OnClick += ToggleTextureViewer;

            // Texture viewer

            textureViewer = UIFactory.CreateVerticalGroup(uiRoot, "TextureViewer", false, false, true, true, 2, new Vector4(5, 5, 5, 5),
                new Color(0.1f, 0.1f, 0.1f), childAlignment: TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(textureViewer, flexibleWidth: 9999, flexibleHeight: 9999);

            // Save helper

            GameObject saveRowObj = UIFactory.CreateHorizontalGroup(textureViewer, "SaveRow", false, false, true, true, 2, new Vector4(2, 2, 2, 2),
                new Color(0.1f, 0.1f, 0.1f));

            ButtonRef saveBtn = UIFactory.CreateButton(saveRowObj, "SaveButton", "Save .PNG", new Color(0.2f, 0.25f, 0.2f));
            UIFactory.SetLayoutElement(saveBtn.Component.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 0);
            saveBtn.OnClick += OnSaveTextureClicked;

            textureSavePathInput = UIFactory.CreateInputField(saveRowObj, "SaveInput", "...");
            UIFactory.SetLayoutElement(textureSavePathInput.UIRoot, minHeight: 25, minWidth: 100, flexibleWidth: 9999);

            // Actual texture viewer

            //GameObject imageViewport = UIFactory.CreateVerticalGroup(textureViewer, "ImageViewport", false, false, true, true);
            //imageRect = imageViewport.GetComponent<RectTransform>();
            //UIFactory.SetLayoutElement(imageViewport, flexibleWidth: 9999, flexibleHeight: 9999);

            GameObject imageHolder = UIFactory.CreateUIObject("ImageHolder", textureViewer);
            textureImageLayout = UIFactory.SetLayoutElement(imageHolder, 1, 1, 0, 0);
            imageHolder.AddComponent<Image>().color = Color.clear;
            var outline = imageHolder.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new(2, 2);

            var actualImageObj = UIFactory.CreateUIObject("ActualImage", imageHolder);
            var actualRect = actualImageObj.GetComponent<RectTransform>();
            actualRect.anchorMin = new(0, 0);
            actualRect.anchorMax = new(1, 1);
            textureImage = actualImageObj.AddComponent<Image>();

            textureViewer.SetActive(false);

            return ret;
        }
    }
}
