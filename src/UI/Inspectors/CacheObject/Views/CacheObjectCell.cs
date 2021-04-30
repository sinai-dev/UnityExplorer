using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Inspectors.IValues;
using UnityExplorer.UI.ObjectPool;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Inspectors.CacheObject.Views
{
    public abstract class CacheObjectCell : ICell
    {
        #region ICell

        public float DefaultHeight => 30f;

        public GameObject UIRoot => uiRoot;
        public GameObject uiRoot;

        public bool Enabled => m_enabled;
        private bool m_enabled;

        public RectTransform Rect => m_rect;
        private RectTransform m_rect;

        public void Disable()
        {
            m_enabled = false;
            uiRoot.SetActive(false);
        }

        public void Enable()
        {
            m_enabled = true;
            uiRoot.SetActive(true);
        }

        #endregion

        public CacheObjectBase Occupant { get; set; }
        public bool SubContentActive => SubContentHolder.activeSelf;

        public LayoutElement MemberLayout;
        public LayoutElement RightGroupLayout;

        public Text NameLabel;
        public Text TypeLabel;
        public Text ValueLabel;
        public Toggle Toggle;
        public Text ToggleText;
        public InputField InputField;

        public ButtonRef InspectButton;
        public ButtonRef SubContentButton;
        public ButtonRef ApplyButton;

        public GameObject SubContentHolder;

        public virtual void OnReturnToPool()
        {
            if (Occupant != null)
            {
                // TODO ?

                SubContentHolder.SetActive(false);

                Occupant = null;
            }
        }

        protected virtual void ApplyClicked()
        {
            Occupant.OnCellApplyClicked();
        }

        protected virtual void InspectClicked()
        {
            InspectorManager.Inspect(Occupant.Value);
        }

        protected virtual void ToggleClicked(bool value)
        {
            ToggleText.text = value.ToString();
        }

        protected virtual void SubContentClicked()
        {
            this.Occupant.OnCellSubContentToggle();
        }

        protected abstract void ConstructEvaluateHolder(GameObject parent);

        protected abstract void ConstructUpdateToggle(GameObject parent);

        // Todo could create these as needed maybe, just need to make sure the transform order is correct.

        public GameObject CreateContent(GameObject parent)
        {
            // Main layout

            uiRoot = UIFactory.CreateUIObject("CacheMemberCell", parent, new Vector2(100, 30));
            m_rect = uiRoot.GetComponent<RectTransform>();
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(uiRoot, true, false, true, true, 2, 0);
            UIFactory.SetLayoutElement(uiRoot, minWidth: 100, flexibleWidth: 9999, minHeight: 30, flexibleHeight: 600);
            UIRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var separator = UIFactory.CreateUIObject("TopSeperator", uiRoot);
            UIFactory.SetLayoutElement(separator, minHeight: 1, flexibleHeight: 0, flexibleWidth: 9999);
            separator.AddComponent<Image>().color = Color.black;

            var horiRow = UIFactory.CreateUIObject("HoriGroup", uiRoot);
            UIFactory.SetLayoutElement(horiRow, minHeight: 29, flexibleHeight: 150, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(horiRow, false, false, true, true, 5, 2, childAlignment: TextAnchor.UpperLeft);
            horiRow.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Left member label

            NameLabel = UIFactory.CreateLabel(horiRow, "MemberLabel", "<notset>", TextAnchor.MiddleLeft);
            NameLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(NameLabel.gameObject, minHeight: 25, minWidth: 20, flexibleHeight: 300, flexibleWidth: 0);
            MemberLayout = NameLabel.GetComponent<LayoutElement>();

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

            SubContentButton = UIFactory.CreateButton(rightHoriGroup, "SubContentButton", "▲");
            UIFactory.SetLayoutElement(SubContentButton.Button.gameObject, minWidth: 25, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
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

            var inputObj = UIFactory.CreateInputField(rightHoriGroup, "InputField", "...", out InputField);
            UIFactory.SetLayoutElement(inputObj, minWidth: 150, flexibleWidth: 0, minHeight: 25, flexibleHeight: 0);

            // Inspect and apply buttons

            InspectButton = UIFactory.CreateButton(rightHoriGroup, "InspectButton", "Inspect", new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(InspectButton.Button.gameObject, minWidth: 60, flexibleWidth: 0, minHeight: 25);
            InspectButton.OnClick += InspectClicked;

            ApplyButton = UIFactory.CreateButton(rightHoriGroup, "ApplyButton", "Apply", new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(ApplyButton.Button.gameObject, minWidth: 70, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            ApplyButton.OnClick += ApplyClicked;

            // Main value label

            ValueLabel = UIFactory.CreateLabel(rightHoriGroup, "ValueLabel", "Value goes here", TextAnchor.MiddleLeft);
            ValueLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(ValueLabel.gameObject, minHeight: 25, flexibleHeight: 150, flexibleWidth: 9999);

            ConstructUpdateToggle(rightHoriGroup);

            // Subcontent (todo?)

            SubContentHolder = UIFactory.CreateUIObject("SubContent", uiRoot);
            UIFactory.SetLayoutElement(SubContentHolder.gameObject, minHeight: 30, flexibleHeight: 500, minWidth: 100, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(SubContentHolder, true, false, true, true, 2, childAlignment: TextAnchor.UpperLeft);
            
            SubContentHolder.SetActive(false);

            return uiRoot;
        }
    }
}
