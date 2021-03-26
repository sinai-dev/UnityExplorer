using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExplorer.Core.Inspectors;
using UnityExplorer.UI.Reusable;
using System.Reflection;
using UnityExplorer.Core.Runtime;
using UnityExplorer.Core;
using UnityExplorer.Search;
using UnityExplorer.UI.Utility;

namespace UnityExplorer.UI.Main
{
    public class SearchPage : BaseMenuPage
    {
        public override string Name => "Search";

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
                        var name = SignatureHighlighter.ParseFullSyntax(ReflectionUtility.GetType(obj), true);

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

            m_sceneDropdown.transform.Find("Label").GetComponent<Text>().text = "Any";
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
                UIFactory.SetDefaultColorTransitionValues(m_selectedContextButton);

            var button = m_contextButtons[context];

            m_selectedContextButton = button;

            var colors = m_selectedContextButton.colors;
            colors.normalColor = new Color(0.35f, 0.7f, 0.35f);
            colors.highlightedColor = colors.normalColor;
            m_selectedContextButton.colors = colors;

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

            Content = UIFactory.CreateVerticalGroup(parent);
            var mainGroup = Content.GetComponent<VerticalLayoutGroup>();
            mainGroup.padding.left = 4;
            mainGroup.padding.right = 4;
            mainGroup.padding.top = 4;
            mainGroup.padding.bottom = 4;
            mainGroup.spacing = 5;
            mainGroup.childForceExpandHeight = true;
            mainGroup.childForceExpandWidth = true;
            mainGroup.SetChildControlHeight(true);
            mainGroup.SetChildControlWidth(true);

            ConstructTopArea();

            ConstructResultsArea();
        }

