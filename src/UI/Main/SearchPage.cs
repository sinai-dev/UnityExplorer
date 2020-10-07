using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Explorer.UI.Shared;
using Explorer.CacheObject;

namespace Explorer.UI.Main
{
    public class SearchPage : BaseMainMenuPage
    {
        public static SearchPage Instance;

        public override string Name { get => "Search"; }

        private string m_searchInput = "";
        private string m_typeInput = "";

        private Vector2 resultsScroll = Vector2.zero;

        public PageHelper Pages = new PageHelper();

        private List<CacheObjectBase> m_searchResults = new List<CacheObjectBase>();

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
            Pages.PageOffset = 0;
        }

        public override void Update() { }

        private void CacheResults(IEnumerable results, bool isStaticClasses = false)
        {
            m_searchResults = new List<CacheObjectBase>();

            foreach (var obj in results)
            {
                var toCache = obj;

#if CPP
                if (toCache is Il2CppSystem.Object ilObject)
                {
                    toCache = ilObject.TryCast<GameObject>() ?? ilObject.TryCast<Transform>()?.gameObject ?? ilObject;
                }
#else
                if (toCache is GameObject || toCache is Transform)
                {
                    toCache = toCache as GameObject ?? (toCache as Transform).gameObject;
                }
#endif

                var cache = CacheFactory.GetCacheObject(toCache);
                cache.IsStaticClassSearchResult = isStaticClasses;
                m_searchResults.Add(cache);
            }

            Pages.ItemCount = m_searchResults.Count;
            Pages.PageOffset = 0;
        }

