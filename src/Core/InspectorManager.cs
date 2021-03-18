using System;
using System.Collections.Generic;
using System.Linq;
using UnityExplorer.Core.Unity;
using UnityExplorer.UI;
using UnityExplorer.UI.Main;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExplorer.Core.Inspectors.Reflection;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI.Main.Home;

namespace UnityExplorer.Core.Inspectors
{
    public class InspectorManager
    {
        public static InspectorManager Instance { get; private set; }

        internal static InspectorManagerUI UI;

        public InspectorManager() 
        {
            Instance = this;

            UI = new InspectorManagerUI(); 
            UI.ConstructInspectorPane();
        }

        public InspectorBase m_activeInspector;
        public readonly List<InspectorBase> m_currentInspectors = new List<InspectorBase>();

        public void Update()
        {
            for (int i = 0; i < m_currentInspectors.Count; i++)
            {
                if (i >= m_currentInspectors.Count)
                    break;

                m_currentInspectors[i].Update();
            }
        }

        public void Inspect(object obj, CacheObjectBase parentMember = null)
        {
            obj = ReflectionProvider.Instance.Cast(obj, ReflectionProvider.Instance.GetActualType(obj));

            UnityEngine.Object unityObj = obj as UnityEngine.Object;

            if (obj.IsNullOrDestroyed(false))
            {
                return;
            }

            // check if currently inspecting this object
            foreach (InspectorBase tab in m_currentInspectors)
            {
                if (ReferenceEquals(obj, tab.Target))
                {
                    SetInspectorTab(tab);
                    return;
                }
#if CPP
                else if (unityObj && tab.Target is UnityEngine.Object uTabObj)
                {
                    if (unityObj.m_CachedPtr == uTabObj.m_CachedPtr)
                    {
                        SetInspectorTab(tab);
                        return;
                    }
                }
#endif
            }

            InspectorBase inspector;
            if (obj is GameObject go)
                inspector = new GameObjectInspector(go);
            else
                inspector = new InstanceInspector(obj);

            if (inspector is ReflectionInspector ri)
                ri.ParentMember = parentMember;

            m_currentInspectors.Add(inspector);
            SetInspectorTab(inspector);
        }

        public void Inspect(Type type)
        {
            if (type == null)
            { 
                ExplorerCore.LogWarning("The provided type was null!");
                return;
            }

            foreach (var tab in m_currentInspectors.Where(x => x is StaticInspector))
            {
                if (ReferenceEquals(tab.Target as Type, type))
                {
                    SetInspectorTab(tab);
                    return;
                }
            }

            var inspector = new StaticInspector(type);

            m_currentInspectors.Add(inspector);
            SetInspectorTab(inspector);
        }

        public void SetInspectorTab(InspectorBase inspector)
        {
            MainMenu.Instance.SetPage(HomePage.Instance);

            if (m_activeInspector == inspector)
                return;

            UnsetInspectorTab();

            m_activeInspector = inspector;
            inspector.SetActive();

            UI.OnSetInspectorTab(inspector);
        }

        public void UnsetInspectorTab()
        {
            if (m_activeInspector == null)
                return;

            m_activeInspector.SetInactive();

            UI.OnUnsetInspectorTab();

            m_activeInspector = null;
        }
    }
}
