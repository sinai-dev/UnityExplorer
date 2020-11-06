using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.Inspectors;

namespace UnityExplorer.UI
{
    public static class UIManager
    {
        public static GameObject CanvasRoot { get; private set; }
        public static EventSystem EventSys { get; private set; }
        public static StandaloneInputModule InputModule { get; private set; }

        public static void Init()
        {
            // Create core UI Canvas and Event System handler
            CreateRootCanvas();

            // Create submodules
            new MainMenu();

            MouseInspector.ConstructUI();

            // Force refresh of anchors
            Canvas.ForceUpdateCanvases();

            CanvasRoot.SetActive(false);
            CanvasRoot.SetActive(true);
        }

        public static void SetEventSystem()
        {
            EventSystem.current = EventSys;
            InputModule.ActivateModule();
        }

        private static GameObject CreateRootCanvas()
        {
            GameObject rootObj = new GameObject("ExplorerCanvas");
            UnityEngine.Object.DontDestroyOnLoad(rootObj);
            rootObj.layer = 5;

            CanvasRoot = rootObj;
            CanvasRoot.transform.position = new Vector3(0f, 0f, 1f);

            EventSys = rootObj.AddComponent<EventSystem>();
            InputModule = rootObj.AddComponent<StandaloneInputModule>();
            InputModule.ActivateModule();

            Canvas canvas = rootObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.referencePixelsPerUnit = 100;
            canvas.sortingOrder = 999;
            canvas.pixelPerfect = false;

            CanvasScaler scaler = rootObj.AddComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            rootObj.AddComponent<GraphicRaycaster>();

            return rootObj;
        }

        public static void Update()
        {
            MainMenu.Instance?.Update();

            if (EventSys && InputModule)
            {
                if (EventSystem.current != EventSys)
                {
                    ForceUnlockCursor.SetEventSystem();
                    //ForceUnlockCursor.Unlock = true;
                }

                // Fix for games which override the InputModule pointer events (eg, VRChat)
#if CPP
                if (InputModule.m_InputPointerEvent != null)
                {
                    PointerEventData evt = InputModule.m_InputPointerEvent;
                    if (!evt.eligibleForClick && evt.selectedObject)
                    {
                        evt.eligibleForClick = true;
                    }
                }
#endif
            }

            if (PanelDragger.Instance != null)
            {
                PanelDragger.Instance.Update();
            }

            foreach (var slider in SliderScrollbar.Instances)
            {
                if (slider.m_slider.gameObject.activeInHierarchy)
                {
                    slider.Update();
                }
            }
        }

        public static void OnSceneChange()
        {
            // todo
            SceneExplorer.Instance?.OnSceneChange();
        }

        public static Sprite CreateSprite(Texture2D tex, Rect size = default)
        {
#if CPP
            Vector2 pivot = Vector2.zero;
            Vector4 border = Vector4.zero;

            if (size == default)
            {
                size = new Rect(0, 0, tex.width, tex.height);
            }

            return Sprite.CreateSprite_Injected(tex, ref size, ref pivot, 100f, 0u, SpriteMeshType.Tight, ref border, false);
#else
            return Sprite.Create(tex, size, Vector2.zero);
#endif
        }

        public static Texture2D MakeSolidTexture(Color color, int width, int height)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();

            return tex;
        }
    }
}
