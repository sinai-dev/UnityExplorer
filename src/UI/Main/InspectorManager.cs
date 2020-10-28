using System;
using System.Collections.Generic;
using System.Linq;
using ExplorerBeta.Helpers;
using ExplorerBeta.UI.Main.Inspectors;
using ExplorerBeta.UI.Shared;
using ExplorerBeta.Unstrip.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ExplorerBeta.UI.Main
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

        public GameObject m_tabBarContent;
        public GameObject m_inspectorContent;

        public void Update()
        {
            foreach (InspectorBase tab in m_currentInspectors)
            {
                tab.Update();
            }
        }

        public void Inspect(object obj)
        {
#if CPP
            obj = obj.Il2CppCast(ReflectionHelpers.GetActualType(obj));
#endif
            UnityEngine.Object unityObj = obj as UnityEngine.Object;

            if (InspectorBase.ObjectNullOrDestroyed(obj, unityObj))
            {
                return;
            }

            MainMenu.Instance.SetPage(HomePage.Instance);

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
            {
                inspector = new GameObjectInspector(go);
            }
            else
            {
                inspector = new InstanceInspector(obj);
            }

            m_currentInspectors.Add(inspector);
            inspector.Content?.SetActive(false);

            SetInspectorTab(inspector);
        }

        public void Inspect(Type type)
        {
            // TODO static type inspection
        }

        public void SetInspectorTab(InspectorBase inspector)
        {
            UnsetInspectorTab();

            m_activeInspector = inspector;

            m_activeInspector.Content?.SetActive(true);

            Color activeColor = new Color(0, 0.25f, 0, 1);
            ColorBlock colors = inspector.tabButton.colors;
            colors.normalColor = activeColor;
            colors.highlightedColor = activeColor;
            inspector.tabButton.colors = colors;
        }

        public void UnsetInspectorTab()
        {
            if (m_activeInspector == null)
            {
                return;
            }

            m_activeInspector.Content?.SetActive(false);

            ColorBlock colors = m_activeInspector.tabButton.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1);
            colors.highlightedColor = new Color(0.1f, 0.3f, 0.1f, 1);
            m_activeInspector.tabButton.colors = colors;

            m_activeInspector = null;
        }

        #region INSPECTOR PANE

        public void ConstructInspectorPane()
        {
            var mainObj = UIFactory.CreateVerticalGroup(HomePage.Instance.Content, new Color(72f / 255f, 72f / 255f, 72f / 255f));
            LayoutElement rightLayout = mainObj.AddComponent<LayoutElement>();
            rightLayout.flexibleWidth = 999999;

            var rightGroup = mainObj.GetComponent<VerticalLayoutGroup>();
            rightGroup.childForceExpandHeight = true;
            rightGroup.childForceExpandWidth = true;
            rightGroup.childControlHeight = true;
            rightGroup.childControlWidth = true;
            rightGroup.spacing = 10;
            rightGroup.padding.left = 8;
            rightGroup.padding.right = 8;
            rightGroup.padding.top = 8;
            rightGroup.padding.bottom = 8;

            var inspectorTitle = UIFactory.CreateLabel(mainObj, TextAnchor.UpperLeft);
            Text title = inspectorTitle.GetComponent<Text>();
            title.text = "Inspector";
            title.fontSize = 20;
            var titleLayout = inspectorTitle.AddComponent<LayoutElement>();
            titleLayout.minHeight = 30;
            titleLayout.flexibleHeight = 0;

            m_tabBarContent = UIFactory.CreateGridGroup(mainObj, new Vector2(185, 20), new Vector2(5, 2), new Color(0.1f, 0.1f, 0.1f, 1));

            var gridGroup = m_tabBarContent.GetComponent<GridLayoutGroup>();
            gridGroup.padding.top = 4;
            gridGroup.padding.left = 4;
            gridGroup.padding.right = 4;
            gridGroup.padding.bottom = 4;

            // inspector content area

            m_inspectorContent = UIFactory.CreateVerticalGroup(mainObj, new Color(0.1f, 0.1f, 0.1f, 1.0f));

            var contentGroup = m_inspectorContent.GetComponent<VerticalLayoutGroup>();
            contentGroup.childForceExpandHeight = true;
            contentGroup.childForceExpandWidth = true;
            contentGroup.childControlHeight = true;
            contentGroup.childControlWidth = true;
            contentGroup.spacing = 5;
            contentGroup.padding.top = 5;
            contentGroup.padding.left = 5;
            contentGroup.padding.right = 5;
            contentGroup.padding.bottom = 5;

            var contentLayout = m_inspectorContent.AddComponent<LayoutElement>();
            contentLayout.preferredHeight = 900;
            contentLayout.flexibleHeight = 10000;
        }

        #endregion
    }
}
