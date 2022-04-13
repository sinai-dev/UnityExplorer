using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Hooks;
using UnityExplorer.UI.Widgets.AutoComplete;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets;
using UniverseLib.UI.Widgets.ScrollView;

namespace UnityExplorer.UI.Panels
{
    public class HookManagerPanel : UEPanel
    {
        public enum Pages
        {
            CurrentHooks,
            ClassMethodSelector,
            HookSourceEditor
        }

        public override UIManager.Panels PanelType => UIManager.Panels.HookManager;

        public override string Name => "Hooks";
        public override bool ShowByDefault => false;

        public override int MinWidth => 500;
        public override int MinHeight => 600;
        public override Vector2 DefaultAnchorMin => new(0.5f, 0.5f);
        public override Vector2 DefaultAnchorMax => new(0.5f, 0.5f);

        public Pages CurrentPage { get; private set; } = Pages.CurrentHooks;

        private GameObject currentHooksPanel;
        public ScrollPool<HookCell> HooksScrollPool;
        private InputFieldRef classSelectorInputField;

        private GameObject addHooksPanel;
        public ScrollPool<AddHookCell> AddHooksScrollPool;
        private Text addHooksLabel;
        private InputFieldRef AddHooksMethodFilterInput;

        private GameObject editorPanel;

        public InputFieldScroller EditorInputScroller { get; private set; }
        public InputFieldRef EditorInput => EditorInputScroller.InputField;
        public Text EditorInputText { get; private set; }
        public Text EditorHighlightText { get; private set; }

        public HookManagerPanel(UIBase owner) : base(owner)
        {
        }

        private void OnClassInputAddClicked()
        {
            HookManager.Instance.OnClassSelectedForHooks(this.classSelectorInputField.Text);
        }

        public void SetAddHooksLabelType(string typeText) => addHooksLabel.text = $"Adding hooks to: {typeText}";

        public void SetPage(Pages page)
        {
            switch (page)
            {
                case Pages.CurrentHooks:
                    currentHooksPanel.SetActive(true);
                    addHooksPanel.SetActive(false);
                    editorPanel.SetActive(false);
                    break;
                case Pages.ClassMethodSelector:
                    currentHooksPanel.SetActive(false);
                    addHooksPanel.SetActive(true);
                    editorPanel.SetActive(false);
                    break;
                case Pages.HookSourceEditor:
                    currentHooksPanel.SetActive(false);
                    addHooksPanel.SetActive(false);
                    editorPanel.SetActive(true);
                    break;
            }
        }

        public void ResetMethodFilter() => AddHooksMethodFilterInput.Text = string.Empty;

