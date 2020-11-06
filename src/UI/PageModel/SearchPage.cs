using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Helpers;
using UnityExplorer.Inspectors;
using UnityExplorer.UI.Shared;

namespace UnityExplorer.UI.PageModel
{
    public class SearchPage : MainMenu.Page
    {
        public override string Name => "Search";

        internal object[] m_results;
        internal readonly List<object> m_resultShortList = new List<object>();

        private int m_lastCount;
        public PageHandler m_resultListPageHandler;
        private GameObject m_resultListContent;
        private readonly List<Text> m_resultListTexts = new List<Text>();

        public override void Init()
        {
            ConstructUI();
        }

        public override void Update()
        {
            //RefreshResultList();
        }

        internal void OnSearchClicked()
        {
            m_results = Resources.FindObjectsOfTypeAll(typeof(UnityEngine.Object));

            RefreshResultList();
        }

        private void OnResultPageTurn()
        {
            RefreshResultList();
        }

        private void RefreshResultList()
        {
            m_resultListPageHandler.ListCount = m_results.Length;

            int newCount = 0;

            foreach (var itemIndex in m_resultListPageHandler)
            {
                newCount++;

                // normalized index starting from 0
                var i = itemIndex - m_resultListPageHandler.StartIndex;

                if (itemIndex >= m_results.Length)
                {
                    if (i > m_lastCount || i >= m_resultListTexts.Count)
                        break;

                    GameObject label = m_resultListTexts[i].transform.parent.parent.gameObject;
                    if (label.activeSelf)
                        label.SetActive(false);
                }
                else
                {
                    var obj = m_results[itemIndex];

                    if (obj == null || obj is UnityEngine.Object uObj && !uObj)
                        continue;

                    if (i >= m_resultShortList.Count)
                    {
                        m_resultShortList.Add(obj);
                        AddResultButton();
                    }
                    else
                    {
                        m_resultShortList[i] = obj;
                    }

                    var text = m_resultListTexts[i];

                    var name = $"<color={SyntaxColors.Class_Instance}>{ReflectionHelpers.GetActualType(obj).Name}</color>" 
                        + $" ({obj.ToString()})";

                    text.text = name;

                    var label = text.transform.parent.parent.gameObject;
                    if (!label.activeSelf)
                        label.SetActive(true);
                }
            }

            m_lastCount = newCount;
        }

        #region UI CONSTRUCTION

        internal void ConstructUI()
        {
            GameObject parent = MainMenu.Instance.PageViewport;

            Content = UIFactory.CreateVerticalGroup(parent);
            var mainGroup = Content.GetComponent<VerticalLayoutGroup>();
            mainGroup.padding.left = 4;
            mainGroup.padding.right = 4;
            mainGroup.padding.top = 4;
            mainGroup.padding.bottom = 4;
            mainGroup.spacing = 5;
            mainGroup.childForceExpandHeight = true;
            mainGroup.childForceExpandWidth = true;
            mainGroup.childControlHeight = true;
            mainGroup.childControlWidth = true;

            ConstructTopArea();

            ConstructResultsArea();
        }

        internal void ConstructTopArea()
        {
            var topAreaObj = UIFactory.CreateVerticalGroup(Content, new Color(0.15f, 0.15f, 0.15f));
            var topGroup = topAreaObj.GetComponent<VerticalLayoutGroup>();
            topGroup.childForceExpandHeight = false;
            topGroup.childControlHeight = true;
            topGroup.childForceExpandWidth = true;
            topGroup.childControlWidth = true;
            topGroup.padding.top = 5;
            topGroup.padding.left = 5;
            topGroup.padding.right = 5;
            topGroup.padding.bottom = 5;
            topGroup.spacing = 5;

            GameObject titleObj = UIFactory.CreateLabel(topAreaObj, TextAnchor.UpperLeft);
            Text titleLabel = titleObj.GetComponent<Text>();
            titleLabel.text = "Search";
            titleLabel.fontSize = 20;
            LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
            titleLayout.minHeight = 30;
            titleLayout.flexibleHeight = 0;

            // top area options

            var tempObj = UIFactory.CreateLabel(topAreaObj, TextAnchor.MiddleLeft);
            var tempText = tempObj.GetComponent<Text>();
            tempText.text = "TODO Options / Filters";

            var testBtnObj = UIFactory.CreateButton(topAreaObj);
            var testText = testBtnObj.GetComponentInChildren<Text>();
            testText.text = "Search";
            LayoutElement searchBtnLayout = testBtnObj.AddComponent<LayoutElement>();
            searchBtnLayout.minHeight = 30;
            searchBtnLayout.flexibleHeight = 0;
            var testBtn = testBtnObj.GetComponent<Button>();
#if MONO
            testBtn.onClick.AddListener(OnSearchClicked);
#else
            testBtn.onClick.AddListener(new Action(OnSearchClicked));
#endif
        }

