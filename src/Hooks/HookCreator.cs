using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.CSConsole;
using UnityExplorer.Runtime;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Widgets;
using UnityExplorer.UI.Widgets.AutoComplete;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets;
using UniverseLib.UI.Widgets.ScrollView;
using UniverseLib.Utility;

namespace UnityExplorer.Hooks
{
    public class HookCreator : ICellPoolDataSource<AddHookCell>
    {
        public int ItemCount => filteredEligibleMethods.Count;

        static readonly List<MethodInfo> currentAddEligibleMethods = new();
        static readonly List<MethodInfo> filteredEligibleMethods = new();
        static readonly List<string> currentEligibleNamesForFiltering = new();

        // hook editor
        static readonly LexerBuilder Lexer = new();
        internal static HookInstance CurrentEditedHook;

        // Add Hooks UI
        internal static GameObject AddHooksRoot;
        internal static ScrollPool<AddHookCell> AddHooksScrollPool;
        internal static Text AddHooksLabel;
        internal static InputFieldRef AddHooksMethodFilterInput;
        internal static InputFieldRef ClassSelectorInputField;
        internal static Type pendingGenericDefinition;
        internal static MethodInfo pendingGenericMethod;

        public static bool PendingGeneric => pendingGenericDefinition != null || pendingGenericMethod != null;

        // Hook Source Editor UI
        public static GameObject EditorRoot { get; private set; }
        public static Text EditingHookLabel { get; private set; }
        public static InputFieldScroller EditorInputScroller { get; private set; }
        public static InputFieldRef EditorInput => EditorInputScroller.InputField;
        public static Text EditorInputText { get; private set; }
        public static Text EditorHighlightText { get; private set; }

        // ~~~~~~ New hook method selector ~~~~~~~

        public void OnClassSelectedForHooks(string typeFullName)
        {
            Type type = ReflectionUtility.GetTypeByName(typeFullName);
            if (type == null)
            {
                ExplorerCore.LogWarning($"Could not find any type by name {typeFullName}!");
                return;
            }
            if (type.IsGenericType)
            {
                pendingGenericDefinition = type;
                HookManagerPanel.genericArgsHandler.Show(OnGenericClassChosen, OnGenericClassCancel, type);
                HookManagerPanel.Instance.SetPage(HookManagerPanel.Pages.GenericArgsSelector);
                return;    
            }

            ShowMethodsForType(type);
        }

        void ShowMethodsForType(Type type)
        {
            SetAddHooksLabelType(SignatureHighlighter.Parse(type, true));

            AddHooksMethodFilterInput.Text = string.Empty;

            filteredEligibleMethods.Clear();
            currentAddEligibleMethods.Clear();
            currentEligibleNamesForFiltering.Clear();
            foreach (MethodInfo method in type.GetMethods(ReflectionUtility.FLAGS))
            {
                if (UERuntimeHelper.IsBlacklisted(method))
                    continue;
                currentAddEligibleMethods.Add(method);
                currentEligibleNamesForFiltering.Add(SignatureHighlighter.RemoveHighlighting(SignatureHighlighter.ParseMethod(method)));
                filteredEligibleMethods.Add(method);
            }

            AddHooksScrollPool.Refresh(true, true);
        }

        void OnGenericClassChosen(Type[] genericArgs)
        {
            Type generic = pendingGenericDefinition.MakeGenericType(genericArgs);
            ShowMethodsForType(generic);
            HookManagerPanel.Instance.SetPage(HookManagerPanel.Pages.ClassMethodSelector);
        }

        void OnGenericClassCancel()
        {
            pendingGenericDefinition = null;
            HookManagerPanel.Instance.SetPage(HookManagerPanel.Pages.ClassMethodSelector);
        }

        public void SetAddHooksLabelType(string typeText)
        {
            AddHooksLabel.text = $"Adding hooks to: {typeText}";

            AddHooksMethodFilterInput.GameObject.SetActive(true);
            AddHooksScrollPool.UIRoot.SetActive(true);
        }

