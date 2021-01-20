using System;
using System.Collections.Generic;
using System.Linq;
using UnityExplorer.Helpers;
using UnityExplorer.UI;
using UnityExplorer.UI.Modules;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExplorer.Inspectors.Reflection;

namespace UnityExplorer.Inspectors
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
            for (int i = 0; i < m_currentInspectors.Count; i++)
            {
                if (i >= m_currentInspectors.Count)
                    break;

                m_currentInspectors[i].Update();
            }
        }

        public void Inspect(object obj, CacheObjectBase parentMember = null)
        {
#if CPP
            obj = obj.Il2CppCast(ReflectionHelpers.GetActualType(obj));
#endif
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

            Color activeColor = new Color(0, 0.25f, 0, 1);
            ColorBlock colors = inspector.tabButton.colors;
            colors.normalColor = activeColor;
            colors.highlightedColor = activeColor;
            inspector.tabButton.colors = colors;
        }

        public void UnsetInspectorTab()
        {
            if (m_activeInspector == null)
                return;

            m_activeInspector.SetInactive();

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
            LayoutElement mainLayout = mainObj.AddComponent<LayoutElement>();
            mainLayout.preferredHeight = 400;
            mainLayout.flexibleHeight = 9000;
            mainLayout.preferredWidth = 620;
            mainLayout.flexibleWidth = 9000;

            var mainGroup = mainObj.GetComponent<VerticalLayoutGroup>();
            mainGroup.childForceExpandHeight = true;
            mainGroup.childForceExpandWidth = true;
            mainGroup.childControlHeight = true;
            mainGroup.childControlWidth = true;
            mainGroup.spacing = 4;
            mainGroup.padding.left = 4;
            mainGroup.padding.right = 4;
            mainGroup.padding.top = 4;
            mainGroup.padding.bottom = 4;

            var topRowObj = UIFactory.CreateHorizontalGroup(mainObj, new Color(1, 1, 1, 0));
            var topRowGroup = topRowObj.GetComponent<HorizontalLayoutGroup>();
            topRowGroup.childForceExpandWidth = false;
            topRowGroup.childControlWidth = true;
            topRowGroup.childForceExpandHeight = true;
            topRowGroup.childControlHeight = true;
            topRowGroup.spacing = 15;

            var inspectorTitle = UIFactory.CreateLabel(topRowObj, TextAnchor.MiddleLeft);
            Text title = inspectorTitle.GetComponent<Text>();
            title.text = "Inspector";
            title.fontSize = 20;
            var titleLayout = inspectorTitle.AddComponent<LayoutElement>();
            titleLayout.minHeight = 30;
            titleLayout.flexibleHeight = 0;
            titleLayout.minWidth = 90;
            titleLayout.flexibleWidth = 20000;

            ConstructToolbar(topRowObj);

            // inspector tab bar

            m_tabBarContent = UIFactory.CreateGridGroup(mainObj, new Vector2(185, 20), new Vector2(5, 2), new Color(0.1f, 0.1f, 0.1f, 1));

            var gridGroup = m_tabBarContent.GetComponent<GridLayoutGroup>();
            gridGroup.padding.top = 3;
            gridGroup.padding.left = 3;
            gridGroup.padding.right = 3;
            gridGroup.padding.bottom = 3;

            // inspector content area

            m_inspectorContent = UIFactory.CreateVerticalGroup(mainObj, new Color(0.1f, 0.1f, 0.1f));
            var inspectorGroup = m_inspectorContent.GetComponent<VerticalLayoutGroup>();
            inspectorGroup.childForceExpandHeight = true;
            inspectorGroup.childForceExpandWidth = true;
            inspectorGroup.childControlHeight = true;
            inspectorGroup.childControlWidth = true;

            m_inspectorContent = UIFactory.CreateVerticalGroup(mainObj, new Color(0.1f, 0.1f, 0.1f));
            var contentGroup = m_inspectorContent.GetComponent<VerticalLayoutGroup>();
            contentGroup.childForceExpandHeight = true;
            contentGroup.childForceExpandWidth = true;
            contentGroup.childControlHeight = true;
            contentGroup.childControlWidth = true;
            contentGroup.padding.top = 2;
            contentGroup.padding.left = 2;
            contentGroup.padding.right = 2;
            contentGroup.padding.bottom = 2;

            var contentLayout = m_inspectorContent.AddComponent<LayoutElement>();
            contentLayout.preferredHeight = 900;
            contentLayout.flexibleHeight = 10000;
            contentLayout.preferredWidth = 600;
            contentLayout.flexibleWidth = 10000;
        }

        private static void ConstructToolbar(GameObject topRowObj)
        {
            var invisObj = UIFactory.CreateHorizontalGroup(topRowObj, new Color(1, 1, 1, 0));
            var invisGroup = invisObj.GetComponent<HorizontalLayoutGroup>();
            invisGroup.childForceExpandWidth = false;
            invisGroup.childForceExpandHeight = false;
            invisGroup.childControlWidth = true;
            invisGroup.childControlHeight = true;
            invisGroup.padding.top = 2;
            invisGroup.padding.bottom = 2;
            invisGroup.padding.left = 2;
            invisGroup.padding.right = 2;
            invisGroup.spacing = 10;

            // inspect under mouse button
            AddMouseInspectButton(topRowObj, MouseInspector.MouseInspectMode.UI);
            AddMouseInspectButton(topRowObj, MouseInspector.MouseInspectMode.World);
        }

        private static void AddMouseInspectButton(GameObject topRowObj, MouseInspector.MouseInspectMode mode)
        {
            var inspectObj = UIFactory.CreateButton(topRowObj);
            var inspectLayout = inspectObj.AddComponent<LayoutElement>();
            inspectLayout.minWidth = 120;
            inspectLayout.flexibleWidth = 0;

            var inspectText = inspectObj.GetComponentInChildren<Text>();
            inspectText.text = "Mouse Inspect";
            inspectText.fontSize = 13;

            if (mode == MouseInspector.MouseInspectMode.UI)
                inspectText.text += " (UI)";

            var inspectBtn = inspectObj.GetComponent<Button>();
            var inspectColors = inspectBtn.colors;
            inspectColors.normalColor = new Color(0.2f, 0.2f, 0.2f);
            inspectBtn.colors = inspectColors;

            inspectBtn.onClick.AddListener(OnInspectMouseClicked);

            void OnInspectMouseClicked()
            {
                MouseInspector.Mode = mode;
                MouseInspector.StartInspect();
            }
        }

#endregion
    }
}
