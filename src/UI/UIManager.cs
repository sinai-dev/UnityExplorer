using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.Inspectors;
using UnityExplorer.UI.PageModel;
using System.IO;
using TMPro;
using System.Reflection;
using UnityExplorer.Helpers;
#if CPP
using UnityExplorer.Unstrip.AssetBundle;
#endif

namespace UnityExplorer.UI
{
    public static class UIManager
    {
        public static GameObject CanvasRoot { get; private set; }
        public static EventSystem EventSys { get; private set; }
        public static StandaloneInputModule InputModule { get; private set; }

        public static TMP_FontAsset ConsoleFont { get; private set; }

        public static void Init()
        {
            var bundlePath = ExplorerCore.EXPLORER_FOLDER + @"\tmp.bundle";
            if (File.Exists(bundlePath))
            {
                var bundle = AssetBundle.LoadFromFile(bundlePath);

                bundle.LoadAllAssets();
                ExplorerCore.Log("Loaded TMP bundle");

                if (TMP_Settings.instance == null)
                {
                    var settings = bundle.LoadAsset<TMP_Settings>("TMP Settings");

#if MONO
                    typeof(TMP_Settings)
                        .GetField("s_Instance", ReflectionHelpers.CommonFlags)
                        .SetValue(null, settings);
#else
                    TMP_Settings.s_Instance = settings;
#endif
                }

                ConsoleFont = bundle.LoadAsset<TMP_FontAsset>("CONSOLA SDF");
            }
            else if (TMP_Settings.instance == null)
            {
                ExplorerCore.LogWarning(@"This game does not seem to have the TMP Resources package, and the TMP AssetBundle was not found at 'Mods\UnityExplorer\tmp.bundle\'!");
                return;
            }

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

        public static void OnSceneChange()
        {
            SceneExplorer.Instance?.OnSceneChange();
            SearchPage.Instance?.OnSceneChange();
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
