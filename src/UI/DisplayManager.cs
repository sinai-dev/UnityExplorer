using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExplorer.Config;
using UniverseLib.Input;

namespace UnityExplorer.UI
{
    public static class DisplayManager
    {
        public static int ActiveDisplayIndex { get; private set; }
        public static Display ActiveDisplay => Display.displays[ActiveDisplayIndex];

        private static Camera canvasCamera;

        internal static void Init()
        {
            SetDisplay(ConfigManager.Target_Display.Value);
            ConfigManager.Target_Display.OnValueChanged += SetDisplay;
        }

        public static Vector3 MousePosition => Display.RelativeMouseAt(InputManager.MousePosition);

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
        }
    }
}
