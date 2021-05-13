using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Input;
using UnityExplorer.UI.CSConsole;
using UnityExplorer.UI.Models;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Utility;
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
            AutoCompleter
        }

        public static bool Initializing { get; private set; } = true;

        public static GameObject CanvasRoot { get; private set; }
        public static Canvas Canvas { get; private set; }
        public static EventSystem EventSys { get; private set; }

        internal static GameObject PoolHolder { get; private set; }

        // panels
        internal static GameObject PanelHolder { get; private set; }
        private static readonly Dictionary<Panels, UIPanel> UIPanels = new Dictionary<Panels, UIPanel>();

        // assets
        internal static Font ConsoleFont { get; private set; }
        internal static Shader BackupShader { get; private set; }

        // Main Navbar UI
        public static RectTransform NavBarRect;
        public static GameObject NavbarButtonHolder;

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

        // Panels

        public static UIPanel GetPanel(Panels panel)
        {
            return UIPanels[panel];
        }

        public static T GetPanel<T>(Panels panel) where T : UIPanel
        {
            return (T)UIPanels[panel];
        }

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

        // Main UI Update loop

        public static void Update()
        {
            if (!CanvasRoot || Initializing)
                return;

            //if (InspectUnderMouse.Inspecting)
            //{
            //    InspectUnderMouse.UpdateInspect();
            //    return;
            //}

            if (InputManager.GetKeyDown(ConfigManager.Main_Menu_Toggle.Value))
                ShowMenu = !ShowMenu;

            if (!ShowMenu)
                return;

            if (InputManager.GetKeyDown(ConfigManager.Force_Unlock_Toggle.Value))
                CursorUnlocker.Unlock = !CursorUnlocker.Unlock;

            if (EventSystem.current != EventSys)
                CursorUnlocker.SetEventSystem();

            UIPanel.UpdateFocus();
            PanelDragger.UpdateInstances();
            UIBehaviourModel.UpdateInstances();
        }

        // Initialization and UI Construction

        internal static void InitUI()
        {
            LoadBundle();

            UIFactory.Init();

            CreateRootCanvas();

            // Global UI Pool Holder
            PoolHolder = new GameObject("PoolHolder");
            PoolHolder.transform.parent = CanvasRoot.transform;
            PoolHolder.SetActive(false);

            CreateTopNavBar();

            // TODO (or probably just do this when showing it for first time)
            //InspectUnderMouse.ConstructUI();

            UIPanels.Add(Panels.AutoCompleter, new AutoCompleteModal());
            UIPanels.Add(Panels.ObjectExplorer, new ObjectExplorerPanel());
            UIPanels.Add(Panels.Inspector, new InspectorPanel());
            UIPanels.Add(Panels.CSConsole, new CSConsolePanel());
            UIPanels.Add(Panels.Options, new OptionsPanel());
            UIPanels.Add(Panels.ConsoleLog, new ConsoleLogPanel());

            foreach (var panel in UIPanels.Values)
                panel.ConstructUI();

            ConsoleController.Init();

            ShowMenu = !ConfigManager.Hide_On_Startup.Value;

            ExplorerCore.Log("UI initialized.");
            Initializing = false;
        }

        private static void CreateRootCanvas()
        {
            CanvasRoot = new GameObject("ExplorerCanvas");
            UnityEngine.Object.DontDestroyOnLoad(CanvasRoot);
            CanvasRoot.hideFlags |= HideFlags.HideAndDontSave;
            CanvasRoot.layer = 5;
            CanvasRoot.transform.position = new Vector3(0f, 0f, 1f);

            EventSys = CanvasRoot.AddComponent<EventSystem>();
            InputManager.AddUIModule();

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
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(navbarPanel, false, true, true, true, 5, 4, 4, 4, 4, TextAnchor.MiddleCenter);
            navbarPanel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f);
            NavBarRect = navbarPanel.GetComponent<RectTransform>();
            NavBarRect.pivot = new Vector2(0.5f, 1f);
            NavBarRect.anchorMin = new Vector2(0.5f, 1f);
            NavBarRect.anchorMax = new Vector2(0.5f, 1f);
            NavBarRect.sizeDelta = new Vector2(1000f, 35f);

            // UnityExplorer title

            string titleTxt = $"{ExplorerCore.NAME} <i><color=grey>{ExplorerCore.VERSION}</color></i>";
            var title = UIFactory.CreateLabel(navbarPanel, "Title", titleTxt, TextAnchor.MiddleLeft, default, true, 18);
            UIFactory.SetLayoutElement(title.gameObject, minWidth: 240, flexibleWidth: 0);

            // Navbar

            NavbarButtonHolder = UIFactory.CreateUIObject("NavButtonHolder", navbarPanel);
            UIFactory.SetLayoutElement(NavbarButtonHolder, flexibleHeight: 999, flexibleWidth: 999);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(NavbarButtonHolder, true, true, true, true, 4, 2, 2, 2, 2);

            // Hide menu button

            var closeBtn = UIFactory.CreateButton(navbarPanel, "CloseButton", ConfigManager.Main_Menu_Toggle.Value.ToString());
            UIFactory.SetLayoutElement(closeBtn.Component.gameObject, minHeight: 25, minWidth: 80, flexibleWidth: 0);
            RuntimeProvider.Instance.SetColorBlock(closeBtn.Component, new Color(0.63f, 0.32f, 0.31f),
                new Color(0.81f, 0.25f, 0.2f), new Color(0.6f, 0.18f, 0.16f));

            ConfigManager.Main_Menu_Toggle.OnValueChanged += (KeyCode val) => { closeBtn.ButtonText.text = val.ToString(); };

            closeBtn.OnClick += () => { ShowMenu = false; };
        }

        #region UI AssetBundle

        private static void LoadBundle()
        {
            AssetBundle bundle = null;
            try
            {
                bundle = LoadBundle("modern");
                if (bundle == null)
                    bundle = LoadBundle("legacy");
            }
            catch { }

            if (bundle == null)
            {
                ExplorerCore.LogWarning("Could not load the ExplorerUI Bundle!");
                ConsoleFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return;
            }

            BackupShader = bundle.LoadAsset<Shader>("DefaultUI");

            // Fix for games which don't ship with 'UI/Default' shader.
            if (Graphic.defaultGraphicMaterial.shader?.name != "UI/Default")
            {
                ExplorerCore.Log("This game does not ship with the 'UI/Default' shader, using manual Default Shader...");
                Graphic.defaultGraphicMaterial.shader = BackupShader;
            }
            else
                BackupShader = Graphic.defaultGraphicMaterial.shader;

            ConsoleFont = bundle.LoadAsset<Font>("CONSOLA");

            ExplorerCore.Log("Loaded UI AssetBundle");
        }

        private static AssetBundle LoadBundle(string id)
        {
            var stream = typeof(ExplorerCore).Assembly
                .GetManifestResourceStream($"UnityExplorer.Resources.explorerui.{id}.bundle");

            return AssetBundle.LoadFromMemory(ReadFully(stream));
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

        #endregion
    }
}
