using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Input;
using UnityExplorer.CSConsole;
using UnityExplorer.Inspectors;
using UnityExplorer.UI.Models;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Widgets;
using UnityExplorer.UI.Widgets.AutoComplete;

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
        }

        public enum VerticalAnchor
        {
            Top,
            Bottom
        }

        public static bool Initializing { get; internal set; } = true;

        public static VerticalAnchor NavbarAnchor = VerticalAnchor.Top;

        public static GameObject CanvasRoot { get; private set; }
        public static Canvas Canvas { get; private set; }
        public static EventSystem EventSys { get; private set; }

        internal static GameObject PoolHolder { get; private set; }
        internal static GameObject PanelHolder { get; private set; }
        private static readonly Dictionary<Panels, UIPanel> UIPanels = new Dictionary<Panels, UIPanel>();

        internal static Font ConsoleFont { get; private set; }
        internal static Font DefaultFont { get; private set; }
        internal static Shader BackupShader { get; private set; }

        public static RectTransform NavBarRect;
        public static GameObject NavbarTabButtonHolder;
        public static Dropdown MouseInspectDropdown;

        private static ButtonRef closeBtn;
        private static ButtonRef pauseBtn;
        private static InputFieldRef timeInput;
        private static bool pauseButtonPausing;
        private static float lastTimeScale;

        // defaults
        internal static readonly Color enabledButtonColor = new Color(0.2f, 0.4f, 0.28f);
        internal static readonly Color disabledButtonColor = new Color(0.25f, 0.25f, 0.25f);

        public const int MAX_INPUTFIELD_CHARS = 16000;
        public const int MAX_TEXT_VERTS = 65000;

        public static bool ShowMenu
        {
            get => s_showMenu;
            set
            {
                if (s_showMenu == value || !CanvasRoot)
                    return;

                s_showMenu = value;
                CanvasRoot.SetActive(value);
                CursorUnlocker.UpdateCursorControl();
            }
        }
        public static bool s_showMenu = true;

        // Initialization

        internal static void InitUI()
        {
            LoadBundle();

            CreateRootCanvas();

            // Global UI Pool Holder
            PoolHolder = new GameObject("PoolHolder");
            PoolHolder.transform.parent = CanvasRoot.transform;
            PoolHolder.SetActive(false);

            CreateTopNavBar();

            UIPanels.Add(Panels.AutoCompleter, new AutoCompleteModal());
            UIPanels.Add(Panels.ObjectExplorer, new ObjectExplorerPanel());
            UIPanels.Add(Panels.Inspector, new InspectorPanel());
            UIPanels.Add(Panels.CSConsole, new CSConsolePanel());
            UIPanels.Add(Panels.HookManager, new HookManagerPanel());
            UIPanels.Add(Panels.ConsoleLog, new LogPanel());
            UIPanels.Add(Panels.Options, new OptionsPanel());
            UIPanels.Add(Panels.UIInspectorResults, new UiInspectorResultsPanel());
            UIPanels.Add(Panels.MouseInspector, new InspectUnderMouse());

            foreach (var panel in UIPanels.Values)
                panel.ConstructUI();

            ConsoleController.Init();

            ShowMenu = !ConfigManager.Hide_On_Startup.Value;

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            // Failsafe fix
            foreach (var dropdown in CanvasRoot.GetComponentsInChildren<Dropdown>(true))
                dropdown.RefreshShownValue();
            timeInput.Text = string.Empty;
            timeInput.Text = Time.timeScale.ToString();

            Initializing = false;
        }

        // Main UI Update loop

        private static int lastScreenWidth;
        private static int lastScreenHeight;

        public static void Update()
        {
            if (!CanvasRoot || Initializing)
                return;

            // if doing Mouse Inspect, update that and return.
            if (InspectUnderMouse.Inspecting)
            {
                InspectUnderMouse.Instance.UpdateInspect();
                return;
            }

            // check master toggle
            if (InputManager.GetKeyDown(ConfigManager.Master_Toggle.Value))
                ShowMenu = !ShowMenu;

            // return if menu closed
            if (!ShowMenu)
                return;

            // Check forceUnlockMouse toggle
            if (InputManager.GetKeyDown(ConfigManager.Force_Unlock_Toggle.Value))
                CursorUnlocker.Unlock = !CursorUnlocker.Unlock;

            // check event system state
            if (!ConfigManager.Disable_EventSystem_Override.Value && EventSystem.current != EventSys)
                CursorUnlocker.SetEventSystem();

            // update focused panel
            UIPanel.UpdateFocus();
            // update UI model instances
            PanelDragger.UpdateInstances();
            InputFieldRef.UpdateInstances();
            UIBehaviourModel.UpdateInstances();

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
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
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
                    NavBarRect.sizeDelta = new Vector2(1080f, 35f);
                    break;

                case VerticalAnchor.Bottom:
                    NavBarRect.anchorMin = new Vector2(0.5f, 0f);
                    NavBarRect.anchorMax = new Vector2(0.5f, 0f);
                    NavBarRect.anchoredPosition = new Vector2(NavBarRect.anchoredPosition.x, 35);
                    NavBarRect.sizeDelta = new Vector2(1080f, 35f);
                    break;
            }
        }

        // listeners

        private static void OnScreenDimensionsChanged()
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

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
            RuntimeProvider.Instance.SetColorBlock(pauseBtn.Component, color, color * 1.2f, color * 0.7f);
            pauseBtn.ButtonText.text = pauseButtonPausing ? "►" : "||";
        }

        // UI Construction

        private static void CreateRootCanvas()
        {
            CanvasRoot = new GameObject("ExplorerCanvas");
            UnityEngine.Object.DontDestroyOnLoad(CanvasRoot);
            CanvasRoot.hideFlags |= HideFlags.HideAndDontSave;
            CanvasRoot.layer = 5;
            CanvasRoot.transform.position = new Vector3(0f, 0f, 1f);

            CanvasRoot.SetActive(false);

            EventSys = CanvasRoot.AddComponent<EventSystem>();
            InputManager.AddUIModule();

            EventSys.enabled = false;
            CanvasRoot.SetActive(true);

            Canvas = CanvasRoot.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceCamera;
            Canvas.referencePixelsPerUnit = 100;
            Canvas.sortingOrder = 999;

            CanvasScaler scaler = CanvasRoot.AddComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            CanvasRoot.AddComponent<GraphicRaycaster>();

            PanelHolder = new GameObject("PanelHolder");
            PanelHolder.transform.SetParent(CanvasRoot.transform, false);
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
            var navbarPanel = UIFactory.CreateUIObject("MainNavbar", CanvasRoot);
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
            timeInput.Text = Time.timeScale.ToString("F2");
            timeInput.Component.GetOnEndEdit().AddListener(OnTimeInputEndEdit);

            pauseBtn = UIFactory.CreateButton(navbarPanel, "PauseButton", "||", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(pauseBtn.Component.gameObject, minHeight: 25, minWidth: 25);
            pauseBtn.OnClick += OnPauseButtonClicked;

            // Inspect under mouse dropdown

            var mouseDropdown = UIFactory.CreateDropdown(navbarPanel, out MouseInspectDropdown, "Mouse Inspect", 14,
                InspectUnderMouse.OnDropdownSelect);
            UIFactory.SetLayoutElement(mouseDropdown, minHeight: 25, minWidth: 140);
            MouseInspectDropdown.options.Add(new Dropdown.OptionData("Mouse Inspect"));
            MouseInspectDropdown.options.Add(new Dropdown.OptionData("World"));
            MouseInspectDropdown.options.Add(new Dropdown.OptionData("UI"));

            // Hide menu button

            closeBtn = UIFactory.CreateButton(navbarPanel, "CloseButton", ConfigManager.Master_Toggle.Value.ToString());
            UIFactory.SetLayoutElement(closeBtn.Component.gameObject, minHeight: 25, minWidth: 80, flexibleWidth: 0);
            RuntimeProvider.Instance.SetColorBlock(closeBtn.Component, new Color(0.63f, 0.32f, 0.31f),
                new Color(0.81f, 0.25f, 0.2f), new Color(0.6f, 0.18f, 0.16f));

            ConfigManager.Master_Toggle.OnValueChanged += Master_Toggle_OnValueChanged;
            closeBtn.OnClick += OnCloseButtonClicked;
        }

        // UI AssetBundle

        internal static AssetBundle ExplorerBundle;

        private static void LoadBundle()
        {
            SetupAssetBundlePatches();

            try
            {
                // Get the Major and Minor of the Unity version
                var split = Application.unityVersion.Split('.');
                int major = int.Parse(split[0]);
                int minor = int.Parse(split[1]);

                // Use appropriate AssetBundle for Unity version
                // >= 2017
                if (major >= 2017)
                    ExplorerBundle = LoadBundle("modern");
                // 5.6.0 to <2017
                else if (major == 5 && minor >= 6)
                    ExplorerBundle = LoadBundle("legacy.5.6");
                // < 5.6.0
                else
                    ExplorerBundle = LoadBundle("legacy");      
            }
            catch
            {
                ExplorerCore.LogWarning($"Exception parsing Unity version, falling back to old AssetBundle load method...");
                ExplorerBundle = LoadBundle("modern") ?? LoadBundle("legacy.5.6") ?? LoadBundle("legacy");
            }

            AssetBundle LoadBundle(string id)
            {
                ExplorerCore.Log($"Loading {id} bundle for Unity {Application.unityVersion}");

                return AssetBundle.LoadFromMemory(ReadFully(typeof(ExplorerCore)
                        .Assembly
                        .GetManifestResourceStream($"UnityExplorer.Resources.{id}.bundle")));
            }

            if (ExplorerBundle == null)
            {
                ExplorerCore.LogWarning("Could not load the ExplorerUI Bundle!");
                DefaultFont = ConsoleFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return;
            }

            // Bundle loaded

            ConsoleFont = ExplorerBundle.LoadAsset<Font>("CONSOLA");
            ConsoleFont.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(ConsoleFont);

            DefaultFont = ExplorerBundle.LoadAsset<Font>("arial");
            DefaultFont.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(DefaultFont);

            BackupShader = ExplorerBundle.LoadAsset<Shader>("DefaultUI");
            BackupShader.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(BackupShader);
            // Fix for games which don't ship with 'UI/Default' shader.
            if (Graphic.defaultGraphicMaterial.shader?.name != "UI/Default")
            {
                ExplorerCore.Log("This game does not ship with the 'UI/Default' shader, using manual Default Shader...");
                Graphic.defaultGraphicMaterial.shader = BackupShader;
            }
            else
                BackupShader = Graphic.defaultGraphicMaterial.shader;
        }

        private static byte[] ReadFully(Stream input)
        {
            using (var ms = new MemoryStream())
            {
                byte[] buffer = new byte[81920];
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) != 0)
                    ms.Write(buffer, 0, read);
                return ms.ToArray();
            }
        }

        // AssetBundle patch

        static bool donePatch;
        private static Type T_AssetBundle => ReflectionUtility.GetTypeByName("UnityEngine.AssetBundle");

        private static void SetupAssetBundlePatches()
        {
            if (!donePatch)
            {
                try
                {
                    if (T_AssetBundle.GetMethod("UnloadAllAssetBundles", AccessTools.all) is MethodInfo unloadAllBundles)
                    {
                        var processor = ExplorerCore.Harmony.CreateProcessor(unloadAllBundles);
                        var prefix = new HarmonyMethod(typeof(UIManager).GetMethod(nameof(Prefix_UnloadAllAssetBundles), AccessTools.all));
                        processor.AddPrefix(prefix);
                        processor.Patch();

                        donePatch = true;
                    }
                }
                catch (Exception ex)
                {
                    ExplorerCore.LogWarning($"Exception setting up AssetBundle.UnloadAllAssetBundles patch: {ex}");
                }
            }
        }


        static bool Prefix_UnloadAllAssetBundles(bool unloadAllObjects)
        {
            try
            {
                var method = typeof(AssetBundle).GetMethod("GetAllLoadedAssetBundles", AccessTools.all);
                if (method == null)
                    return true;
                var bundles = method.Invoke(null, ArgumentUtility.EmptyArgs) as AssetBundle[];
                foreach (var obj in bundles)
                {
                    if (obj.m_CachedPtr == ExplorerBundle.m_CachedPtr)
                        continue;

                    obj.Unload(unloadAllObjects);
                }
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception unloading AssetBundles: {ex}");
            }

            return false;
        }
    }
}
