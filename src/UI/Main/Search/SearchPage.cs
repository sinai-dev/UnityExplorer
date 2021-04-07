using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Reflection;
using UnityExplorer.Core.Runtime;
using UnityExplorer.Core;
using UnityExplorer.UI.Utility;
using UnityExplorer.Core.Search;
using UnityExplorer.UI.Main.Home;
using UnityExplorer.UI.Inspectors;

namespace UnityExplorer.UI.Main.Search
{
    public class SearchPage : BaseMenuPage
    {
        public override string Name => "Search";
        public override MenuPages Type => MenuPages.Search;

        public static SearchPage Instance;

        internal SearchContext m_context;
        private SceneFilter m_sceneFilter;
        private ChildFilter m_childFilter;

        // ui elements

        private Text m_resultCountText;

        private InputField m_customTypeInput;

        private InputField m_nameInput;

        private Button m_selectedContextButton;
        private readonly Dictionary<SearchContext, Button> m_contextButtons = new Dictionary<SearchContext, Button>();

        internal Dropdown m_sceneDropdown;
        private int m_lastSceneCount = -1;

        private GameObject m_extraFilterRow;

        // Results

        internal object[] m_results;
        internal readonly List<object> m_resultShortList = new List<object>();

        private int m_lastCount;
        public PageHandler m_resultListPageHandler;
        private GameObject m_resultListContent;
        private readonly List<Text> m_resultListTexts = new List<Text>();

        public SearchPage()
        {
            Instance = this;
        }

        public override bool Init()
        {
            ConstructUI();

            return true;
        }

        public void OnSceneChange()
        {
            m_results = new object[0];
            RefreshResultList();
        }

        public override void Update()
        {
            if (HaveScenesChanged())
            {
                RefreshSceneDropdown();
            }

            if (m_customTypeInput.isFocused && m_context != SearchContext.Custom)
            {
                OnContextButtonClicked(SearchContext.Custom);
            }
        }

        // Updating result list content

        private void RefreshResultList()
        {
            if (m_resultListPageHandler == null || m_results == null)
                return;

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
                    var unityObj = obj as UnityEngine.Object;

                    if (obj == null || (unityObj != null && !unityObj))
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

                    if (m_context != SearchContext.StaticClass)
                    {
                        var name = SignatureHighlighter.ParseFullSyntax(ReflectionUtility.GetActualType(obj), true);

                        if (unityObj && m_context != SearchContext.Singleton)
                        {
                            if (unityObj && !string.IsNullOrEmpty(unityObj.name))
                                name += $": {unityObj.name}";
                            else
                                name += ": <i><color=grey>untitled</color></i>";
                        }

                        text.text = name;
                    }
                    else
                    {
                        var type = obj as Type;
                        text.text = SignatureHighlighter.ParseFullSyntax(type, true);
                    }

                    var label = text.transform.parent.parent.gameObject;
                    if (!label.activeSelf)
                        label.SetActive(true);
                }
            }

