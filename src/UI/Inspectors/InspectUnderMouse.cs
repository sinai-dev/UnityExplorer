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
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;

namespace UnityExplorer.UI.Inspectors
{
    public enum MouseInspectMode
    {
        World,
        UI
    }

    public class InspectUnderMouse : UIPanel
    {
        public static InspectUnderMouse Instance { get; private set; }

        public InspectUnderMouse() { Instance = this; }

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

        // UIPanel
        public override string Name => "Inspect Under Mouse";
        public override UIManager.Panels PanelType => UIManager.Panels.MouseInspector;
        public override int MinWidth => -1;
        public override int MinHeight => -1;
        public override bool CanDragAndResize => false;
        public override bool NavButtonWanted => false;
        public override bool ShouldSaveActiveState => false;
        public override bool ShowByDefault => false;

        internal static Text objNameLabel;
        internal static Text objPathLabel;
        internal static Text mousePosLabel;

        // Mouse Inspector
        public static bool Inspecting { get; set; }
        public static MouseInspectMode Mode { get; set; }

        private static GameObject lastHitObject;
        private static Vector3 lastMousePos;

        private static readonly List<Graphic> wasDisabledGraphics = new List<Graphic>();
        private static readonly List<CanvasGroup> wasDisabledCanvasGroups = new List<CanvasGroup>();
        private static readonly List<GameObject> objectsAddedCastersTo = new List<GameObject>();

        internal static Camera MainCamera;
        internal static GraphicRaycaster[] graphicRaycasters;


        public void StartInspect(MouseInspectMode mode)
        {
            MainCamera = Camera.main;
            if (!MainCamera)
                return;

            PanelDragger.ForceEnd();

            Mode = mode;
            Inspecting = true;
            UIManager.NavBarRect.gameObject.SetActive(false);
            UIManager.PanelHolder.SetActive(false);

            UIRoot.SetActive(true);

            if (mode == MouseInspectMode.UI)
                SetupUIRaycast();
        }

        internal void ClearHitData()
        {
            lastHitObject = null;
            objNameLabel.text = "No hits...";
            objPathLabel.text = "";
        }

        public void StopInspect()
        {
            Inspecting = false;
            UIManager.NavBarRect.gameObject.SetActive(true);
            UIManager.PanelHolder.SetActive(true);
            UIRoot.SetActive(false);

            if (Mode == MouseInspectMode.UI)
                StopUIInspect();

            ClearHitData();
        }

        private static float timeOfLastRaycast;

