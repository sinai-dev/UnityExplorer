using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Inspectors;

namespace UnityExplorer.UI.Main.Home.Inspectors
{
    public abstract class InspectorBaseUI
    {
        public abstract GameObject Content { get; set; }
        public Button tabButton;
        public Text tabText;

        public void AddInspectorTab(InspectorBase parent)
        {
            var tabContent = InspectorManager.UI.m_tabBarContent;

            var tabGroupObj = UIFactory.CreateHorizontalGroup(tabContent);
            var tabGroup = tabGroupObj.GetComponent<HorizontalLayoutGroup>();
            tabGroup.childForceExpandWidth = true;
            tabGroup.childControlWidth = true;
            var tabLayout = tabGroupObj.AddComponent<LayoutElement>();
            tabLayout.minWidth = 185;
            tabLayout.flexibleWidth = 0;
            tabGroupObj.AddComponent<Mask>();

            var targetButtonObj = UIFactory.CreateButton(tabGroupObj);
            targetButtonObj.AddComponent<Mask>();
            var targetButtonLayout = targetButtonObj.AddComponent<LayoutElement>();
            targetButtonLayout.minWidth = 165;
            targetButtonLayout.flexibleWidth = 0;

            tabText = targetButtonObj.GetComponentInChildren<Text>();
            tabText.horizontalOverflow = HorizontalWrapMode.Overflow;
            tabText.alignment = TextAnchor.MiddleLeft;

            tabButton = targetButtonObj.GetComponent<Button>();

            tabButton.onClick.AddListener(() => { InspectorManager.Instance.SetInspectorTab(parent); });

            var closeBtnObj = UIFactory.CreateButton(tabGroupObj);
            var closeBtnLayout = closeBtnObj.AddComponent<LayoutElement>();
            closeBtnLayout.minWidth = 20;
            closeBtnLayout.flexibleWidth = 0;
            var closeBtnText = closeBtnObj.GetComponentInChildren<Text>();
            closeBtnText.text = "X";
            closeBtnText.color = new Color(1, 0, 0, 1);

            var closeBtn = closeBtnObj.GetComponent<Button>();

            closeBtn.onClick.AddListener(parent.Destroy);

            var closeColors = closeBtn.colors;
            closeColors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1);
            closeBtn.colors = closeColors;
        }
    }
}
