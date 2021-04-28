using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.ObjectPool;
using UnityExplorer.UI.Panels;

namespace UnityExplorer.UI.Inspectors
{
    public static class InspectorManager
    {
        public static readonly List<InspectorBase> Inspectors = new List<InspectorBase>();

        public static InspectorBase ActiveInspector { get; private set; }

        public static float PanelWidth;

        public static void Inspect(object obj)
        {
            if (obj.IsNullOrDestroyed())
                return;

            obj = obj.TryCast();

            if (TryFocusActiveInspector(obj))
                return;

            if (obj is GameObject)
                CreateInspector<GameObjectInspector>(obj);
            else
                CreateInspector<ReflectionInspector>(obj);
        }

        private static bool TryFocusActiveInspector(object target)
        {
            foreach (var inspector in Inspectors)
            {
                if (inspector.InspectorTarget.ReferenceEqual(target))
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
                ActiveInspector.OnSetInactive();
        }

        private static void CreateInspector<T>(object target, bool staticReflection = false) where T : InspectorBase
        {
            var inspector = Pool<T>.Borrow();
            Inspectors.Add(inspector);
            inspector.InspectorTarget = target;

            UIManager.SetPanelActive(UIManager.Panels.Inspector, true);
            inspector.UIRoot.transform.SetParent(InspectorPanel.Instance.ContentHolder.transform, false);

            if (inspector is ReflectionInspector reflectInspector)
                reflectInspector.StaticOnly = staticReflection;

            inspector.OnBorrowedFromPool(target);
            SetInspectorActive(inspector);
        }

        internal static void ReleaseInspector<T>(T inspector) where T : InspectorBase
        {
            inspector.OnReturnToPool();
            Pool<T>.Return(inspector);

            Inspectors.Remove(inspector);
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
