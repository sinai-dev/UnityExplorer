using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.Helpers;
using UnityExplorer.Input;
using UnityExplorer.UI;
using UnityExplorer.Unstrip;

namespace UnityExplorer.Inspectors
{
    public class MouseInspector
    {
        public enum MouseInspectMode
        {
            World,
            UI
        }

        public static bool Enabled { get; set; }

        public static MouseInspectMode Mode { get; set; }

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

            // recache Graphic Raycasters each time we start
            var casters = ResourcesUnstrip.FindObjectsOfTypeAll(typeof(GraphicRaycaster));
            m_gCasters = new GraphicRaycaster[casters.Length];
            for (int i = 0; i < casters.Length; i++)
            {
#if CPP
                m_gCasters[i] = casters[i].TryCast<GraphicRaycaster>();
#else
                m_gCasters[i] = casters[i] as GraphicRaycaster;
#endif
            }
        }

        public static void StopInspect()
        {
            Enabled = false;
            MainMenu.Instance.MainPanel.SetActive(true);
            s_UIContent.SetActive(false);

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

            if (!UnityHelpers.MainCamera)
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

        internal static void RaycastWorld(Vector2 mousePos)
        {
            var ray = UnityHelpers.MainCamera.ScreenPointToRay(mousePos);
            var casts = Physics.RaycastAll(ray, 1000f);

            if (casts.Length > 0)
            {
                foreach (var cast in casts)
                {
                    if (cast.transform)
                    {
                        var obj = cast.transform.gameObject;

                        OnHitGameObject(obj);

                        break;
                    }
                }
            }
            else
            {
                if (s_lastHit)
                    ClearHitData();
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
            foreach (var gr in m_gCasters)
            {
                gr.Raycast(ped, list);

                if (list.Count > 0)
                {
                    foreach (var hit in list)
                    {
                        if (hit.gameObject)
                        {
                            var obj = hit.gameObject;

                            OnHitGameObject(obj);

                            break;
                        }
                    }
                }
                else
                {
                    if (s_lastHit)
                        ClearHitData();
                }
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
            baseRect.sizeDelta = new Vector2(700, 150);

            var group = content.GetComponent<VerticalLayoutGroup>();
            group.childForceExpandHeight = true;

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
            s_objPathLabel.fontStyle = FontStyle.Italic;
            s_objPathLabel.horizontalOverflow = HorizontalWrapMode.Wrap;

            var pathLayout = pathLabelObj.AddComponent<LayoutElement>();
            pathLayout.minHeight = 75;
            pathLayout.flexibleHeight = 0;

            s_UIContent.SetActive(false);
        }

#endregion
    }
}
