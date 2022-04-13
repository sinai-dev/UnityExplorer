using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Config;
using UnityExplorer.Inspectors.MouseInspectors;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;
using UniverseLib.Input;
using UniverseLib.UI;
using UniverseLib.UI.Panels;
using UniverseLib.Utility;

namespace UnityExplorer.Inspectors
{
    public enum MouseInspectMode
    {
        World,
        UI
    }

    public class MouseInspector : UEPanel
    {
        public static MouseInspector Instance { get; private set; }

        private readonly WorldInspector worldInspector;
        private readonly UiInspector uiInspector;

        public static bool Inspecting { get; set; }
        public static MouseInspectMode Mode { get; set; }

        public MouseInspectorBase CurrentInspector => Mode switch
        {
            MouseInspectMode.UI => uiInspector,
            MouseInspectMode.World => worldInspector,
            _ => null,
        };

        private static Vector3 lastMousePos;

        // UIPanel
        internal static readonly string UIBaseGUID = $"{ExplorerCore.GUID}.MouseInspector";
        private UIBase inspectorUIBase;

        public override string Name => "Inspect Under Mouse";
        public override UIManager.Panels PanelType => UIManager.Panels.MouseInspector;
        public override int MinWidth => -1;
        public override int MinHeight => -1;
        public override Vector2 DefaultAnchorMin => Vector2.zero;
        public override Vector2 DefaultAnchorMax => Vector2.zero;

        public override bool CanDragAndResize => false;
        public override bool NavButtonWanted => false;
        public override bool ShouldSaveActiveState => false;
        public override bool ShowByDefault => false;

        internal Text objNameLabel;
        internal Text objPathLabel;
        internal Text mousePosLabel;

        public MouseInspector(UIBase owner) : base(owner)
        {
            Instance = this;
            worldInspector = new WorldInspector();
            uiInspector = new UiInspector();
        }

        public static void OnDropdownSelect(int index)
        {
            switch (index)
            {
                case 0: return;
                case 1: Instance.StartInspect(MouseInspectMode.World); break;
                case 2: Instance.StartInspect(MouseInspectMode.UI); break;
            }
            InspectorPanel.Instance.MouseInspectDropdown.value = 0;
        }

        public void StartInspect(MouseInspectMode mode)
        {
            Mode = mode;
            Inspecting = true;

            CurrentInspector.OnBeginMouseInspect();

            PanelManager.ForceEndResize();
            UIManager.NavBarRect.gameObject.SetActive(false);
            UIManager.UiBase.Panels.PanelHolder.SetActive(false);
            UIManager.UiBase.SetOnTop();

            SetActive(true);
        }

        internal void ClearHitData()
        {
            CurrentInspector.ClearHitData();

            objNameLabel.text = "No hits...";
            objPathLabel.text = "";
        }

        public void StopInspect()
        {
            CurrentInspector.OnEndInspect();
            ClearHitData();
            Inspecting = false;

            UIManager.NavBarRect.gameObject.SetActive(true);
            UIManager.UiBase.Panels.PanelHolder.SetActive(true);

            Dropdown drop = InspectorPanel.Instance.MouseInspectDropdown;
            if (drop.transform.Find("Dropdown List") is Transform list)
                drop.DestroyDropdownList(list.gameObject);

            UIRoot.SetActive(false);
        }

        private static float timeOfLastRaycast;

        public bool TryUpdate()
        {
            if (ConfigManager.World_MouseInspect_Keybind.Value != KeyCode.None)
            {
                if (InputManager.GetKeyDown(ConfigManager.World_MouseInspect_Keybind.Value))
                    Instance.StartInspect(MouseInspectMode.World);
            }

            if (ConfigManager.World_MouseInspect_Keybind.Value != KeyCode.None)
            {
                if (InputManager.GetKeyDown(ConfigManager.World_MouseInspect_Keybind.Value))
                    Instance.StartInspect(MouseInspectMode.World);
            }

            if (Inspecting)
                UpdateInspect();

            return Inspecting;
        }

        public void UpdateInspect()
        {
            if (InputManager.GetKeyDown(KeyCode.Escape))
            {
                StopInspect();
                return;
            }

            if (InputManager.GetMouseButtonDown(0))
            {
                CurrentInspector.OnSelectMouseInspect();
                StopInspect();
                return;
            }

            Vector3 mousePos = InputManager.MousePosition;
            if (mousePos != lastMousePos)
                UpdatePosition(mousePos);

            if (!timeOfLastRaycast.OccuredEarlierThan(0.1f))
                return;
            timeOfLastRaycast = Time.realtimeSinceStartup;

            CurrentInspector.UpdateMouseInspect(mousePos);
        }

        internal void UpdatePosition(Vector2 mousePos)
        {
            lastMousePos = mousePos;

            // use the raw mouse pos for the label
            mousePosLabel.text = $"<color=grey>Mouse Position:</color> {mousePos.ToString()}";

            // constrain the mouse pos we use within certain bounds
            if (mousePos.x < 350)
                mousePos.x = 350;
            if (mousePos.x > Screen.width - 350)
                mousePos.x = Screen.width - 350;
            if (mousePos.y < Rect.rect.height)
                mousePos.y += Rect.rect.height + 10;
            else
                mousePos.y -= 10;

            // calculate and set our UI position
            Vector3 inversePos = inspectorUIBase.RootObject.transform.InverseTransformPoint(mousePos);
            UIRoot.transform.localPosition = new Vector3(inversePos.x, inversePos.y, 0);
        }

        // UI Construction

        public override void SetDefaultSizeAndPosition()
        {
            base.SetDefaultSizeAndPosition();

            Rect.anchorMin = Vector2.zero;
            Rect.anchorMax = Vector2.zero;
            Rect.pivot = new Vector2(0.5f, 1);
            Rect.sizeDelta = new Vector2(700, 150);
        }

        protected override void ConstructPanelContent()
        {
            // hide title bar
            this.TitleBar.SetActive(false);
            this.UIRoot.transform.SetParent(UIManager.UIRoot.transform, false);

            GameObject inspectContent = UIFactory.CreateVerticalGroup(this.ContentRoot, "InspectContent", true, true, true, true, 3, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(inspectContent, flexibleWidth: 9999, flexibleHeight: 9999);

            // Title text

            Text title = UIFactory.CreateLabel(inspectContent,
                "InspectLabel",
                "<b>Mouse Inspector</b> (press <b>ESC</b> to cancel)",
                TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(title.gameObject, flexibleWidth: 9999);

            mousePosLabel = UIFactory.CreateLabel(inspectContent, "MousePosLabel", "Mouse Position:", TextAnchor.MiddleCenter);

            objNameLabel = UIFactory.CreateLabel(inspectContent, "HitLabelObj", "No hits...", TextAnchor.MiddleLeft);
            objNameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;

            objPathLabel = UIFactory.CreateLabel(inspectContent, "PathLabel", "", TextAnchor.MiddleLeft);
            objPathLabel.fontStyle = FontStyle.Italic;
            objPathLabel.horizontalOverflow = HorizontalWrapMode.Wrap;

            UIFactory.SetLayoutElement(objPathLabel.gameObject, minHeight: 75);

            UIRoot.SetActive(false);

            // Create a new canvas for this panel to live on.
            // It needs to always be shown on the main display, other panels can move displays.

            inspectorUIBase = UniversalUI.RegisterUI(UIBaseGUID, null);
            UIRoot.transform.SetParent(inspectorUIBase.RootObject.transform);
        }
    }
}
