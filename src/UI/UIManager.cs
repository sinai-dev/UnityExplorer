using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Config;
using UnityExplorer.CSConsole;
using UnityExplorer.Inspectors;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Widgets.AutoComplete;
using UniverseLib;
using UniverseLib.Input;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.ObjectPool;
using UniverseLib.UI.Widgets;
using UniverseLib.UI.Widgets.ScrollView;
using UniverseLib.Utility;

namespace UnityExplorer.UI
{
    public static class UIManager
    {
        public enum Panels
        {
            ObjectExplorer,
            Inspector,
            CSConsole,
            Options,
            ConsoleLog,
            AutoCompleter,
            MouseInspector,
            UIInspectorResults,
            HookManager,
            Clipboard
        }

        public enum VerticalAnchor
        {
            Top,
            Bottom
        }

        public static VerticalAnchor NavbarAnchor = VerticalAnchor.Top;

        public static bool Initializing { get; internal set; } = true;

        internal static UIBase UiBase { get; private set; }
        public static GameObject UIRoot => UiBase?.RootObject;
        public static RectTransform UIRootRect { get; private set; }
        public static Canvas UICanvas { get; private set; }

        internal static GameObject PanelHolder { get; private set; }
        internal static readonly Dictionary<Panels, UIPanel> UIPanels = new();

        public static RectTransform NavBarRect;
        public static GameObject NavbarTabButtonHolder;
        private static readonly Vector2 NAVBAR_DIMENSIONS = new(1020f, 35f);

        private static ButtonRef closeBtn;
        private static ButtonRef pauseBtn;
        private static InputFieldRef timeInput;
        private static bool pauseButtonPausing;
        private static float lastTimeScale;

        private static int lastScreenWidth;
        private static int lastScreenHeight;

        public static bool ShowMenu
        {
            get => UiBase != null && UiBase.Enabled;
            set
            {
                if (UiBase == null || !UIRoot || UiBase.Enabled == value)
                    return;

                UniversalUI.SetUIActive(ExplorerCore.GUID, value);
                UniversalUI.SetUIActive(MouseInspector.UIBaseGUID, value);
            }
        }

        // Initialization

        internal static void InitUI()
        {
            UiBase = UniversalUI.RegisterUI(ExplorerCore.GUID, Update);

            UIRootRect = UIRoot.GetComponent<RectTransform>();
            UICanvas = UIRoot.GetComponent<Canvas>();

            DisplayManager.Init();

            var display = DisplayManager.ActiveDisplay;
            lastScreenWidth = display.renderingWidth;
            lastScreenHeight = display.renderingHeight;

            // Create UI.
            CreatePanelHolder();
            CreateTopNavBar();
            // This could be automated with Assembly.GetTypes(),
            // but the order is important and I'd have to write something to handle the order.
            UIPanels.Add(Panels.AutoCompleter, new AutoCompleteModal());
            UIPanels.Add(Panels.ObjectExplorer, new ObjectExplorerPanel());
            UIPanels.Add(Panels.Inspector, new InspectorPanel());
            UIPanels.Add(Panels.CSConsole, new CSConsolePanel());
            UIPanels.Add(Panels.HookManager, new HookManagerPanel());
            UIPanels.Add(Panels.Clipboard, new ClipboardPanel());
            UIPanels.Add(Panels.ConsoleLog, new LogPanel());
            UIPanels.Add(Panels.Options, new OptionsPanel());
            UIPanels.Add(Panels.UIInspectorResults, new UiInspectorResultsPanel());
            UIPanels.Add(Panels.MouseInspector, new MouseInspector());

            foreach (var panel in UIPanels.Values)
                panel.ConstructUI();

            // Call some initialize methods
            Notification.Init();
            ConsoleController.Init();

            // Add this listener to prevent ScrollPool doing anything while we are resizing panels
            ScrollPool<ICell>.writingLockedListeners.Add(() => !PanelDragger.Resizing);

            // Set default menu visibility
            ShowMenu = !ConfigManager.Hide_On_Startup.Value;

            // Failsafe fix, in some games all dropdowns displayed values are blank on startup for some reason.
            foreach (var dropdown in UIRoot.GetComponentsInChildren<Dropdown>(true))
                dropdown.RefreshShownValue();

            Initializing = false;
        }

        // Main UI Update loop

