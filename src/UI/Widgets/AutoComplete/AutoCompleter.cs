using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.Core.Input;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI;
using UnityExplorer.UI.Models;

namespace UnityExplorer.UI.Widgets.AutoComplete
{
    public class AutoCompleter : UIPanel
    {
        // Static

        public static AutoCompleter Instance => UIManager.AutoCompleter;

        // Instance

        public AutoCompleter()
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

        public override string Name => "AutoCompleter";
        public override UIManager.Panels PanelType => UIManager.Panels.AutoCompleter;

        public override bool CanDrag => false;

        public ISuggestionProvider CurrentHandler { get; private set; }

        public ButtonListSource<Suggestion> dataHandler;
        public ScrollPool scrollPool;

        private List<Suggestion> suggestions = new List<Suggestion>();

        private int lastCaretPos;

        public override void Update()
        {
            if (!UIRoot || !UIRoot.activeSelf)
                return;

            if (suggestions.Any() && CurrentHandler != null)
            {
                if (!CurrentHandler.InputField.gameObject.activeInHierarchy)
                    ReleaseOwnership(CurrentHandler);
                else
                {
                    lastCaretPos = CurrentHandler.InputField.caretPosition;
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

        public void SetSuggestions(List<Suggestion> collection)
        {
            suggestions = collection;

            if (!suggestions.Any())
                UIRoot.SetActive(false);
            else
            {
                UIRoot.SetActive(true);
                UIRoot.transform.SetAsLastSibling();
                dataHandler.RefreshData();
                scrollPool.RefreshAndJumpToTop();
            }
        }

        private void OnCellClicked(int dataIndex)
        {
            var suggestion = suggestions[dataIndex];
            CurrentHandler.OnSuggestionClicked(suggestion);
        }

        private void SetCell(ButtonCell<Suggestion> cell, int index)
        {
            if (index < 0 || index >= suggestions.Count)
            {
                cell.Disable();
                return;
            }

            var suggestion = suggestions[index];
            cell.buttonText.text = suggestion.DisplayText;
        }

        private void UpdatePosition()
        {
            if (CurrentHandler == null || !CurrentHandler.InputField.isFocused)
                return;

            Vector3 pos;
            var input = CurrentHandler.InputField;

            var textGen = input.textComponent.cachedTextGenerator;
            int caretPos = 0;
            if (CurrentHandler.AnchorToCaretPosition)
            {
                caretPos = lastCaretPos--;

                caretPos = Math.Max(0, caretPos);
                caretPos = Math.Min(textGen.characterCount - 1, caretPos);
            }

            pos = textGen.characters[caretPos].cursorPos;
            pos = input.transform.TransformPoint(pos);

            uiRoot.transform.position = new Vector3(pos.x + 10, pos.y - 20, 0);

            this.Dragger.OnEndResize();
        }

        public override void SetDefaultPosAndAnchors()
        {
            var mainRect = uiRoot.GetComponent<RectTransform>();
            mainRect.pivot = new Vector2(0f, 1f);
            mainRect.anchorMin = new Vector2(0.42f, 0.4f);
            mainRect.anchorMax = new Vector2(0.68f, 0.6f);
        }

        public override void ConstructPanelContent()
        {
            dataHandler = new ButtonListSource<Suggestion>(scrollPool, GetEntries, SetCell, ShouldDisplay, OnCellClicked);

            var prototypeCell = ButtonCell<Suggestion>.CreatePrototypeCell(this.content);
            prototypeCell.GetComponentInChildren<Text>().supportRichText = true;

            scrollPool = UIFactory.CreateScrollPool(this.content, "AutoCompleter", out GameObject scrollObj, out GameObject scrollContent);
            scrollPool.Initialize(dataHandler, prototypeCell);
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);

            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(scrollContent, true, false, true, false);

            UIRoot.SetActive(false);
        }

        public override void SaveToConfigManager()
        {
            // not savable
        }

        public override void LoadSaveData()
        {
            // not savable
        }
    }
}
