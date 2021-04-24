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

        public static GameObject CanvasRoot { get; private set; }
        public static Canvas Canvas { get; private set; }
        public static EventSystem EventSys { get; private set; }

        // panels
        internal static GameObject PanelHolder { get; private set; }

        public static ObjectExplorer Explorer { get; private set; }
        public static InspectorPanel Inspector { get; private set; }
        public static AutoCompleter AutoCompleter { get; private set; }

        private static readonly Dictionary<Panels, Button> navButtonDict = new Dictionary<Panels, Button>();
        internal static readonly Color navButtonEnabledColor = new Color(0.2f, 0.4f, 0.28f);
        internal static readonly Color navButtonDisabledColor = new Color(0.25f, 0.25f, 0.25f);

        // bundle assets
        internal static Font ConsoleFont { get; private set; }
        internal static Shader BackupShader { get; private set; }

        // main menu toggle
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

        public static void Update()
        {
            if (!CanvasRoot)
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

            if (InputManager.GetKeyDown(ConfigManager.Force_Unlock_Keybind.Value))
                CursorUnlocker.Unlock = !CursorUnlocker.Unlock;

            if (EventSystem.current != EventSys)
                CursorUnlocker.SetEventSystem();

            UIPanel.UpdateFocus();
            PanelDragger.UpdateInstances();

            UIBehaviourModel.UpdateInstances();
            AutoCompleter.Update();
        }

        public static UIPanel GetPanel(Panels panel)
        {
            switch (panel)
            {
                case Panels.ObjectExplorer:
                    return Explorer;
                case Panels.Inspector:
                    return Inspector;
                case Panels.AutoCompleter:
                    return AutoCompleter;
                default:
                    throw new NotImplementedException($"TODO GetPanel: {panel}");
            }
        }

        public static void TogglePanel(Panels panel)
        {
            var uiPanel = GetPanel(panel);
            SetPanelActive(panel, !uiPanel.Enabled);
        }

        public static void SetPanelActive(Panels panel, bool active)
        {
            var obj = GetPanel(panel);
            obj.SetActive(active);
            if (active)
            {
                obj.UIRoot.transform.SetAsLastSibling();
                UIPanel.InvokeOnPanelsReordered();
            }

            if (navButtonDict.ContainsKey(panel))
            {
                var color = active ? navButtonEnabledColor : navButtonDisabledColor;
                RuntimeProvider.Instance.SetColorBlock(navButtonDict[panel], color, color * 1.2f);
            }
        }

        internal static void InitUI()
        {
            LoadBundle();

            UIFactory.Init();

            CreateRootCanvas();
            CreateTopNavBar();

            AutoCompleter = new AutoCompleter();
            AutoCompleter.ConstructUI();

            //InspectUnderMouse.ConstructUI();

            Explorer = new ObjectExplorer();
            Explorer.ConstructUI();

            Inspector = new InspectorPanel();
            Inspector.ConstructUI();

            ShowMenu = !ConfigManager.Hide_On_Startup.Value;

            ExplorerCore.Log("UI initialized.");
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

        public static void CreateTopNavBar()
        {
            var panel = UIFactory.CreateUIObject("MainNavbar", CanvasRoot);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(panel, false, true, true, true, 5, 3, 3, 10, 10, TextAnchor.MiddleCenter);
            panel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.sizeDelta = new Vector2(900f, 35f);

            string titleTxt = $"{ExplorerCore.NAME} <i><color=grey>{ExplorerCore.VERSION}</color></i>";
            var title = UIFactory.CreateLabel(panel, "Title", titleTxt, TextAnchor.MiddleLeft, default, true, 18);
            UIFactory.SetLayoutElement(title.gameObject, minWidth: 240, flexibleWidth: 0);

            CreateNavButton(panel, Panels.ObjectExplorer, "Object Explorer");
            CreateNavButton(panel, Panels.Inspector,      "Inspector");
            CreateNavButton(panel, Panels.CSConsole,      "C# Console");
            CreateNavButton(panel, Panels.Options,        "Options");
            CreateNavButton(panel, Panels.ConsoleLog,     "Console Log");

            // close button

            var closeBtn = UIFactory.CreateButton(panel, "CloseButton", "X", () => { ShowMenu = false; });
            UIFactory.SetLayoutElement(closeBtn.gameObject, minHeight: 25, minWidth: 25, flexibleWidth: 0);
            RuntimeProvider.Instance.SetColorBlock(closeBtn, new Color(0.63f, 0.32f, 0.31f),
                new Color(0.81f, 0.25f, 0.2f), new Color(0.6f, 0.18f, 0.16f));
        }

        private static void CreateNavButton(GameObject navbar, Panels panel, string label)
        {
            var button = UIFactory.CreateButton(navbar, $"Button_{panel}", label);
            UIFactory.SetLayoutElement(button.gameObject, minWidth: 118, flexibleWidth: 0);
            RuntimeProvider.Instance.SetColorBlock(button, navButtonDisabledColor, navButtonDisabledColor * 1.2f);
            button.onClick.AddListener(() =>
            {
                TogglePanel(panel);
            });
            navButtonDict.Add(panel, button);
        }

        // Could be cool, need to investigate properly.
        // It works but the input/eventsystem doesnt respond properly or at all.
        //public static void TrySetTargetDisplay(int displayIndex)
        //{
        //    ExplorerCore.Log("displays connected: " + Display.displays.Length);
        //    // Display.displays[0] is the primary, default display and is always ON, so start at index 1.

        //    if (Display.displays.Length > displayIndex)
        //    { 
        //        Display.displays[displayIndex].Activate();
        //        Canvas.targetDisplay = displayIndex;
        //    }
        //}

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
