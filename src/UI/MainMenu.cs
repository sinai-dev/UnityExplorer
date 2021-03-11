using System;
using System.Collections.Generic;
using UnityExplorer.CSConsole;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Modules;
using UnityExplorer.Config;
using UnityExplorer.Helpers;

namespace UnityExplorer.UI
{
    public class MainMenu
    {
        public abstract class Page
        {
            public abstract string Name { get; }

            public GameObject Content;
            public Button RefNavbarButton { get; set; }

            public bool Enabled
            {
                get => Content?.activeSelf ?? false;
                set => Content?.SetActive(true);
            }


            public abstract void Init();
            public abstract void Update();
        }

        public static MainMenu Instance { get; set; }

        public PanelDragger Dragger { get; private set; }

        public GameObject MainPanel { get; private set; }
        public GameObject PageViewport { get; private set; }

        public readonly List<Page> Pages = new List<Page>();
        private Page m_activePage;

        // Navbar buttons
        private Button m_lastNavButtonPressed;
        private readonly Color m_navButtonNormal = new Color(0.3f, 0.3f, 0.3f, 1);
        private readonly Color m_navButtonHighlight = new Color(0.3f, 0.6f, 0.3f);
        private readonly Color m_navButtonSelected = new Color(0.2f, 0.5f, 0.2f, 1); 

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
            Pages.Add(new CSConsolePage());
            Pages.Add(new OptionsPage());

            ConstructMenu();

            foreach (Page page in Pages)
            {
                page.Init();
            }

            // hide menu until each page has init layout (bit of a hack)
            initPos = MainPanel.transform.position;
            MainPanel.transform.position = new Vector3(9999, 9999);
        }

        internal Vector3 initPos;
        internal bool pageLayoutInit;
        internal int layoutInitIndex;

        public void Update()
        {
            if (!pageLayoutInit)
            {
                if (layoutInitIndex < Pages.Count)
                {
                    SetPage(Pages[layoutInitIndex]);
                    layoutInitIndex++;
                }
                else
                {
                    pageLayoutInit = true;
                    MainPanel.transform.position = initPos;
                    SetPage(Pages[0]);
                }
                return;
            }

            m_activePage?.Update();
        }

        public void SetPage(Page page)
        {
            if (page == null || m_activePage == page)
                return;

            // WIP, was going to hide current page if you press current page's button,
            // but the main panel does not resize so its just a big empty gap there.
            // Could be good if I resize that gap, not bothering for now. 
            // Would need a fix in PanelDragger as well.

            //if (m_activePage == page)
            //{
            //    SetButtonInactiveColors(page.RefNavbarButton);
            //    m_activePage.Content.SetActive(false);
            //    m_activePage = null;
            //    return;
            //}

            m_activePage?.Content?.SetActive(false);

            // unique case for console page, at the moment this will just go here
            if (m_activePage is CSConsolePage)
                AutoCompleter.m_mainObj?.SetActive(false);

            m_activePage = page;

            m_activePage.Content?.SetActive(true);

            Button button = page.RefNavbarButton;
            SetButtonActiveColors(button);

            if (m_lastNavButtonPressed && m_lastNavButtonPressed != button)
                SetButtonInactiveColors(m_lastNavButtonPressed);

            m_lastNavButtonPressed = button;
        }

