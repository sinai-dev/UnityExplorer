using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Inspectors;
using UnityExplorer.Core.Inspectors.Reflection;
using UnityExplorer.UI.Reusable;
using UnityExplorer.UI.Utility;

namespace UnityExplorer.UI.Main.Home.Inspectors
{
    public class ReflectionInspectorUI : InspectorBaseUI
    {
        public ReflectionInspectorUI(ReflectionInspector parent)
        {
            this.Parent = parent;
        }

        public ReflectionInspector Parent;

        // UI members

        private GameObject m_content;
        public override GameObject Content
        {
            get => m_content;
            set => m_content = value;
        }

        internal Text m_nameFilterText;
        internal MemberTypes m_memberFilter;
        internal Button m_lastActiveMemButton;

        internal PageHandler m_pageHandler;
        internal SliderScrollbar m_sliderScroller;
        internal GameObject m_scrollContent;
        internal RectTransform m_scrollContentRect;

        internal bool m_widthUpdateWanted;
        internal bool m_widthUpdateWaiting;

        internal GameObject m_filterAreaObj;
        internal GameObject m_updateRowObj;
        internal GameObject m_memberListObj;

        internal void ConstructUI()
        {
            var parent = InspectorManager.UI.m_inspectorContent;
            this.Content = UIFactory.CreateVerticalGroup(parent, new Color(0.15f, 0.15f, 0.15f));
            var mainGroup = Content.GetComponent<VerticalLayoutGroup>();
            mainGroup.childForceExpandHeight = false;
            mainGroup.childForceExpandWidth = true;
            mainGroup.childControlHeight = true;
            mainGroup.childControlWidth = true;
            mainGroup.spacing = 5;
            mainGroup.padding.top = 4;
            mainGroup.padding.left = 4;
            mainGroup.padding.right = 4;
            mainGroup.padding.bottom = 4;

            ConstructTopArea();

            ConstructMemberList();
        }

        internal void ConstructTopArea()
        {
            var nameRowObj = UIFactory.CreateHorizontalGroup(Content, new Color(1, 1, 1, 0));
            var nameRow = nameRowObj.GetComponent<HorizontalLayoutGroup>();
            nameRow.childForceExpandWidth = true;
            nameRow.childForceExpandHeight = true;
            nameRow.childControlHeight = true;
            nameRow.childControlWidth = true;
            nameRow.padding.top = 2;
            var nameRowLayout = nameRowObj.AddComponent<LayoutElement>();
            nameRowLayout.minHeight = 25;
            nameRowLayout.flexibleHeight = 0;
            nameRowLayout.minWidth = 200;
            nameRowLayout.flexibleWidth = 5000;

            var typeLabel = UIFactory.CreateLabel(nameRowObj, TextAnchor.MiddleLeft);
            var typeLabelText = typeLabel.GetComponent<Text>();
            typeLabelText.text = "Type:";
            typeLabelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            var typeLabelTextLayout = typeLabel.AddComponent<LayoutElement>();
            typeLabelTextLayout.minWidth = 40;
            typeLabelTextLayout.flexibleWidth = 0;
            typeLabelTextLayout.minHeight = 25;

            var typeDisplayObj = UIFactory.CreateLabel(nameRowObj, TextAnchor.MiddleLeft);
            var typeDisplayText = typeDisplayObj.GetComponent<Text>();
            typeDisplayText.text = SignatureHighlighter.ParseFullSyntax(Parent.m_targetType, true);
            var typeDisplayLayout = typeDisplayObj.AddComponent<LayoutElement>();
            typeDisplayLayout.minHeight = 25;
            typeDisplayLayout.flexibleWidth = 5000;

            // instance helper tools

            if (Parent is InstanceInspector instanceInspector)
            {
                instanceInspector.CreateInstanceUIModule();
                instanceInspector.InstanceUI.ConstructInstanceHelpers();
            }

            ConstructFilterArea();

            ConstructUpdateRow();
        }