        public static void Update()
        {
            if (!UIRoot)
                return;

            // If we are doing a Mouse Inspect, we don't need to update anything else.
            if (MouseInspector.Instance.TryUpdate())
                return;

            // Update Notification modal
            Notification.Update();

            // Check forceUnlockMouse toggle
            if (InputManager.GetKeyDown(ConfigManager.Force_Unlock_Toggle.Value))
                UniverseLib.Config.ConfigManager.Force_Unlock_Mouse = !UniverseLib.Config.ConfigManager.Force_Unlock_Mouse;

            // update focused panel
            UIPanel.UpdateFocus();
            PanelDragger.UpdateInstances();

            // update the timescale value
            if (!timeInput.Component.isFocused && lastTimeScale != Time.timeScale)
            {
                if (pauseButtonPausing && Time.timeScale != 0.0f)
                {
                    pauseButtonPausing = false;
                    OnPauseButtonToggled();
                }

                if (!pauseButtonPausing)
                {
                    timeInput.Text = Time.timeScale.ToString("F2");
                    lastTimeScale = Time.timeScale;
                }
            }

            // check screen dimension change
            var display = DisplayManager.ActiveDisplay;
            if (display.renderingWidth != lastScreenWidth || display.renderingHeight != lastScreenHeight)
                OnScreenDimensionsChanged();
        }

        // Panels

        public static UIPanel GetPanel(Panels panel) => UIPanels[panel];

        public static T GetPanel<T>(Panels panel) where T : UIPanel => (T)UIPanels[panel];

        public static void TogglePanel(Panels panel)
        {
            var uiPanel = GetPanel(panel);
            SetPanelActive(panel, !uiPanel.Enabled);
        }

        public static void SetPanelActive(Panels panel, bool active)
        {
            var obj = GetPanel(panel);
            SetPanelActive(obj, active);
        }

        public static void SetPanelActive(UIPanel panel, bool active)
        {
            panel.SetActive(active);
            if (active)
            {
                panel.UIRoot.transform.SetAsLastSibling();
                UIPanel.InvokeOnPanelsReordered();
            }
        }

        internal static void SetPanelActive(Transform transform, bool value)
        {
            if (UIPanel.transformToPanelDict.TryGetValue(transform.GetInstanceID(), out UIPanel panel))
                SetPanelActive(panel, value);
        }

        // navbar

        public static void SetNavBarAnchor()
        {
            switch (NavbarAnchor)
            {
                case VerticalAnchor.Top:
                    NavBarRect.anchorMin = new Vector2(0.5f, 1f);
                    NavBarRect.anchorMax = new Vector2(0.5f, 1f);
                    NavBarRect.anchoredPosition = new Vector2(NavBarRect.anchoredPosition.x, 0);
                    NavBarRect.sizeDelta = NAVBAR_DIMENSIONS;
                    break;

                case VerticalAnchor.Bottom:
                    NavBarRect.anchorMin = new Vector2(0.5f, 0f);
                    NavBarRect.anchorMax = new Vector2(0.5f, 0f);
                    NavBarRect.anchoredPosition = new Vector2(NavBarRect.anchoredPosition.x, 35);
                    NavBarRect.sizeDelta = NAVBAR_DIMENSIONS;
                    break;
            }
        }

        // listeners

        private static void OnScreenDimensionsChanged()
        {
            var display = DisplayManager.ActiveDisplay;
            lastScreenWidth = display.renderingWidth;
            lastScreenHeight = display.renderingHeight;

            foreach (var panel in UIPanels)
            {
                panel.Value.EnsureValidSize();
                UIPanel.EnsureValidPosition(panel.Value.Rect);
                panel.Value.Dragger.OnEndResize();
            }
        }

        private static void OnCloseButtonClicked()
        {
            ShowMenu = false;
        }

        private static void Master_Toggle_OnValueChanged(KeyCode val)
        {
            closeBtn.ButtonText.text = val.ToString();
        }

        // Time controls

        private static void OnTimeInputEndEdit(string val)
        {
            if (pauseButtonPausing)
                return;

            if (float.TryParse(val, out float f))
            {
                Time.timeScale = f;
                lastTimeScale = f;
            }

            timeInput.Text = Time.timeScale.ToString("F2");
        }

