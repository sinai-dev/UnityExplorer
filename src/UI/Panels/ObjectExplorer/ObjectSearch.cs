using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Search;
using UnityExplorer.UI.Inspectors;
using UnityExplorer.UI.Models;
using UnityExplorer.UI.ObjectPool;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.Widgets;
using UnityExplorer.UI.Widgets.AutoComplete;

namespace UnityExplorer.UI.Panels
{
    public class ObjectSearch : UIModel
    {
        public ObjectExplorer Parent { get; }

        public ObjectSearch(ObjectExplorer parent)
        {
            Parent = parent;
        }

        private SearchContext m_context = SearchContext.UnityObject;
        private SceneFilter m_sceneFilter = SceneFilter.Any;
        private ChildFilter m_childFilter = ChildFilter.Any;

        public ButtonListSource<object> dataHandler;

        private ScrollPool<ButtonCell> resultsScrollPool;
        private List<object> currentResults = new List<object>();

        public override GameObject UIRoot => uiRoot;
        private GameObject uiRoot;

        private GameObject sceneFilterRow;
        private GameObject childFilterRow;
        private GameObject unityObjectClassRow;

        private InputField nameInputField;
        private InputField classInputField;

        private Text resultsLabel;

        public List<object> GetEntries() => currentResults;

        private void OnContextDropdownChanged(int value)
        {
            m_context = (SearchContext)value;

            // show/hide other filters depending on what we just selected.
            bool shouldShowGoFilters = m_context == SearchContext.GameObject 
                                       || m_context == SearchContext.Component 
                                       || m_context == SearchContext.Custom;

            sceneFilterRow.SetActive(shouldShowGoFilters);
            childFilterRow.SetActive(shouldShowGoFilters);

            unityObjectClassRow.SetActive(m_context == SearchContext.Custom);
        }

        private void OnSceneFilterDropChanged(int value) => m_sceneFilter = (SceneFilter)value;

        private void OnChildFilterDropChanged(int value) => m_childFilter = (ChildFilter)value;

        public void DoSearch()
        {
            cachedCellTexts.Clear();

            if (m_context == SearchContext.Singleton)
                currentResults = SearchProvider.SingletonSearch(nameInputField.text);
            else if (m_context == SearchContext.StaticClass)
                currentResults = SearchProvider.StaticClassSearch(nameInputField.text);
            else
            {
                string compType = "";
                if (m_context == SearchContext.Custom)
                    compType = classInputField.text;

                currentResults = SearchProvider.UnityObjectSearch(nameInputField.text, compType, m_context, m_childFilter, m_sceneFilter);
            }

            dataHandler.RefreshData();
            resultsScrollPool.RefreshCells(true);

            resultsLabel.text = $"{currentResults.Count} results";
        }

        // Cache the syntax-highlighted text for each search result to reduce allocs.
        private static readonly Dictionary<int, string> cachedCellTexts = new Dictionary<int, string>();

        public void SetCell(ButtonCell cell, int index)
        {
            if (!cachedCellTexts.ContainsKey(index))
            {
                string text;
                if (m_context == SearchContext.StaticClass)
                    text = SignatureHighlighter.ParseFullType(currentResults[index] as Type, true, true);
                else
                    text = ToStringUtility.ToStringWithType(currentResults[index], currentResults[index]?.GetActualType());

                cachedCellTexts.Add(index, text);
            }

            cell.Button.ButtonText.text = cachedCellTexts[index];
        }

        private void OnCellClicked(int dataIndex)
        {
            if (m_context == SearchContext.StaticClass)
                InspectorManager.Inspect(currentResults[dataIndex] as Type);
            else
                InspectorManager.Inspect(currentResults[dataIndex]);
        }

        private bool ShouldDisplayCell(object arg1, string arg2) => true;

