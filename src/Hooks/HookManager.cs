using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityExplorer.Runtime;
using UnityExplorer.CSConsole;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Widgets;
using UniverseLib;
using UniverseLib.UI.Widgets;

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
        private readonly HashSet<string> hookedSignatures = new HashSet<string>();
        private readonly OrderedDictionary currentHooks = new OrderedDictionary();

        // adding hooks
        private readonly List<MethodInfo> currentAddEligableMethods = new List<MethodInfo>();
        private readonly List<MethodInfo> filteredEligableMethods = new List<MethodInfo>();

        // hook editor
        private readonly LexerBuilder Lexer = new LexerBuilder();
        private HookInstance currentEditedHook;

        // ~~~~~~~~~~~ Main Current Hooks window ~~~~~~~~~~~

        public void EnableOrDisableHookClicked(int index)
        {
            var hook = (HookInstance)currentHooks[index];
            hook.TogglePatch();

            Panel.HooksScrollPool.Refresh(true, false);
        }

        public void DeleteHookClicked(int index)
        {
            var hook = (HookInstance)currentHooks[index];
            hook.Unpatch();
            currentHooks.RemoveAt(index);
            hookedSignatures.Remove(hook.TargetMethod.FullDescription());

            Panel.HooksScrollPool.Refresh(true, false);
        }

        public void EditPatchClicked(int index)
        {
            Panel.SetPage(HookManagerPanel.Pages.HookSourceEditor);
            var hook = (HookInstance)currentHooks[index];
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
            var hook = (HookInstance)this.currentHooks[index];

            cell.MethodNameLabel.text = SignatureHighlighter.HighlightMethod(hook.TargetMethod);

            cell.ToggleActiveButton.ButtonText.text = hook.Enabled ? "Enabled" : "Disabled";
            RuntimeProvider.Instance.SetColorBlockAuto(cell.ToggleActiveButton.Component,
                hook.Enabled ? new Color(0.15f, 0.2f, 0.15f) : new Color(0.2f, 0.2f, 0.15f));
        }

        // ~~~~~~~~~~~ Add Hooks window ~~~~~~~~~~~

        public void OnClassSelectedForHooks(string typeFullName)
        {
            var type = ReflectionUtility.GetTypeByName(typeFullName);
            if (type == null)
            {
                ExplorerCore.LogWarning($"Could not find any type by name {typeFullName}!");
                return;
            }

            Panel.SetAddHooksLabelType(SignatureHighlighter.Parse(type, true));

            Panel.ResetMethodFilter();
            filteredEligableMethods.Clear();
            currentAddEligableMethods.Clear();
            foreach (var method in type.GetMethods(ReflectionUtility.FLAGS))
            {
                if (method.IsGenericMethod /* || method.IsAbstract */ || RuntimeHelper.IsBlacklisted(method))
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
            var sig = method.FullDescription();
            if (hookedSignatures.Contains(sig))
                return;

            var hook = new HookInstance(method);
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
                foreach (var method in currentAddEligableMethods)
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
            var method = this.filteredEligableMethods[index];

            cell.MethodNameLabel.text = SignatureHighlighter.HighlightMethod(method);

            var sig = method.FullDescription();
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
            var input = Panel.EditorInput.Text;
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
