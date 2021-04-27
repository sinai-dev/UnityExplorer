using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Inspectors.CacheObject.Views
{
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
        public int CurrentDataIndex { get; set; }

        public LayoutElement MemberLayout;
        public LayoutElement ReturnTypeLayout;
        public LayoutElement RightGroupLayout;

        public Text MemberLabel;
        public Text TypeLabel;

        public GameObject RightGroupHolder;
        public ButtonRef InspectButton;
        public Text ValueLabel;

        public GameObject SubContentHolder;

        public GameObject CreateContent(GameObject parent)
        {
            uiRoot = UIFactory.CreateUIObject("CacheMemberCell", parent, new Vector2(100, 30));
            m_rect = uiRoot.GetComponent<RectTransform>();
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(uiRoot, true, true, true, true, 2, 0);
            UIFactory.SetLayoutElement(uiRoot, minWidth: 100, flexibleWidth: 9999, minHeight: 30, flexibleHeight: 600);
            UIRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var separator = UIFactory.CreateUIObject("TopSeperator", uiRoot);
            UIFactory.SetLayoutElement(separator, minHeight: 1, flexibleHeight: 0, flexibleWidth: 9999);
            separator.AddComponent<Image>().color = Color.black;

            var horiRow = UIFactory.CreateUIObject("HoriGroup", uiRoot);
            UIFactory.SetLayoutElement(horiRow, minHeight: 29, flexibleHeight: 150, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(horiRow, false, true, true, true, 5, 2, childAlignment: TextAnchor.UpperLeft);
            horiRow.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            MemberLabel = UIFactory.CreateLabel(horiRow, "MemberLabel", "<notset>", TextAnchor.UpperLeft);
            MemberLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(MemberLabel.gameObject, minHeight: 25, minWidth: 20, flexibleHeight: 300, flexibleWidth: 0);
            MemberLayout = MemberLabel.GetComponent<LayoutElement>();

            TypeLabel = UIFactory.CreateLabel(horiRow, "ReturnLabel", "<notset>", TextAnchor.UpperLeft);
            TypeLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(TypeLabel.gameObject, minHeight: 25, flexibleHeight: 150, minWidth: 20, flexibleWidth: 0);
            ReturnTypeLayout = TypeLabel.GetComponent<LayoutElement>();

            RightGroupHolder = UIFactory.CreateUIObject("RightGroup", horiRow);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(RightGroupHolder, false, false, true, true, 4, childAlignment: TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(RightGroupHolder, minHeight: 25, minWidth: 200, flexibleWidth: 9999, flexibleHeight: 150);
            RightGroupLayout = RightGroupHolder.GetComponent<LayoutElement>();

            InspectButton = UIFactory.CreateButton(RightGroupHolder, "InspectButton", "Inspect", new Color(0.23f, 0.23f, 0.23f));
            UIFactory.SetLayoutElement(InspectButton.Button.gameObject, minWidth: 60, flexibleWidth: 0, minHeight: 25);

            ValueLabel = UIFactory.CreateLabel(RightGroupHolder, "ValueLabel", "Value goes here", TextAnchor.MiddleLeft);
            ValueLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(ValueLabel.gameObject, minHeight: 25, flexibleHeight: 150, flexibleWidth: 9999);

            // Subcontent (todo?)

            SubContentHolder = UIFactory.CreateUIObject("SubContent", uiRoot);
            UIFactory.SetLayoutElement(SubContentHolder.gameObject, minHeight: 30, flexibleHeight: 500, minWidth: 100, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(SubContentHolder, true, false, true, true, 2, childAlignment: TextAnchor.UpperLeft);
            
            SubContentHolder.SetActive(false);

            return uiRoot;
        }
    }
}