        internal void SetButtonActiveColors(Button button)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = m_navButtonSelected;
            button.colors = colors;
        }

        internal void SetButtonInactiveColors(Button button)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = m_navButtonNormal;
            button.colors = colors;
        }

        #region UI Construction

        private void ConstructMenu()
        {
            MainPanel = UIFactory.CreatePanel(UIManager.CanvasRoot, "MainMenu", out GameObject content);

            RectTransform panelRect = MainPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.25f, 0.1f);
            panelRect.anchorMax = new Vector2(0.78f, 0.95f);

            MainPanel.AddComponent<Mask>();

            ConstructTitleBar(content);

            ConstructNavbar(content);

            ConstructMainViewport(content);

            new DebugConsole(content);
        }

        private void ConstructTitleBar(GameObject content)
        {
            // Core title bar holder

            GameObject titleBar = UIFactory.CreateHorizontalGroup(content);

            HorizontalLayoutGroup titleGroup = titleBar.GetComponent<HorizontalLayoutGroup>();
            titleGroup.childControlHeight = true;
            titleGroup.childControlWidth = true;
            titleGroup.childForceExpandHeight = true;
            titleGroup.childForceExpandWidth = true;
            titleGroup.padding.left = 15;
            titleGroup.padding.right = 3;
            titleGroup.padding.top = 3;
            titleGroup.padding.bottom = 3;

            LayoutElement titleLayout = titleBar.AddComponent<LayoutElement>();
            titleLayout.minHeight = 25;
            titleLayout.flexibleHeight = 0;

            // Explorer label

            GameObject textObj = UIFactory.CreateLabel(titleBar, TextAnchor.MiddleLeft);

            Text text = textObj.GetComponent<Text>();
            text.text = $"<b>UnityExplorer</b> <i>v{ExplorerCore.VERSION}</i>";
            text.fontSize = 15;
            LayoutElement textLayout = textObj.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 5000;

            // Add PanelDragger using the label object

            Dragger = new PanelDragger(titleBar.GetComponent<RectTransform>(), MainPanel.GetComponent<RectTransform>());

            // Hide button

            GameObject hideBtnObj = UIFactory.CreateButton(titleBar);

            Button hideBtn = hideBtnObj.GetComponent<Button>();
            hideBtn.onClick.AddListener(() => { UIManager.ShowMenu = false; });
            ColorBlock colorBlock = hideBtn.colors;
            colorBlock.normalColor = new Color(65f / 255f, 23f / 255f, 23f / 255f);
            colorBlock.pressedColor = new Color(35f / 255f, 10f / 255f, 10f / 255f);
            colorBlock.highlightedColor = new Color(156f / 255f, 0f, 0f);
            hideBtn.colors = colorBlock;

            LayoutElement btnLayout = hideBtnObj.AddComponent<LayoutElement>();
            btnLayout.minWidth = 90;
            btnLayout.flexibleWidth = 2;

            Text hideText = hideBtnObj.GetComponentInChildren<Text>();
            hideText.color = Color.white;
            hideText.resizeTextForBestFit = true;
            hideText.resizeTextMinSize = 8;
            hideText.resizeTextMaxSize = 14;
            hideText.text = $"Hide ({ExplorerConfig.Instance.Main_Menu_Toggle})";

            ExplorerConfig.OnConfigChanged += ModConfig_OnConfigChanged; 
            
            void ModConfig_OnConfigChanged()
            {
                hideText.text = $"Hide ({ExplorerConfig.Instance.Main_Menu_Toggle})";
            }
        }

        private void ConstructNavbar(GameObject content)
        {
            GameObject navbarObj = UIFactory.CreateHorizontalGroup(content);

            HorizontalLayoutGroup navGroup = navbarObj.GetComponent<HorizontalLayoutGroup>();
            navGroup.spacing = 5;
            navGroup.childControlHeight = true;
            navGroup.childControlWidth = true;
            navGroup.childForceExpandHeight = true;
            navGroup.childForceExpandWidth = true;

            LayoutElement navLayout = navbarObj.AddComponent<LayoutElement>();
            navLayout.minHeight = 25;
            navLayout.flexibleHeight = 0;

            foreach (Page page in Pages)
            {
                GameObject btnObj = UIFactory.CreateButton(navbarObj);
                Button btn = btnObj.GetComponent<Button>();

                page.RefNavbarButton = btn;

                btn.onClick.AddListener(() => { SetPage(page); });

                Text text = btnObj.GetComponentInChildren<Text>();
                text.text = page.Name;

                // Set button colors
                ColorBlock colorBlock = btn.colors;
                colorBlock.normalColor = m_navButtonNormal;
                //try { colorBlock.selectedColor = colorBlock.normalColor; } catch { }
                colorBlock.highlightedColor = m_navButtonHighlight;
                colorBlock.pressedColor = m_navButtonSelected;
                btn.colors = colorBlock;
            }
        }

        private void ConstructMainViewport(GameObject content)
        {
            GameObject mainObj = UIFactory.CreateHorizontalGroup(content);
            HorizontalLayoutGroup mainGroup = mainObj.GetComponent<HorizontalLayoutGroup>();
            mainGroup.childControlHeight = true;
            mainGroup.childControlWidth = true;
            mainGroup.childForceExpandHeight = true;
            mainGroup.childForceExpandWidth = true;

            PageViewport = mainObj;
        }

        #endregion
    }
}
