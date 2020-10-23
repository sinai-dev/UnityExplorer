using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExplorerBeta.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ExplorerBeta.UI.Shared;
using Explorer.UI.Main.Pages;

namespace ExplorerBeta.UI.Main
{
    public class MainMenu
    {
        public static MainMenu Instance { get; set; }

        public PanelDragger Dragger { get; private set; }

        public GameObject MainPanel { get; private set; }
        public GameObject PageViewport { get; private set; }

        public readonly List<BaseMenuPage> Pages = new List<BaseMenuPage>();
        private BaseMenuPage m_activePage;

        // Navbar buttons
        private Button m_lastNavButtonPressed;
        private readonly Color m_navButtonNormal = new Color(65f/255f, 66f/255f, 66f/255f);
        private readonly Color m_navButtonHighlight = new Color(50f/255f, 195f/255f, 50f/255f);
        private readonly Color m_navButtonSelected = new Color(60f/255f, 120f/255f, 60f/255f);

        public MainMenu()
        {
            if (Instance != null)
            {
                ExplorerCore.LogWarning("An instance of MainMenu already exists, cannot create another!");
                return;
            }

            Instance = this;

            Pages.Add(new HomePage());
            Pages.Add(new SearchPage());
            Pages.Add(new ConsolePage());
            Pages.Add(new OptionsPage());

            ConstructMenu();

            foreach (var page in Pages)
            {
                page.Init();
            }

            SetPage(Pages[0]);
        }

        public void Update()
        {
            // todo
        }

        // todo
        private void SetPage(BaseMenuPage page)
        {
            if (m_activePage == page || page == null)
                return;

            m_activePage?.Content?.SetActive(false);

            m_activePage = page;

            m_activePage.Content?.SetActive(true);

            var button = page.RefNavbarButton;

            var colors = button.colors;
            colors.normalColor = m_navButtonSelected;
            colors.selectedColor = m_navButtonSelected;
            button.colors = colors;

            if (m_lastNavButtonPressed && m_lastNavButtonPressed != button)
            {
                var oldColors = m_lastNavButtonPressed.colors;
                oldColors.normalColor = m_navButtonNormal;
                oldColors.selectedColor = m_navButtonNormal;
                m_lastNavButtonPressed.colors = oldColors;
            }

            m_lastNavButtonPressed = button;
        }

        #region UI Interaction Callbacks

        // ... none needed yet

        #endregion

        #region UI Construction

        private void ConstructMenu()
        {
            MainPanel = UIFactory.CreatePanel(UIManager.CanvasRoot, "MainMenu", out GameObject content);

            var panelRect = MainPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.25f, 0.1f);
            panelRect.anchorMax = new Vector2(0.75f, 0.95f);

            ConstructTitleBar(content);

            ConstructNavbar(content);

            ConstructMainViewport(content);
        }

        private void ConstructTitleBar(GameObject content)
        {
            // Core title bar holder

            var titleBar = UIFactory.CreateHorizontalGroup(content);

            var titleGroup = titleBar.GetComponent<HorizontalLayoutGroup>();
            titleGroup.childControlHeight = true;
            titleGroup.childControlWidth = true;
            titleGroup.childForceExpandHeight = true;
            titleGroup.childForceExpandWidth = true;
            titleGroup.padding.left = 15;
            titleGroup.padding.right = 3;
            titleGroup.padding.top = 3;
            titleGroup.padding.bottom = 3;

            var titleLayout = titleBar.AddComponent<LayoutElement>();
            titleLayout.minHeight = 35;
            titleLayout.flexibleHeight = 0;

            // Explorer label

            var textObj = UIFactory.CreateLabel(titleBar, TextAnchor.MiddleLeft);

            var text = textObj.GetComponent<Text>();
            text.text = $"<b>Explorer</b> <i>v{ExplorerCore.VERSION}</i>";
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = 20;

            var textLayout = textObj.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 50;

            // Add PanelDragger using the label object

            Dragger = new PanelDragger(titleBar.GetComponent<RectTransform>(), MainPanel.GetComponent<RectTransform>());

            // Hide button

            var hideBtnObj = UIFactory.CreateButton(titleBar);

            var hideBtn = hideBtnObj.GetComponent<Button>();
            hideBtn.onClick.AddListener(new Action(() => { ExplorerCore.ShowMenu = false; }));
            var colorBlock = hideBtn.colors;
            colorBlock.normalColor = new Color(65f/255f, 23f/255f, 23f/255f);
            colorBlock.pressedColor = new Color(35f/255f, 10f/255f, 10f/255f);
            colorBlock.highlightedColor = new Color(156f/255f, 0f, 0f);
            hideBtn.colors = colorBlock;

            var btnLayout = hideBtnObj.AddComponent<LayoutElement>();
            btnLayout.minWidth = 90;
            btnLayout.flexibleWidth = 2;

            var hideText = hideBtnObj.GetComponentInChildren<Text>();
            // Todo use actual keycode from mod config, update on OnSettingsChanged or whatever
            hideText.text = "Hide (F7)";
            hideText.color = Color.white;
            hideText.resizeTextForBestFit = true;
            hideText.resizeTextMinSize = 8;
            hideText.resizeTextMaxSize = 16;
        }

        private void ConstructNavbar(GameObject content)
        {
            // Todo add pages programatically

            var navbarObj = UIFactory.CreateHorizontalGroup(content);

            var navGroup = navbarObj.GetComponent<HorizontalLayoutGroup>();
            navGroup.padding.left = 3;
            navGroup.padding.right = 3;
            navGroup.padding.top = 3;
            navGroup.padding.bottom = 3;
            navGroup.spacing = 5;
            navGroup.childControlHeight = true;
            navGroup.childControlWidth = true;
            navGroup.childForceExpandHeight = true;
            navGroup.childForceExpandWidth = true;

            var navLayout = navbarObj.AddComponent<LayoutElement>();
            navLayout.minHeight = 35;
            navLayout.flexibleHeight = 0;

            foreach (var page in Pages)
            {
                var btnObj = UIFactory.CreateButton(navbarObj);
                var btn = btnObj.GetComponent<Button>();

                page.RefNavbarButton = btn;

                btn.onClick.AddListener(new Action(() => { SetPage(page); }));

                var text = btnObj.GetComponentInChildren<Text>();
                text.text = page.Name;

                // Set button colors
                var colorBlock = btn.colors;
                colorBlock.normalColor = m_navButtonNormal;
                colorBlock.selectedColor = colorBlock.normalColor;
                colorBlock.highlightedColor = m_navButtonHighlight;
                colorBlock.pressedColor = m_navButtonSelected;
                btn.colors = colorBlock;
            }
        }

        private void ConstructMainViewport(GameObject content)
        {
            var mainObj = UIFactory.CreateHorizontalGroup(content);
            var mainGroup = mainObj.GetComponent<HorizontalLayoutGroup>();
            mainGroup.childControlHeight = true;
            mainGroup.childControlWidth = true;
            mainGroup.childForceExpandHeight = true;
            mainGroup.childForceExpandWidth = true;

            PageViewport = mainObj;
        }

        #endregion
    }
}
