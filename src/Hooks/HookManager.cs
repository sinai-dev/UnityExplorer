using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using UnityEngine;
using UnityExplorer.CSConsole;
using UnityExplorer.Runtime;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;
using UniverseLib;
using UniverseLib.UI.Widgets.ScrollView;
using UniverseLib.Utility;

namespace UnityExplorer.Hooks
{
    public class HookManager : ICellPoolDataSource<HookCell>, ICellPoolDataSource<AddHookCell>
    {
        private static HookManager s_instance;
        public static HookManager Instance => s_instance ?? (s_instance = new HookManager());

        public HookManagerPanel Panel => UIManager.GetPanel<HookManagerPanel>(UIManager.Panels.HookManager);

        // This class acts as the data source for both current hooks and eligable methods when adding hooks.
        // 'isAddingMethods' keeps track of which pool is currently the displayed one, so our ItemCount reflects the
        // correct pool cells.
        private bool isAddingMethods;
        public int ItemCount => isAddingMethods ? filteredEligableMethods.Count : currentHooks.Count;

        // current hooks
        private readonly HashSet<string> hookedSignatures = new();
        private readonly OrderedDictionary currentHooks = new();

        // adding hooks
        private readonly List<MethodInfo> currentAddEligableMethods = new();
        private readonly List<MethodInfo> filteredEligableMethods = new();

        // hook editor
        private readonly LexerBuilder Lexer = new();
        private HookInstance currentEditedHook;

        // ~~~~~~~~~~~ Main Current Hooks window ~~~~~~~~~~~

        public void EnableOrDisableHookClicked(int index)
        {
            HookInstance hook = (HookInstance)currentHooks[index];
            hook.TogglePatch();

            Panel.HooksScrollPool.Refresh(true, false);
        }

        public void DeleteHookClicked(int index)
        {
            HookInstance hook = (HookInstance)currentHooks[index];
            hook.Unpatch();
            currentHooks.RemoveAt(index);
            hookedSignatures.Remove(hook.TargetMethod.FullDescription());

            Panel.HooksScrollPool.Refresh(true, false);
        }

        public void EditPatchClicked(int index)
        {
            Panel.SetPage(HookManagerPanel.Pages.HookSourceEditor);
            HookInstance hook = (HookInstance)currentHooks[index];
            currentEditedHook = hook;
            Panel.EditorInput.Text = hook.PatchSourceCode;
        }

        // Set current hook cell

        public void OnCellBorrowed(HookCell cell) { }

        public void SetCell(HookCell cell, int index)
        {
            if (index >= this.currentHooks.Count)
            {
                cell.Disable();
                return;
            }

            cell.CurrentDisplayedIndex = index;
            HookInstance hook = (HookInstance)this.currentHooks[index];

            cell.MethodNameLabel.text = SignatureHighlighter.HighlightMethod(hook.TargetMethod);

            cell.ToggleActiveButton.ButtonText.text = hook.Enabled ? "Enabled" : "Disabled";
            RuntimeHelper.SetColorBlockAuto(cell.ToggleActiveButton.Component,
                hook.Enabled ? new Color(0.15f, 0.2f, 0.15f) : new Color(0.2f, 0.2f, 0.15f));
        }

        // ~~~~~~~~~~~ Add Hooks window ~~~~~~~~~~~

        public void OnClassSelectedForHooks(string typeFullName)
        {
            Type type = ReflectionUtility.GetTypeByName(typeFullName);
            if (type == null)
            {
                ExplorerCore.LogWarning($"Could not find any type by name {typeFullName}!");
                return;
            }

            Panel.SetAddHooksLabelType(SignatureHighlighter.Parse(type, true));

            Panel.ResetMethodFilter();
            filteredEligableMethods.Clear();
            currentAddEligableMethods.Clear();
            foreach (MethodInfo method in type.GetMethods(ReflectionUtility.FLAGS))
            {
                if (method.IsGenericMethod || UERuntimeHelper.IsBlacklisted(method))
                    continue;
                currentAddEligableMethods.Add(method);
                filteredEligableMethods.Add(method);
            }

            isAddingMethods = true;
            Panel.SetPage(HookManagerPanel.Pages.ClassMethodSelector);
            Panel.AddHooksScrollPool.Refresh(true, true);
        }

        public void DoneAddingHooks()
        {
            isAddingMethods = false;
            Panel.SetPage(HookManagerPanel.Pages.CurrentHooks);
            Panel.HooksScrollPool.Refresh(true, false);
        }

        public void AddHookClicked(int index)
        {
            if (index >= this.filteredEligableMethods.Count)
                return;

            AddHook(filteredEligableMethods[index]);
            Panel.AddHooksScrollPool.Refresh(true, false);
        }

        public void AddHook(MethodInfo method)
        {
            string sig = method.FullDescription();
            if (hookedSignatures.Contains(sig))
                return;

            HookInstance hook = new(method);
            if (hook.Enabled)
            {
                hookedSignatures.Add(sig);
                currentHooks.Add(sig, hook);
            }
        }

        public void OnAddHookFilterInputChanged(string input)
        {
            filteredEligableMethods.Clear();

            if (string.IsNullOrEmpty(input))
                filteredEligableMethods.AddRange(currentAddEligableMethods);
            else
            {
                foreach (MethodInfo method in currentAddEligableMethods)
                {
                    if (method.Name.ContainsIgnoreCase(input))
                        filteredEligableMethods.Add(method);
                }
            }

            Panel.AddHooksScrollPool.Refresh(true, true);
        }

        // Set eligable method cell

        public void OnCellBorrowed(AddHookCell cell) { }

        public void SetCell(AddHookCell cell, int index)
        {
            if (index >= this.filteredEligableMethods.Count)
            {
                cell.Disable();
                return;
            }

            cell.CurrentDisplayedIndex = index;
            MethodInfo method = this.filteredEligableMethods[index];

            cell.MethodNameLabel.text = SignatureHighlighter.HighlightMethod(method);

            string sig = method.FullDescription();
            if (hookedSignatures.Contains(sig))
            {
                cell.HookButton.Component.gameObject.SetActive(false);
                cell.HookedLabel.gameObject.SetActive(true);
            }
            else
            {
                cell.HookButton.Component.gameObject.SetActive(true);
                cell.HookedLabel.gameObject.SetActive(false);
            }
        }

        // ~~~~~~~~~~~ Hook source editor window ~~~~~~~~~~~

        public void OnEditorInputChanged(string value)
        {
            Panel.EditorHighlightText.text = Lexer.BuildHighlightedString(value, 0, value.Length - 1, 0,
                Panel.EditorInput.Component.caretPosition, out _);
        }

        public void EditorInputCancel()
        {
            currentEditedHook = null;
            Panel.SetPage(HookManagerPanel.Pages.CurrentHooks);
        }

        public void EditorInputSave()
        {
            string input = Panel.EditorInput.Text;
            bool wasEnabled = currentEditedHook.Enabled;
            if (currentEditedHook.CompileAndGenerateProcessor(input))
            {
                if (wasEnabled)
                    currentEditedHook.Patch();
                currentEditedHook.PatchSourceCode = input;
                currentEditedHook = null;
                Panel.SetPage(HookManagerPanel.Pages.CurrentHooks);
            }
        }
    }
}