        internal void ConstructFilterArea()
        {
            // Filters

            var filterAreaObj = UIFactory.CreateVerticalGroup(Content, new Color(0.1f, 0.1f, 0.1f));
            var filterLayout = filterAreaObj.AddComponent<LayoutElement>();
            filterLayout.minHeight = 60;
            var filterGroup = filterAreaObj.GetComponent<VerticalLayoutGroup>();
            filterGroup.childForceExpandWidth = true;
            filterGroup.childForceExpandHeight = true;
            filterGroup.childControlWidth = true;
            filterGroup.childControlHeight = true;
            filterGroup.spacing = 4;
            filterGroup.padding.left = 4;
            filterGroup.padding.right = 4;
            filterGroup.padding.top = 4;
            filterGroup.padding.bottom = 4;

            m_filterAreaObj = filterAreaObj;

            // name filter

            var nameFilterRowObj = UIFactory.CreateHorizontalGroup(filterAreaObj, new Color(1, 1, 1, 0));
            var nameFilterGroup = nameFilterRowObj.GetComponent<HorizontalLayoutGroup>();
            nameFilterGroup.childForceExpandHeight = false;
            nameFilterGroup.childForceExpandWidth = false;
            nameFilterGroup.childControlWidth = true;
            nameFilterGroup.childControlHeight = true;
            nameFilterGroup.spacing = 5;
            var nameFilterLayout = nameFilterRowObj.AddComponent<LayoutElement>();
            nameFilterLayout.minHeight = 25;
            nameFilterLayout.flexibleHeight = 0;
            nameFilterLayout.flexibleWidth = 5000;

            var nameLabelObj = UIFactory.CreateLabel(nameFilterRowObj, TextAnchor.MiddleLeft);
            var nameLabelLayout = nameLabelObj.AddComponent<LayoutElement>();
            nameLabelLayout.minWidth = 100;
            nameLabelLayout.minHeight = 25;
            nameLabelLayout.flexibleWidth = 0;
            var nameLabelText = nameLabelObj.GetComponent<Text>();
            nameLabelText.text = "Filter names:";
            nameLabelText.color = Color.grey;

            var nameInputObj = UIFactory.CreateInputField(nameFilterRowObj, 14, (int)TextAnchor.MiddleLeft, (int)HorizontalWrapMode.Overflow);
            var nameInputLayout = nameInputObj.AddComponent<LayoutElement>();
            nameInputLayout.flexibleWidth = 5000;
            nameInputLayout.minWidth = 100;
            nameInputLayout.minHeight = 25;
            var nameInput = nameInputObj.GetComponent<InputField>();
            nameInput.onValueChanged.AddListener((string val) => { Parent.FilterMembers(val); });
            m_nameFilterText = nameInput.textComponent;

            // membertype filter

            var memberFilterRowObj = UIFactory.CreateHorizontalGroup(filterAreaObj, new Color(1, 1, 1, 0));
            var memFilterGroup = memberFilterRowObj.GetComponent<HorizontalLayoutGroup>();
            memFilterGroup.childForceExpandHeight = false;
            memFilterGroup.childForceExpandWidth = false;
            memFilterGroup.childControlWidth = true;
            memFilterGroup.childControlHeight = true;
            memFilterGroup.spacing = 5;
            var memFilterLayout = memberFilterRowObj.AddComponent<LayoutElement>();
            memFilterLayout.minHeight = 25;
            memFilterLayout.flexibleHeight = 0;
            memFilterLayout.flexibleWidth = 5000;

            var memLabelObj = UIFactory.CreateLabel(memberFilterRowObj, TextAnchor.MiddleLeft);
            var memLabelLayout = memLabelObj.AddComponent<LayoutElement>();
            memLabelLayout.minWidth = 100;
            memLabelLayout.minHeight = 25;
            memLabelLayout.flexibleWidth = 0;
            var memLabelText = memLabelObj.GetComponent<Text>();
            memLabelText.text = "Filter members:";
            memLabelText.color = Color.grey;

            AddFilterButton(memberFilterRowObj, MemberTypes.All);
            AddFilterButton(memberFilterRowObj, MemberTypes.Method);
            AddFilterButton(memberFilterRowObj, MemberTypes.Property, true);
            AddFilterButton(memberFilterRowObj, MemberTypes.Field);

            // Instance filters

            if (Parent is InstanceInspector instanceInspector)
            {
                instanceInspector.InstanceUI.ConstructInstanceFilters(filterAreaObj);
            }
        }

