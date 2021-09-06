using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.Core;
using UnityExplorer.Core.Input;
using UnityExplorer.Core.Runtime;
using UnityExplorer.Inspectors.MouseInspectors;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;

namespace UnityExplorer.Inspectors
{
    public enum MouseInspectMode
    {
        World,
        UI
    }

    public class InspectUnderMouse : UIPanel
    {
        public static InspectUnderMouse Instance { get; private set; }

        private readonly WorldInspector worldInspector;
        private readonly UiInspector uiInspector;

        public static bool Inspecting { get; set; }
        public static MouseInspectMode Mode { get; set; }

        private static Vector3 lastMousePos;

        public MouseInspectorBase CurrentInspector
        {
            get
            {
                switch (Mode)
                {
                    case MouseInspectMode.UI:
                        return uiInspector;
                    case MouseInspectMode.World:
                        return worldInspector;
                }
                return null;
            }
        }

        // UIPanel
        public override string Name => "Inspect Under Mouse";
        public override UIManager.Panels PanelType => UIManager.Panels.MouseInspector;
        public override int MinWidth => -1;
        public override int MinHeight => -1;
        public override bool CanDragAndResize => false;
        public override bool NavButtonWanted => false;
        public override bool ShouldSaveActiveState => false;
        public override bool ShowByDefault => false;

        internal Text objNameLabel;
        internal Text objPathLabel;
        internal Text mousePosLabel;

        public InspectUnderMouse() 
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
            UIManager.MouseInspectDropdown.value = 0;
        }

        public void StartInspect(MouseInspectMode mode)
        {
            Mode = mode;
            Inspecting = true;

            CurrentInspector.OnBeginMouseInspect();

            PanelDragger.ForceEnd();
            UIManager.NavBarRect.gameObject.SetActive(false);
            UIManager.PanelHolder.SetActive(false);

            UIRoot.SetActive(true);
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
            UIManager.PanelHolder.SetActive(true);

            var drop = UIManager.MouseInspectDropdown;
            if (drop.transform.Find("Dropdown List") is Transform list)
                drop.DestroyDropdownList(list.gameObject);

            UIRoot.SetActive(false);
        }

        private static float timeOfLastRaycast;

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

            var mousePos = InputManager.MousePosition;
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
            var inversePos = UIManager.CanvasRoot.transform.InverseTransformPoint(mousePos);
            UIRoot.transform.localPosition = new Vector3(inversePos.x, inversePos.y, 0);
        }

        // UI Construction

        protected internal override void DoSetDefaultPosAndAnchors()
        {
            Rect.anchorMin = Vector2.zero;
            Rect.anchorMax = Vector2.zero;
            Rect.pivot = new Vector2(0.5f, 1);
            Rect.sizeDelta = new Vector2(700, 150);
        }

        public override void ConstructPanelContent()
        {
            // hide title bar
            this.titleBar.SetActive(false);
            this.UIRoot.transform.SetParent(UIManager.CanvasRoot.transform, false);

            var inspectContent = UIFactory.CreateVerticalGroup(this.content, "InspectContent", true, true, true, true, 3, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(inspectContent, flexibleWidth: 9999, flexibleHeight: 9999);

            // Title text

            var title = UIFactory.CreateLabel(inspectContent, 
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
        }

        public override void DoSaveToConfigElement() { }

        public override string GetSaveDataFromConfigManager() => null;
    }
}
