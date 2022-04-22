using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityExplorer.CacheObject;
using UnityExplorer.Inspectors;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;
using UniverseLib;
using UniverseLib.UI.ObjectPool;
using UniverseLib.Utility;

namespace UnityExplorer
{
    public static class InspectorManager
    {
        public static readonly List<InspectorBase> Inspectors = new();

        public static InspectorBase ActiveInspector { get; private set; }
        private static InspectorBase lastActiveInspector;

        public static float PanelWidth;

        public static event Action OnInspectedTabsChanged;

        public static void Inspect(object obj, CacheObjectBase parent = null)
        {
            if (obj.IsNullOrDestroyed())
                return;

            obj = obj.TryCast();

            if (TryFocusActiveInspector(obj))
                return;

            if (obj is GameObject)
                CreateInspector<GameObjectInspector>(obj);
            else
                CreateInspector<ReflectionInspector>(obj, false, parent);
        }

        public static void Inspect(Type type)
        {
            if (TryFocusActiveInspector(type))
                return;

            CreateInspector<ReflectionInspector>(type, true);
        }

        static bool TryFocusActiveInspector(object target)
        {
            foreach (InspectorBase inspector in Inspectors)
            {
                bool shouldFocus = false;

                if (target is Type targetAsType)
                {
                    if (inspector.TargetType.FullName == targetAsType.FullName)
                        shouldFocus = true;
                }
                else if(inspector.Target.ReferenceEqual(target))
                {
                    shouldFocus = true;
                }

                if (shouldFocus)
                {
                    UIManager.SetPanelActive(UIManager.Panels.Inspector, true);
                    SetInspectorActive(inspector);
                    return true;
                }
            }
            return false;
        }

        public static void SetInspectorActive(InspectorBase inspector)
        {
            UnsetActiveInspector();

            ActiveInspector = inspector;
            inspector.OnSetActive();
        }

        public static void UnsetActiveInspector()
        {
            if (ActiveInspector != null)
            {
                lastActiveInspector = ActiveInspector;
                ActiveInspector.OnSetInactive();
                ActiveInspector = null;
            }
        }

        public static void CloseAllTabs()
        {
            if (Inspectors.Any())
            {
                for (int i = Inspectors.Count - 1; i >= 0; i--)
                    Inspectors[i].CloseInspector();

                Inspectors.Clear();
            }

            UIManager.SetPanelActive(UIManager.Panels.Inspector, false);
        }

        static void CreateInspector<T>(object target, bool staticReflection = false, CacheObjectBase parent = null) where T : InspectorBase
        {
            T inspector = Pool<T>.Borrow();
            Inspectors.Add(inspector);
            inspector.Target = target;

            if (parent != null && parent.CanWrite)
            {
                // only set parent cache object if we are inspecting a struct, otherwise there is no point.
                if (target.GetType().IsValueType && inspector is ReflectionInspector ri)
                    ri.ParentCacheObject = parent;
            }

            UIManager.SetPanelActive(UIManager.Panels.Inspector, true);
            inspector.UIRoot.transform.SetParent(InspectorPanel.Instance.ContentHolder.transform, false);

            if (inspector is ReflectionInspector reflectInspector)
                reflectInspector.StaticOnly = staticReflection;

            inspector.OnBorrowedFromPool(target);
            SetInspectorActive(inspector);

            OnInspectedTabsChanged?.Invoke();
        }

        public static void ReleaseInspector<T>(T inspector) where T : InspectorBase
        {
            if (lastActiveInspector == inspector)
                lastActiveInspector = null;

            bool wasActive = ActiveInspector == inspector;
            int wasIdx = Inspectors.IndexOf(inspector);

            Inspectors.Remove(inspector);
            inspector.OnReturnToPool();
            Pool<T>.Return(inspector);

            if (wasActive)
            {
                ActiveInspector = null;
                // Try focus another inspector, or close the window.
                if (lastActiveInspector != null)
                {
                    SetInspectorActive(lastActiveInspector);
                    lastActiveInspector = null;
                }
                else if (Inspectors.Any())
                {
                    int newIdx = Math.Min(Inspectors.Count - 1, Math.Max(0, wasIdx - 1));
                    SetInspectorActive(Inspectors[newIdx]);
                }
                else
                {
                    UIManager.SetPanelActive(UIManager.Panels.Inspector, false);
                }
            }

            OnInspectedTabsChanged?.Invoke();
        }

        internal static void Update()
        {
            for (int i = Inspectors.Count - 1; i >= 0; i--)
                Inspectors[i].Update();
        }

        internal static void OnPanelResized(float width)
        {
            PanelWidth = width;

            foreach (InspectorBase obj in Inspectors)
            {
                if (obj is ReflectionInspector inspector)
                {
                    inspector.SetLayouts();
                }
            }
        }
    }
}