        public override void ConstructUI(GameObject parent)
        {
            uiRoot = UIFactory.CreateVerticalGroup(parent, "ObjectSearch", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(uiRoot, flexibleHeight: 9999);

            // Search context row

            var contextGroup = UIFactory.CreateHorizontalGroup(uiRoot, "SearchContextRow", false, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(contextGroup, minHeight: 25, flexibleHeight: 0);

            var contextLbl = UIFactory.CreateLabel(contextGroup, "SearchContextLabel", "Searching for:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(contextLbl.gameObject, minWidth: 110, flexibleWidth: 0);

            var contextDropObj = UIFactory.CreateDropdown(contextGroup, out Dropdown contextDrop, null, 14, OnContextDropdownChanged);
            foreach (var name in Enum.GetNames(typeof(SearchContext)))
                contextDrop.options.Add(new Dropdown.OptionData(name));
            UIFactory.SetLayoutElement(contextDropObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            // Unity class input

            unityObjectClassRow = UIFactory.CreateHorizontalGroup(uiRoot, "UnityClassRow", false, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(unityObjectClassRow, minHeight: 25, flexibleHeight: 0);

            var unityClassLbl = UIFactory.CreateLabel(unityObjectClassRow, "UnityClassLabel", "Custom Type:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(unityClassLbl.gameObject, minWidth: 110, flexibleWidth: 0);

            var classInputObj = UIFactory.CreateInputField(unityObjectClassRow, "ClassInput", "...", out this.classInputField);
            UIFactory.SetLayoutElement(classInputObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            new TypeCompleter(typeof(UnityEngine.Object), classInputField);

            unityObjectClassRow.SetActive(false);

            // Child filter row

            childFilterRow = UIFactory.CreateHorizontalGroup(uiRoot, "ChildFilterRow", false, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(childFilterRow, minHeight: 25, flexibleHeight: 0);

            var childLbl = UIFactory.CreateLabel(childFilterRow, "ChildLabel", "Child filter:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(childLbl.gameObject, minWidth: 110, flexibleWidth: 0);

            var childDropObj = UIFactory.CreateDropdown(childFilterRow, out Dropdown childDrop, null, 14, OnChildFilterDropChanged);
            foreach (var name in Enum.GetNames(typeof(ChildFilter)))
                childDrop.options.Add(new Dropdown.OptionData(name));
            UIFactory.SetLayoutElement(childDropObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            childFilterRow.SetActive(false);

            // Scene filter row

            sceneFilterRow = UIFactory.CreateHorizontalGroup(uiRoot, "SceneFilterRow", false, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(sceneFilterRow, minHeight: 25, flexibleHeight: 0);

            var sceneLbl = UIFactory.CreateLabel(sceneFilterRow, "SceneLabel", "Scene filter:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(sceneLbl.gameObject, minWidth: 110, flexibleWidth: 0);

            var sceneDropObj = UIFactory.CreateDropdown(sceneFilterRow, out Dropdown sceneDrop, null, 14, OnSceneFilterDropChanged);
            foreach (var name in Enum.GetNames(typeof(SceneFilter)))
                sceneDrop.options.Add(new Dropdown.OptionData(name));
            UIFactory.SetLayoutElement(sceneDropObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            sceneFilterRow.SetActive(false);

            // Name filter input

            var nameRow = UIFactory.CreateHorizontalGroup(uiRoot, "NameRow", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(nameRow, minHeight: 25, flexibleHeight: 0);

            var nameLbl = UIFactory.CreateLabel(nameRow, "NameFilterLabel", "Name contains:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(nameLbl.gameObject, minWidth: 110, flexibleWidth: 0);

            var nameInputObj = UIFactory.CreateInputField(nameRow, "NameFilterInput", "...", out this.nameInputField);
            UIFactory.SetLayoutElement(nameInputObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            // Search button

            var searchButton = UIFactory.CreateButton(uiRoot, "SearchButton", "Search");
            UIFactory.SetLayoutElement(searchButton.Button.gameObject, minHeight: 25, flexibleHeight: 0);
            searchButton.OnClick += DoSearch;

            // Results count label

            var resultsCountRow = UIFactory.CreateHorizontalGroup(uiRoot, "ResultsCountRow", true, true, true, true);
            UIFactory.SetLayoutElement(resultsCountRow, minHeight: 25, flexibleHeight: 0);

            resultsLabel = UIFactory.CreateLabel(resultsCountRow, "ResultsLabel", "0 results", TextAnchor.MiddleCenter);

            // RESULTS SCROLL POOL

            dataHandler = new ButtonListSource<object>(resultsScrollPool, GetEntries, SetCell, ShouldDisplayCell, OnCellClicked);
            resultsScrollPool = UIFactory.CreateScrollPool<ButtonCell>(uiRoot, "ResultsList", out GameObject scrollObj, out GameObject scrollContent);

            //if (!Pool<ButtonCell>.PrototypeObject)
            //    Pool<ButtonCell>.PrototypeObject = ButtonCell.CreatePrototypeCell(Pool<ButtonCell>.InactiveHolder).gameObject;

            resultsScrollPool.Initialize(dataHandler);//, ButtonCell.CreatePrototypeCell(uiRoot));
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
        }
    }
}
