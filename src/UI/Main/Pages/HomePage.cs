using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExplorerBeta.UI;
using ExplorerBeta.UI.Main;
using UnityEngine;
using UnityEngine.UI;

namespace Explorer.UI.Main.Pages
{
    public class HomePage : BaseMenuPage
    {
        public override string Name => "Home";

        private GameObject m_mainViewport;

        public override void Init()
        {
            ConstructMenu();
        }

        public override void Update()
        {
            
        }

        #region UI Construction

        private void ConstructMenu()
        {
            var parent = MainMenu.Instance.PageViewport;

            m_mainViewport = UIFactory.CreateHorizontalGroup(parent);
            var mainGroup = m_mainViewport.GetComponent<HorizontalLayoutGroup>();
            mainGroup.padding.left = 3;
            mainGroup.padding.right = 3;
            mainGroup.padding.top = 3;
            mainGroup.padding.bottom = 3;
            mainGroup.spacing = 5;
            mainGroup.childForceExpandHeight = true;
            mainGroup.childForceExpandWidth = true;
            mainGroup.childControlHeight = true;
            mainGroup.childControlWidth = true;

            var leftPaneObj = UIFactory.CreateVerticalGroup(m_mainViewport, new Color(72f / 255f, 72f / 255f, 72f / 255f));
            var leftLayout = leftPaneObj.AddComponent<LayoutElement>();
            leftLayout.minWidth = 350;
            leftLayout.flexibleWidth = 0;

            var leftGroup = leftPaneObj.GetComponent<VerticalLayoutGroup>();
            leftGroup.padding.left = 8;
            leftGroup.padding.right = 8;
            leftGroup.padding.top = 8;
            leftGroup.padding.bottom = 8;
            leftGroup.spacing = 5;
            leftGroup.childControlWidth = true;
            leftGroup.childControlHeight = true;
            leftGroup.childForceExpandWidth = false;
            leftGroup.childForceExpandHeight = true;

            var rightPaneObj = UIFactory.CreateVerticalGroup(m_mainViewport, new Color(72f / 255f, 72f / 255f, 72f / 255f));
            var rightLayout = rightPaneObj.AddComponent<LayoutElement>();
            rightLayout.flexibleWidth = 999999;

            var rightGroup = rightPaneObj.GetComponent<VerticalLayoutGroup>();
            rightGroup.childForceExpandHeight = true;
            rightGroup.childForceExpandWidth = true;
            rightGroup.childControlHeight = true;
            rightGroup.childControlWidth = true;
        }

        #endregion
    }
}
