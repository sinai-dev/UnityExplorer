using System.Collections;
using UnityEngine.EventSystems;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;

namespace UnityExplorer.Inspectors.MouseInspectors
{
    public class UiInspector : MouseInspectorBase
    {
        public static readonly List<GameObject> LastHitObjects = new();

        private static GraphicRaycaster[] graphicRaycasters;

        private static readonly List<GameObject> currentHitObjects = new();

        private static readonly List<Graphic> wasDisabledGraphics = new();
        private static readonly List<CanvasGroup> wasDisabledCanvasGroups = new();
        private static readonly List<GameObject> objectsAddedCastersTo = new();

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
            MouseInspectorResultsPanel panel = UIManager.GetPanel<MouseInspectorResultsPanel>(UIManager.Panels.UIInspectorResults);
            panel.SetActive(true);
            panel.ShowResults();
        }

        public override void UpdateMouseInspect(Vector2 mousePos)
        {
            currentHitObjects.Clear();

            PointerEventData ped = new(null)
            {
                position = mousePos
            };

            foreach (GraphicRaycaster gr in graphicRaycasters)
            {
                if (!gr || !gr.canvas)
                    continue;

                List<RaycastResult> list = new();
                RuntimeHelper.GraphicRaycast(gr, ped, list);
                if (list.Count > 0)
                {
                    foreach (RaycastResult hit in list)
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
            foreach (UnityEngine.Object obj in RuntimeHelper.FindObjectsOfTypeAll(typeof(Canvas)))
            {
                Canvas canvas = obj.TryCast<Canvas>();
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
            UnityEngine.Object[] casters = RuntimeHelper.FindObjectsOfTypeAll(typeof(GraphicRaycaster));
            graphicRaycasters = new GraphicRaycaster[casters.Length];
            for (int i = 0; i < casters.Length; i++)
            {
                graphicRaycasters[i] = casters[i].TryCast<GraphicRaycaster>();
            }

            // enable raycastTarget on Graphics
            foreach (UnityEngine.Object obj in RuntimeHelper.FindObjectsOfTypeAll(typeof(Graphic)))
            {
                Graphic graphic = obj.TryCast<Graphic>();
                if (!graphic || !graphic.enabled || graphic.raycastTarget || !graphic.gameObject.activeInHierarchy)
                    continue;
                graphic.raycastTarget = true;
                //ExplorerCore.Log("Enabled raycastTarget on " + graphic.name);
                wasDisabledGraphics.Add(graphic);
            }

            // enable blocksRaycasts on CanvasGroups
            foreach (UnityEngine.Object obj in RuntimeHelper.FindObjectsOfTypeAll(typeof(CanvasGroup)))
            {
                CanvasGroup canvas = obj.TryCast<CanvasGroup>();
                if (!canvas || !canvas.gameObject.activeInHierarchy || canvas.blocksRaycasts)
                    continue;
                canvas.blocksRaycasts = true;
                //ExplorerCore.Log("Enabled raycasts on " + canvas.name);
                wasDisabledCanvasGroups.Add(canvas);
            }
        }

        public override void OnEndInspect()
        {
            foreach (GameObject obj in objectsAddedCastersTo)
            {
                if (obj.GetComponent<GraphicRaycaster>() is GraphicRaycaster raycaster)
                    GameObject.Destroy(raycaster);
            }

            foreach (Graphic graphic in wasDisabledGraphics)
                graphic.raycastTarget = false;

            foreach (CanvasGroup canvas in wasDisabledCanvasGroups)
                canvas.blocksRaycasts = false;

            objectsAddedCastersTo.Clear();
            wasDisabledCanvasGroups.Clear();
            wasDisabledGraphics.Clear();
        }
    }
}
