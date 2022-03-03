using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UniverseLib;
using UniverseLib.Utility;

namespace UnityExplorer.Inspectors.MouseInspectors
{
    public class WorldInspector : MouseInspectorBase
    {
        private static Camera MainCamera;
        private static GameObject lastHitObject;

        public override void OnBeginMouseInspect()
        {
            MainCamera = Camera.main;

            if (!MainCamera)
            {
                ExplorerCore.LogWarning("No MainCamera found! Cannot inspect world!");
                return;
            }
        }

        public override void ClearHitData()
        {
            lastHitObject = null;
        }

        public override void OnSelectMouseInspect()
        {
            InspectorManager.Inspect(lastHitObject);
        }

        public override void UpdateMouseInspect(Vector2 mousePos)
        {
            if (!MainCamera)
                MainCamera = Camera.main;
            if (!MainCamera)
            {
                ExplorerCore.LogWarning("No Main Camera was found, unable to inspect world!");
                MouseInspector.Instance.StopInspect();
                return;
            }

            var ray = MainCamera.ScreenPointToRay(mousePos);
            Physics.Raycast(ray, out RaycastHit hit, 1000f);

            if (hit.transform)
                OnHitGameObject(hit.transform.gameObject);
            else if (lastHitObject)
                MouseInspector.Instance.ClearHitData();
        }

        internal void OnHitGameObject(GameObject obj)
        {
            if (obj != lastHitObject)
            {
                lastHitObject = obj;
                MouseInspector.Instance.objNameLabel.text = $"<b>Click to Inspect:</b> <color=cyan>{obj.name}</color>";
                MouseInspector.Instance.objPathLabel.text = $"Path: {obj.transform.GetTransformPath(true)}";
            }
        }

        public override void OnEndInspect()
        {
            // not needed
        }
    }
}
