using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace UnityExplorer.UI.CacheObject.Views
{
    public class ConfigEntryCell : CacheObjectCell
    {
        public override GameObject CreateContent(GameObject parent)
        {
            // Main layout

            UIRoot = UIFactory.CreateUIObject(this.GetType().Name, parent, new Vector2(100, 30));
            Rect = UIRoot.GetComponent<RectTransform>();
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(UIRoot, false, false, true, true, 4, 4, 4, 4, 4, childAlignment: TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(UIRoot, minWidth: 100, flexibleWidth: 9999, minHeight: 30, flexibleHeight: 600);
            UIRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Left label

            NameLabel = UIFactory.CreateLabel(UIRoot, "NameLabel", "<notset>", TextAnchor.MiddleLeft);
            NameLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(NameLabel.gameObject, minHeight: 25, flexibleWidth: 9999, flexibleHeight: 300);
            NameLayout = NameLabel.GetComponent<LayoutElement>();

            // horizontal group

            var horiGroup = UIFactory.CreateUIObject("RightHoriGroup", UIRoot);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(horiGroup, false, false, true, true, 4, childAlignment: TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(horiGroup, minHeight: 25, minWidth: 200, flexibleWidth: 9999, flexibleHeight: 800);

            SubContentButton = UIFactory.CreateButton(horiGroup, "SubContentButton", "▲", subInactiveColor);
            UIFactory.SetLayoutElement(SubContentButton.Component.gameObject, minWidth: 25, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            SubContentButton.OnClick += SubContentClicked;

            // Type label

            TypeLabel = UIFactory.CreateLabel(horiGroup, "TypeLabel", "<notset>", TextAnchor.MiddleLeft);
            TypeLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(TypeLabel.gameObject, minHeight: 25, flexibleHeight: 150, minWidth: 60, flexibleWidth: 0);

            // Bool and number value interaction

            var toggleObj = UIFactory.CreateToggle(horiGroup, "Toggle", out Toggle, out ToggleText);
            UIFactory.SetLayoutElement(toggleObj, minWidth: 70, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            ToggleText.color = SignatureHighlighter.KeywordBlue;
            Toggle.onValueChanged.AddListener(ToggleClicked);

            InputField = UIFactory.CreateInputField(horiGroup, "InputField", "...");
            UIFactory.SetLayoutElement(InputField.UIRoot, minWidth: 150, flexibleWidth: 0, minHeight: 25, flexibleHeight: 0);

            // Apply

            ApplyButton = UIFactory.CreateButton(horiGroup, "ApplyButton", "Apply", new Color(0.15f, 0.19f, 0.15f));
            UIFactory.SetLayoutElement(ApplyButton.Component.gameObject, minWidth: 70, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            ApplyButton.OnClick += ApplyClicked;

            // Main value label

            ValueLabel = UIFactory.CreateLabel(horiGroup, "ValueLabel", "Value goes here", TextAnchor.MiddleLeft);
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

        protected override void ConstructEvaluateHolder(GameObject parent) { }
    }
}
