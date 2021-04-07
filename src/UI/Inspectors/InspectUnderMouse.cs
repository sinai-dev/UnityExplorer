using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.Core;
using UnityExplorer.Core.Unity;
using UnityExplorer.Core.Input;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI;
using UnityExplorer.UI.Main;
using UnityExplorer.UI.Inspectors;

namespace UnityExplorer.UI.Main.Home
{
    public class InspectUnderMouse
    {
        public enum MouseInspectMode
        {
            World,
            UI
        }

        public static bool Inspecting { get; set; }

        public static MouseInspectMode Mode { get; set; }

        private static GameObject s_lastHit;
        private static Vector3 s_lastMousePos;

        private static readonly List<Graphic> _wasDisabledGraphics = new List<Graphic>();
        private static readonly List<CanvasGroup> _wasDisabledCanvasGroups = new List<CanvasGroup>();
        private static readonly List<GameObject> _objectsAddedCastersTo = new List<GameObject>();

        internal static Camera MainCamera;
        internal static GraphicRaycaster[] graphicRaycasters;

        public static void Init()
        {
            ConstructUI();
        }

        public static void StartInspect(MouseInspectMode mode)
        {
            MainCamera = Camera.main;
            if (!MainCamera)
                return;

            Mode = mode;
            Inspecting = true;
            MainMenu.Instance.MainPanel.SetActive(false);

            s_UIContent.SetActive(true);

            if (mode == MouseInspectMode.UI)
                SetupUIRaycast();
        }

        internal static void ClearHitData()
        {
            s_lastHit = null;
            s_objNameLabel.text = "No hits...";
            s_objPathLabel.text = "";
        }

        public static void StopInspect()
        {
            Inspecting = false;
            MainMenu.Instance.MainPanel.SetActive(true);
            s_UIContent.SetActive(false);

            if (Mode == MouseInspectMode.UI)
                StopUIInspect();

            ClearHitData();
        }

        public static void UpdateInspect()
        {
            if (InputManager.GetKeyDown(KeyCode.Escape))
            {
                StopInspect();
                return;
            }

            var mousePos = InputManager.MousePosition;

            if (mousePos != s_lastMousePos)
                UpdatePosition(mousePos);

            // actual inspect raycast 

            switch (Mode)
            {
                case MouseInspectMode.UI:
                    RaycastUI(mousePos); break;
                case MouseInspectMode.World:
                    RaycastWorld(mousePos); break;
            }
        }

        internal static void UpdatePosition(Vector2 mousePos)
        {
            s_lastMousePos = mousePos;

            var inversePos = UIManager.CanvasRoot.transform.InverseTransformPoint(mousePos);

            s_mousePosLabel.text = $"<color=grey>Mouse Position:</color> {mousePos.ToString()}";

            float yFix = mousePos.y < 120 ? 80 : -80;
            s_UIContent.transform.localPosition = new Vector3(inversePos.x, inversePos.y + yFix, 0);
        }

        internal static void OnHitGameObject(GameObject obj)
        {
            if (obj != s_lastHit)
            {
                s_lastHit = obj;
                s_objNameLabel.text = $"<b>Click to Inspect:</b> <color=cyan>{obj.name}</color>";
                s_objPathLabel.text = $"Path: {obj.transform.GetTransformPath(true)}";
            }

            if (InputManager.GetMouseButtonDown(0))
            {
                StopInspect();
                InspectorManager.Instance.Inspect(obj);
            }
        }

        // Collider raycasting

