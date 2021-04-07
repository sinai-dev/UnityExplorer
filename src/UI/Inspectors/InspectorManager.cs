using System;
using System.Collections.Generic;
using System.Linq;
using UnityExplorer.Core.Unity;
using UnityExplorer.UI;
using UnityExplorer.UI.Main;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI.Main.Home;
using UnityExplorer.UI.CacheObject;
using UnityExplorer.UI.Inspectors.GameObjects;
using UnityExplorer.UI.Inspectors.Reflection;

namespace UnityExplorer.UI.Inspectors
{
    public class InspectorManager
    {
        public static InspectorManager Instance { get; private set; }

        public InspectorManager()
        {
            Instance = this;

            ConstructInspectorPane();
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
                if (RuntimeProvider.Instance.IsReferenceEqual(obj, tab.Target))
                {
                    SetInspectorTab(tab);
                    return;
                }
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

            OnSetInspectorTab(inspector);
        }

        public void UnsetInspectorTab()
        {
            if (m_activeInspector == null)
                return;

            m_activeInspector.SetInactive();

            OnUnsetInspectorTab();

            m_activeInspector = null;
        }

        public static GameObject m_tabBarContent;
        public static GameObject m_inspectorContent;

        public void OnSetInspectorTab(InspectorBase inspector)
        {
            Color activeColor = new Color(0, 0.25f, 0, 1);
            inspector.m_tabButton.colors = RuntimeProvider.Instance.SetColorBlock(inspector.m_tabButton.colors, activeColor, activeColor);
        }

        public void OnUnsetInspectorTab()
        {
            m_activeInspector.m_tabButton.colors = RuntimeProvider.Instance.SetColorBlock(m_activeInspector.m_tabButton.colors, 
                new Color(0.2f, 0.2f, 0.2f, 1), new Color(0.1f, 0.3f, 0.1f, 1));
        }

        public void ConstructInspectorPane()
        {
            var mainObj = UIFactory.CreateVerticalGroup(HomePage.Instance.Content, 
                "InspectorManager_Root", 
                true, true, true, true, 
                4, 
                new Vector4(4,4,4,4));

            UIFactory.SetLayoutElement(mainObj, preferredHeight: 400, flexibleHeight: 9000, preferredWidth: 620, flexibleWidth: 9000);

            var topRowObj = UIFactory.CreateHorizontalGroup(mainObj, "TopRow", false, true, true, true, 15);
            
            var inspectorTitle = UIFactory.CreateLabel(topRowObj, "Title", "Inspector", TextAnchor.MiddleLeft, default, true, 25);

            UIFactory.SetLayoutElement(inspectorTitle.gameObject, minHeight: 30, flexibleHeight: 0, minWidth: 90, flexibleWidth: 20000);

            ConstructToolbar(topRowObj);

            // inspector tab bar

            m_tabBarContent = UIFactory.CreateGridGroup(mainObj, "TabHolder", new Vector2(185, 20), new Vector2(5, 2), new Color(0.1f, 0.1f, 0.1f, 1));

            var gridGroup = m_tabBarContent.GetComponent<GridLayoutGroup>();
            gridGroup.padding.top = 3;
            gridGroup.padding.left = 3;
            gridGroup.padding.right = 3;
            gridGroup.padding.bottom = 3;

            // inspector content area

            m_inspectorContent = UIFactory.CreateVerticalGroup(mainObj, "InspectorContent", 
                true, true, true, true, 
                0, 
                new Vector4(2,2,2,2), 
                new Color(0.1f, 0.1f, 0.1f));

            UIFactory.SetLayoutElement(m_inspectorContent, preferredHeight: 900, flexibleHeight: 10000, preferredWidth: 600, flexibleWidth: 10000);
        }

        private static void ConstructToolbar(GameObject topRowObj)
        {
            // invisible group
            UIFactory.CreateHorizontalGroup(topRowObj, "Toolbar", false, false, true, true, 10, new Vector4(2, 2, 2, 2), new Color(1,1,1,0));

            // inspect under mouse button
            AddMouseInspectButton(topRowObj, "UI", InspectUnderMouse.MouseInspectMode.UI);
            AddMouseInspectButton(topRowObj, "3D", InspectUnderMouse.MouseInspectMode.World);
        }

        private static void AddMouseInspectButton(GameObject topRowObj, string suffix, InspectUnderMouse.MouseInspectMode mode)
        {
            string lbl = $"Mouse Inspect ({suffix})";

            var inspectObj = UIFactory.CreateButton(topRowObj, 
                lbl, 
                lbl, 
                () => { InspectUnderMouse.StartInspect(mode); }, 
                new Color(0.2f, 0.2f, 0.2f));

            UIFactory.SetLayoutElement(inspectObj.gameObject, minWidth: 150, flexibleWidth: 0);
        }
    }
}
