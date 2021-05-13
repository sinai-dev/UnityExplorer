using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.Core.Input;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI;
using UnityExplorer.UI.ObjectPool;
using UnityExplorer.UI.Panels;

namespace UnityExplorer.UI.Widgets.AutoComplete
{
    // Shared modal panel for "AutoComplete" suggestions.
    // A data source implements ISuggestionProvider and uses TakeOwnership and ReleaseOwnership
    // for control, and SetSuggestions to set the actual suggestion data.

    public class AutoCompleteModal : UIPanel
    {
        public static AutoCompleteModal Instance => UIManager.GetPanel<AutoCompleteModal>(UIManager.Panels.AutoCompleter);

        public override string Name => "AutoCompleter";
        public override UIManager.Panels PanelType => UIManager.Panels.AutoCompleter;
        public override int MinWidth => -1;
        public override int MinHeight => -1;

        public override bool CanDragAndResize => false;
        public override bool ShouldSaveActiveState => false;
        public override bool NavButtonWanted => false;

        public ISuggestionProvider CurrentHandler { get; private set; }

        public ButtonListSource<Suggestion> dataHandler;
        public ScrollPool<ButtonCell> scrollPool;

        private List<Suggestion> suggestions = new List<Suggestion>();

        public AutoCompleteModal()
        {
            OnPanelsReordered += UIPanel_OnPanelsReordered;
            OnClickedOutsidePanels += AutoCompleter_OnClickedOutsidePanels;
        }

        private void AutoCompleter_OnClickedOutsidePanels()
        {
            if (!this.UIRoot || !this.UIRoot.activeInHierarchy)
                return;

            if (CurrentHandler != null)
                ReleaseOwnership(CurrentHandler);
            else
                UIRoot.SetActive(false);
        }

        private void UIPanel_OnPanelsReordered()
        {
            if (!this.UIRoot || !this.UIRoot.activeInHierarchy)
                return;

            if (this.UIRoot.transform.GetSiblingIndex() != UIManager.PanelHolder.transform.childCount - 1)
            {
                if (CurrentHandler != null)
                    ReleaseOwnership(CurrentHandler);
                else
                    UIRoot.SetActive(false);
            }
        }

        public override void Update()
        {
            if (!UIRoot || !UIRoot.activeSelf)
                return;

            if (suggestions.Any() && CurrentHandler != null)
            {
                if (!CurrentHandler.InputField.UIRoot.activeInHierarchy)
                    ReleaseOwnership(CurrentHandler);
                else
                {
                    UpdatePosition();
                }
            }
        }

        public void TakeOwnership(ISuggestionProvider provider)
        {
            CurrentHandler = provider;
        }

        public void ReleaseOwnership(ISuggestionProvider provider)
        {
            if (CurrentHandler == null)
                return;

            if (CurrentHandler == provider)
            {
                CurrentHandler = null;
                UIRoot.SetActive(false);
            }
        }

        private List<Suggestion> GetEntries() => suggestions;

        private bool ShouldDisplay(Suggestion data, string filter) => true;

        public void SetSuggestions(IEnumerable<Suggestion> collection)
        {
            suggestions = collection as List<Suggestion> ?? collection.ToList();

            if (!suggestions.Any())
                UIRoot.SetActive(false);
            else
            {
                UIRoot.SetActive(true);
                UIRoot.transform.SetAsLastSibling();
                dataHandler.RefreshData();
                scrollPool.Refresh(true, true);
            }
        }

        private void OnCellClicked(int dataIndex)
        {
            var suggestion = suggestions[dataIndex];
            CurrentHandler.OnSuggestionClicked(suggestion);
        }

        private void SetCell(ButtonCell cell, int index)
        {
            if (index < 0 || index >= suggestions.Count)
            {
                cell.Disable();
                return;
            }

            var suggestion = suggestions[index];
            cell.Button.ButtonText.text = suggestion.DisplayText;
        }

        private int lastCaretPosition;
        private Vector3 lastInputPosition;

        private void UpdatePosition()
        {
            if (CurrentHandler == null || !CurrentHandler.InputField.Component.isFocused)
                return;

            var input = CurrentHandler.InputField;

            if (input.Component.caretPosition == lastCaretPosition && input.UIRoot.transform.position == lastInputPosition)
                return;
            lastInputPosition = input.UIRoot.transform.position;
            lastCaretPosition = input.Component.caretPosition;

            if (CurrentHandler.AnchorToCaretPosition)
            {
                var textGen = input.Component.cachedInputTextGenerator;
                int caretIdx = Math.Max(0, Math.Min(textGen.characterCount - 1, input.Component.caretPosition));

                // normalize the caret horizontal position
                Vector3 caretPos = textGen.characters[caretIdx].cursorPos;
                // transform to world point
                caretPos = input.UIRoot.transform.TransformPoint(caretPos);
                caretPos += new Vector3(input.Rect.rect.width * 0.5f, -(input.Rect.rect.height * 0.5f), 0);

                uiRoot.transform.position = new Vector3(caretPos.x + 10, caretPos.y - 30, 0);
            }
            else
            {
                var textGen = input.Component.textComponent.cachedTextGenerator;
                var pos = input.UIRoot.transform.TransformPoint(textGen.characters[0].cursorPos);
                uiRoot.transform.position = new Vector3(pos.x + 10, pos.y - 20, 0);
            }

            this.Dragger.OnEndResize();
        }

        protected internal override void DoSetDefaultPosAndAnchors()
        {
            var mainRect = uiRoot.GetComponent<RectTransform>();
            mainRect.pivot = new Vector2(0f, 1f);
            mainRect.anchorMin = new Vector2(0.42f, 0.4f);
            mainRect.anchorMax = new Vector2(0.68f, 0.6f);
        }

        public override void ConstructPanelContent()
        {
            dataHandler = new ButtonListSource<Suggestion>(scrollPool, GetEntries, SetCell, ShouldDisplay, OnCellClicked);

            scrollPool = UIFactory.CreateScrollPool<ButtonCell>(this.content, "AutoCompleter", out GameObject scrollObj, out GameObject scrollContent);
            scrollPool.Initialize(dataHandler);
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);

            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(scrollContent, true, false, true, false);

            UIRoot.SetActive(false);
        }

        public override void DoSaveToConfigElement()
        {
            // not savable
        }

        public override string GetSaveDataFromConfigManager() => null;
    }
}
