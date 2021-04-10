using System;
using System.Collections.Generic;
using UnityExplorer.Core.CSharp;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Unity;
using UnityExplorer.UI.Main;
using UnityExplorer.UI.Main.Home;
using UnityExplorer.UI.Main.Search;
using UnityExplorer.UI.Main.CSConsole;
using UnityExplorer.UI.Main.Options;
using UnityExplorer.Core.Runtime;

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

        public static Action<MenuPages> OnActiveTabChanged;

        // Navbar buttons
        private Button m_lastNavButtonPressed;
        private readonly Color m_navButtonNormal = new Color(0.3f, 0.3f, 0.3f, 1);
        private readonly Color m_navButtonHighlight = new Color(0.3f, 0.6f, 0.3f);
        private readonly Color m_navButtonSelected = new Color(0.2f, 0.5f, 0.2f, 1);

        internal Vector3 initPos;
        internal bool pageLayoutInit;
        internal int layoutInitIndex;

        public static void Create()
        {
            if (Instance != null)
            {
                ExplorerCore.LogWarning("An instance of MainMenu already exists, cannot create another!");
                return;
            }

            Instance = new MainMenu();
            Instance.Init();
        }

        private void Init()
        {
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
                    page.WasDisabled = true;
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

                    SetPage(ConfigManager.Default_Tab.Value);
                }
                return;
            }

            m_activePage?.Update();
        }

        public void SetPage(MenuPages page)
        {
            var pageObj = Pages.Find(it => it.Type == page);
            if (pageObj == null || pageObj.WasDisabled)
                return;
            SetPage(pageObj);
        }

        public void SetPage(BaseMenuPage page)
        {
            if (page == null || m_activePage == page || page.WasDisabled)
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

            OnActiveTabChanged?.Invoke(m_activePage.Type);
        }

        internal void SetButtonActiveColors(Button button)
        {
            RuntimeProvider.Instance.SetColorBlock(button, m_navButtonSelected);
        }

        internal void SetButtonInactiveColors(Button button)
        {
            RuntimeProvider.Instance.SetColorBlock(button, m_navButtonNormal);
        }

        #region UI Construction

        private void ConstructMenu()
        {
            MainPanel = UIFactory.CreatePanel("MainMenu", out GameObject content,
                ConfigManager.Last_Window_Anchors.Value, ConfigManager.Last_Window_Position.Value);

            ConstructTitleBar(content);

            ConstructNavbar(content);

            PageViewport = UIFactory.CreateHorizontalGroup(content, "MainViewPort", true, true, true, true);

            new DebugConsole(content);
        }

        private void ConstructTitleBar(GameObject content)
        {
            // Core title bar holder

            GameObject titleBar = UIFactory.CreateHorizontalGroup(content, "MainTitleBar", true, true, true, true, 0, new Vector4(3,3,15,3));
            UIFactory.SetLayoutElement(titleBar, minWidth: 25, flexibleHeight: 0);

            // Main title label

            var text = UIFactory.CreateLabel(titleBar, "TitleLabel", $"<b>UnityExplorer</b> <i>v{ExplorerCore.VERSION}</i>", TextAnchor.MiddleLeft,
                default, true, 15);
            UIFactory.SetLayoutElement(text.gameObject, flexibleWidth: 5000);

            // Add PanelDragger using the label object

            Dragger = new PanelDragger(titleBar.GetComponent<RectTransform>(), MainPanel.GetComponent<RectTransform>());

            // Hide button

            var hideButton = UIFactory.CreateButton(titleBar, 
                "HideButton", 
                $"Hide ({ConfigManager.Main_Menu_Toggle.Value})", 
                () => { UIManager.ShowMenu = false; }); 
            
            RuntimeProvider.Instance.SetColorBlock(hideButton, new Color(65f / 255f, 23f / 255f, 23f / 255f),
                 new Color(35f / 255f, 10f / 255f, 10f / 255f), new Color(156f / 255f, 0f, 0f));

            UIFactory.SetLayoutElement(hideButton.gameObject, minWidth: 90, flexibleWidth: 0);

            Text hideText = hideButton.GetComponentInChildren<Text>();
            hideText.color = Color.white;
            hideText.resizeTextForBestFit = true;
            hideText.resizeTextMinSize = 8;
            hideText.resizeTextMaxSize = 14;

            ConfigManager.Main_Menu_Toggle.OnValueChanged += (KeyCode val) => 
            {
                hideText.text = $"Hide ({val})";
            };
        }

        private void ConstructNavbar(GameObject content)
        {
            GameObject navbarObj = UIFactory.CreateHorizontalGroup(content, "MainNavBar", true, true, true, true, 5);
            UIFactory.SetLayoutElement(navbarObj, minHeight: 25, flexibleHeight: 0);

            foreach (var page in Pages)
            {
                Button btn = UIFactory.CreateButton(navbarObj, 
                    $"Button_{page.Name}",
                    page.Name,
                    () => { SetPage(page); });

                RuntimeProvider.Instance.SetColorBlock(btn, m_navButtonNormal, m_navButtonHighlight, m_navButtonSelected);

                page.RefNavbarButton = btn;
            }
        }

        #endregion
    }
}
