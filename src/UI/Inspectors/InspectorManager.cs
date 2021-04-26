using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExplorer.UI.ObjectPool;
using UnityExplorer.UI.Panels;

namespace UnityExplorer.UI.Inspectors
{
    public static class InspectorManager
    {
        public static readonly List<InspectorBase> Inspectors = new List<InspectorBase>();

        public static InspectorBase ActiveInspector { get; private set; }
        
        public static void Inspect(object obj)
        {
            obj = obj.TryCast();
            if (obj is GameObject)
                CreateInspector<GameObjectInspector>(obj);
            else
                CreateInspector<InstanceInspector>(obj);
        }

        public static void Inspect(Type type)
        {
            CreateInspector<StaticInspector>(type);
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

        private static void CreateInspector<T>(object target) where T : InspectorBase
        {
            var inspector = Pool<T>.Borrow();
            Inspectors.Add(inspector);

            inspector.UIRoot.transform.SetParent(InspectorPanel.Instance.ContentHolder.transform, false);

            inspector.OnBorrowedFromPool(target);
            SetInspectorActive(inspector);

            UIManager.SetPanelActive(UIManager.Panels.Inspector, true);
        }

        internal static void ReleaseInspector<T>(T inspector) where T : InspectorBase
        {
            inspector.OnReturnToPool();
            Pool<T>.Return(inspector);

            Inspectors.Remove(inspector);
        }

        internal static void Update()
        {
            foreach (var inspector in Inspectors)
            {
                inspector.Update();
            }
        }

        internal static void OnPanelResized()
        {
            
        }
    }
}
