using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Inspectors;
using UnityExplorer.UI.IValues;
using UnityExplorer.UI.ObjectPool;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.CacheObject.Views
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
        public LayoutElement RightGroupLayout;

        public Text NameLabel;
        public Text TypeLabel;
        public Text ValueLabel;
        public Toggle Toggle;
        public Text ToggleText;
        public InputFieldRef InputField;

        public ButtonRef InspectButton;
        public ButtonRef SubContentButton;
        public ButtonRef ApplyButton;

        public GameObject SubContentHolder;

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

        private readonly Color subInactiveColor = new Color(0.23f, 0.23f, 0.23f);
        private readonly Color subActiveColor = new Color(0.23f, 0.33f, 0.23f);

        public void RefreshSubcontentButton()
        {
            if (!this.SubContentHolder.activeSelf)
            {
                this.SubContentButton.ButtonText.text = "▲";
                RuntimeProvider.Instance.SetColorBlock(SubContentButton.Component, subInactiveColor, subInactiveColor * 1.3f);
            }
            else
            {
                this.SubContentButton.ButtonText.text = "▼";
                RuntimeProvider.Instance.SetColorBlock(SubContentButton.Component, subActiveColor, subActiveColor * 1.3f);
            }
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

            // Left member label

            NameLabel = UIFactory.CreateLabel(horiRow, "MemberLabel", "<notset>", TextAnchor.MiddleLeft);
            NameLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(NameLabel.gameObject, minHeight: 25, minWidth: 20, flexibleHeight: 300, flexibleWidth: 0);
            NameLayout = NameLabel.GetComponent<LayoutElement>();

            // Right vertical group

            var rightGroupHolder = UIFactory.CreateUIObject("RightGroup", horiRow);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(rightGroupHolder, false, false, true, true, 4, childAlignment: TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(rightGroupHolder, minHeight: 25, minWidth: 200, flexibleWidth: 9999, flexibleHeight: 800);
            RightGroupLayout = rightGroupHolder.GetComponent<LayoutElement>();

            ConstructEvaluateHolder(rightGroupHolder);

            // Right horizontal group

            var rightHoriGroup = UIFactory.CreateUIObject("RightHoriGroup", rightGroupHolder);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(rightHoriGroup, false, false, true, true, 4, childAlignment: TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(rightHoriGroup, minHeight: 25, minWidth: 200, flexibleWidth: 9999, flexibleHeight: 800);

            SubContentButton = UIFactory.CreateButton(rightHoriGroup, "SubContentButton", "▲", subInactiveColor);
            UIFactory.SetLayoutElement(SubContentButton.Component.gameObject, minWidth: 25, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            SubContentButton.OnClick += SubContentClicked;

            // Type label

            TypeLabel = UIFactory.CreateLabel(rightHoriGroup, "ReturnLabel", "<notset>", TextAnchor.MiddleLeft);
            TypeLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(TypeLabel.gameObject, minHeight: 25, flexibleHeight: 150, minWidth: 60, flexibleWidth: 0);

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
