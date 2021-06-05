using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.Core.Input;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI;
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

        public static ISuggestionProvider CurrentHandler { get; private set; }

        public static ButtonListHandler<Suggestion, ButtonCell> dataHandler;
        public static ScrollPool<ButtonCell> scrollPool;
        private static GameObject navigationTipRow;

        private static List<Suggestion> Suggestions = new List<Suggestion>();
        private static int SelectedIndex = 0;

        public static Suggestion SelectedSuggestion => Suggestions[SelectedIndex];

        public static bool Suggesting(ISuggestionProvider handler) => CurrentHandler == handler && Instance.UIRoot.activeSelf;

        public AutoCompleteModal()
        {
            OnPanelsReordered += UIPanel_OnPanelsReordered;
            OnClickedOutsidePanels += AutoCompleter_OnClickedOutsidePanels;
        }

        public void TakeOwnership(ISuggestionProvider provider)
        {
            CurrentHandler = provider;
            navigationTipRow.SetActive(provider.AllowNavigation);
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

        public void SetSuggestions(IEnumerable<Suggestion> suggestions)
        {
            Suggestions = suggestions as List<Suggestion> ?? suggestions.ToList();
            SelectedIndex = 0;

            if (!Suggestions.Any())
                base.UIRoot.SetActive(false);
            else
            {
                base.UIRoot.SetActive(true);
                base.UIRoot.transform.SetAsLastSibling();
                dataHandler.RefreshData();
                scrollPool.Refresh(true, true);
            }
        }

        private static float timeOfLastNavHold = -1f;

        /// <summary>
        /// Returns true if the AutoCompleteModal used the navigation input, false if not.
        /// The navigation inputs are Control+Up/Down, and Control+Enter.
        /// </summary>
        public static bool CheckNavigation(ISuggestionProvider handler)
        {
            if (!Suggesting(handler))
                return false;

            bool up = InputManager.GetKey(KeyCode.UpArrow);
            bool down = InputManager.GetKey(KeyCode.DownArrow);

            if (up || down)
            {
                if (up)
                {
                    if (InputManager.GetKeyDown(KeyCode.UpArrow))
                    {
                        SetSelectedSuggestion(SelectedIndex - 1);
                        timeOfLastNavHold = Time.realtimeSinceStartup + 0.3f;
                    }
                    else if (timeOfLastNavHold.OccuredEarlierThan(0.05f))
                    {
                        SetSelectedSuggestion(SelectedIndex - 1);
                        timeOfLastNavHold = Time.realtimeSinceStartup;
                    }
                }
                else
                {
                    if (InputManager.GetKeyDown(KeyCode.DownArrow))
                    {
                        SetSelectedSuggestion(SelectedIndex + 1);
                        timeOfLastNavHold = Time.realtimeSinceStartup + 0.3f;
                    }
                    else if (timeOfLastNavHold.OccuredEarlierThan(0.05f))
                    {
                        SetSelectedSuggestion(SelectedIndex + 1);
                        timeOfLastNavHold = Time.realtimeSinceStartup;
                    }
                }

                return true;
            }

            return !timeOfLastNavHold.OccuredEarlierThan(0.2f);
        }

        public static bool CheckEnter(ISuggestionProvider handler)
        {
            return Suggesting(handler) && InputManager.GetKeyDown(KeyCode.Return);
        }

        public static bool CheckEscape(ISuggestionProvider handler)
        {
            return Suggesting(handler) && InputManager.GetKeyDown(KeyCode.Escape);
        }

        private static void SetSelectedSuggestion(int index)
        {
            if (index < 0 || index >= Suggestions.Count)
                return;

            SelectedIndex = index;
            scrollPool.Refresh(true, false);
        }

        // Internal update

        public override void Update()
        {
            if (!UIRoot || !UIRoot.activeSelf)
                return;

            if (Suggestions.Any() && CurrentHandler != null)
            {
                if (!CurrentHandler.InputField.UIRoot.activeInHierarchy)
                    ReleaseOwnership(CurrentHandler);
                else
                {
                    UpdatePosition();
                }
            }
        }

        // Setting autocomplete cell buttons

        private readonly Color selectedSuggestionColor = new Color(45 / 255f, 75 / 255f, 80 / 255f);
        private readonly Color inactiveSuggestionColor = new Color(0.11f, 0.11f, 0.11f);

        private List<Suggestion> GetEntries() => Suggestions;

        private bool ShouldDisplay(Suggestion data, string filter) => true;

        private void OnCellClicked(int dataIndex)
        {
            var suggestion = Suggestions[dataIndex];
            CurrentHandler.OnSuggestionClicked(suggestion);
        }

        private bool setFirstCell;

        private void SetCell(ButtonCell cell, int index)
        {
            if (index < 0 || index >= Suggestions.Count)
            {
                cell.Disable();
                return;
            }

            var suggestion = Suggestions[index];
            cell.Button.ButtonText.text = suggestion.DisplayText;

            if (CurrentHandler.AllowNavigation && index == SelectedIndex && setFirstCell)
            {
                float diff = 0f;
                // if cell is too far down
                if (cell.Rect.MinY() > scrollPool.Viewport.MinY())
                    diff = cell.Rect.MinY() - scrollPool.Viewport.MinY();
                // if cell is too far up
                else if (cell.Rect.MaxY() < scrollPool.Viewport.MaxY())
                    diff = cell.Rect.MaxY() - scrollPool.Viewport.MaxY();

                if (diff != 0.0f)
                {
                    var pos = scrollPool.Content.anchoredPosition;
                    pos.y -= diff;
                    scrollPool.Content.anchoredPosition = pos;
                }

                RuntimeProvider.Instance.SetColorBlock(cell.Button.Component, selectedSuggestionColor);
            }
            else
                RuntimeProvider.Instance.SetColorBlock(cell.Button.Component, inactiveSuggestionColor);

            setFirstCell = true;
        }

        // Updating panel position

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

        // Event listeners for panel

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

        // UI Construction

        protected internal override void DoSetDefaultPosAndAnchors()
        {
            Rect.pivot = new Vector2(0f, 1f);
            Rect.anchorMin = new Vector2(0.42f, 0.4f);
            Rect.anchorMax = new Vector2(0.68f, 0.6f);
        }

        public override void ConstructPanelContent()
        {
            dataHandler = new ButtonListHandler<Suggestion, ButtonCell>(scrollPool, GetEntries, SetCell, ShouldDisplay, OnCellClicked);

            scrollPool = UIFactory.CreateScrollPool<ButtonCell>(this.content, "AutoCompleter", out GameObject scrollObj,
                out GameObject scrollContent);
            scrollPool.Initialize(dataHandler);
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(scrollContent, true, false, true, false);

            navigationTipRow = UIFactory.CreateHorizontalGroup(this.content, "BottomRow", true, true, true, true, 0, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(navigationTipRow, minHeight: 20, flexibleWidth: 9999);
            UIFactory.CreateLabel(navigationTipRow, "HelpText", "Up/Down to select, Enter to use, Esc to close",
                TextAnchor.MiddleLeft, Color.grey, false, 13);

            UIRoot.SetActive(false);
        }

        public override void DoSaveToConfigElement()
        {
            // not savable
        }

        public override string GetSaveDataFromConfigManager() => null;
    }
}