        internal void ConstructResultsArea()
        {
            // Result group holder (NOT actual result list content)

            var resultGroupObj = UIFactory.CreateVerticalGroup(Content, new Color(0.1f, 0.1f, 0.1f));
            var resultGroup = resultGroupObj.GetComponent<VerticalLayoutGroup>();
            resultGroup.childForceExpandHeight = false;
            resultGroup.childForceExpandWidth = true;
            resultGroup.childControlHeight = true;
            resultGroup.childControlWidth = true;
            resultGroup.spacing = 5;
            resultGroup.padding.top = 5;
            resultGroup.padding.right = 5;
            resultGroup.padding.left = 5;
            resultGroup.padding.bottom = 5;

            m_resultListPageHandler = new PageHandler();
            m_resultListPageHandler.ConstructUI(resultGroupObj);
            m_resultListPageHandler.OnPageChanged += OnResultPageTurn;

            GameObject scrollObj = UIFactory.CreateScrollView(resultGroupObj, out m_resultListContent, new Color(0.15f, 0.15f, 0.15f));

            // actual result list content
            var contentGroup = m_resultListContent.GetComponent<VerticalLayoutGroup>();
            contentGroup.spacing = 2;
        }

        internal void AddResultButton()
        {
            int thisIndex = m_resultListTexts.Count();

            GameObject btnGroupObj = UIFactory.CreateHorizontalGroup(m_resultListContent, new Color(0.1f, 0.1f, 0.1f));
            HorizontalLayoutGroup btnGroup = btnGroupObj.GetComponent<HorizontalLayoutGroup>();
            btnGroup.childForceExpandWidth = true;
            btnGroup.childControlWidth = true;
            btnGroup.childForceExpandHeight = false;
            btnGroup.childControlHeight = true;
            btnGroup.padding.top = 3;
            btnGroup.padding.left = 3;
            btnGroup.padding.right = 3;
            btnGroup.padding.bottom = 3;
            LayoutElement btnLayout = btnGroupObj.AddComponent<LayoutElement>();
            btnLayout.flexibleWidth = 320;
            btnLayout.minHeight = 25;
            btnLayout.flexibleHeight = 0;
            btnGroupObj.AddComponent<Mask>();

            GameObject mainButtonObj = UIFactory.CreateButton(btnGroupObj);
            LayoutElement mainBtnLayout = mainButtonObj.AddComponent<LayoutElement>();
            mainBtnLayout.minHeight = 25;
            mainBtnLayout.flexibleHeight = 0;
            mainBtnLayout.minWidth = 230;
            mainBtnLayout.flexibleWidth = 0;
            Button mainBtn = mainButtonObj.GetComponent<Button>();
            ColorBlock mainColors = mainBtn.colors;
            mainColors.normalColor = new Color(0.1f, 0.1f, 0.1f);
            mainColors.highlightedColor = new Color(0.2f, 0.2f, 0.2f, 1);
            mainBtn.colors = mainColors;
#if CPP
            mainBtn.onClick.AddListener(new Action(() => { SceneListObjectClicked(thisIndex); }));
#else
            mainBtn.onClick.AddListener(() => { InspectorManager.Instance.Inspect(m_resultShortList[thisIndex]); });
#endif

            Text mainText = mainButtonObj.GetComponentInChildren<Text>();
            mainText.alignment = TextAnchor.MiddleLeft;
            mainText.horizontalOverflow = HorizontalWrapMode.Overflow;
            m_resultListTexts.Add(mainText);
        }

#endregion
    }
}
