using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExplorerBeta.Input;
using ExplorerBeta.UI.Main;
using ExplorerBeta.UI.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
 
namespace ExplorerBeta.UI
{
    // This class itself is fine.

    public static class UIManager
    {
        public static GameObject CanvasRoot { get; private set; }
        public static EventSystem EventSys { get; private set; }
        public static StandaloneInputModule InputModule { get; private set; }

        public static void Init()
        {
            var res = UIFactory.UIResources = new UIFactory.Resources();
            var bg = CreateSprite(MakeSolidTexture(new Color(0.1f, 0.1f, 0.1f), 1, 1), new Rect(0, 0, 1, 1));
            res.background = bg;

            // Create core UI Canvas and Event System handler
            CreateRootCanvas();

            // Create submodules
            new MainMenu();
        }

        public static void SetEventSystem()
        {
            EventSystem.current = EventSys;
            InputModule.ActivateModule();
        }

        private static GameObject CreateRootCanvas()
        {
            var rootObj = new GameObject("ExplorerCanvas");
            UnityEngine.Object.DontDestroyOnLoad(rootObj);
            rootObj.layer = 5;

            CanvasRoot = rootObj;
            CanvasRoot.transform.position = new Vector3(0f, 0f, 1f);

            EventSys = rootObj.AddComponent<EventSystem>();
            InputModule = rootObj.AddComponent<StandaloneInputModule>();
            InputModule.ActivateModule();

            var canvas = rootObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.referencePixelsPerUnit = 100;
            canvas.sortingOrder = 999;

            var scaler = rootObj.AddComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            rootObj.AddComponent<GraphicRaycaster>();

            return rootObj;
        }

        public static void Update()
        {
            if (EventSys && InputModule)
            {
                if (EventSystem.current != EventSys)
                {
                    ForceUnlockCursor.SetEventSystem();
                    //ForceUnlockCursor.Unlock = true;
                }

                // Fix for games which override the InputModule pointer events (eg, VRChat)
                if (InputModule.m_InputPointerEvent != null)
                {
                    var evt = InputModule.m_InputPointerEvent;
                    if (!evt.eligibleForClick && evt.selectedObject)
                    {
                        evt.eligibleForClick = true;
                    }
                }
            }

            if (PanelDragger.Instance != null)
            {
                PanelDragger.Instance.Update();
            }
        }

        public static void OnSceneChange()
        {
            // todo
        }

        public static Sprite CreateSprite(Texture2D tex, Rect size)
        {
#if CPP
            var pivot = Vector2.zero;
            var border = Vector4.zero;
            return Sprite.CreateSprite_Injected(tex, ref size, ref pivot, 100f, 0u, SpriteMeshType.Tight, ref border, false);
#else
            return Sprite.Create(tex, size, Vector2.zero);
#endif
        }

        public static Texture2D MakeSolidTexture(Color color, int width, int height)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            var tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();

            return tex;
        }
    }
}
