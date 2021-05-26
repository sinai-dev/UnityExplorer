using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI;
using UnityExplorer.UI.CacheObject;
using UnityExplorer.UI.Inspectors;
using UnityExplorer.UI.Models;
using UnityExplorer.UI.Panels;

namespace UnityExplorer
{
    public static class InspectorManager
    {
        public static readonly List<InspectorBase> Inspectors = new List<InspectorBase>();

        public static InspectorBase ActiveInspector { get; private set; }
        private static InspectorBase lastActiveInspector;

        public static float PanelWidth;

        internal static void CloseAllTabs()
        {
            if (Inspectors.Any())
            {
                for (int i = Inspectors.Count - 1; i >= 0; i--)
                    Inspectors[i].CloseInspector();

                Inspectors.Clear();
            }

            UIManager.SetPanelActive(UIManager.Panels.Inspector, false);
        }

        public static void Inspect(object obj, CacheObjectBase sourceCache = null)
        {
            if (obj.IsNullOrDestroyed())
                return;

            obj = obj.TryCast();

            if (TryFocusActiveInspector(obj))
                return;

            if (obj is GameObject)
                CreateInspector<GameObjectInspector>(obj);
            else
                CreateInspector<ReflectionInspector>(obj, false, sourceCache);
        }

        private static bool TryFocusActiveInspector(object target)
        {
            foreach (var inspector in Inspectors)
            {
                if (inspector.Target.ReferenceEqual(target))
                {
                    UIManager.SetPanelActive(UIManager.Panels.Inspector, true);
                    SetInspectorActive(inspector);
                    return true;
                }
            }
            return false;
        }

        public static void Inspect(Type type)
        {
            CreateInspector<ReflectionInspector>(type, true);
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

        private static void CreateInspector<T>(object target, bool staticReflection = false, 
            CacheObjectBase sourceCache = null) where T : InspectorBase
        {
            var inspector = Pool<T>.Borrow();
            Inspectors.Add(inspector);
            inspector.Target = target;

            if (sourceCache != null && sourceCache.CanWrite)
            {
                // only set parent cache object if we are inspecting a struct, otherwise there is no point.
                if (target.GetType().IsValueType && inspector is ReflectionInspector ri)
                    ri.ParentCacheObject = sourceCache;
            }

            UIManager.SetPanelActive(UIManager.Panels.Inspector, true);
            inspector.UIRoot.transform.SetParent(InspectorPanel.Instance.ContentHolder.transform, false);

            if (inspector is ReflectionInspector reflectInspector)
                reflectInspector.StaticOnly = staticReflection;

            inspector.OnBorrowedFromPool(target);
            SetInspectorActive(inspector);
        }

        internal static void ReleaseInspector<T>(T inspector) where T : InspectorBase
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
        }

        internal static void Update()
        {
            for (int i = Inspectors.Count - 1; i >= 0; i--)
                Inspectors[i].Update();
        }

        internal static void OnPanelResized(float width)
        {
            PanelWidth = width;

            foreach (var obj in Inspectors)
            {
                if (obj is ReflectionInspector inspector)
                {
                    inspector.SetLayouts();
                }
            }
        }
    }
}
