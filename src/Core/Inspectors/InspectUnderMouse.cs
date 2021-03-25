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
using UnityExplorer.UI.Main.Home.Inspectors;

namespace UnityExplorer.Core.Inspectors
{
    public class InspectUnderMouse
    {
        public enum MouseInspectMode
        {
            World,
            UI
        }

        public static bool Enabled { get; set; }

        public static MouseInspectMode Mode { get; set; }

        private static GameObject s_lastHit;
        private static Vector3 s_lastMousePos;

        internal static MouseInspectorUI UI;

        static InspectUnderMouse()
        {
            UI = new MouseInspectorUI();
        }

        private static readonly List<Graphic> _wasDisabledGraphics = new List<Graphic>();
        private static readonly List<CanvasGroup> _wasDisabledCanvasGroups = new List<CanvasGroup>();
        private static readonly List<GameObject> _objectsAddedCastersTo = new List<GameObject>();

        public static void StartInspect(MouseInspectMode mode)
        {
            Mode = mode;
            Enabled = true;
            MainMenu.Instance.MainPanel.SetActive(false);
            
            UI.s_UIContent.SetActive(true);

            if (mode == MouseInspectMode.UI)
            {
                SetupUIRaycast();
            }
        }

        internal static void ClearHitData()
        {
            s_lastHit = null;
            UI.s_objNameLabel.text = "No hits...";
            UI.s_objPathLabel.text = "";
        }

        public static void StopInspect()
        {
            Enabled = false;
            MainMenu.Instance.MainPanel.SetActive(true);
            UI.s_UIContent.SetActive(false);

            if (Mode == MouseInspectMode.UI)
                StopUIInspect();

            ClearHitData();
        }

        internal static GraphicRaycaster[] m_gCasters;

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

            if (!UnityHelper.MainCamera)
                return;

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

            UI.s_mousePosLabel.text = $"<color=grey>Mouse Position:</color> {mousePos.ToString()}";

            float yFix = mousePos.y < 120 ? 80 : -80;
            UI.s_UIContent.transform.localPosition = new Vector3(inversePos.x, inversePos.y + yFix, 0);
        }

        internal static void OnHitGameObject(GameObject obj)
        {
            if (obj != s_lastHit)
            {
                s_lastHit = obj;
                UI.s_objNameLabel.text = $"<b>Click to Inspect:</b> <color=cyan>{obj.name}</color>";
                UI.s_objPathLabel.text = $"Path: {obj.transform.GetTransformPath(true)}";
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
            var ray = UnityHelper.MainCamera.ScreenPointToRay(mousePos);
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
            m_gCasters = new GraphicRaycaster[casters.Length];
            for (int i = 0; i < casters.Length; i++)
            {
                m_gCasters[i] = casters[i].Cast(typeof(GraphicRaycaster)) as GraphicRaycaster;
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

#if MONO
            var list = new List<RaycastResult>();
#else
            var list = new Il2CppSystem.Collections.Generic.List<RaycastResult>();
#endif
            //ExplorerCore.Log("~~~~~~~~~ begin raycast ~~~~~~~~");
            GameObject hitObject = null;
            int highestLayer = int.MinValue;
            int highestOrder = int.MinValue;
            int highestDepth = int.MinValue;
            foreach (var gr in m_gCasters)
            {
                gr.Raycast(ped, list);

                if (list.Count > 0)
                {
                    foreach (var hit in list)
                    {
                        if (!hit.gameObject)
                            continue;

                        if (hit.gameObject.GetComponent<CanvasGroup>() is CanvasGroup group && group.alpha == 0)
                            continue;

                        if (hit.gameObject.GetComponent<Graphic>() is Graphic graphic && graphic.color.a == 0f)
                            continue;

                        //ExplorerCore.Log("Hit: " + hit.gameObject.name + ", depth: " + hit.depth + ", layer: " + hit.sortingLayer + ", order: " + hit.sortingOrder);

                        if (hit.sortingLayer < highestLayer)
                            continue;

                        if (hit.sortingLayer > highestLayer)
                        {
                            highestLayer = hit.sortingLayer;
                            highestOrder = int.MinValue;
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
    }
}
