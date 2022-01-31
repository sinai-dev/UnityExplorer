using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Panels;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets.ScrollView;
using UniverseLib.Utility;

namespace UnityExplorer.CacheObject.Views
{
    public abstract class CacheObjectCell : ICell
    {
        #region ICell

        public float DefaultHeight => 30f;

        public GameObject UIRoot { get; set; }

        public bool Enabled => m_enabled;
        private bool m_enabled;

        public RectTransform Rect { get; set; }

        public void Disable()
        {
            m_enabled = false;
            UIRoot.SetActive(false);
        }

        public void Enable()
        {
            m_enabled = true;
            UIRoot.SetActive(true);
        }

        #endregion

        public CacheObjectBase Occupant { get; set; }
        public bool SubContentActive => SubContentHolder.activeSelf;

        public LayoutElement NameLayout;
        public GameObject RightGroupContent;
        public LayoutElement RightGroupLayout;
        public GameObject SubContentHolder;

        public Text NameLabel;
        public InputFieldRef HiddenNameLabel; // for selecting the name label
        public Text TypeLabel;
        public Text ValueLabel;
        public Toggle Toggle;
        public Text ToggleText;
        public InputFieldRef InputField;

        public ButtonRef InspectButton;
        public ButtonRef SubContentButton;
        public ButtonRef ApplyButton;

        public ButtonRef CopyButton;
        public ButtonRef PasteButton;

        public readonly Color subInactiveColor = new(0.23f, 0.23f, 0.23f);
        public readonly Color subActiveColor = new(0.23f, 0.33f, 0.23f);

        protected virtual void ApplyClicked()
        {
            Occupant.OnCellApplyClicked();
        }

        protected virtual void InspectClicked()
        {
            InspectorManager.Inspect(Occupant.Value, this.Occupant);
        }

        protected virtual void ToggleClicked(bool value)
        {
            ToggleText.text = value.ToString();
        }

        protected virtual void SubContentClicked()
        {
            this.Occupant.OnCellSubContentToggle();
        }

        protected virtual void OnCopyClicked()
        {
            ClipboardPanel.Copy(this.Occupant.Value);
        }

        protected virtual void OnPasteClicked()
        {
            if (ClipboardPanel.TryPaste(this.Occupant.FallbackType, out object paste))
                this.Occupant.SetUserValue(paste);
        }

        public void RefreshSubcontentButton()
        {
            this.SubContentButton.ButtonText.text = SubContentHolder.activeSelf ? "▼" : "▲";
            Color color = SubContentHolder.activeSelf ? subActiveColor : subInactiveColor;
            RuntimeHelper.SetColorBlock(SubContentButton.Component, color, color * 1.3f);
        }

        protected abstract void ConstructEvaluateHolder(GameObject parent);