        private void AddFilterButton(GameObject parent, MemberTypes type, bool setEnabled = false)
        {
            var btnObj = UIFactory.CreateButton(parent, new Color(0.2f, 0.2f, 0.2f));

            var btnLayout = btnObj.AddComponent<LayoutElement>();
            btnLayout.minHeight = 25;
            btnLayout.minWidth = 70;

            var text = btnObj.GetComponentInChildren<Text>();
            text.text = type.ToString();

            var btn = btnObj.GetComponent<Button>();

            btn.onClick.AddListener(() => { Parent.OnMemberFilterClicked(type, btn); });

            var colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.7f, 0.3f);

            if (setEnabled)
            {
                colors.normalColor = new Color(0.2f, 0.6f, 0.2f);
                m_memberFilter = type;
                m_lastActiveMemButton = btn;
            }

            btn.colors = colors;
        }

        internal void ConstructUpdateRow()
        {
            var optionsRowObj = UIFactory.CreateHorizontalGroup(Content, new Color(1, 1, 1, 0));
            var optionsLayout = optionsRowObj.AddComponent<LayoutElement>();
            optionsLayout.minHeight = 25;
            var optionsGroup = optionsRowObj.GetComponent<HorizontalLayoutGroup>();
            optionsGroup.childForceExpandHeight = true;
            optionsGroup.childForceExpandWidth = false;
            optionsGroup.childAlignment = TextAnchor.MiddleLeft;
            optionsGroup.spacing = 10;

            m_updateRowObj = optionsRowObj;

            // update button

            var updateButtonObj = UIFactory.CreateButton(optionsRowObj, new Color(0.2f, 0.2f, 0.2f));
            var updateBtnLayout = updateButtonObj.AddComponent<LayoutElement>();
            updateBtnLayout.minWidth = 110;
            updateBtnLayout.flexibleWidth = 0;
            var updateText = updateButtonObj.GetComponentInChildren<Text>();
            updateText.text = "Update Values";
            var updateBtn = updateButtonObj.GetComponent<Button>();
            updateBtn.onClick.AddListener(() =>
            {
                bool orig = Parent.m_autoUpdate;
                Parent.m_autoUpdate = true;
                Parent.Update();
                if (!orig) Parent.m_autoUpdate = orig;
            });

            // auto update

            var autoUpdateObj = UIFactory.CreateToggle(optionsRowObj, out Toggle autoUpdateToggle, out Text autoUpdateText);
            var autoUpdateLayout = autoUpdateObj.AddComponent<LayoutElement>();
            autoUpdateLayout.minWidth = 150;
            autoUpdateLayout.minHeight = 25;
            autoUpdateText.text = "Auto-update?";
            autoUpdateToggle.isOn = false;
            autoUpdateToggle.onValueChanged.AddListener((bool val) => { Parent.m_autoUpdate = val; });
        }

        internal void ConstructMemberList()
        {
            var scrollobj = UIFactory.CreateScrollView(Content, out m_scrollContent, out m_sliderScroller, new Color(0.05f, 0.05f, 0.05f));

            m_memberListObj = scrollobj;
            m_scrollContentRect = m_scrollContent.GetComponent<RectTransform>();

            var scrollGroup = m_scrollContent.GetComponent<VerticalLayoutGroup>();
            scrollGroup.spacing = 3;
            scrollGroup.padding.left = 0;
            scrollGroup.padding.right = 0;
            scrollGroup.childForceExpandHeight = true;

            m_pageHandler = new PageHandler(m_sliderScroller);
            m_pageHandler.ConstructUI(Content);
            m_pageHandler.OnPageChanged += Parent.OnPageTurned;
        }
    }
}
