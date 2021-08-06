using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityExplorer.ObjectExplorer
{
    public static class SceneHandler
    {
        /// <summary>
        /// The currently inspected Scene.
        /// </summary>
        public static Scene? SelectedScene
        {
            get => selectedScene;
            internal set
            {
                if (selectedScene != null && selectedScene == value)
                    return;
                selectedScene = value;
                OnInspectedSceneChanged?.Invoke((Scene)selectedScene);
            }
        }
        private static Scene? selectedScene;

        /// <summary>
        /// The GameObjects in the currently inspected scene.
        /// </summary>
        public static GameObject[] CurrentRootObjects { get; private set; } = new GameObject[0];

        /// <summary>
        /// All currently loaded Scenes.
        /// </summary>
        public static List<Scene> LoadedScenes { get; private set; } = new List<Scene>();
        private static HashSet<Scene> previousLoadedScenes;

        /// <summary>
        /// The names of all scenes in the build settings, if they could be retrieved.
        /// </summary>
        public static readonly List<string> AllSceneNames = new List<string>();

        /// <summary>
        /// Whether or not we successfuly retrieved the names of the scenes in the build settings.
        /// </summary>
        public static bool WasAbleToGetScenesInBuild { get; private set; }

        /// <summary>
        /// Invoked when the currently inspected Scene changes. The argument is the new scene.
        /// </summary>
        public static event Action<Scene> OnInspectedSceneChanged;

        /// <summary>
        /// Invoked whenever the list of currently loaded Scenes changes. The argument contains all loaded scenes after the change.
        /// </summary>
        public static event Action<List<Scene>> OnLoadedScenesChanged;

        /// <summary>
        /// Equivalent to <see cref="SceneManager.sceneCount"/> + 2, to include 'DontDestroyOnLoad' and the 'None' scene.
        /// </summary>
        public static int LoadedSceneCount => SceneManager.sceneCount + 2;

        internal static Scene DontDestroyScene => DontDestroyMe.scene;
        internal static int DontDestroyHandle => DontDestroyScene.handle;

        internal static GameObject DontDestroyMe
        {
            get
            {
                if (!dontDestroyObject)
                {
                    dontDestroyObject = new GameObject("DontDestroyMe");
                    GameObject.DontDestroyOnLoad(dontDestroyObject);
                }
                return dontDestroyObject;
            }
        }
        private static GameObject dontDestroyObject;

        public static bool InspectingAssetScene => SelectedScene.HasValue && SelectedScene.Value == default;

        internal static void Init()
        {
            // Try to get all scenes in the build settings. This may not work.
            try
            {
                Type sceneUtil = ReflectionUtility.GetTypeByName("UnityEngine.SceneManagement.SceneUtility");
                if (sceneUtil == null)
                    throw new Exception("This version of Unity does not ship with the 'SceneUtility' class, or it was not unstripped.");

                var method = sceneUtil.GetMethod("GetScenePathByBuildIndex", ReflectionUtility.FLAGS);
                int sceneCount = SceneManager.sceneCountInBuildSettings;
                for (int i = 0; i < sceneCount; i++)
                {
                    var scenePath = (string)method.Invoke(null, new object[] { i });
                    AllSceneNames.Add(scenePath);
                }
            }
            catch (Exception ex)
            {
                WasAbleToGetScenesInBuild = false;
                ExplorerCore.LogWarning($"Unable to generate list of all Scenes in the build: {ex}");
            }
        }

        internal static void Update()
        {
            // check if the loaded scenes changed. always confirm DontDestroy / HideAndDontSave
            int confirmedCount = 2;
            bool inspectedExists = SelectedScene == DontDestroyScene || (SelectedScene.HasValue && SelectedScene.Value == default);

            LoadedScenes.Clear();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene == default || !scene.isLoaded)
                    continue;

                // If no changes yet, ensure the previous list contained the scene
                if (previousLoadedScenes != null && previousLoadedScenes.Contains(scene))
                    confirmedCount++;

                // If we have not yet confirmed inspectedExists, check if this scene is our currently inspected one.
                if (!inspectedExists && scene == SelectedScene)
                    inspectedExists = true;

                LoadedScenes.Add(scene);
            }

            bool anyChange = confirmedCount != LoadedScenes.Count;

            LoadedScenes.Add(DontDestroyScene);
            LoadedScenes.Add(default);
            previousLoadedScenes = new HashSet<Scene>(LoadedScenes);

            // Default to first scene if none selected or previous selection no longer exists.
            if (!inspectedExists)
                SelectedScene = LoadedScenes.First();

            // Notify on the list changing at all
            if (anyChange)
                OnLoadedScenesChanged?.Invoke(LoadedScenes);

            // Finally, update the root objects list.
            if (SelectedScene != null && ((Scene)SelectedScene).IsValid())
                CurrentRootObjects = RuntimeProvider.Instance.GetRootGameObjects((Scene)SelectedScene);
            else
            {
                var allObjects = RuntimeProvider.Instance.FindObjectsOfTypeAll(typeof(GameObject));
                var objects = new List<GameObject>();
                foreach (var obj in allObjects)
                {
                    var go = obj.TryCast<GameObject>();
                    if (go.transform.parent == null && !go.scene.IsValid())
                        objects.Add(go);
                }
                CurrentRootObjects = objects.ToArray();
            }
        }
    }
}
