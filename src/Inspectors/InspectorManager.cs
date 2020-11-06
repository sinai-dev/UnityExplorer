using System;
using System.Collections.Generic;
using System.Linq;
using UnityExplorer.Helpers;
using UnityExplorer.UI;
using UnityExplorer.UI.PageModel;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

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

        private readonly List<Text> testTexts = new List<Text>();

        public void Update()
        {
            for (int i = 0; i < m_currentInspectors.Count; i++)
            {
                if (i >= m_currentInspectors.Count)
                    break;

                m_currentInspectors[i].Update();
            }

            // ======= test ======== //
            foreach (var text in testTexts)
            {
                text.text = Time.time.ToString();
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
            inspector.SetContentInactive();

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

            inspector.SetContentActive();

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

            m_activeInspector.SetContentInactive();

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
            mainGroup.spacing = 2;
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
            titleLayout.flexibleWidth = 0;

            ConstructToolbar(topRowObj);

            // inspector tab bar

            m_tabBarContent = UIFactory.CreateGridGroup(mainObj, new Vector2(185, 20), new Vector2(5, 2), new Color(0.1f, 0.1f, 0.1f, 1));

            var gridGroup = m_tabBarContent.GetComponent<GridLayoutGroup>();
            gridGroup.padding.top = 4;
            gridGroup.padding.left = 4;
            gridGroup.padding.right = 4;
            gridGroup.padding.bottom = 4;

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
            contentGroup.padding.top = 5;
            contentGroup.padding.left = 5;
            contentGroup.padding.right = 5;
            contentGroup.padding.bottom = 5;

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

            // time scale group

            var timeGroupObj = UIFactory.CreateHorizontalGroup(invisObj, new Color(1, 1, 1, 0));
            var timeGroup = timeGroupObj.GetComponent<HorizontalLayoutGroup>();
            timeGroup.childForceExpandWidth = false;
            timeGroup.childControlWidth = true;
            timeGroup.childForceExpandHeight = false;
            timeGroup.childControlHeight = true;
            timeGroup.padding.top = 2;
            timeGroup.padding.left = 5;
            timeGroup.padding.right = 2;
            timeGroup.padding.bottom = 2;
            timeGroup.spacing = 5;
            timeGroup.childAlignment = TextAnchor.MiddleCenter;
            var timeGroupLayout = timeGroupObj.AddComponent<LayoutElement>();
            timeGroupLayout.minWidth = 100;
            timeGroupLayout.flexibleWidth = 300;
            timeGroupLayout.minHeight = 25;
            timeGroupLayout.flexibleHeight = 0;

            // time scale title

            var timeTitleObj = UIFactory.CreateLabel(timeGroupObj, TextAnchor.MiddleLeft);
            var timeTitle = timeTitleObj.GetComponent<Text>();
            timeTitle.text = "Time Scale:";
            timeTitle.color = new Color(21f / 255f, 192f / 255f, 235f / 255f);
            var titleLayout = timeTitleObj.AddComponent<LayoutElement>();
            titleLayout.minHeight = 25;
            titleLayout.minWidth = 80;
            titleLayout.flexibleHeight = 0;
            timeTitle.horizontalOverflow = HorizontalWrapMode.Overflow;

            // actual active time label

            var timeLabelObj = UIFactory.CreateLabel(timeGroupObj, TextAnchor.MiddleLeft);
            var timeLabelLayout = timeLabelObj.AddComponent<LayoutElement>();
            timeLabelLayout.minWidth = 40;
            timeLabelLayout.minHeight = 25;
            timeLabelLayout.flexibleHeight = 0;

            // todo make static and update
            var s_timeText = timeLabelObj.GetComponent<Text>();
            s_timeText.text = Time.timeScale.ToString("F1");

            // time scale input

            var timeInputObj = UIFactory.CreateTMPInput(timeGroupObj, 14, 0, (int)TextAlignmentOptions.MidlineLeft);
            var timeInput = timeInputObj.GetComponent<TMP_InputField>();
            timeInput.characterValidation = TMP_InputField.CharacterValidation.Decimal;
            var timeInputLayout = timeInputObj.AddComponent<LayoutElement>();
            timeInputLayout.minWidth = 90;
            timeInputLayout.flexibleWidth = 0;
            timeInputLayout.minHeight = 25;
            timeInputLayout.flexibleHeight = 0;

            // time scale apply button

            var applyBtnObj = UIFactory.CreateButton(timeGroupObj);
            var applyBtn = applyBtnObj.GetComponent<Button>();
#if MONO
            applyBtn.onClick.AddListener(SetTimeScale);
#else
            applyBtn.onClick.AddListener(new Action(SetTimeScale));
#endif
            var applyText = applyBtnObj.GetComponentInChildren<Text>();
            applyText.text = "Apply";
            applyText.fontSize = 14;
            var applyLayout = applyBtnObj.AddComponent<LayoutElement>();
            applyLayout.minWidth = 50;
            applyLayout.minHeight = 25;
            applyLayout.flexibleHeight = 0;

            void SetTimeScale()
            {
                var scale = float.Parse(timeInput.text);
                Time.timeScale = scale;
                s_timeText.text = Time.timeScale.ToString("F1");
            }


            // inspect under mouse button

            var inspectObj = UIFactory.CreateButton(topRowObj);
            var inspectLayout = inspectObj.AddComponent<LayoutElement>();
            inspectLayout.minWidth = 120;
            inspectLayout.flexibleWidth = 0;
            var inspectBtn = inspectObj.GetComponent<Button>();
            var inspectColors = inspectBtn.colors;
            inspectColors.normalColor = new Color(0.2f, 0.2f, 0.2f);
            inspectBtn.colors = inspectColors;
            var inspectText = inspectObj.GetComponentInChildren<Text>();
            inspectText.text = "Mouse Inspect";
            inspectText.fontSize = 13;
#if MONO
            inspectBtn.onClick.AddListener(OnInspectMouseClicked);
#else
            inspectBtn.onClick.AddListener(new Action(OnInspectMouseClicked));
#endif

            void OnInspectMouseClicked()
            {
                MouseInspector.StartInspect();
            }
        }

#endregion
    }
}
