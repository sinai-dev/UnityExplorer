using System.Collections;
using UnityEngine;
using UnityExplorer.Config;
using UniverseLib;
using UniverseLib.Input;

namespace UnityExplorer.UI
{
    public static class DisplayManager
    {
        public static int ActiveDisplayIndex { get; private set; }
        public static Display ActiveDisplay => Display.displays[ActiveDisplayIndex];

        public static int Width => ActiveDisplay.renderingWidth;
        public static int Height => ActiveDisplay.renderingHeight;

        public static Vector3 MousePosition => Application.isEditor
            ? InputManager.MousePosition
            : Display.RelativeMouseAt(InputManager.MousePosition);

        public static bool MouseInTargetDisplay => MousePosition.z == ActiveDisplayIndex;

        private static Camera canvasCamera;

        internal static void Init()
        {
            SetDisplay(ConfigManager.Target_Display.Value);
            ConfigManager.Target_Display.OnValueChanged += SetDisplay;
        }

        public static void SetDisplay(int display)
        {
            if (ActiveDisplayIndex == display)
                return;

            if (Display.displays.Length <= display)
            {
                ExplorerCore.LogWarning($"Cannot set display index to {display} as there are not enough monitors connected!");

                if (ConfigManager.Target_Display.Value == display)
                    ConfigManager.Target_Display.Value = 0;

                return;
            }

            ActiveDisplayIndex = display;
            ActiveDisplay.Activate();

            UIManager.UICanvas.targetDisplay = display;

            // ensure a camera is targeting the display
            if (!Camera.main || Camera.main.targetDisplay != display)
            {
                if (!canvasCamera)
                {
                    canvasCamera = new GameObject("UnityExplorer_CanvasCamera").AddComponent<Camera>();
                    GameObject.DontDestroyOnLoad(canvasCamera.gameObject);
                    canvasCamera.hideFlags = HideFlags.HideAndDontSave;
                }
                canvasCamera.targetDisplay = display;
            }

            RuntimeHelper.StartCoroutine(FixPanels());
        }

        private static IEnumerator FixPanels()
        {
            yield return null;
            yield return null;

            foreach (Panels.UEPanel panel in UIManager.UIPanels.Values)
            {
                panel.EnsureValidSize();
                panel.EnsureValidPosition();
                panel.Dragger.OnEndResize();
            }
        }
    }
}
