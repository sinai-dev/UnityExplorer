using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Inspectors.CacheObject.Views
{
    // Todo add C# events for the unity UI listeners

    public class CacheMemberCell : ICell
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

        public ReflectionInspector CurrentOwner { get; set; }
        public CacheMember CurrentOccupant { get; set; }

        public Action<CacheMember> OnApplyClicked;
        public Action<CacheMember> OnInspectClicked;
        public Action<CacheMember> OnSubContentClicked;
        public Action<CacheMember> OnUpdateClicked;
        public Action<CacheMember> OnEvaluateClicked;

        public LayoutElement MemberLayout;
        public LayoutElement RightGroupLayout;

        public Text MemberLabel;

        //public GameObject RightGroupHolder;
        public Text TypeLabel;
        public Text ValueLabel;
        public Toggle Toggle;
        public Text ToggleText;
        public InputField InputField;

        public GameObject EvaluateHolder;
        public ButtonRef EvaluateButton;

        public ButtonRef InspectButton;
        public ButtonRef SubContentButton;
        public ButtonRef ApplyButton;
        public ButtonRef UpdateButton;

        public GameObject SubContentHolder;

        public void OnReturnToPool()
        {
            // remove listeners
            OnApplyClicked = null;
            OnInspectClicked = null;
            OnSubContentClicked = null;
            OnUpdateClicked = null;
            OnEvaluateClicked = null;

            CurrentOwner = null;
        }

        private void ApplyClicked()
        {
            OnApplyClicked?.Invoke(CurrentOccupant);
        }

        private void InspectClicked()
        {
            OnInspectClicked?.Invoke(CurrentOccupant);
        }

        private void SubContentClicked()
        {
            OnSubContentClicked?.Invoke(CurrentOccupant);
        }

        private void UpdateClicked()
        {
            OnUpdateClicked?.Invoke(CurrentOccupant);
        }

        private void EvaluateClicked()
        {
            OnEvaluateClicked?.Invoke(CurrentOccupant);
        }

        private void ToggleClicked(bool value)
        {
            ToggleText.text = value.ToString();
        }

        public GameObject CreateContent(GameObject parent)
        {
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

            MemberLabel = UIFactory.CreateLabel(horiRow, "MemberLabel", "<notset>", TextAnchor.MiddleLeft);
            MemberLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(MemberLabel.gameObject, minHeight: 25, minWidth: 20, flexibleHeight: 300, flexibleWidth: 0);
            MemberLayout = MemberLabel.GetComponent<LayoutElement>();

            var rightGroupHolder = UIFactory.CreateUIObject("RightGroup", horiRow);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(rightGroupHolder, false, false, true, true, 4, childAlignment: TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(rightGroupHolder, minHeight: 25, minWidth: 200, flexibleWidth: 9999, flexibleHeight: 800);
            RightGroupLayout = rightGroupHolder.GetComponent<LayoutElement>();

            EvaluateHolder = UIFactory.CreateUIObject("EvalGroup", rightGroupHolder);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(EvaluateHolder, false, false, true, true, 3);
            UIFactory.SetLayoutElement(EvaluateHolder, minHeight: 25, flexibleWidth: 9999, flexibleHeight: 775);

            EvaluateButton = UIFactory.CreateButton(EvaluateHolder, "EvaluateButton", "Evaluate", new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(EvaluateButton.Button.gameObject, minWidth: 100, minHeight: 25);
            EvaluateButton.OnClick += EvaluateClicked;

            var rightHoriGroup = UIFactory.CreateUIObject("RightHoriGroup", rightGroupHolder);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(rightHoriGroup, false, false, true, true, 4, childAlignment: TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(rightHoriGroup, minHeight: 25, minWidth: 200, flexibleWidth: 9999, flexibleHeight: 800);

            SubContentButton = UIFactory.CreateButton(rightHoriGroup, "SubContentButton", "▲");
            UIFactory.SetLayoutElement(SubContentButton.Button.gameObject, minWidth: 25, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            SubContentButton.OnClick += SubContentClicked;

            TypeLabel = UIFactory.CreateLabel(rightHoriGroup, "ReturnLabel", "<notset>", TextAnchor.MiddleLeft);
            TypeLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(TypeLabel.gameObject, minHeight: 25, flexibleHeight: 150, minWidth: 70, flexibleWidth: 0);
            //ReturnTypeLayout = TypeLabel.GetComponent<LayoutElement>();

            var toggleObj = UIFactory.CreateToggle(rightHoriGroup, "Toggle", out Toggle, out ToggleText);
            UIFactory.SetLayoutElement(toggleObj, minWidth: 70, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            ToggleText.color = SignatureHighlighter.KeywordBlue;
            Toggle.onValueChanged.AddListener(ToggleClicked);

            var inputObj = UIFactory.CreateInputField(rightHoriGroup, "InputField", "...", out InputField);
            UIFactory.SetLayoutElement(inputObj, minWidth: 150, flexibleWidth: 0, minHeight: 25, flexibleHeight: 0);

            InspectButton = UIFactory.CreateButton(rightHoriGroup, "InspectButton", "Inspect", new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(InspectButton.Button.gameObject, minWidth: 60, flexibleWidth: 0, minHeight: 25);
            InspectButton.OnClick += InspectClicked;

            ApplyButton = UIFactory.CreateButton(rightHoriGroup, "ApplyButton", "Apply", new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(ApplyButton.Button.gameObject, minWidth: 70, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            ApplyButton.OnClick += ApplyClicked;

            ValueLabel = UIFactory.CreateLabel(rightHoriGroup, "ValueLabel", "Value goes here", TextAnchor.MiddleLeft);
            ValueLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(ValueLabel.gameObject, minHeight: 25, flexibleHeight: 150, flexibleWidth: 9999);

            UpdateButton = UIFactory.CreateButton(rightHoriGroup, "UpdateButton", "Update", new Color(0.15f, 0.2f, 0.15f));
            UIFactory.SetLayoutElement(UpdateButton.Button.gameObject, minWidth: 65, flexibleWidth: 0, minHeight: 25, flexibleHeight: 0);
            UpdateButton.OnClick += UpdateClicked;

            // Subcontent (todo?)

            SubContentHolder = UIFactory.CreateUIObject("SubContent", uiRoot);
            UIFactory.SetLayoutElement(SubContentHolder.gameObject, minHeight: 30, flexibleHeight: 500, minWidth: 100, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(SubContentHolder, true, false, true, true, 2, childAlignment: TextAnchor.UpperLeft);
            
            SubContentHolder.SetActive(false);

            return uiRoot;
        }
    }
}
