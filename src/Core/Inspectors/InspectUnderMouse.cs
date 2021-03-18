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

        public static void StartInspect()
        {
            Enabled = true;
            MainMenu.Instance.MainPanel.SetActive(false);
            
            UI.s_UIContent.SetActive(true);

            // recache Graphic Raycasters each time we start
            var casters = RuntimeProvider.Instance.FindObjectsOfTypeAll(typeof(GraphicRaycaster));
            m_gCasters = new GraphicRaycaster[casters.Length];
            for (int i = 0; i < casters.Length; i++)
            {
                m_gCasters[i] = casters[i].Cast(typeof(GraphicRaycaster)) as GraphicRaycaster;
            }
        }

        public static void StopInspect()
        {
            Enabled = false;
            MainMenu.Instance.MainPanel.SetActive(true);
            UI.s_UIContent.SetActive(false);

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

            UI.s_mousePosLabel.text = $"<color=grey>Mouse Position:</color> {mousePos.ToString()}";

            float yFix = mousePos.y < 120 ? 80 : -80;
            UI.s_UIContent.transform.localPosition = new Vector3(inversePos.x, inversePos.y + yFix, 0);
        }

        internal static void ClearHitData()
        {
            s_lastHit = null;
            UI.s_objNameLabel.text = "No hits...";
            UI.s_objPathLabel.text = "";
        }
    }
}
