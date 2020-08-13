using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using UnhollowerRuntimeLib;
using MelonLoader;
using UnhollowerBaseLib;

namespace Explorer
{
    public class SearchPage : WindowPage
    {
        public static SearchPage Instance;

        public override string Name { get => "Object Search"; set => base.Name = value; }

        private string m_searchInput = "";
        private string m_typeInput = "";
        private int m_limit = 100;
        private int m_pageOffset = 0;
        private List<object> m_searchResults = new List<object>();
        private Vector2 resultsScroll = Vector2.zero;

        public SceneFilter SceneMode = SceneFilter.Any;
        public TypeFilter TypeMode = TypeFilter.Object;

        public enum SceneFilter
        {
            Any,
            This,
            DontDestroy,
            None
        }

        public enum TypeFilter
        {
            Object,
            GameObject,
            Component,
            Custom
        }

        public override void Init()
        {
            Instance = this;
        }

        public void OnSceneChange()
        {
            m_searchResults.Clear();
            m_pageOffset = 0;
        }

        public override void Update()
        {
        }

        public override void DrawWindow()
        {
            try
            {
                // helpers
                GUILayout.BeginHorizontal(GUI.skin.box, null);
                GUILayout.Label("<b><color=orange>Helpers</color></b>", new GUILayoutOption[] { GUILayout.Width(70) });
                if (GUILayout.Button("Find Static Instances", new GUILayoutOption[] { GUILayout.Width(180) }))
                {
                    m_searchResults = GetInstanceClassScanner().ToList();
                    m_pageOffset = 0;
                }
                GUILayout.EndHorizontal();

                // search box
                SearchBox();

                // results
                GUILayout.BeginVertical(GUI.skin.box, null);

                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUILayout.Label("<b><color=orange>Results </color></b>" + " (" + m_searchResults.Count + ")", null);
                GUI.skin.label.alignment = TextAnchor.UpperLeft;

                int count = m_searchResults.Count;

                if (count > CppExplorer.ArrayLimit)
                {
                    // prev/next page buttons
                    GUILayout.BeginHorizontal(null);
                    int maxOffset = (int)Mathf.Ceil(count / CppExplorer.ArrayLimit);
                    if (GUILayout.Button("< Prev", null))
                    {
                        if (m_pageOffset > 0) m_pageOffset--;
                    }

                    GUILayout.Label($"Page {m_pageOffset + 1}/{maxOffset + 1}", new GUILayoutOption[] { GUILayout.Width(80) });

                    if (GUILayout.Button("Next >", null))
                    {
                        if (m_pageOffset < maxOffset) m_pageOffset++;
                    }
                    GUILayout.EndHorizontal();
                }

                resultsScroll = GUILayout.BeginScrollView(resultsScroll, GUI.skin.scrollView);

                var _temprect = new Rect(MainMenu.MainRect.x, MainMenu.MainRect.y, MainMenu.MainRect.width + 160, MainMenu.MainRect.height);

                if (m_searchResults.Count > 0)
                {
                    int offset = m_pageOffset * CppExplorer.ArrayLimit;
                    int preiterated = 0;

                    if (offset >= count) m_pageOffset = 0;

                    for (int i = 0; i < m_searchResults.Count; i++)
                    {
                        if (offset > 0 && preiterated < offset)
                        {
                            preiterated++;
                            continue;
                        }

                        if (i - offset > CppExplorer.ArrayLimit - 1)
                        {
                            break;
                        }

                        var obj = m_searchResults[i];

                        bool _ = false;
                        int __ = 0;
                        UIStyles.DrawValue(ref obj, ref _, ref __, _temprect);
                    }
                }
                else
                {
                    GUILayout.Label("<color=red><i>No results found!</i></color>", null);
                }

                GUILayout.EndScrollView();

                GUILayout.EndVertical();
            }
            catch
            {
                m_searchResults.Clear();
            }
        }

        private void SearchBox()
        {
            GUILayout.BeginVertical(GUI.skin.box, null);

            // ----- GameObject Search -----
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("<b><color=orange>Search</color></b>", null);
            GUI.skin.label.alignment = TextAnchor.UpperLeft;

            GUILayout.BeginHorizontal(null);

            GUILayout.Label("Name Contains:", new GUILayoutOption[] { GUILayout.Width(100) });
            m_searchInput = GUILayout.TextField(m_searchInput, new GUILayoutOption[] { GUILayout.Width(200) });

            GUI.skin.label.alignment = TextAnchor.MiddleRight;
            GUILayout.Label("Results per page:", new GUILayoutOption[] { GUILayout.Width(120) });
            var resultinput = m_limit.ToString();
            resultinput = GUILayout.TextField(resultinput, new GUILayoutOption[] { GUILayout.Width(55) });
            if (int.TryParse(resultinput, out int _i) && _i > 0)
            {
                m_limit = _i;
            }
            GUI.skin.label.alignment = TextAnchor.UpperLeft;

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(null);

            GUILayout.Label("Class Filter:", new GUILayoutOption[] { GUILayout.Width(100) });
            ClassFilterToggle(TypeFilter.Object, "Object");
            ClassFilterToggle(TypeFilter.GameObject, "GameObject");
            ClassFilterToggle(TypeFilter.Component, "Component");
            ClassFilterToggle(TypeFilter.Custom, "Custom");
            GUILayout.EndHorizontal();
            if (TypeMode == TypeFilter.Custom)
            {
                GUILayout.BeginHorizontal(null);
                GUI.skin.label.alignment = TextAnchor.MiddleRight;
                GUILayout.Label("Custom Class:", new GUILayoutOption[] { GUILayout.Width(250) });
                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                m_typeInput = GUILayout.TextField(m_typeInput, new GUILayoutOption[] { GUILayout.Width(250) });
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal(null);
            GUILayout.Label("Scene Filter:", new GUILayoutOption[] { GUILayout.Width(100) });
            SceneFilterToggle(SceneFilter.Any, "Any", 60);
            SceneFilterToggle(SceneFilter.This, "This Scene", 100);
            SceneFilterToggle(SceneFilter.DontDestroy, "DontDestroyOnLoad", 140);
            SceneFilterToggle(SceneFilter.None, "No Scene", 80);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("<b><color=cyan>Search</color></b>", null))
            {
                Search();
            }

            GUILayout.EndVertical();
        }