        public static void AddHookClicked(int index)
        {
            if (index >= filteredEligibleMethods.Count)
                return;

            MethodInfo method = filteredEligibleMethods[index];
            if (!method.IsGenericMethod && HookList.hookedSignatures.Contains(method.FullDescription()))
            {
                ExplorerCore.Log($"Non-generic methods can only be hooked once.");
                return;
            }
            else if (method.IsGenericMethod)
            {
                pendingGenericMethod = method;
                HookManagerPanel.genericArgsHandler.Show(OnGenericMethodChosen, OnGenericMethodCancel, method);
                HookManagerPanel.Instance.SetPage(HookManagerPanel.Pages.GenericArgsSelector);
                return;
            }

            AddHook(filteredEligibleMethods[index]);
        }

        static void OnGenericMethodChosen(Type[] arguments)
        {
            MethodInfo generic = pendingGenericMethod.MakeGenericMethod(arguments);
            AddHook(generic);
        }

        static void OnGenericMethodCancel()
        {
            pendingGenericMethod = null;
            HookManagerPanel.Instance.SetPage(HookManagerPanel.Pages.ClassMethodSelector);
        }

        public static void AddHook(MethodInfo method)
        {
            HookManagerPanel.Instance.SetPage(HookManagerPanel.Pages.ClassMethodSelector);

            string sig = method.FullDescription();
            if (HookList.hookedSignatures.Contains(sig))
            {
                ExplorerCore.LogWarning($"Method is already hooked!");
                return;
            }

            HookInstance hook = new(method);
            if (hook.Enabled)
            {
                HookList.hookedSignatures.Add(sig);
                HookList.currentHooks.Add(sig, hook);
            }

            AddHooksScrollPool.Refresh(true, false);
            HookList.HooksScrollPool.Refresh(true, false);
        }

        public void OnAddHookFilterInputChanged(string input)
        {
            filteredEligibleMethods.Clear();

            if (string.IsNullOrEmpty(input))
                filteredEligibleMethods.AddRange(currentAddEligibleMethods);
            else
            {
                for (int i = 0; i < currentAddEligibleMethods.Count; i++)
                {
                    MethodInfo eligible = currentAddEligibleMethods[i];
                    string sig = currentEligibleNamesForFiltering[i];
                    if (sig.ContainsIgnoreCase(input))
                        filteredEligibleMethods.Add(eligible);
                }
            }

            AddHooksScrollPool.Refresh(true, true);
        }

        // Set eligible method cell

        public void OnCellBorrowed(AddHookCell cell) { }

        public void SetCell(AddHookCell cell, int index)
        {
            if (index >= filteredEligibleMethods.Count)
            {
                cell.Disable();
                return;
            }

            cell.CurrentDisplayedIndex = index;
            MethodInfo method = filteredEligibleMethods[index];

            cell.MethodNameLabel.text = SignatureHighlighter.ParseMethod(method);
        }

        // ~~~~~~~~ Hook source editor ~~~~~~~~

        internal static void SetEditedHook(HookInstance hook)
        {
            CurrentEditedHook = hook;
            EditingHookLabel.text = $"Editing: {SignatureHighlighter.Parse(hook.TargetMethod.DeclaringType, false, hook.TargetMethod)}";
            EditorInput.Text = hook.PatchSourceCode;
        }

        internal static void OnEditorInputChanged(string value)
        {
            EditorHighlightText.text = Lexer.BuildHighlightedString(value, 0, value.Length - 1, 0, EditorInput.Component.caretPosition, out _);
        }

        internal static void EditorInputCancel()
        {
            CurrentEditedHook = null;
            HookManagerPanel.Instance.SetPage(HookManagerPanel.Pages.ClassMethodSelector);
        }

        internal static void EditorInputSave()
        {
            string input = EditorInput.Text;
            bool wasEnabled = CurrentEditedHook.Enabled;
            if (CurrentEditedHook.CompileAndGenerateProcessor(input))
            {
                if (wasEnabled)
                    CurrentEditedHook.Patch();

                CurrentEditedHook.PatchSourceCode = input;
                CurrentEditedHook = null;
                HookManagerPanel.Instance.SetPage(HookManagerPanel.Pages.ClassMethodSelector);
            }
        }

        // UI Construction