        internal static void RaycastWorld(Vector2 mousePos)
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
                if (s_lastHit)
                    ClearHitData();
            }
        }

        // UI Graphic raycasting

        private static void SetupUIRaycast()
        {
            foreach (var obj in RuntimeProvider.Instance.FindObjectsOfTypeAll(typeof(Canvas)))
            {
                var canvas = obj.Cast(typeof(Canvas)) as Canvas;
                if (!canvas || !canvas.enabled || !canvas.gameObject.activeInHierarchy)
                    continue;
                if (!canvas.GetComponent<GraphicRaycaster>())
                {
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                    //ExplorerCore.Log("Added raycaster to " + canvas.name);
                    _objectsAddedCastersTo.Add(canvas.gameObject);
                }
            }

            // recache Graphic Raycasters each time we start
            var casters = RuntimeProvider.Instance.FindObjectsOfTypeAll(typeof(GraphicRaycaster));
            graphicRaycasters = new GraphicRaycaster[casters.Length];
            for (int i = 0; i < casters.Length; i++)
            {
                graphicRaycasters[i] = casters[i].Cast(typeof(GraphicRaycaster)) as GraphicRaycaster;
            }

            // enable raycastTarget on Graphics
            foreach (var obj in RuntimeProvider.Instance.FindObjectsOfTypeAll(typeof(Graphic)))
            {
                var graphic = obj.Cast(typeof(Graphic)) as Graphic;
                if (!graphic || !graphic.enabled || graphic.raycastTarget || !graphic.gameObject.activeInHierarchy)
                    continue;
                graphic.raycastTarget = true;
                //ExplorerCore.Log("Enabled raycastTarget on " + graphic.name);
                _wasDisabledGraphics.Add(graphic);
            }

            // enable blocksRaycasts on CanvasGroups
            foreach (var obj in RuntimeProvider.Instance.FindObjectsOfTypeAll(typeof(CanvasGroup)))
            {
                var canvas = obj.Cast(typeof(CanvasGroup)) as CanvasGroup;
                if (!canvas || !canvas.gameObject.activeInHierarchy || canvas.blocksRaycasts)
                    continue;
                canvas.blocksRaycasts = true;
                //ExplorerCore.Log("Enabled raycasts on " + canvas.name);
                _wasDisabledCanvasGroups.Add(canvas);
            }
        }

        internal static void RaycastUI(Vector2 mousePos)
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
                var list = new List<RaycastResult>();
                RuntimeProvider.Instance.GraphicRaycast(gr, ped, list);

                //gr.Raycast(ped, list);

                if (list.Count > 0)
                {
                    foreach (var hit in list)
                    {
                        // Manual trying to determine which object is "on top".
                        // Not perfect, but not terrible.

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
                    if (s_lastHit)
                        ClearHitData();
                }
            }

            if (hitObject)
                OnHitGameObject(hitObject);

            //ExplorerCore.Log("~~~~~~~~~ end raycast ~~~~~~~~");
        }

        private static void StopUIInspect()
        {
            foreach (var obj in _objectsAddedCastersTo)
            {
                if (obj.GetComponent<GraphicRaycaster>() is GraphicRaycaster raycaster)
                    GameObject.Destroy(raycaster);
            }

            foreach (var graphic in _wasDisabledGraphics)
                graphic.raycastTarget = false;

            foreach (var canvas in _wasDisabledCanvasGroups)
                canvas.blocksRaycasts = false;

            _objectsAddedCastersTo.Clear();
            _wasDisabledCanvasGroups.Clear();
            _wasDisabledGraphics.Clear();
        }

        internal static Text s_objNameLabel;
        internal static Text s_objPathLabel;
        internal static Text s_mousePosLabel;
        internal static GameObject s_UIContent;

        internal static void ConstructUI()
        {
            s_UIContent = UIFactory.CreatePanel("InspectUnderMouse_UI", out GameObject content);

            var baseRect = s_UIContent.GetComponent<RectTransform>();
            var half = new Vector2(0.5f, 0.5f);
            baseRect.anchorMin = half;
            baseRect.anchorMax = half;
            baseRect.pivot = half;
            baseRect.sizeDelta = new Vector2(700, 150);

            var group = content.GetComponent<VerticalLayoutGroup>();
            group.childForceExpandHeight = true;

            // Title text

            UIFactory.CreateLabel(content, "InspectLabel", "<b>Mouse Inspector</b> (press <b>ESC</b> to cancel)", TextAnchor.MiddleCenter);

            s_mousePosLabel = UIFactory.CreateLabel(content, "MousePosLabel", "Mouse Position:", TextAnchor.MiddleCenter);

            s_objNameLabel = UIFactory.CreateLabel(content, "HitLabelObj", "No hits...", TextAnchor.MiddleLeft);
            s_objNameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;

            s_objPathLabel = UIFactory.CreateLabel(content, "PathLabel", "", TextAnchor.MiddleLeft);
            s_objPathLabel.fontStyle = FontStyle.Italic;
            s_objPathLabel.horizontalOverflow = HorizontalWrapMode.Wrap;

            UIFactory.SetLayoutElement(s_objPathLabel.gameObject, minHeight: 75);

            s_UIContent.SetActive(false);
        }
    }
}
