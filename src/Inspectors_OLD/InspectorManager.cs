//using System;
//using System.Collections.Generic;
//using System.Linq;
//using UnityExplorer.UI;
//using UnityEngine;
//using UnityEngine.SceneManagement;
//using UnityEngine.UI;
//using UnityExplorer.Core.Runtime;
//using UnityExplorer.UI.CacheObject;
//using UnityExplorer.Inspectors.Reflection;
//using UnityExplorer.UI.Panels;

//namespace UnityExplorer.Inspectors
//{
//    public static class InspectorManager
//    {
//        public static InspectorBase m_activeInspector;
//        public static readonly List<InspectorBase> ActiveInspectors = new List<InspectorBase>();

//        public static void Update()
//        {
//            try
//            {
//                for (int i = 0; i < ActiveInspectors.Count; i++)
//                    ActiveInspectors[i].Update();
//            }
//            catch (Exception ex)
//            {
//                ExplorerCore.LogWarning(ex);
//            }
//        }

//        public static void DestroyInspector(InspectorBase inspector)
//        {
//            if (inspector is ReflectionInspector ri)
//                ri.Destroy();
//            else
//                inspector.Destroy();
//        }

//        public static void Inspect(object obj, CacheObjectBase parentMember = null)
//        {
//            var type = ReflectionProvider.Instance.GetActualType(obj);

//            // only need to set parent member for structs
//            if (!type.IsValueType)
//                parentMember = null;

//            obj = ReflectionProvider.Instance.Cast(obj, type);

//            if (obj.IsNullOrDestroyed(false))
//                return;

//            // check if currently inspecting this object
//            foreach (InspectorBase tab in ActiveInspectors)
//            {
//                if (obj.ReferenceEqual(tab.Target))
//                {
//                    SetInspectorTab(tab);
//                    return;
//                }
//            }

//            InspectorBase inspector;
//            if (obj is GameObject go)
//            {
//                ExplorerCore.Log("TODO");
//                return;
//                // inspector = new GameObjectInspector(go);
//            }
//            else
//                inspector = new InstanceInspector(obj);

//            if (inspector is ReflectionInspector ri)
//                ri.ParentMember = parentMember;

//            ActiveInspectors.Add(inspector);
//            SetInspectorTab(inspector);
//        }

//        public static void Inspect(Type type)
//        {
//            if (type == null)
//            {
//                ExplorerCore.LogWarning("The provided type was null!");
//                return;
//            }

//            foreach (var tab in ActiveInspectors.Where(x => x is StaticInspector))
//            {
//                if (ReferenceEquals(tab.Target as Type, type))
//                {
//                    SetInspectorTab(tab);
//                    return;
//                }
//            }

//            var inspector = new StaticInspector(type);

//            ActiveInspectors.Add(inspector);
//            SetInspectorTab(inspector);
//        }

//        public static void SetInspectorTab(InspectorBase inspector)
//        {
//            UIManager.SetPanelActive(UIManager.Panels.Inspector, true);

//            if (m_activeInspector == inspector)
//                return;

//            UnsetInspectorTab();

//            m_activeInspector = inspector;
//            inspector.SetActive();

//            OnSetInspectorTab(inspector);
//        }

//        public static void UnsetInspectorTab()
//        {
//            if (m_activeInspector == null)
//                return;

//            m_activeInspector.SetInactive();

//            OnUnsetInspectorTab();

//            m_activeInspector = null;
//        }

//        public static void OnSetInspectorTab(InspectorBase inspector)
//        {
//            Color activeColor = new Color(0, 0.25f, 0, 1);
//            RuntimeProvider.Instance.SetColorBlock(inspector.m_tabButton, activeColor, activeColor);
//        }

//        public static void OnUnsetInspectorTab()
//        {
//            RuntimeProvider.Instance.SetColorBlock(m_activeInspector.m_tabButton,
//                new Color(0.2f, 0.2f, 0.2f, 1), new Color(0.1f, 0.3f, 0.1f, 1));
//        }

//        internal static void OnPanelResized()
//        {
//            foreach (var instance in ActiveInspectors)
//            {
//                instance.OnPanelResized();
//            }
//        }
//    }
//}