            m_lastCount = newCount;
        }

        // scene dropdown update

        internal bool HaveScenesChanged()
        {
            if (m_lastSceneCount != SceneManager.sceneCount)
                return true;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                int dropdownIndex = i + 3;
                if (dropdownIndex >= m_sceneDropdown.options.Count
                    || m_sceneDropdown.options[dropdownIndex].text != SceneManager.GetSceneAt(i).name)
                    return true;
            }

            return false;
        }

        internal void RefreshSceneDropdown()
        {
            m_sceneDropdown.OnCancel(null);

            m_sceneDropdown.options.Clear();

            m_sceneDropdown.options.Add(new Dropdown.OptionData
            {
                text = "Any"
            });

            m_sceneDropdown.options.Add(new Dropdown.OptionData
            {
                text = "None (Asset / Resource)"
            });
            m_sceneDropdown.options.Add(new Dropdown.OptionData
            {
                text = "DontDestroyOnLoad"
            });

            m_lastSceneCount = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                m_lastSceneCount++;

                var scene = SceneManager.GetSceneAt(i).name;
                m_sceneDropdown.options.Add(new Dropdown.OptionData
                {
                    text = scene
                });
            }

            m_sceneDropdown.itemText.text = "Any";
            m_sceneFilter = SceneFilter.Any;
        }

        // ~~~~~ UI Callbacks ~~~~~

        internal void OnSearchClicked()
        {
            m_resultListPageHandler.CurrentPage = 0;

            if (m_context == SearchContext.StaticClass)
                m_results = SearchProvider.StaticClassSearch(m_nameInput.text);
            else if (m_context == SearchContext.Singleton)
                m_results = SearchProvider.SingletonSearch(m_nameInput.text);
            else
                m_results = SearchProvider.UnityObjectSearch(m_nameInput.text, m_customTypeInput.text, m_context, m_childFilter, m_sceneFilter);

            if (m_results == null)
                m_results = new object[0];

            RefreshResultList();

            if (m_results.Length > 0)
                m_resultCountText.text = $"{m_results.Length} Results";
            else
                m_resultCountText.text = "No results...";
        }

        private void OnResultPageTurn()
        {
            RefreshResultList();
        }

        internal void OnResultClicked(int index)
        {
            if (m_context == SearchContext.StaticClass)
                InspectorManager.Instance.Inspect((Type)m_resultShortList[index]);
            else
                InspectorManager.Instance.Inspect(m_resultShortList[index]);
        }

        internal void OnContextButtonClicked(SearchContext context)
        {
            if (m_selectedContextButton && m_context == context)
                return;

            if (m_selectedContextButton)
                UIFactory.SetDefaultSelectableColors(m_selectedContextButton);

            var button = m_contextButtons[context];

            m_selectedContextButton = button;

            m_selectedContextButton.colors = RuntimeProvider.Instance.SetColorBlock(m_selectedContextButton.colors, 
                new Color(0.35f, 0.7f, 0.35f), new Color(0.35f, 0.7f, 0.35f));

            m_context = context;

            // if extra filters are valid
            if (context == SearchContext.Component
                || context == SearchContext.GameObject
                || context == SearchContext.Custom)
            {
                m_extraFilterRow?.SetActive(true);
            }
            else
            {
                m_extraFilterRow?.SetActive(false);
            }
        }

        #region UI CONSTRUCTION

        internal void ConstructUI()
        {
            GameObject parent = MainMenu.Instance.PageViewport;

            Content = UIFactory.CreateVerticalGroup(parent, "SearchPage", true, true, true, true, 5, new Vector4(4,4,4,4));

            ConstructTopArea();

            ConstructResultsArea();
        }

        internal void ConstructTopArea()
        {
            var topAreaObj = UIFactory.CreateVerticalGroup(Content, "TitleArea", true, false, true, true, 5, new Vector4(5,5,5,5),
                new Color(0.15f, 0.15f, 0.15f));

            var titleLabel = UIFactory.CreateLabel(topAreaObj, "SearchTitle", "Search", TextAnchor.UpperLeft, Color.white, true, 25);
            UIFactory.SetLayoutElement(titleLabel.gameObject, minHeight: 30, flexibleHeight: 0);

            // top area options

            var optionsGroupObj = UIFactory.CreateVerticalGroup(topAreaObj, "OptionsArea", true, false, true, true, 10, new Vector4(4,4,4,4),
                new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(optionsGroupObj, minWidth: 500, minHeight: 70, flexibleHeight: 100);

            // search context row

            var contextRowObj = UIFactory.CreateHorizontalGroup(optionsGroupObj, "ContextFilters", false, false, true, true, 3, default,
                new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(contextRowObj, minHeight: 25);

            var contextLabelObj = UIFactory.CreateLabel(contextRowObj, "ContextLabel", "Searching for:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(contextLabelObj.gameObject, minWidth: 125, minHeight: 25);

            // context buttons

            AddContextButton(contextRowObj, "UnityEngine.Object", SearchContext.UnityObject, 140);
            AddContextButton(contextRowObj, "GameObject", SearchContext.GameObject);
            AddContextButton(contextRowObj, "Component", SearchContext.Component);
            AddContextButton(contextRowObj, "Custom...", SearchContext.Custom);

            // custom type input

            var customTypeObj = UIFactory.CreateInputField(contextRowObj, "CustomTypeInput", "eg. UnityEngine.Texture2D, etc...");
            UIFactory.SetLayoutElement(customTypeObj, minWidth: 250, flexibleWidth: 2000, minHeight: 25, flexibleHeight: 0);
            m_customTypeInput = customTypeObj.GetComponent<InputField>();

            // static class and singleton buttons

            var secondRow = UIFactory.CreateHorizontalGroup(optionsGroupObj, "SecondRow", false, false, true, true, 3, default, new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(secondRow, minHeight: 25);

            var spacer = UIFactory.CreateUIObject("spacer", secondRow);
            UIFactory.SetLayoutElement(spacer, minWidth: 25, minHeight: 25);

            AddContextButton(secondRow, "Static Class", SearchContext.StaticClass);
            AddContextButton(secondRow, "Singleton", SearchContext.Singleton);

            // search input

            var nameRowObj = UIFactory.CreateHorizontalGroup(optionsGroupObj, "SearchInput", true, false, true, true, 0, default, new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(nameRowObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 5000);

            var nameLabel = UIFactory.CreateLabel(nameRowObj, "NameLabel", "Name contains:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(nameLabel.gameObject, minWidth: 125, minHeight: 25);

            var nameInputObj = UIFactory.CreateInputField(nameRowObj, "NameInputField", "...");
            m_nameInput = nameInputObj.GetComponent<InputField>();
            UIFactory.SetLayoutElement(nameInputObj, minWidth: 150, flexibleWidth: 5000, minHeight: 25);

            // extra filter row

            m_extraFilterRow = UIFactory.CreateHorizontalGroup(optionsGroupObj, "ExtraFilterRow", false, true, true, true, 0, default, new Color(1, 1, 1, 0));
            m_extraFilterRow.SetActive(false);
            UIFactory.SetLayoutElement(m_extraFilterRow, minHeight: 25, minWidth: 125, flexibleHeight: 0, flexibleWidth: 150);

            // scene filter

            var sceneLabelObj = UIFactory.CreateLabel(m_extraFilterRow, "SceneFilterLabel", "Scene filter:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(sceneLabelObj.gameObject, minWidth: 125, minHeight: 25);

            var sceneDropObj = UIFactory.CreateDropdown(m_extraFilterRow, 
                out m_sceneDropdown,
                "Any", 
                12, 
                (int value) => { m_sceneFilter = (SceneFilter)value; }
            );

            UIFactory.SetLayoutElement(sceneDropObj, minWidth: 220, minHeight: 25);

            // invisible space

            var invis = UIFactory.CreateUIObject("spacer", m_extraFilterRow);
            UIFactory.SetLayoutElement(invis, minWidth: 25, flexibleWidth: 0);

            // children filter

            var childLabelObj = UIFactory.CreateLabel(m_extraFilterRow, "ChildFilterLabel", "Child filter:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(childLabelObj.gameObject, minWidth: 100, minHeight: 25);

            var childDropObj = UIFactory.CreateDropdown(m_extraFilterRow, 
                out Dropdown childDrop, 
                "Any", 
                12,
                (int value) => { m_childFilter = (ChildFilter)value; },
                new[] { "Any", "Root Objects Only", "Children Only" });

            UIFactory.SetLayoutElement(childDropObj, minWidth: 180, minHeight: 25);

            // search button

            var searchBtnObj = UIFactory.CreateButton(topAreaObj, "SearchButton", "Search", OnSearchClicked);
            UIFactory.SetLayoutElement(searchBtnObj.gameObject, minHeight: 30, flexibleHeight: 0);
        }

        internal void AddContextButton(GameObject parent, string label, SearchContext context, float width = 110)
        {
            var btn = UIFactory.CreateButton(parent, $"Context_{context}", label, () => { OnContextButtonClicked(context); });
            UIFactory.SetLayoutElement(btn.gameObject, minHeight: 25, minWidth: (int)width);

            m_contextButtons.Add(context, btn);

            // if first button
            if (!m_selectedContextButton)
                OnContextButtonClicked(context);
        }

        internal void ConstructResultsArea()
        {
            // Result group holder (NOT actual result list content)

            var resultGroupObj = UIFactory.CreateVerticalGroup(Content, "SearchResults", true, false, true, true, 5, new Vector4(5,5,5,5), 
                new Color(1, 1, 1, 0));

            m_resultCountText = UIFactory.CreateLabel(resultGroupObj, "ResultsLabel", "No results...", TextAnchor.MiddleCenter);

            GameObject scrollObj = UIFactory.CreateScrollView(resultGroupObj,
                "ResultsScrollView",
                out m_resultListContent,
                out SliderScrollbar scroller,
                new Color(0.07f, 0.07f, 0.07f, 1));

            m_resultListPageHandler = new PageHandler(scroller);
            m_resultListPageHandler.ConstructUI(resultGroupObj);
            m_resultListPageHandler.OnPageChanged += OnResultPageTurn;

            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(m_resultListContent, forceHeight: false, childControlHeight: true, spacing: 2);
        }

        internal void AddResultButton()
        {
            int thisIndex = m_resultListTexts.Count();

            GameObject btnGroupObj = UIFactory.CreateHorizontalGroup(m_resultListContent, "ResultButtonGroup", 
                true, false, true, true, 0, new Vector4(1,1,1,1), new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(btnGroupObj, flexibleWidth: 320, minHeight: 25, flexibleHeight: 0);
            btnGroupObj.AddComponent<Mask>();

            var mainColors = new ColorBlock();
            RuntimeProvider.Instance.SetColorBlock(mainColors, new Color(0.1f, 0.1f, 0.1f),
                new Color(0.2f, 0.2f, 0.2f), new Color(0.05f, 0.05f, 0.05f));

            var mainButton = UIFactory.CreateButton(btnGroupObj, 
                "ResultButton",
                "", 
                () => { OnResultClicked(thisIndex); },
                mainColors);

            UIFactory.SetLayoutElement(mainButton.gameObject, minHeight: 25, flexibleHeight: 0, minWidth: 320, flexibleWidth: 0);

            Text text = mainButton.GetComponentInChildren<Text>();
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            m_resultListTexts.Add(text);
        }

        #endregion
    }
}