        public virtual GameObject CreateContent(GameObject parent)
        {
            // Main layout

            UIRoot = UIFactory.CreateUIObject(this.GetType().Name, parent, new Vector2(100, 30));
            Rect = UIRoot.GetComponent<RectTransform>();
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(UIRoot, false, false, true, true, childAlignment: TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(UIRoot, minWidth: 100, flexibleWidth: 9999, minHeight: 30, flexibleHeight: 600);
            UIRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var horiRow = UIFactory.CreateUIObject("HoriGroup", UIRoot);
            UIFactory.SetLayoutElement(horiRow, minHeight: 29, flexibleHeight: 150, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(horiRow, false, false, true, true, 5, 2, childAlignment: TextAnchor.UpperLeft);
            horiRow.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Left name label

            NameLabel = UIFactory.CreateLabel(horiRow, "NameLabel", "<notset>", TextAnchor.MiddleLeft);
            NameLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            NameLayout = UIFactory.SetLayoutElement(NameLabel.gameObject, minHeight: 25, minWidth: 20, flexibleHeight: 300, flexibleWidth: 0);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(NameLabel.gameObject, true, true, true, true);

            HiddenNameLabel = UIFactory.CreateInputField(NameLabel.gameObject, "HiddenNameLabel", "");
            var hiddenRect = HiddenNameLabel.Component.GetComponent<RectTransform>();
            hiddenRect.anchorMin = Vector2.zero;
            hiddenRect.anchorMax = Vector2.one;
            HiddenNameLabel.Component.readOnly = true;
            HiddenNameLabel.Component.lineType = UnityEngine.UI.InputField.LineType.MultiLineNewline;
            HiddenNameLabel.Component.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            HiddenNameLabel.Component.gameObject.GetComponent<Image>().color = Color.clear;
            HiddenNameLabel.Component.textComponent.color = Color.clear;
            UIFactory.SetLayoutElement(HiddenNameLabel.Component.gameObject, minHeight: 25, minWidth: 20, flexibleHeight: 300, flexibleWidth: 0);

            // Right vertical group

            RightGroupContent = UIFactory.CreateUIObject("RightGroup", horiRow);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(RightGroupContent, false, false, true, true, 4, childAlignment: TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(RightGroupContent, minHeight: 25, minWidth: 200, flexibleWidth: 9999, flexibleHeight: 800);
            RightGroupLayout = RightGroupContent.GetComponent<LayoutElement>();

            ConstructEvaluateHolder(RightGroupContent);

            // Right horizontal group

            var rightHoriGroup = UIFactory.CreateUIObject("RightHoriGroup", RightGroupContent);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(rightHoriGroup, false, false, true, true, 4, childAlignment: TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(rightHoriGroup, minHeight: 25, minWidth: 200, flexibleWidth: 9999, flexibleHeight: 800);

            SubContentButton = UIFactory.CreateButton(rightHoriGroup, "SubContentButton", "▲", subInactiveColor);
            UIFactory.SetLayoutElement(SubContentButton.Component.gameObject, minWidth: 25, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            SubContentButton.OnClick += SubContentClicked;

            // Type label

            TypeLabel = UIFactory.CreateLabel(rightHoriGroup, "ReturnLabel", "<notset>", TextAnchor.MiddleLeft);
            TypeLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(TypeLabel.gameObject, minHeight: 25, flexibleHeight: 150, minWidth: 45, flexibleWidth: 0);

            // Bool and number value interaction

            var toggleObj = UIFactory.CreateToggle(rightHoriGroup, "Toggle", out Toggle, out ToggleText);
            UIFactory.SetLayoutElement(toggleObj, minWidth: 70, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            ToggleText.color = SignatureHighlighter.KeywordBlue;
            Toggle.onValueChanged.AddListener(ToggleClicked);

            InputField = UIFactory.CreateInputField(rightHoriGroup, "InputField", "...");
            UIFactory.SetLayoutElement(InputField.UIRoot, minWidth: 150, flexibleWidth: 0, minHeight: 25, flexibleHeight: 0);

            // Apply

            ApplyButton = UIFactory.CreateButton(rightHoriGroup, "ApplyButton", "Apply", new Color(0.15f, 0.19f, 0.15f));
            UIFactory.SetLayoutElement(ApplyButton.Component.gameObject, minWidth: 70, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            ApplyButton.OnClick += ApplyClicked;

            // Inspect 

            InspectButton = UIFactory.CreateButton(rightHoriGroup, "InspectButton", "Inspect", new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(InspectButton.Component.gameObject, minWidth: 70, flexibleWidth: 0, minHeight: 25);
            InspectButton.OnClick += InspectClicked;

            // Main value label

            ValueLabel = UIFactory.CreateLabel(rightHoriGroup, "ValueLabel", "Value goes here", TextAnchor.MiddleLeft);
            ValueLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(ValueLabel.gameObject, minHeight: 25, flexibleHeight: 150, flexibleWidth: 9999);

            // Copy and Paste buttons

            var buttonHolder = UIFactory.CreateHorizontalGroup(rightHoriGroup, "CopyPasteButtons", false, false, true, true, 4, 
                bgColor: new(1,1,1,0), childAlignment: TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(buttonHolder, minWidth: 60, flexibleWidth: 0);

            CopyButton = UIFactory.CreateButton(buttonHolder, "CopyButton", "Copy", new Color(0.13f, 0.13f, 0.13f, 1f));
            UIFactory.SetLayoutElement(CopyButton.Component.gameObject, minHeight: 25, minWidth: 28, flexibleWidth: 0);
            CopyButton.ButtonText.color = Color.yellow;
            CopyButton.ButtonText.fontSize = 10;
            CopyButton.OnClick += OnCopyClicked;

            PasteButton = UIFactory.CreateButton(buttonHolder, "PasteButton", "Paste", new Color(0.13f, 0.13f, 0.13f, 1f));
            UIFactory.SetLayoutElement(PasteButton.Component.gameObject, minHeight: 25, minWidth: 28, flexibleWidth: 0);
            PasteButton.ButtonText.color = Color.green;
            PasteButton.ButtonText.fontSize = 10;
            PasteButton.OnClick += OnPasteClicked;

            // Subcontent

            SubContentHolder = UIFactory.CreateUIObject("SubContent", UIRoot);
            UIFactory.SetLayoutElement(SubContentHolder.gameObject, minHeight: 30, flexibleHeight: 600, minWidth: 100, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(SubContentHolder, true, true, true, true, 2, childAlignment: TextAnchor.UpperLeft);
            //SubContentHolder.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.MinSize;
            SubContentHolder.SetActive(false);

            // Bottom separator
            var separator = UIFactory.CreateUIObject("BottomSeperator", UIRoot);
            UIFactory.SetLayoutElement(separator, minHeight: 1, flexibleHeight: 0, flexibleWidth: 9999);
            separator.AddComponent<Image>().color = Color.black;

            return UIRoot;
        }
    }
}