        public override void SetDefaultSizeAndPosition()
        {
            base.SetDefaultSizeAndPosition();

            this.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MinWidth);
            this.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, MinHeight);
        }

        protected override void ConstructPanelContent()
        {
            // ~~~~~~~~~ Active hooks scroll pool

            currentHooksPanel = UIFactory.CreateUIObject("CurrentHooksPanel", this.ContentRoot);
            UIFactory.SetLayoutElement(currentHooksPanel, flexibleHeight: 9999, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(currentHooksPanel, true, true, true, true);

            GameObject addRow = UIFactory.CreateHorizontalGroup(currentHooksPanel, "AddRow", false, true, true, true, 4,
                new Vector4(2, 2, 2, 2), new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(addRow, minHeight: 30, flexibleWidth: 9999);

            classSelectorInputField = UIFactory.CreateInputField(addRow, "ClassInput", "Enter a class to add hooks to...");
            UIFactory.SetLayoutElement(classSelectorInputField.Component.gameObject, flexibleWidth: 9999);
            new TypeCompleter(typeof(object), classSelectorInputField, true, false);

            ButtonRef addButton = UIFactory.CreateButton(addRow, "AddButton", "Add Hooks");
            UIFactory.SetLayoutElement(addButton.Component.gameObject, minWidth: 100, minHeight: 25);
            addButton.OnClick += OnClassInputAddClicked;

            Text hooksLabel = UIFactory.CreateLabel(currentHooksPanel, "HooksLabel", "Current Hooks", TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(hooksLabel.gameObject, minHeight: 30, flexibleWidth: 9999);

            HooksScrollPool = UIFactory.CreateScrollPool<HookCell>(currentHooksPanel, "HooksScrollPool",
                out GameObject hooksScroll, out GameObject hooksContent);
            UIFactory.SetLayoutElement(hooksScroll, flexibleHeight: 9999);
            HooksScrollPool.Initialize(HookManager.Instance);

            // ~~~~~~~~~ Add hooks panel

            addHooksPanel = UIFactory.CreateUIObject("AddHooksPanel", this.ContentRoot);
            UIFactory.SetLayoutElement(addHooksPanel, flexibleHeight: 9999, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(addHooksPanel, true, true, true, true);

            addHooksLabel = UIFactory.CreateLabel(addHooksPanel, "AddLabel", "NOT SET", TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(addHooksLabel.gameObject, minHeight: 30, minWidth: 100, flexibleWidth: 9999);

            GameObject buttonRow = UIFactory.CreateHorizontalGroup(addHooksPanel, "ButtonRow", false, false, true, true, 5);
            UIFactory.SetLayoutElement(buttonRow, minHeight: 25, flexibleWidth: 9999);

            ButtonRef doneButton = UIFactory.CreateButton(buttonRow, "DoneButton", "Done", new Color(0.2f, 0.3f, 0.2f));
            UIFactory.SetLayoutElement(doneButton.Component.gameObject, minHeight: 25, flexibleWidth: 9999);
            doneButton.OnClick += HookManager.Instance.DoneAddingHooks;

            AddHooksMethodFilterInput = UIFactory.CreateInputField(addHooksPanel, "FilterInputField", "Filter method names...");
            UIFactory.SetLayoutElement(AddHooksMethodFilterInput.Component.gameObject, minHeight: 30, flexibleWidth: 9999);
            AddHooksMethodFilterInput.OnValueChanged += HookManager.Instance.OnAddHookFilterInputChanged;

            AddHooksScrollPool = UIFactory.CreateScrollPool<AddHookCell>(addHooksPanel, "MethodAddScrollPool",
                out GameObject addScrollRoot, out GameObject addContent);
            UIFactory.SetLayoutElement(addScrollRoot, flexibleHeight: 9999);
            AddHooksScrollPool.Initialize(HookManager.Instance);

            addHooksPanel.gameObject.SetActive(false);

            // ~~~~~~~~~ Hook source editor panel

            editorPanel = UIFactory.CreateUIObject("HookSourceEditor", this.ContentRoot);
            UIFactory.SetLayoutElement(editorPanel, flexibleHeight: 9999, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(editorPanel, true, true, true, true);

            Text editorLabel = UIFactory.CreateLabel(editorPanel,
                "EditorLabel",
                "Edit Harmony patch source as desired. Accepted method names are Prefix, Postfix, Finalizer and Transpiler (can define multiple).\n\n" +
                "Hooks are temporary! Please copy the source into your IDE to avoid losing work if you wish to keep it!",
                TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(editorLabel.gameObject, minHeight: 25, flexibleWidth: 9999);

            GameObject editorButtonRow = UIFactory.CreateHorizontalGroup(editorPanel, "ButtonRow", false, false, true, true, 5);
            UIFactory.SetLayoutElement(editorButtonRow, minHeight: 25, flexibleWidth: 9999);

            ButtonRef editorSaveButton = UIFactory.CreateButton(editorButtonRow, "DoneButton", "Save and Return", new Color(0.2f, 0.3f, 0.2f));
            UIFactory.SetLayoutElement(editorSaveButton.Component.gameObject, minHeight: 25, flexibleWidth: 9999);
            editorSaveButton.OnClick += HookManager.Instance.EditorInputSave;

            ButtonRef editorDoneButton = UIFactory.CreateButton(editorButtonRow, "DoneButton", "Cancel and Return", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(editorDoneButton.Component.gameObject, minHeight: 25, flexibleWidth: 9999);
            editorDoneButton.OnClick += HookManager.Instance.EditorInputCancel;

            int fontSize = 16;
            GameObject inputObj = UIFactory.CreateScrollInputField(editorPanel, "EditorInput", "", out InputFieldScroller inputScroller, fontSize);
            EditorInputScroller = inputScroller;
            EditorInput.OnValueChanged += HookManager.Instance.OnEditorInputChanged;

            EditorInputText = EditorInput.Component.textComponent;
            EditorInputText.supportRichText = false;
            EditorInputText.color = Color.clear;
            EditorInput.Component.customCaretColor = true;
            EditorInput.Component.caretColor = Color.white;
            EditorInput.PlaceholderText.fontSize = fontSize;

            // Lexer highlight text overlay
            GameObject highlightTextObj = UIFactory.CreateUIObject("HighlightText", EditorInputText.gameObject);
            RectTransform highlightTextRect = highlightTextObj.GetComponent<RectTransform>();
            highlightTextRect.pivot = new Vector2(0, 1);
            highlightTextRect.anchorMin = Vector2.zero;
            highlightTextRect.anchorMax = Vector2.one;
            highlightTextRect.offsetMin = Vector2.zero;
            highlightTextRect.offsetMax = Vector2.zero;

            EditorHighlightText = highlightTextObj.AddComponent<Text>();
            EditorHighlightText.color = Color.white;
            EditorHighlightText.supportRichText = true;
            EditorHighlightText.fontSize = fontSize;

            // Set fonts
            EditorInputText.font = UniversalUI.ConsoleFont;
            EditorInput.PlaceholderText.font = UniversalUI.ConsoleFont;
            EditorHighlightText.font = UniversalUI.ConsoleFont;

            editorPanel.SetActive(false);
        }
    }
}