        public void UpdateInspect()
        {
            if (InputManager.GetKeyDown(KeyCode.Escape))
            {
                StopInspect();
                return;
            }

            if (lastHitObject && InputManager.GetMouseButtonDown(0))
            {
                var target = lastHitObject;
                StopInspect();
                InspectorManager.Inspect(target);
                return;
            }

            var mousePos = InputManager.MousePosition;

            if (mousePos != lastMousePos)
                UpdatePosition(mousePos);

            if (!timeOfLastRaycast.OccuredEarlierThan(0.1f))
                return;

            timeOfLastRaycast = Time.realtimeSinceStartup;

            // actual inspect raycast 

            switch (Mode)
            {
                case MouseInspectMode.UI:
                    RaycastUI(mousePos); break;
                case MouseInspectMode.World:
                    RaycastWorld(mousePos); break;
            }
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

        internal void OnHitGameObject(GameObject obj)
        {
            if (obj != lastHitObject)
            {
                lastHitObject = obj;
                objNameLabel.text = $"<b>Click to Inspect:</b> <color=cyan>{obj.name}</color>";
                objPathLabel.text = $"Path: {obj.transform.GetTransformPath(true)}";
            }
        }

        // Collider raycasting

        internal void RaycastWorld(Vector2 mousePos)
        {
            var ray = MainCamera.ScreenPointToRay(mousePos);
            Physics.Raycast(ray, out RaycastHit hit, 1000f);

            if (hit.transform)
            {
                var obj = hit.transform.gameObject;
                OnHitGameObject(obj);
            }
            else
            {
                if (lastHitObject)
                    ClearHitData();
            }
        }

        // UI Graphic raycasting

        private static void SetupUIRaycast()
        {
            foreach (var obj in RuntimeProvider.Instance.FindObjectsOfTypeAll(typeof(Canvas)))
            {
                var canvas = obj.TryCast<Canvas>();
                if (!canvas || !canvas.enabled || !canvas.gameObject.activeInHierarchy)
                    continue;
                if (!canvas.GetComponent<GraphicRaycaster>())
                {
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                    //ExplorerCore.Log("Added raycaster to " + canvas.name);
                    objectsAddedCastersTo.Add(canvas.gameObject);
                }
            }

            // recache Graphic Raycasters each time we start
            var casters = RuntimeProvider.Instance.FindObjectsOfTypeAll(typeof(GraphicRaycaster));
            graphicRaycasters = new GraphicRaycaster[casters.Length];
            for (int i = 0; i < casters.Length; i++)
            {
                graphicRaycasters[i] = casters[i].TryCast<GraphicRaycaster>();
            }

            // enable raycastTarget on Graphics
            foreach (var obj in RuntimeProvider.Instance.FindObjectsOfTypeAll(typeof(Graphic)))
            {
                var graphic = obj.TryCast<Graphic>();
                if (!graphic || !graphic.enabled || graphic.raycastTarget || !graphic.gameObject.activeInHierarchy)
                    continue;
                graphic.raycastTarget = true;
                //ExplorerCore.Log("Enabled raycastTarget on " + graphic.name);
                wasDisabledGraphics.Add(graphic);
            }

            // enable blocksRaycasts on CanvasGroups
            foreach (var obj in RuntimeProvider.Instance.FindObjectsOfTypeAll(typeof(CanvasGroup)))
            {
                var canvas = obj.TryCast<CanvasGroup>();
                if (!canvas || !canvas.gameObject.activeInHierarchy || canvas.blocksRaycasts)
                    continue;
                canvas.blocksRaycasts = true;
                //ExplorerCore.Log("Enabled raycasts on " + canvas.name);
                wasDisabledCanvasGroups.Add(canvas);
            }
        }

        internal void RaycastUI(Vector2 mousePos)
        {
            var ped = new PointerEventData(null)
            {
                position = mousePos
            };

            //ExplorerCore.Log("~~~~~~~~~ begin raycast ~~~~~~~~");
            GameObject hitObject = null;
            int highestLayer = int.MinValue;
            int highestOrder = int.MinValue;
            int highestDepth = int.MinValue;
            foreach (var gr in graphicRaycasters)
            {
                if (!gr || !gr.canvas)
                    continue;

                var list = new List<RaycastResult>();
                RuntimeProvider.Instance.GraphicRaycast(gr, ped, list);

                if (list.Count > 0)
                {
                    foreach (var hit in list)
                    {
                        // Manualy trying to determine which object is "on top".
                        // Could be improved, but seems to work pretty well and isn't as laggy as you would expect.

                        if (!hit.gameObject)
                            continue;

                        if (hit.gameObject.GetComponent<CanvasGroup>() is CanvasGroup group && group.alpha == 0)
                            continue;

                        if (hit.gameObject.GetComponent<Graphic>() is Graphic graphic && graphic.color.a == 0f)
                            continue;

                        if (hit.sortingLayer < highestLayer)
                            continue;

                        if (hit.sortingLayer > highestLayer)
                        {
                            highestLayer = hit.sortingLayer;
                            highestDepth = int.MinValue;
                        }

                        if (hit.depth < highestDepth)
                            continue;

                        if (hit.depth > highestDepth)
                        {
                            highestDepth = hit.depth;
                            highestOrder = int.MinValue;
                        }

                        if (hit.sortingOrder <= highestOrder)
                            continue;

                        highestOrder = hit.sortingOrder;
                        hitObject = hit.gameObject;
                    }
                }
                else
                {
                    if (lastHitObject)
                        ClearHitData();
                }
            }

            if (hitObject)
                OnHitGameObject(hitObject);

            //ExplorerCore.Log("~~~~~~~~~ end raycast ~~~~~~~~");
        }

        private static void StopUIInspect()
        {
            foreach (var obj in objectsAddedCastersTo)
            {
                if (obj.GetComponent<GraphicRaycaster>() is GraphicRaycaster raycaster)
                    GameObject.Destroy(raycaster);
            }

            foreach (var graphic in wasDisabledGraphics)
                graphic.raycastTarget = false;

            foreach (var canvas in wasDisabledCanvasGroups)
                canvas.blocksRaycasts = false;

            objectsAddedCastersTo.Clear();
            wasDisabledCanvasGroups.Clear();
            wasDisabledGraphics.Clear();
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

            var title = UIFactory.CreateLabel(inspectContent, "InspectLabel", "<b>Mouse Inspector</b> (press <b>ESC</b> to cancel)", TextAnchor.MiddleCenter);
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
