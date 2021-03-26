using System;
using System.Collections.Generic;
using UnityExplorer.Core.CSharp;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Unity;
using UnityExplorer.UI.Main;
using UnityExplorer.UI.Main.CSConsole;

namespace UnityExplorer.UI.Main
{
    public class MainMenu
    {
        public static MainMenu Instance { get; set; }

        public PanelDragger Dragger { get; private set; }

        public GameObject MainPanel { get; private set; }
        public GameObject PageViewport { get; private set; }

        public readonly List<BaseMenuPage> Pages = new List<BaseMenuPage>();
        private BaseMenuPage m_activePage;

        public static Action<int> OnActiveTabChanged;

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
            Pages.Add(new CSharpConsole());
            Pages.Add(new OptionsPage());

            ConstructMenu();

            for (int i = 0; i < Pages.Count; i++)
            {
                var page = Pages[i];
                
                if (!page.Init())
                {
                    // page init failed.
                    Pages.RemoveAt(i);
                    i--;

                    if (page.RefNavbarButton)
                        page.RefNavbarButton.interactable = false;
                    
                    if (page.Content)
                        GameObject.Destroy(page.Content);
                }
            }

            // hide menu until each page has init layout (bit of a hack)
            initPos = MainPanel.transform.position;
            MainPanel.transform.position = new Vector3(9999, 9999);
        }

        internal Vector3 initPos;
        internal bool pageLayoutInit;
        internal int layoutInitIndex;

        private int origDesiredPage = -1;

        public void Update()
        {
            if (!pageLayoutInit)
            {
                if (origDesiredPage == -1)
                    origDesiredPage = ExplorerConfig.Instance?.Active_Tab ?? 0;

                if (layoutInitIndex < Pages.Count)
                {
                    SetPage(Pages[layoutInitIndex]);
                    layoutInitIndex++;
                }
                else
                {
                    pageLayoutInit = true;
                    MainPanel.transform.position = initPos;
                    SetPage(Pages[origDesiredPage]);
                }
                return;
            }

            m_activePage?.Update();
        }

        public void SetPage(BaseMenuPage page)
        {
            if (page == null || m_activePage == page)
                return;

            m_activePage?.Content?.SetActive(false);

            // unique case for console page, at the moment this will just go here
            if (m_activePage is CSharpConsole)
                AutoCompleter.m_mainObj?.SetActive(false);

            m_activePage = page;

            m_activePage.Content?.SetActive(true);

            Button button = page.RefNavbarButton;
            SetButtonActiveColors(button);

            if (m_lastNavButtonPressed && m_lastNavButtonPressed != button)
                SetButtonInactiveColors(m_lastNavButtonPressed);

            m_lastNavButtonPressed = button;

            OnActiveTabChanged?.Invoke(Pages.IndexOf(m_activePage));
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
            var anchors = ExplorerConfig.Instance.GetWindowAnchorsVector();
            SetPanelAnchors(panelRect, anchors);

            if (panelRect.rect.width < 400 || panelRect.rect.height < 400)
            {
                anchors = ExplorerConfig.DefaultWindowAnchors();
                SetPanelAnchors(panelRect, anchors);
            }

            MainPanel.AddComponent<Mask>();

            ConstructTitleBar(content);

            ConstructNavbar(content);

            ConstructMainViewport(content);

            new DebugConsole(content);
        }

        private void SetPanelAnchors(RectTransform panelRect, Vector4 anchors)
        {
            panelRect.anchorMin = new Vector2(anchors.x, anchors.y);
            panelRect.anchorMax = new Vector2(anchors.z, anchors.w);
        }

        private void ConstructTitleBar(GameObject content)
        {
            // Core title bar holder

            GameObject titleBar = UIFactory.CreateHorizontalGroup(content);

            HorizontalLayoutGroup titleGroup = titleBar.GetComponent<HorizontalLayoutGroup>();
            titleGroup.SetChildControlHeight(true);
            titleGroup.SetChildControlWidth(true);
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
            navGroup.SetChildControlHeight(true);
            navGroup.SetChildControlWidth(true);
            navGroup.childForceExpandHeight = true;
            navGroup.childForceExpandWidth = true;

            LayoutElement navLayout = navbarObj.AddComponent<LayoutElement>();
            navLayout.minHeight = 25;
            navLayout.flexibleHeight = 0;

            foreach (BaseMenuPage page in Pages)
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
            mainGroup.SetChildControlHeight(true);
            mainGroup.SetChildControlWidth(true);
            mainGroup.childForceExpandHeight = true;
            mainGroup.childForceExpandWidth = true;

            PageViewport = mainObj;
        }

        #endregion
    }
}
