using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.Hooks;
using UnityExplorer.UI.Widgets;
using UnityExplorer.UI.Widgets.AutoComplete;

namespace UnityExplorer.UI.Panels
{
    public class HookManagerPanel : UIPanel
    {
        public override UIManager.Panels PanelType => UIManager.Panels.HookManager;

        public override string Name => "Hooks";
        public override int MinWidth => 500;
        public override int MinHeight => 600;
        public override bool ShowByDefault => false;

        public ScrollPool<HookCell> HooksScrollPool;
        public ScrollPool<AddHookCell> MethodResultsScrollPool;

        private GameObject addHooksPanel;
        private GameObject currentHooksPanel;
        private InputFieldRef classInputField;
        private Text addHooksLabel;
        private InputFieldRef methodFilterInput;

        public override string GetSaveDataFromConfigManager() => ConfigManager.HookManagerData.Value;

        public override void DoSaveToConfigElement() => ConfigManager.HookManagerData.Value = this.ToSaveData();

        private void OnClassInputAddClicked()
        {
            HookManager.Instance.OnClassSelectedForHooks(this.classInputField.Text);
        }

        public void SetAddHooksLabelType(string typeText) => addHooksLabel.text = $"Adding hooks to: {typeText}";

        public void SetAddPanelActive(bool show)
        {
            addHooksPanel.SetActive(show);
            currentHooksPanel.SetActive(!show);
        }

        public void ResetMethodFilter() => methodFilterInput.Text = string.Empty;

        public override void ConstructPanelContent()
        {
            // Active hooks scroll pool

            currentHooksPanel = UIFactory.CreateUIObject("CurrentHooksPanel", this.content);
            UIFactory.SetLayoutElement(currentHooksPanel, flexibleHeight: 9999, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(currentHooksPanel, true, true, true, true);

            var addRow = UIFactory.CreateHorizontalGroup(currentHooksPanel, "AddRow", false, true, true, true, 4,
                new Vector4(2, 2, 2, 2), new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(addRow, minHeight: 30, flexibleWidth: 9999);

            classInputField = UIFactory.CreateInputField(addRow, "ClassInput", "Enter a class to add hooks to...");
            UIFactory.SetLayoutElement(classInputField.Component.gameObject, flexibleWidth: 9999);
            new TypeCompleter(typeof(object), classInputField, false, false);

            var addButton = UIFactory.CreateButton(addRow, "AddButton", "Add Hooks");
            UIFactory.SetLayoutElement(addButton.Component.gameObject, minWidth: 100, minHeight: 25);
            addButton.OnClick += OnClassInputAddClicked;

            var hooksLabel = UIFactory.CreateLabel(currentHooksPanel, "HooksLabel", "Current Hooks", TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(hooksLabel.gameObject, minHeight: 30, flexibleWidth: 9999);

            HooksScrollPool = UIFactory.CreateScrollPool<HookCell>(currentHooksPanel, "HooksScrollPool", 
                out GameObject hooksScroll, out GameObject hooksContent);
            UIFactory.SetLayoutElement(hooksScroll, flexibleHeight: 9999);
            HooksScrollPool.Initialize(HookManager.Instance);

            // Add hooks panel

            addHooksPanel = UIFactory.CreateUIObject("AddHooksPanel", this.content);
            UIFactory.SetLayoutElement(addHooksPanel, flexibleHeight: 9999, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(addHooksPanel, true, true, true, true);

            addHooksLabel = UIFactory.CreateLabel(addHooksPanel, "AddLabel", "NOT SET", TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(addHooksLabel.gameObject, minHeight: 30, minWidth: 100, flexibleWidth: 9999);

            var buttonRow = UIFactory.CreateHorizontalGroup(addHooksPanel, "ButtonRow", false, false, true, true, 5);
            UIFactory.SetLayoutElement(buttonRow, minHeight: 25, flexibleWidth: 9999);

            var doneButton = UIFactory.CreateButton(buttonRow, "DoneButton", "Done", new Color(0.2f, 0.3f, 0.2f));
            UIFactory.SetLayoutElement(doneButton.Component.gameObject, minHeight: 25, flexibleWidth: 9999);
            doneButton.OnClick += HookManager.Instance.CloseAddHooks;

            var patchAllButton = UIFactory.CreateButton(buttonRow, "PatchAllButton", "Hook ALL methods in class", new Color(0.3f, 0.3f, 0.2f));
            UIFactory.SetLayoutElement(patchAllButton.Component.gameObject, minHeight: 25, flexibleWidth: 9999);
            patchAllButton.OnClick += HookManager.Instance.OnHookAllClicked;

            methodFilterInput = UIFactory.CreateInputField(addHooksPanel, "FilterInputField", "Filter method names...");
            UIFactory.SetLayoutElement(methodFilterInput.Component.gameObject, minHeight: 30, flexibleWidth: 9999);
            methodFilterInput.OnValueChanged += HookManager.Instance.OnAddHookFilterInputChanged;

            MethodResultsScrollPool = UIFactory.CreateScrollPool<AddHookCell>(addHooksPanel, "MethodAddScrollPool",
                out GameObject addScrollRoot, out GameObject addContent);
            UIFactory.SetLayoutElement(addScrollRoot, flexibleHeight: 9999);
            MethodResultsScrollPool.Initialize(HookManager.Instance);

            addHooksPanel.gameObject.SetActive(false);
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