        private void ClassFilterToggle(TypeFilter mode, string label)
        {
            if (TypeMode == mode)
            {
                GUI.color = Color.green;
            }
            else
            {
                GUI.color = Color.white;
            }
            if (GUILayout.Button(label, new GUILayoutOption[] { GUILayout.Width(100) }))
            {
                TypeMode = mode;
            }
            GUI.color = Color.white;
        }

        private void SceneFilterToggle(SceneFilter mode, string label, float width)
        {
            if (SceneMode == mode)
            {
                GUI.color = Color.green;
            }
            else
            {
                GUI.color = Color.white;
            }
            if (GUILayout.Button(label, new GUILayoutOption[] { GUILayout.Width(width) }))
            {
                SceneMode = mode;
            }
            GUI.color = Color.white;
        }


        // -------------- ACTUAL METHODS (not Gui draw) ----------------- //        

        // ======= search functions =======

        private void Search()
        {
            m_pageOffset = 0;
            m_searchResults = FindAllObjectsOfType(m_searchInput, m_typeInput);
        }

        private List<object> FindAllObjectsOfType(string _search, string _type)
        {
            Il2CppSystem.Type type = null;

            if (TypeMode == TypeFilter.Custom)
            {
                try
                {
                    var findType = CppExplorer.GetType(_type);
                    type = Il2CppSystem.Type.GetType(findType.AssemblyQualifiedName);
                }
                catch (Exception e)
                {
                    MelonLogger.Log("Exception: " + e.GetType() + ", " + e.Message + "\r\n" + e.StackTrace);
                }
            }
            else if (TypeMode == TypeFilter.Object)
            {
                type = CppExplorer.ObjectType;
            }
            else if (TypeMode == TypeFilter.GameObject)
            {
                type = CppExplorer.GameObjectType;
            }
            else if (TypeMode == TypeFilter.Component)
            {
                type = CppExplorer.ComponentType;
            }

            if (!CppExplorer.ObjectType.IsAssignableFrom(type))
            {
                MelonLogger.LogError("Your Class Type must inherit from UnityEngine.Object! Leave blank to default to UnityEngine.Object");
                return new List<object>();
            }

            var matches = new List<object>();

            var allObjectsOfType = Resources.FindObjectsOfTypeAll(type);

            foreach (var obj in allObjectsOfType)
            {
                if (_search != "" && !obj.name.ToLower().Contains(_search.ToLower()))
                {
                    continue;
                }

                if (SceneMode != SceneFilter.Any && !FilterScene(obj, this.SceneMode))
                {
                    continue;
                }

                if (!matches.Contains(obj))
                {
                    matches.Add(obj);
                }
            }

            return matches;
        }

        public static bool FilterScene(object obj, SceneFilter filter)
        {
            GameObject go;
            if (obj is Il2CppSystem.Object ilObject)
            {
                go = ilObject.TryCast<GameObject>() ?? ilObject.TryCast<Component>().gameObject;
            }
            else
            {
                go = (obj as GameObject) ?? (obj as Component).gameObject;
            }

            if (!go)
            {
                // object is not on a GameObject, cannot perform scene filter operation.
                return false;
            }

            if (filter == SceneFilter.None)
            {
                return string.IsNullOrEmpty(go.scene.name);
            }
            else if (filter == SceneFilter.This)
            {
                return go.scene.name == CppExplorer.ActiveSceneName;
            }
            else if (filter == SceneFilter.DontDestroy)
            {
                return go.scene.name == "DontDestroyOnLoad";
            }
            return false;
        }

        // ====== other ========

        // credit: ManlyMarco (RuntimeUnityEditor)
        public static IEnumerable<object> GetInstanceClassScanner()
        {
            var query = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => !x.FullName.StartsWith("Mono"))
                .SelectMany(GetTypesSafe)
                .Where(t => t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters);

            foreach (var type in query)
            {
                object obj = null;
                try
                {
                    obj = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)?.GetValue(null, null);
                }
                catch
                {
                    try
                    {
                        obj = type.GetField("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)?.GetValue(null);
                    }
                    catch
                    {
                    }
                }
                if (obj != null && !obj.ToString().StartsWith("Mono"))
                {
                    yield return obj;
                }
            }
        }

        public static IEnumerable<Type> GetTypesSafe(Assembly asm)
        {
            try { return asm.GetTypes(); }
            catch (ReflectionTypeLoadException e) { return e.Types.Where(x => x != null); }
            catch { return Enumerable.Empty<Type>(); }
        }
    }
}
