using UnityExplorer.UI.Widgets.AutoComplete;
using UniverseLib.Input;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets.ButtonList;
using UniverseLib.UI.Widgets.ScrollView;

namespace UnityExplorer.UI.Panels
{
    // Shared modal panel for "AutoComplete" suggestions.
    // A data source implements ISuggestionProvider and uses TakeOwnership and ReleaseOwnership
    // for control, and SetSuggestions to set the actual suggestion data.

    public class AutoCompleteModal : UEPanel
    {
        public static AutoCompleteModal Instance => UIManager.GetPanel<AutoCompleteModal>(UIManager.Panels.AutoCompleter);

        public override string Name => "AutoCompleter";
        public override UIManager.Panels PanelType => UIManager.Panels.AutoCompleter;

        public override int MinWidth => 100;
        public override int MinHeight => 25;
        public override Vector2 DefaultAnchorMin => new(MIN_X, 0.4f);
        public override Vector2 DefaultAnchorMax => new(0.68f, MAX_Y);
        const float MIN_X = 0.42f;
        const float MAX_Y = 0.6f;

        public override bool CanDragAndResize => true;
        public override bool ShouldSaveActiveState => false;
        public override bool NavButtonWanted => false;

        public static ISuggestionProvider CurrentHandler { get; private set; }

        public static ButtonListHandler<Suggestion, ButtonCell> buttonListDataHandler;
        public static ScrollPool<ButtonCell> scrollPool;
        private static GameObject navigationTipRow;

        private static List<Suggestion> Suggestions = new();
        private static int SelectedIndex = 0;

        public static Suggestion SelectedSuggestion => Suggestions[SelectedIndex];

        public static bool Suggesting(ISuggestionProvider handler) => CurrentHandler == handler && Instance.UIRoot.activeSelf;

        public AutoCompleteModal(UIBase owner) : base(owner)
        {
            UIManager.UiBase.Panels.OnPanelsReordered += UIPanel_OnPanelsReordered;
            UIManager.UiBase.Panels.OnClickedOutsidePanels += AutoCompleter_OnClickedOutsidePanels;
        }