        public override void DrawWindow()
        {
            try
            {
                // helpers
                GUIUnstrip.BeginHorizontal(GUIContent.none, GUI.skin.box, null);
                GUILayout.Label("<b><color=orange>Helpers</color></b>", new GUILayoutOption[] { GUILayout.Width(70) });
                if (GUILayout.Button("Find Static Instances", new GUILayoutOption[] { GUILayout.Width(180) }))
                {
                    CacheResults(GetStaticInstances());
                }
                if (GUILayout.Button("Find Static Classes", new GUILayoutOption[] { GUILayout.Width(180) }))
                {
                    CacheResults(GetStaticClasses(), true);
                }
                GUILayout.EndHorizontal();

                // search box
                SearchBox();

                // results
                GUIUnstrip.BeginVertical(GUIContent.none, GUI.skin.box, null);

                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUILayout.Label("<b><color=orange>Results </color></b>" + " (" + m_searchResults.Count + ")", new GUILayoutOption[0]);
                GUI.skin.label.alignment = TextAnchor.UpperLeft;

                int count = m_searchResults.Count;

                GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);

                Pages.DrawLimitInputArea();

                if (count > Pages.ItemsPerPage)
                {
                    // prev/next page buttons

                    if (Pages.ItemCount > Pages.ItemsPerPage)
                    {
                        if (GUILayout.Button("< Prev", new GUILayoutOption[] { GUILayout.Width(80) }))
                        {
                            Pages.TurnPage(Turn.Left, ref this.resultsScroll);
                        }

                        Pages.CurrentPageLabel();

                        if (GUILayout.Button("Next >", new GUILayoutOption[] { GUILayout.Width(80) }))
                        {
                            Pages.TurnPage(Turn.Right, ref this.resultsScroll);
                        }
                    }
                }

                GUILayout.EndHorizontal();

                resultsScroll = GUIUnstrip.BeginScrollView(resultsScroll);

                var _temprect = new Rect(MainMenu.MainRect.x, MainMenu.MainRect.y, MainMenu.MainRect.width + 160, MainMenu.MainRect.height);

                if (m_searchResults.Count > 0)
                {
                    int offset = Pages.CalculateOffsetIndex();

                    for (int i = offset; i < offset + Pages.ItemsPerPage && i < count; i++)
                    {
                        m_searchResults[i].Draw(MainMenu.MainRect, 0f);
                    }
                }
                else
                {
                    GUILayout.Label("<color=red><i>No results found!</i></color>", new GUILayoutOption[0]);
                }

                GUIUnstrip.EndScrollView();
                GUILayout.EndVertical();
            }
            catch
            {
                m_searchResults.Clear();
            }
        }

        private void SearchBox()
        {
            GUIUnstrip.BeginVertical(GUIContent.none, GUI.skin.box, null);

            // ----- GameObject Search -----
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("<b><color=orange>Search</color></b>", new GUILayoutOption[0]);
            GUI.skin.label.alignment = TextAnchor.UpperLeft;

            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);

            GUILayout.Label("Name Contains:", new GUILayoutOption[] { GUILayout.Width(100) });
            m_searchInput = GUIUnstrip.TextField(m_searchInput, new GUILayoutOption[] { GUILayout.Width(200) });

            GUILayout.EndHorizontal();

            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);

            GUILayout.Label("Class Filter:", new GUILayoutOption[] { GUILayout.Width(100) });
            ClassFilterToggle(TypeFilter.Object, "Object");
            ClassFilterToggle(TypeFilter.GameObject, "GameObject");
            ClassFilterToggle(TypeFilter.Component, "Component");
            ClassFilterToggle(TypeFilter.Custom, "Custom");
            GUILayout.EndHorizontal();
            if (TypeMode == TypeFilter.Custom)
            {
                GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
                GUI.skin.label.alignment = TextAnchor.MiddleRight;
                GUILayout.Label("Custom Class:", new GUILayoutOption[] { GUILayout.Width(250) });
                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                m_typeInput = GUIUnstrip.TextField(m_typeInput, new GUILayoutOption[] { GUILayout.Width(250) });
                GUILayout.EndHorizontal();
            }

            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label("Scene Filter:", new GUILayoutOption[] { GUILayout.Width(100) });
            SceneFilterToggle(SceneFilter.Any, "Any", 60);
            SceneFilterToggle(SceneFilter.This, "This Scene", 100);
            SceneFilterToggle(SceneFilter.DontDestroy, "DontDestroyOnLoad", 140);
            SceneFilterToggle(SceneFilter.None, "No Scene", 80);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("<b><color=cyan>Search</color></b>", new GUILayoutOption[0]))
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
            Pages.PageOffset = 0;
            CacheResults(FindAllObjectsOfType(m_searchInput, m_typeInput));
        }

        private List<object> FindAllObjectsOfType(string searchQuery, string typeName)
        {
#if CPP
            Il2CppSystem.Type searchType = null;

#else
            Type searchType = null;
#endif
            if (TypeMode == TypeFilter.Custom)
            {
                try
                {
                    if (ReflectionHelpers.GetTypeByName(typeName) is Type t)
                    {
#if CPP
                        searchType = Il2CppSystem.Type.GetType(t.AssemblyQualifiedName);
#else
                        searchType = t;
#endif
                    }
                    else
                    {
                        throw new Exception($"Could not find a Type by the name of '{typeName}'!");
                    }
                }
                catch (Exception e)
                {
                    ExplorerCore.Log("Exception getting Search Type: " + e.GetType() + ", " + e.Message);
                }
            }
            else if (TypeMode == TypeFilter.Object)
            {
                searchType = ReflectionHelpers.ObjectType;
            }
            else if (TypeMode == TypeFilter.GameObject)
            {
                searchType = ReflectionHelpers.GameObjectType;
            }
            else if (TypeMode == TypeFilter.Component)
            {
                searchType = ReflectionHelpers.ComponentType;
            }

            if (!ReflectionHelpers.ObjectType.IsAssignableFrom(searchType))
            {
                if (searchType != null)
                {
                    ExplorerCore.LogWarning("Your Custom Class Type must inherit from UnityEngine.Object!");
                }
                return new List<object>();
            }

            var matches = new List<object>();

            var allObjectsOfType = Resources.FindObjectsOfTypeAll(searchType);

            //ExplorerCore.Log("Found count: " + allObjectsOfType.Length);

            int i = 0;
            foreach (var obj in allObjectsOfType)
            {
                if (i >= 2000) break;

                if (searchQuery != "" && !obj.name.ToLower().Contains(searchQuery.ToLower()))
                {
                    continue;
                }

                if (searchType.FullName == ReflectionHelpers.ComponentType.FullName
#if CPP
                    && ReflectionHelpers.TransformType.IsAssignableFrom(obj.GetIl2CppType()))
#else
                    && ReflectionHelpers.TransformType.IsAssignableFrom(obj.GetType()))
#endif
                {
                    // Transforms shouldn't really be counted as Components, skip them.
                    // They're more akin to GameObjects.
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

                i++;
            }

            return matches;
        }

        public static bool FilterScene(object obj, SceneFilter filter)
        {
            GameObject go = null;
#if CPP
            if (obj is Il2CppSystem.Object ilObject)
            {
                go = ilObject.TryCast<GameObject>() ?? ilObject.TryCast<Component>().gameObject;
            }
#else
            if (obj is GameObject || obj is Component)
            {
                go = (obj as GameObject) ?? (obj as Component).gameObject;                
            }
#endif

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
                return go.scene.name == UnityHelpers.ActiveSceneName;
            }
            else if (filter == SceneFilter.DontDestroy)
            {
                return go.scene.name == "DontDestroyOnLoad";
            }
            return false;
        }

        // ====== other ========

        private static bool FilterName(string name)
        {
            // Don't really want these instances.
            return !name.StartsWith("Mono")
                && !name.StartsWith("System")
                && !name.StartsWith("Il2CppSystem")
                && !name.StartsWith("Iced");
        }

        // credit: ManlyMarco (RuntimeUnityEditor)
        public static IEnumerable<object> GetStaticInstances()
        {
            var query = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(t => t.TryGetTypes())
                        .Where(t => t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters);

            var flags = BindingFlags.Public | BindingFlags.Static;
            var flatFlags = flags | BindingFlags.FlattenHierarchy;

            foreach (var type in query)
            {
                object obj = null;
                try
                {
                    var pi = type.GetProperty("Instance", flags);

                    if (pi == null)
                    {
                        pi = type.GetProperty("Instance", flatFlags);
                    }

                    if (pi != null)
                    {
                        obj = pi.GetValue(null, null);
                    }
                    else
                    {
                        var fi = type.GetField("Instance", flags);

                        if (fi == null)
                        {
                            fi = type.GetField("Instance", flatFlags);
                        }

                        if (fi != null)
                        {
                            obj = fi.GetValue(null);
                        }
                    }
                }
                catch { }

                if (obj != null)
                {
                    var t = ReflectionHelpers.GetActualType(obj);

                    if (!FilterName(t.FullName) || ReflectionHelpers.IsEnumerable(t))
                    {
                        continue;
                    }

                    yield return obj;
                }
            }
        }

        private IEnumerable GetStaticClasses()
        {
            var list = new List<Type>();

            var input = m_searchInput?.ToLower() ?? "";

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in asm.TryGetTypes())
                    {
                        if (!type.IsAbstract || !type.IsSealed)
                        {
                            continue;
                        }

                        if (!string.IsNullOrEmpty(input))
                        {
                            var typename = type.FullName.ToLower();

                            if (!typename.Contains(input))
                            {
                                continue;
                            }
                        }

                        list.Add(type);
                    }
                }
                catch { }
            }

            return list;
        }
    }
}
