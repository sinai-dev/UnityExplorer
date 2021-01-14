using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.Inspectors;
using UnityExplorer.UI.Modules;
using System.IO;
using System.Reflection;
using UnityExplorer.Helpers;
using UnityExplorer.UI.Shared;
using UnityExplorer.Input;
#if CPP
using UnityExplorer.Unstrip;
#endif

namespace UnityExplorer.UI
{
    public static class UIManager
    {
        public static GameObject CanvasRoot { get; private set; }
        public static EventSystem EventSys { get; private set; }

        internal static Font ConsoleFont { get; private set; }

        internal static Sprite ResizeCursor { get; private set; }
        internal static Shader BackupShader { get; private set; }

        public static void Init()
        {
            LoadBundle();

            // Create core UI Canvas and Event System handler
            CreateRootCanvas();

            // Create submodules
            new MainMenu();
            MouseInspector.ConstructUI();
            PanelDragger.LoadCursorImage();

            // Force refresh of anchors
            Canvas.ForceUpdateCanvases();
        }

        public static void OnSceneChange()
        {
            SceneExplorer.Instance?.OnSceneChange();
            SearchPage.Instance?.OnSceneChange();
        }
        public static void Update()
        {
            MainMenu.Instance?.Update();

            if (EventSys)
            {
                if (EventSystem.current != EventSys)
                    ForceUnlockCursor.SetEventSystem();
#if CPP
                // Some IL2CPP games behave weird with multiple UI Input Systems, some fixes for them.
                var evt = InputManager.InputPointerEvent;
                if (evt != null)
                {
                    if (!evt.eligibleForClick && evt.selectedObject)
                        evt.eligibleForClick = true;
                }
#endif
            }

            if (PanelDragger.Instance != null)
                PanelDragger.Instance.Update();

            for (int i = 0; i < SliderScrollbar.Instances.Count; i++)
            {
                var slider = SliderScrollbar.Instances[i];

                if (slider.CheckDestroyed())
                    i--;
                else
                    slider.Update();
            }

            for (int i = 0; i < InputFieldScroller.Instances.Count; i++)
            {
                var input = InputFieldScroller.Instances[i];

                if (input.sliderScroller.CheckDestroyed())
                    i--;
                else
                    input.Update();
            }
        }

        private static AssetBundle LoadExplorerUi(string id)
        {
            return AssetBundle.LoadFromMemory(ReadFully(typeof(ExplorerCore).Assembly.GetManifestResourceStream($"UnityExplorer.Resources.explorerui.{id}.bundle")));
        }

        private static byte[] ReadFully(this Stream input)
        {
            using (var ms = new MemoryStream())
            {
                byte[] buffer = new byte[81920];
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) != 0)
                    ms.Write(buffer, 0, read);

                return ms.ToArray();
            }
        }

        private static void LoadBundle()
        {
            AssetBundle bundle = null;

            try
            {
                bundle = LoadExplorerUi("modern");
            }
            catch
            {
                ExplorerCore.Log("Failed to load modern ExplorerUI Bundle, falling back to legacy");

                try
                {
                    bundle = LoadExplorerUi("legacy");
                }
                catch
                {
                    // ignored
                }
            }

            if (bundle == null)
            {
                ExplorerCore.LogWarning("Could not load the ExplorerUI Bundle!");
                return;
            }

            BackupShader = bundle.LoadAsset<Shader>("DefaultUI");

            // Fix for games which don't ship with 'UI/Default' shader.
            if (Graphic.defaultGraphicMaterial.shader?.name != "UI/Default")
            {
                ExplorerCore.Log("This game does not ship with the 'UI/Default' shader, using manual Default Shader...");
                Graphic.defaultGraphicMaterial.shader = BackupShader;
            }

            ResizeCursor = bundle.LoadAsset<Sprite>("cursor");

            ConsoleFont = bundle.LoadAsset<Font>("CONSOLA");

            ExplorerCore.Log("Loaded UI bundle");
        }

        private static GameObject CreateRootCanvas()
        {
            GameObject rootObj = new GameObject("ExplorerCanvas");
            UnityEngine.Object.DontDestroyOnLoad(rootObj);
            rootObj.layer = 5;

            CanvasRoot = rootObj;
            CanvasRoot.transform.position = new Vector3(0f, 0f, 1f);

            EventSys = rootObj.AddComponent<EventSystem>();
            InputManager.AddUIModule();

            Canvas canvas = rootObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.referencePixelsPerUnit = 100;
            canvas.sortingOrder = 999;
            //canvas.pixelPerfect = false;

            CanvasScaler scaler = rootObj.AddComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            rootObj.AddComponent<GraphicRaycaster>();

            return rootObj;
        }
    }
}
