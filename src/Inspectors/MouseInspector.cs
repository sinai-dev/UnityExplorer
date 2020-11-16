using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Helpers;
using UnityExplorer.Input;
using UnityExplorer.UI;

namespace UnityExplorer.Inspectors
{
    public class MouseInspector
    {
        public static bool Enabled { get; set; }

        //internal static Text s_objUnderMouseName;
        internal static Text s_objNameLabel;
        internal static Text s_objPathLabel;
        internal static Text s_mousePosLabel;

        private static GameObject s_lastHit;
        private static Vector3 s_lastMousePos;

        internal static GameObject s_UIContent;

        public static void StartInspect()
        {
            Enabled = true;
            MainMenu.Instance.MainPanel.SetActive(false);
            s_UIContent.SetActive(true);
        }

        public static void StopInspect()
        {
            Enabled = false;
            MainMenu.Instance.MainPanel.SetActive(true);
            s_UIContent.SetActive(false);

            ClearHitData();
        }

        public static void UpdateInspect()
        {
            if (InputManager.GetKeyDown(KeyCode.Escape))
            {
                StopInspect();
            }

            var mousePos = InputManager.MousePosition;

            if (mousePos != s_lastMousePos)
            {
                s_lastMousePos = mousePos;

                var inversePos = UIManager.CanvasRoot.transform.InverseTransformPoint(mousePos);

                s_mousePosLabel.text = $"<color=grey>Mouse Position:</color> {((Vector2)InputManager.MousePosition).ToString()}";

                float yFix = mousePos.y < 120 ? 80 : -80;

                s_UIContent.transform.localPosition = new Vector3(inversePos.x, inversePos.y + yFix, 0);
            }

            if (!UnityHelpers.MainCamera)
                return;

            // actual inspect raycast 
            var ray = UnityHelpers.MainCamera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                var obj = hit.transform.gameObject;

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
            else
            {
                if (s_lastHit)
                    ClearHitData();
            }
        }

        internal static void ClearHitData()
        {
            s_lastHit = null;
            s_objNameLabel.text = "No hits...";
            s_objPathLabel.text = "";
        }

        #region UI Construction

        internal static void ConstructUI()
        {
            s_UIContent = UIFactory.CreatePanel(UIManager.CanvasRoot, "MouseInspect", out GameObject content);

            s_UIContent.AddComponent<Mask>();

            var baseRect = s_UIContent.GetComponent<RectTransform>();
            var half = new Vector2(0.5f, 0.5f);
            baseRect.anchorMin = half;
            baseRect.anchorMax = half;
            baseRect.pivot = half;
            baseRect.sizeDelta = new Vector2(700, 100);

            // Title text

            var titleObj = UIFactory.CreateLabel(content, TextAnchor.MiddleCenter);
            var titleText = titleObj.GetComponent<Text>();
            titleText.text = "<b>Mouse Inspector</b> (press <b>ESC</b> to cancel)";

            var mousePosObj = UIFactory.CreateLabel(content, TextAnchor.MiddleCenter);
            s_mousePosLabel = mousePosObj.GetComponent<Text>();
            s_mousePosLabel.text = "Mouse Position:";

            var hitLabelObj = UIFactory.CreateLabel(content, TextAnchor.MiddleLeft);
            s_objNameLabel = hitLabelObj.GetComponent<Text>();
            s_objNameLabel.text = "No hits...";
            s_objNameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;

            var pathLabelObj = UIFactory.CreateLabel(content, TextAnchor.MiddleLeft);
            s_objPathLabel = pathLabelObj.GetComponent<Text>();
            s_objPathLabel.color = Color.grey;
            s_objPathLabel.fontStyle = FontStyle.Italic;
            s_objPathLabel.horizontalOverflow = HorizontalWrapMode.Overflow;

            s_UIContent.SetActive(false);
        }

        #endregion
    }
}