        private static void OnPauseButtonClicked()
        {
            pauseButtonPausing = !pauseButtonPausing;

            Time.timeScale = pauseButtonPausing ? 0f : lastTimeScale;

            OnPauseButtonToggled();
        }

        private static void OnPauseButtonToggled()
        {
            timeInput.Component.text = Time.timeScale.ToString("F2");
            timeInput.Component.readOnly = pauseButtonPausing;
            timeInput.Component.textComponent.color = pauseButtonPausing ? Color.grey : Color.white;

            Color color = pauseButtonPausing ? new Color(0.3f, 0.3f, 0.2f) : new Color(0.2f, 0.2f, 0.2f);
            RuntimeHelper.SetColorBlock(pauseBtn.Component, color, color * 1.2f, color * 0.7f);
            pauseBtn.ButtonText.text = pauseButtonPausing ? "►" : "||";
        }

        // UI Construction

        private static void CreatePanelHolder()
        {
            PanelHolder = new GameObject("PanelHolder");
            PanelHolder.transform.SetParent(UIRoot.transform, false);
            PanelHolder.layer = 5;
            var rect = PanelHolder.AddComponent<RectTransform>();
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            PanelHolder.transform.SetAsFirstSibling();
        }

        private static void CreateTopNavBar()
        {
            var navbarPanel = UIFactory.CreateUIObject("MainNavbar", UIRoot);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(navbarPanel, false, false, true, true, 5, 4, 4, 4, 4, TextAnchor.MiddleCenter);
            navbarPanel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f);
            NavBarRect = navbarPanel.GetComponent<RectTransform>();
            NavBarRect.pivot = new Vector2(0.5f, 1f);

            NavbarAnchor = ConfigManager.Main_Navbar_Anchor.Value;
            SetNavBarAnchor();
            ConfigManager.Main_Navbar_Anchor.OnValueChanged += (VerticalAnchor val) =>
            {
                NavbarAnchor = val;
                SetNavBarAnchor();
            };

            // UnityExplorer title

            string titleTxt = $"{ExplorerCore.NAME} <i><color=grey>{ExplorerCore.VERSION}</color></i>";
            var title = UIFactory.CreateLabel(navbarPanel, "Title", titleTxt, TextAnchor.MiddleLeft, default, true, 17);
            UIFactory.SetLayoutElement(title.gameObject, minWidth: 170, flexibleWidth: 0);

            // panel tabs

            NavbarTabButtonHolder = UIFactory.CreateUIObject("NavTabButtonHolder", navbarPanel);
            UIFactory.SetLayoutElement(NavbarTabButtonHolder, minHeight: 25, flexibleHeight: 999, flexibleWidth: 999);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(NavbarTabButtonHolder, false, true, true, true, 4, 2, 2, 2, 2);

            // Time controls

            var timeLabel = UIFactory.CreateLabel(navbarPanel, "TimeLabel", "Time:", TextAnchor.MiddleRight, Color.grey);
            UIFactory.SetLayoutElement(timeLabel.gameObject, minHeight: 25, minWidth: 50);

            timeInput = UIFactory.CreateInputField(navbarPanel, "TimeInput", "timeScale");
            UIFactory.SetLayoutElement(timeInput.Component.gameObject, minHeight: 25, minWidth: 40);
            timeInput.Component.GetOnEndEdit().AddListener(OnTimeInputEndEdit);

            timeInput.Text = string.Empty;
            timeInput.Text = Time.timeScale.ToString();

            pauseBtn = UIFactory.CreateButton(navbarPanel, "PauseButton", "||", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(pauseBtn.Component.gameObject, minHeight: 25, minWidth: 25);
            pauseBtn.OnClick += OnPauseButtonClicked;

            // Hide menu button

            closeBtn = UIFactory.CreateButton(navbarPanel, "CloseButton", ConfigManager.Master_Toggle.Value.ToString());
            UIFactory.SetLayoutElement(closeBtn.Component.gameObject, minHeight: 25, minWidth: 80, flexibleWidth: 0);
            RuntimeHelper.SetColorBlock(closeBtn.Component, new Color(0.63f, 0.32f, 0.31f),
                new Color(0.81f, 0.25f, 0.2f), new Color(0.6f, 0.18f, 0.16f));

            ConfigManager.Master_Toggle.OnValueChanged += Master_Toggle_OnValueChanged;
            closeBtn.OnClick += OnCloseButtonClicked;
        }
    }
}