        internal void ConstructTopArea()
        {
            var topAreaObj = UIFactory.CreateVerticalGroup(Content, new Color(0.15f, 0.15f, 0.15f));
            var topGroup = topAreaObj.GetComponent<VerticalLayoutGroup>();
            topGroup.childForceExpandHeight = false;
            topGroup.SetChildControlHeight(true);
            topGroup.childForceExpandWidth = true;
            topGroup.SetChildControlWidth(true);
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

            var optionsGroupObj = UIFactory.CreateVerticalGroup(topAreaObj, new Color(0.1f, 0.1f, 0.1f));
            var optionsGroup = optionsGroupObj.GetComponent<VerticalLayoutGroup>();
            optionsGroup.childForceExpandHeight = false;
            optionsGroup.SetChildControlHeight(true);
            optionsGroup.childForceExpandWidth = true;
            optionsGroup.SetChildControlWidth(true);
            optionsGroup.spacing = 10;
            optionsGroup.padding.top = 4;
            optionsGroup.padding.right = 4;
            optionsGroup.padding.left = 4;
            optionsGroup.padding.bottom = 4;
            var optionsLayout = optionsGroupObj.AddComponent<LayoutElement>();
            optionsLayout.minWidth = 500;
            optionsLayout.minHeight = 70;
            optionsLayout.flexibleHeight = 100;

            // search context row

            var contextRowObj = UIFactory.CreateHorizontalGroup(optionsGroupObj, new Color(1, 1, 1, 0));
            var contextGroup = contextRowObj.GetComponent<HorizontalLayoutGroup>();
            contextGroup.childForceExpandWidth = false;
            contextGroup.SetChildControlWidth(true);
            contextGroup.childForceExpandHeight = false;
            contextGroup.SetChildControlHeight(true);
            contextGroup.spacing = 3;
            var contextLayout = contextRowObj.AddComponent<LayoutElement>();
            contextLayout.minHeight = 25;

            var contextLabelObj = UIFactory.CreateLabel(contextRowObj, TextAnchor.MiddleLeft);
            var contextText = contextLabelObj.GetComponent<Text>();
            contextText.text = "Searching for:";
            var contextLabelLayout = contextLabelObj.AddComponent<LayoutElement>();
            contextLabelLayout.minWidth = 125;
            contextLabelLayout.minHeight = 25;

            // context buttons

            AddContextButton(contextRowObj, "UnityEngine.Object",   SearchContext.UnityObject, 140);
            AddContextButton(contextRowObj, "GameObject",           SearchContext.GameObject);
            AddContextButton(contextRowObj, "Component",            SearchContext.Component);
            AddContextButton(contextRowObj, "Custom...",            SearchContext.Custom);

            // custom type input

            var customTypeObj = UIFactory.CreateInputField(contextRowObj);
            var customTypeLayout = customTypeObj.AddComponent<LayoutElement>();
            customTypeLayout.minWidth = 250;
            customTypeLayout.flexibleWidth = 2000;
            customTypeLayout.minHeight = 25;
            customTypeLayout.flexibleHeight = 0;
            m_customTypeInput = customTypeObj.GetComponent<InputField>();
            m_customTypeInput.placeholder.gameObject.GetComponent<Text>().text = "eg. UnityEngine.Texture2D, etc...";

            // static class and singleton buttons

            var secondRow = UIFactory.CreateHorizontalGroup(optionsGroupObj, new Color(1, 1, 1, 0));
            var secondGroup = secondRow.GetComponent<HorizontalLayoutGroup>();
            secondGroup.childForceExpandWidth = false;
            secondGroup.childForceExpandHeight = false;
            secondGroup.spacing = 3;
            var secondLayout = secondRow.AddComponent<LayoutElement>();
            secondLayout.minHeight = 25;
            var spacer = UIFactory.CreateUIObject("spacer", secondRow);
            var spaceLayout = spacer.AddComponent<LayoutElement>();
            spaceLayout.minWidth = 125;
            spaceLayout.minHeight = 25;

            AddContextButton(secondRow, "Static Class", SearchContext.StaticClass);
            AddContextButton(secondRow, "Singleton", SearchContext.Singleton);

            // search input

            var nameRowObj = UIFactory.CreateHorizontalGroup(optionsGroupObj, new Color(1, 1, 1, 0));
            var nameRowGroup = nameRowObj.GetComponent<HorizontalLayoutGroup>();
            nameRowGroup.childForceExpandWidth = true;
            nameRowGroup.SetChildControlWidth(true);
            nameRowGroup.childForceExpandHeight = false;
            nameRowGroup.SetChildControlHeight(true);
            var nameRowLayout = nameRowObj.AddComponent<LayoutElement>();
            nameRowLayout.minHeight = 25;
            nameRowLayout.flexibleHeight = 0;
            nameRowLayout.flexibleWidth = 5000;

            var nameLabelObj = UIFactory.CreateLabel(nameRowObj, TextAnchor.MiddleLeft);
            var nameLabelText = nameLabelObj.GetComponent<Text>();
            nameLabelText.text = "Name contains:";
            var nameLabelLayout = nameLabelObj.AddComponent<LayoutElement>();
            nameLabelLayout.minWidth = 125;
            nameLabelLayout.minHeight = 25;

            var nameInputObj = UIFactory.CreateInputField(nameRowObj);
            m_nameInput = nameInputObj.GetComponent<InputField>();
            //m_nameInput.placeholder.gameObject.GetComponent<TextMeshProUGUI>().text = "";
            var nameInputLayout = nameInputObj.AddComponent<LayoutElement>();
            nameInputLayout.minWidth = 150;
            nameInputLayout.flexibleWidth = 5000;
            nameInputLayout.minHeight = 25;

            // extra filter row

            m_extraFilterRow = UIFactory.CreateHorizontalGroup(optionsGroupObj, new Color(1, 1, 1, 0));
            m_extraFilterRow.SetActive(false);
            var extraGroup = m_extraFilterRow.GetComponent<HorizontalLayoutGroup>();
            extraGroup.childForceExpandHeight = true;
            extraGroup.SetChildControlHeight(true);
            extraGroup.childForceExpandWidth = false;
            extraGroup.SetChildControlWidth(true);
            var filterRowLayout = m_extraFilterRow.AddComponent<LayoutElement>();
            filterRowLayout.minHeight = 25;
            filterRowLayout.flexibleHeight = 0;
            filterRowLayout.minWidth = 125;
            filterRowLayout.flexibleWidth = 150;

            // scene filter

            var sceneLabelObj = UIFactory.CreateLabel(m_extraFilterRow, TextAnchor.MiddleLeft);
            var sceneLabel = sceneLabelObj.GetComponent<Text>();
            sceneLabel.text = "Scene Filter:";
            var sceneLayout = sceneLabelObj.AddComponent<LayoutElement>();
            sceneLayout.minWidth = 125;
            sceneLayout.minHeight = 25;

            var sceneDropObj = UIFactory.CreateDropdown(m_extraFilterRow, out m_sceneDropdown);
            m_sceneDropdown.itemText.text = "Any";
            m_sceneDropdown.itemText.fontSize = 12;
            var sceneDropLayout = sceneDropObj.AddComponent<LayoutElement>();
            sceneDropLayout.minWidth = 220;
            sceneDropLayout.minHeight = 25;

            m_sceneDropdown.onValueChanged.AddListener(OnSceneDropdownChanged);
            void OnSceneDropdownChanged(int value)
            {
                if (value < 4)
                    m_sceneFilter = (SceneFilter)value;
                else
                    m_sceneFilter = SceneFilter.Explicit;
            }

            // invisible space

            var invis = UIFactory.CreateUIObject("spacer", m_extraFilterRow);
            var invisLayout = invis.AddComponent<LayoutElement>();
            invisLayout.minWidth = 25;
            invisLayout.flexibleWidth = 0;

            // children filter

            var childLabelObj = UIFactory.CreateLabel(m_extraFilterRow, TextAnchor.MiddleLeft);
            var childLabel = childLabelObj.GetComponent<Text>();
            childLabel.text = "Child Filter:";
            var childLayout = childLabelObj.AddComponent<LayoutElement>();
            childLayout.minWidth = 100;
            childLayout.minHeight = 25;

            var childDropObj = UIFactory.CreateDropdown(m_extraFilterRow, out Dropdown childDrop);
            childDrop.itemText.text = "Any";
            childDrop.itemText.fontSize = 12;
            var childDropLayout = childDropObj.AddComponent<LayoutElement>();
            childDropLayout.minWidth = 180;
            childDropLayout.minHeight = 25;

            childDrop.options.Add(new Dropdown.OptionData { text = "Any" });
            childDrop.options.Add(new Dropdown.OptionData { text = "Root Objects Only" });
            childDrop.options.Add(new Dropdown.OptionData { text = "Children Only" });

            childDrop.onValueChanged.AddListener(OnChildDropdownChanged);
            void OnChildDropdownChanged(int value)
            {
                m_childFilter = (ChildFilter)value;
            }

            // search button

            var searchBtnObj = UIFactory.CreateButton(topAreaObj);
            var searchText = searchBtnObj.GetComponentInChildren<Text>();
            searchText.text = "Search";
            LayoutElement searchBtnLayout = searchBtnObj.AddComponent<LayoutElement>();
            searchBtnLayout.minHeight = 30;
            searchBtnLayout.flexibleHeight = 0;
            var searchBtn = searchBtnObj.GetComponent<Button>();

            searchBtn.onClick.AddListener(OnSearchClicked);
        }