        public static void TakeOwnership(ISuggestionProvider provider)
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
                Suggestions.Clear();
                CurrentHandler = null;
                UIRoot.SetActive(false);
            }
        }

        public void SetSuggestions(List<Suggestion> suggestions, bool jumpToTop = true)
        {
            Suggestions = suggestions;

            if (jumpToTop)
            {
                SelectedIndex = 0;
                if (scrollPool.DataSource.ItemCount > 0)
                    scrollPool.JumpToIndex(0, null);
            }

            if (!Suggestions.Any())
                base.UIRoot.SetActive(false);
            else
            {
                base.UIRoot.SetActive(true);
                base.UIRoot.transform.SetAsLastSibling();
                buttonListDataHandler.RefreshData();
                scrollPool.Refresh(true, jumpToTop);
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

            scrollPool.JumpToIndex(index, null);
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
                    UpdatePosition();
            }
        }

        // Setting autocomplete cell buttons

        private readonly Color selectedSuggestionColor = new(45 / 255f, 75 / 255f, 80 / 255f);
        private readonly Color inactiveSuggestionColor = new(0.11f, 0.11f, 0.11f);

        private List<Suggestion> GetEntries() => Suggestions;

        private bool ShouldDisplay(Suggestion data, string filter) => true;

        private void OnCellClicked(int dataIndex)
        {
            Suggestion suggestion = Suggestions[dataIndex];
            CurrentHandler.OnSuggestionClicked(suggestion);
        }

        private bool setFirstCell;

        private void SetCell(ButtonCell cell, int index)
        {
            if (CurrentHandler == null)
            {
                UIRoot.SetActive(false);
                return;
            }

            if (index < 0 || index >= Suggestions.Count)
            {
                cell.Disable();
                return;
            }

            Suggestion suggestion = Suggestions[index];
            cell.Button.ButtonText.text = suggestion.DisplayText;

            if (CurrentHandler.AllowNavigation && index == SelectedIndex && setFirstCell)
            {
                RuntimeHelper.SetColorBlock(cell.Button.Component, selectedSuggestionColor);
            }
            else
                RuntimeHelper.SetColorBlock(cell.Button.Component, inactiveSuggestionColor);

            setFirstCell = true;
        }

        // Updating panel position

        private int lastCaretPosition;
        private Vector3 lastInputPosition;

        internal void UpdatePosition()
        {
            if (CurrentHandler == null)
                return;

            InputFieldRef input = CurrentHandler.InputField;

            //if (!input.Component.isFocused 
            //    || (input.Component.caretPosition == lastCaretPosition && input.UIRoot.transform.position == lastInputPosition))
            //    return;

            if (input.Component.caretPosition == lastCaretPosition && input.UIRoot.transform.position == lastInputPosition)
                return;
            
            if (CurrentHandler.AnchorToCaretPosition)
            {
                if (!input.Component.isFocused)
                    return;

                TextGenerator textGen = input.Component.cachedInputTextGenerator;
                int caretIdx = Math.Max(0, Math.Min(textGen.characterCount - 1, input.Component.caretPosition));

                // normalize the caret horizontal position
                Vector3 caretPos = textGen.characters[caretIdx].cursorPos;
                // transform to world point
                caretPos = input.UIRoot.transform.TransformPoint(caretPos);
                caretPos += new Vector3(input.Transform.rect.width * 0.5f, -(input.Transform.rect.height * 0.5f), 0);

                uiRoot.transform.position = new Vector3(caretPos.x + 10, caretPos.y - 30, 0);
            }
            else
            {
                uiRoot.transform.position = input.Transform.position + new Vector3(-(input.Transform.rect.width / 2) + 10, -20, 0);
            }

            lastInputPosition = input.UIRoot.transform.position;
            lastCaretPosition = input.Component.caretPosition;

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

            if (this.UIRoot.transform.GetSiblingIndex() != UIManager.UiBase.Panels.PanelHolder.transform.childCount - 1)
            {
                if (CurrentHandler != null)
                    ReleaseOwnership(CurrentHandler);
                else
                    UIRoot.SetActive(false);
            }
        }

        public override void OnFinishResize()
        {
            float xDiff = Rect.anchorMin.x - MIN_X;
            float yDiff = Rect.anchorMax.y - MAX_Y;

            if (xDiff != 0 || yDiff != 0)
            {
                Rect.anchorMin = new(MIN_X, Rect.anchorMin.y - yDiff);
                Rect.anchorMax = new(Rect.anchorMax.x - xDiff, MAX_Y);
            }

            base.OnFinishResize();
        }

        // UI Construction

        protected override void ConstructPanelContent()
        {
            // hide the titlebar
            this.TitleBar.gameObject.SetActive(false);

            buttonListDataHandler = new ButtonListHandler<Suggestion, ButtonCell>(scrollPool, GetEntries, SetCell, ShouldDisplay, OnCellClicked);

            scrollPool = UIFactory.CreateScrollPool<ButtonCell>(this.ContentRoot, "AutoCompleter", out GameObject scrollObj,
                out GameObject scrollContent);
            scrollPool.Initialize(buttonListDataHandler);
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(scrollContent, true, false, true, false);

            navigationTipRow = UIFactory.CreateHorizontalGroup(this.ContentRoot, "BottomRow", true, true, true, true, 0, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(navigationTipRow, minHeight: 20, flexibleWidth: 9999);
            UIFactory.CreateLabel(navigationTipRow, "HelpText", "Up/Down to select, Enter to use, Esc to close",
                TextAnchor.MiddleLeft, Color.grey, false, 13);

            UIRoot.SetActive(false);
        }
    }
}