        internal void ConstructAddHooksView(GameObject rightGroup)
        {
            AddHooksRoot = UIFactory.CreateUIObject("AddHooksPanel", rightGroup);
            UIFactory.SetLayoutElement(AddHooksRoot, flexibleHeight: 9999, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(AddHooksRoot, false, false, true, true);

            GameObject addRow = UIFactory.CreateHorizontalGroup(AddHooksRoot, "AddRow", false, true, true, true, 4,
                new Vector4(2, 2, 2, 2), new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(addRow, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            ClassSelectorInputField = UIFactory.CreateInputField(addRow, "ClassInput", "Enter a class to add hooks to...");
            UIFactory.SetLayoutElement(ClassSelectorInputField.Component.gameObject, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 0);
            TypeCompleter completer = new(typeof(object), ClassSelectorInputField, true, false, true);
            //completer.AllTypes = true;

            ButtonRef addButton = UIFactory.CreateButton(addRow, "AddButton", "View Methods");
            UIFactory.SetLayoutElement(addButton.Component.gameObject, minWidth: 110, minHeight: 25);
            addButton.OnClick += () => { OnClassSelectedForHooks(ClassSelectorInputField.Text); };

            AddHooksLabel = UIFactory.CreateLabel(AddHooksRoot, "AddLabel", "Choose a class to begin...", TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(AddHooksLabel.gameObject, minHeight: 30, minWidth: 100, flexibleWidth: 9999);

            AddHooksMethodFilterInput = UIFactory.CreateInputField(AddHooksRoot, "FilterInputField", "Filter method names...");
            UIFactory.SetLayoutElement(AddHooksMethodFilterInput.Component.gameObject, minHeight: 30, flexibleWidth: 9999);
            AddHooksMethodFilterInput.OnValueChanged += OnAddHookFilterInputChanged;

            AddHooksScrollPool = UIFactory.CreateScrollPool<AddHookCell>(AddHooksRoot, "MethodAddScrollPool",
                out GameObject addScrollRoot, out GameObject addContent);
            UIFactory.SetLayoutElement(addScrollRoot, flexibleHeight: 9999);
            AddHooksScrollPool.Initialize(this);

            AddHooksMethodFilterInput.GameObject.SetActive(false);
            AddHooksScrollPool.UIRoot.SetActive(false);
        }

        public void ConstructEditor(GameObject parent)
        {
            EditorRoot = UIFactory.CreateUIObject("HookSourceEditor", parent);
            UIFactory.SetLayoutElement(EditorRoot, flexibleHeight: 9999, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(EditorRoot, true, true, true, true, 2, 3, 3, 3, 3);

            EditingHookLabel = UIFactory.CreateLabel(EditorRoot, "EditingHookLabel", "NOT SET", TextAnchor.MiddleCenter);
            EditingHookLabel.fontStyle = FontStyle.Bold;
            UIFactory.SetLayoutElement(EditingHookLabel.gameObject, flexibleWidth: 9999, minHeight: 25);

            Text editorLabel = UIFactory.CreateLabel(EditorRoot,
                "EditorLabel",
                "* Accepted method names are <b>Prefix</b>, <b>Postfix</b>, <b>Finalizer</b> and <b>Transpiler</b> (can define multiple).\n" +
                "* Your patch methods must be static.\n" +
                "* Hooks are temporary! Copy the source into your IDE to avoid losing work if you wish to keep it!",
                TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(editorLabel.gameObject, minHeight: 25, flexibleWidth: 9999);

            GameObject editorButtonRow = UIFactory.CreateHorizontalGroup(EditorRoot, "ButtonRow", false, false, true, true, 5);
            UIFactory.SetLayoutElement(editorButtonRow, minHeight: 25, flexibleWidth: 9999);

            ButtonRef editorSaveButton = UIFactory.CreateButton(editorButtonRow, "DoneButton", "Save and Return", new Color(0.2f, 0.3f, 0.2f));
            UIFactory.SetLayoutElement(editorSaveButton.Component.gameObject, minHeight: 25, flexibleWidth: 9999);
            editorSaveButton.OnClick += EditorInputSave;

            ButtonRef editorDoneButton = UIFactory.CreateButton(editorButtonRow, "DoneButton", "Cancel and Return", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(editorDoneButton.Component.gameObject, minHeight: 25, flexibleWidth: 9999);
            editorDoneButton.OnClick += EditorInputCancel;

            int fontSize = 16;
            GameObject inputObj = UIFactory.CreateScrollInputField(EditorRoot, "EditorInput", "", out InputFieldScroller inputScroller, fontSize);
            EditorInputScroller = inputScroller;
            EditorInput.OnValueChanged += OnEditorInputChanged;

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
        }
    }
}