        internal void AddContextButton(GameObject parent, string label, SearchContext context, float width = 110)
        {
            var btnObj = UIFactory.CreateButton(parent);

            var btn = btnObj.GetComponent<Button>();

            m_contextButtons.Add(context, btn);

            btn.onClick.AddListener(() => { OnContextButtonClicked(context); });

            var btnLayout = btnObj.AddComponent<LayoutElement>();
            btnLayout.minHeight = 25;
            btnLayout.minWidth = width;

            var btnText = btnObj.GetComponentInChildren<Text>();
            btnText.text = label;

            // if first button
            if (!m_selectedContextButton)
            {
                OnContextButtonClicked(context);
            }
        }

        internal void ConstructResultsArea()
        {
            // Result group holder (NOT actual result list content)

            var resultGroupObj = UIFactory.CreateVerticalGroup(Content, new Color(1,1,1,0));
            var resultGroup = resultGroupObj.GetComponent<VerticalLayoutGroup>();
            resultGroup.childForceExpandHeight = false;
            resultGroup.childForceExpandWidth = true;
            resultGroup.SetChildControlHeight(true);
            resultGroup.SetChildControlWidth(true);
            resultGroup.spacing = 5;
            resultGroup.padding.top = 5;
            resultGroup.padding.right = 5;
            resultGroup.padding.left = 5;
            resultGroup.padding.bottom = 5;

            var resultCountObj = UIFactory.CreateLabel(resultGroupObj, TextAnchor.MiddleCenter);
            m_resultCountText = resultCountObj.GetComponent<Text>();
            m_resultCountText.text = "No results...";

            GameObject scrollObj = UIFactory.CreateScrollView(resultGroupObj, 
                out m_resultListContent, 
                out SliderScrollbar scroller, 
                new Color(0.07f, 0.07f, 0.07f, 1));

            m_resultListPageHandler = new PageHandler(scroller);
            m_resultListPageHandler.ConstructUI(resultGroupObj);
            m_resultListPageHandler.OnPageChanged += OnResultPageTurn;

            // actual result list content
            var contentGroup = m_resultListContent.GetComponent<VerticalLayoutGroup>();
            contentGroup.spacing = 2;
            contentGroup.childForceExpandHeight = false;
            contentGroup.SetChildControlHeight(true);
        }

        internal void AddResultButton()
        {
            int thisIndex = m_resultListTexts.Count();

            GameObject btnGroupObj = UIFactory.CreateHorizontalGroup(m_resultListContent, new Color(0.1f, 0.1f, 0.1f));
            HorizontalLayoutGroup btnGroup = btnGroupObj.GetComponent<HorizontalLayoutGroup>();
            btnGroup.childForceExpandWidth = true;
            btnGroup.SetChildControlWidth(true);
            btnGroup.childForceExpandHeight = false;
            btnGroup.SetChildControlHeight(true);
            btnGroup.padding.top = 1;
            btnGroup.padding.left = 1;
            btnGroup.padding.right = 1;
            btnGroup.padding.bottom = 1;
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

            mainBtn.onClick.AddListener(() => { OnResultClicked(thisIndex); });

            Text mainText = mainButtonObj.GetComponentInChildren<Text>();
            mainText.alignment = TextAnchor.MiddleLeft;
            mainText.horizontalOverflow = HorizontalWrapMode.Overflow;
            m_resultListTexts.Add(mainText);
        }

#endregion
    }
}
