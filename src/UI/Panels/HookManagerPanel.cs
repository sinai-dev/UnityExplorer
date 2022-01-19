using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Config;
using UnityExplorer.Hooks;
using UnityExplorer.UI.Widgets;
using UnityExplorer.UI.Widgets.AutoComplete;
using UniverseLib.UI;
using UniverseLib.UI.Widgets;

namespace UnityExplorer.UI.Panels
{
    public class HookManagerPanel : UIPanel
    {
        public enum Pages
        {
            CurrentHooks,
            ClassMethodSelector,
            HookSourceEditor
        }

        public override UIManager.Panels PanelType => UIManager.Panels.HookManager;

        public override string Name => "Hooks";
        public override int MinWidth => 500;
        public override int MinHeight => 600;
        public override bool ShowByDefault => false;

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

        public override void ConstructPanelContent()
        {
            // ~~~~~~~~~ Active hooks scroll pool

            currentHooksPanel = UIFactory.CreateUIObject("CurrentHooksPanel", this.content);
            UIFactory.SetLayoutElement(currentHooksPanel, flexibleHeight: 9999, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(currentHooksPanel, true, true, true, true);

            var addRow = UIFactory.CreateHorizontalGroup(currentHooksPanel, "AddRow", false, true, true, true, 4,
                new Vector4(2, 2, 2, 2), new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(addRow, minHeight: 30, flexibleWidth: 9999);

            classSelectorInputField = UIFactory.CreateInputField(addRow, "ClassInput", "Enter a class to add hooks to...");
            UIFactory.SetLayoutElement(classSelectorInputField.Component.gameObject, flexibleWidth: 9999);
            new TypeCompleter(typeof(object), classSelectorInputField, true, false);

            var addButton = UIFactory.CreateButton(addRow, "AddButton", "Add Hooks");
            UIFactory.SetLayoutElement(addButton.Component.gameObject, minWidth: 100, minHeight: 25);
            addButton.OnClick += OnClassInputAddClicked;

            var hooksLabel = UIFactory.CreateLabel(currentHooksPanel, "HooksLabel", "Current Hooks", TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(hooksLabel.gameObject, minHeight: 30, flexibleWidth: 9999);

            HooksScrollPool = UIFactory.CreateScrollPool<HookCell>(currentHooksPanel, "HooksScrollPool", 
                out GameObject hooksScroll, out GameObject hooksContent);
            UIFactory.SetLayoutElement(hooksScroll, flexibleHeight: 9999);
            HooksScrollPool.Initialize(HookManager.Instance);

            // ~~~~~~~~~ Add hooks panel

            addHooksPanel = UIFactory.CreateUIObject("AddHooksPanel", this.content);
            UIFactory.SetLayoutElement(addHooksPanel, flexibleHeight: 9999, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(addHooksPanel, true, true, true, true);

            addHooksLabel = UIFactory.CreateLabel(addHooksPanel, "AddLabel", "NOT SET", TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(addHooksLabel.gameObject, minHeight: 30, minWidth: 100, flexibleWidth: 9999);

            var buttonRow = UIFactory.CreateHorizontalGroup(addHooksPanel, "ButtonRow", false, false, true, true, 5);
            UIFactory.SetLayoutElement(buttonRow, minHeight: 25, flexibleWidth: 9999);

            var doneButton = UIFactory.CreateButton(buttonRow, "DoneButton", "Done", new Color(0.2f, 0.3f, 0.2f));
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

            editorPanel = UIFactory.CreateUIObject("HookSourceEditor", this.content);
            UIFactory.SetLayoutElement(editorPanel, flexibleHeight: 9999, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(editorPanel, true, true, true, true);

            var editorLabel = UIFactory.CreateLabel(editorPanel, 
                "EditorLabel", 
                "Edit Harmony patch source as desired. Accepted method names are Prefix, Postfix, Finalizer and Transpiler (can define multiple).\n\n" +
                "Hooks are temporary! Please copy the source into your IDE to avoid losing work if you wish to keep it!", 
                TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(editorLabel.gameObject, minHeight: 25, flexibleWidth: 9999);

            var editorButtonRow = UIFactory.CreateHorizontalGroup(editorPanel, "ButtonRow", false, false, true, true, 5);
            UIFactory.SetLayoutElement(editorButtonRow, minHeight: 25, flexibleWidth: 9999);

            var editorSaveButton = UIFactory.CreateButton(editorButtonRow, "DoneButton", "Save and Return", new Color(0.2f, 0.3f, 0.2f));
            UIFactory.SetLayoutElement(editorSaveButton.Component.gameObject, minHeight: 25, flexibleWidth: 9999);
            editorSaveButton.OnClick += HookManager.Instance.EditorInputSave;

            var editorDoneButton = UIFactory.CreateButton(editorButtonRow, "DoneButton", "Cancel and Return", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(editorDoneButton.Component.gameObject, minHeight: 25, flexibleWidth: 9999);
            editorDoneButton.OnClick += HookManager.Instance.EditorInputCancel;

            int fontSize = 16;
            var inputObj = UIFactory.CreateScrollInputField(editorPanel, "EditorInput", "", out var inputScroller, fontSize);
            EditorInputScroller = inputScroller;
            EditorInput.OnValueChanged += HookManager.Instance.OnEditorInputChanged;

            EditorInputText = EditorInput.Component.textComponent;
            EditorInputText.supportRichText = false;
            EditorInputText.color = Color.clear;
            EditorInput.Component.customCaretColor = true;
            EditorInput.Component.caretColor = Color.white;
            EditorInput.PlaceholderText.fontSize = fontSize;

            // Lexer highlight text overlay
            var highlightTextObj = UIFactory.CreateUIObject("HighlightText", EditorInputText.gameObject);
            var highlightTextRect = highlightTextObj.GetComponent<RectTransform>();
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

        protected internal override void DoSetDefaultPosAndAnchors()
        {
            this.Rect.anchorMin = new Vector2(0.5f, 0.5f);
            this.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            this.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MinWidth);
            this.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, MinHeight);
        }
    }
}
