using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;
using UniverseLib;
using UniverseLib.Input;

namespace UnityExplorer.Inspectors.MouseInspectors
{
    public class UiInspector : MouseInspectorBase
    {
        public static readonly List<GameObject> LastHitObjects = new List<GameObject>();

        private static GraphicRaycaster[] graphicRaycasters;

        private static readonly List<GameObject> currentHitObjects = new List<GameObject>();

        private static readonly List<Graphic> wasDisabledGraphics = new List<Graphic>();
        private static readonly List<CanvasGroup> wasDisabledCanvasGroups = new List<CanvasGroup>();
        private static readonly List<GameObject> objectsAddedCastersTo = new List<GameObject>();

        public override void OnBeginMouseInspect()
        {
            SetupUIRaycast();
            MouseInspector.Instance.objPathLabel.text = "";
        }

        public override void ClearHitData()
        {
            currentHitObjects.Clear();
        }

        public override void OnSelectMouseInspect()
        {
            LastHitObjects.Clear();
            LastHitObjects.AddRange(currentHitObjects);
            RuntimeHelper.StartCoroutine(SetPanelActiveCoro());
        }

        IEnumerator SetPanelActiveCoro()
        {
            yield return null;
            var panel = UIManager.GetPanel<UiInspectorResultsPanel>(UIManager.Panels.UIInspectorResults);
            panel.SetActive(true);
            panel.ShowResults();
        }

        public override void UpdateMouseInspect(Vector2 mousePos)
        {
            currentHitObjects.Clear();

            var ped = new PointerEventData(null)
            {
                position = mousePos
            };

            foreach (var gr in graphicRaycasters)
            {
                if (!gr || !gr.canvas)
                    continue;
            
                var list = new List<RaycastResult>();
                RuntimeHelper.GraphicRaycast(gr, ped, list);
                if (list.Count > 0)
                {
                    foreach (var hit in list)
                    {
                        if (hit.gameObject)
                            currentHitObjects.Add(hit.gameObject);
                    }
                }
            }
            
            if (currentHitObjects.Any())
                MouseInspector.Instance.objNameLabel.text = $"Click to view UI Objects under mouse: {currentHitObjects.Count}";
            else
                MouseInspector.Instance.objNameLabel.text = $"No UI objects under mouse.";
        }

        private static void SetupUIRaycast()
        {
            foreach (var obj in RuntimeHelper.FindObjectsOfTypeAll(typeof(Canvas)))
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
            var casters = RuntimeHelper.FindObjectsOfTypeAll(typeof(GraphicRaycaster));
            graphicRaycasters = new GraphicRaycaster[casters.Length];
            for (int i = 0; i < casters.Length; i++)
            {
                graphicRaycasters[i] = casters[i].TryCast<GraphicRaycaster>();
            }

            // enable raycastTarget on Graphics
            foreach (var obj in RuntimeHelper.FindObjectsOfTypeAll(typeof(Graphic)))
            {
                var graphic = obj.TryCast<Graphic>();
                if (!graphic || !graphic.enabled || graphic.raycastTarget || !graphic.gameObject.activeInHierarchy)
                    continue;
                graphic.raycastTarget = true;
                //ExplorerCore.Log("Enabled raycastTarget on " + graphic.name);
                wasDisabledGraphics.Add(graphic);
            }

            // enable blocksRaycasts on CanvasGroups
            foreach (var obj in RuntimeHelper.FindObjectsOfTypeAll(typeof(CanvasGroup)))
            {
                var canvas = obj.TryCast<CanvasGroup>();
                if (!canvas || !canvas.gameObject.activeInHierarchy || canvas.blocksRaycasts)
                    continue;
                canvas.blocksRaycasts = true;
                //ExplorerCore.Log("Enabled raycasts on " + canvas.name);
                wasDisabledCanvasGroups.Add(canvas);
            }
        }

        public override void OnEndInspect()
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
    }
}
