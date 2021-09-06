using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.Hooks
{
    public class HookManager : ICellPoolDataSource<HookCell>, ICellPoolDataSource<AddHookCell>
    {
        private static HookManager s_instance;
        public static HookManager Instance => s_instance ?? (s_instance = new HookManager());

        public HookManagerPanel Panel => UIManager.GetPanel<HookManagerPanel>(UIManager.Panels.HookManager);

        public int ItemCount => addingMethods ? currentFilteredMethods.Count : currentHooks.Count;

        private bool addingMethods;
        private HashSet<string> hookedSignatures = new HashSet<string>();
        private readonly OrderedDictionary currentHooks = new OrderedDictionary();
        private readonly List<MethodInfo> currentAddEligableMethods = new List<MethodInfo>();
        private readonly List<MethodInfo> currentFilteredMethods = new List<MethodInfo>();

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
            currentFilteredMethods.Clear();
            currentAddEligableMethods.Clear();
            foreach (var method in type.GetMethods(ReflectionUtility.FLAGS))
            {
                if (method.IsGenericMethod || method.IsAbstract || ReflectionUtility.IsBlacklisted(method))
                    continue;
                currentAddEligableMethods.Add(method);
                currentFilteredMethods.Add(method);
            }

            addingMethods = true;
            Panel.SetAddPanelActive(true);
            Panel.MethodResultsScrollPool.Refresh(true, true);
        }

        public void CloseAddHooks()
        {
            addingMethods = false;
            Panel.SetAddPanelActive(false);
            Panel.HooksScrollPool.Refresh(true, false);
        }

        public void OnHookAllClicked()
        {
            foreach (var method in currentAddEligableMethods)
                AddHook(method);
            Panel.MethodResultsScrollPool.Refresh(true, false);
        }

        public void AddHookClicked(int index)
        {
            if (index >= this.currentFilteredMethods.Count)
                return;

            AddHook(currentFilteredMethods[index]);
            Panel.MethodResultsScrollPool.Refresh(true, false);
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
            var hook = (HookInstance)currentHooks[index];
            ExplorerCore.Log(hook.GeneratedSource);
        }

        public void OnAddHookFilterInputChanged(string input)
        {
            currentFilteredMethods.Clear();

            if (string.IsNullOrEmpty(input))
                currentFilteredMethods.AddRange(currentAddEligableMethods);
            else
            {
                foreach (var method in currentAddEligableMethods)
                {
                    if (method.Name.ContainsIgnoreCase(input))
                        currentFilteredMethods.Add(method);
                }
            }

            Panel.MethodResultsScrollPool.Refresh(true, true);
        }

        // OnBorrow methods not needed
        public void OnCellBorrowed(HookCell cell) { }
        public void OnCellBorrowed(AddHookCell cell) { }

        // Set current hook cell

        public void SetCell(HookCell cell, int index)
        {
            if (index >= this.currentHooks.Count)
            {
                cell.Disable();
                return;
            }

            cell.CurrentDisplayedIndex = index;
            var hook = (HookInstance)this.currentHooks[index];
            
            cell.MethodNameLabel.text = HighlightMethod(hook.TargetMethod);

            cell.ToggleActiveButton.ButtonText.text = hook.Enabled ? "Enabled" : "Disabled";
            RuntimeProvider.Instance.SetColorBlockAuto(cell.ToggleActiveButton.Component, 
                hook.Enabled ? new Color(0.15f, 0.2f, 0.15f) : new Color(0.2f, 0.2f, 0.15f));
        }

        // Set eligable method cell

        public void SetCell(AddHookCell cell, int index)
        {
            if (index >= this.currentFilteredMethods.Count)
            {
                cell.Disable();
                return;
            }

            cell.CurrentDisplayedIndex = index;
            var method = this.currentFilteredMethods[index];

            cell.MethodNameLabel.text = HighlightMethod(method);

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

        // private static readonly string VOID_HIGHLIGHT = $"<color=#{SignatureHighlighter.keywordBlueHex}>void</color> ";

        private static readonly Dictionary<string, string> highlightedMethods = new Dictionary<string, string>();

        private string HighlightMethod(MethodInfo method)
        {
            var sig = method.FullDescription();
            if (highlightedMethods.ContainsKey(sig))
                return highlightedMethods[sig];

            var sb = new StringBuilder();

            // declaring type
            sb.Append(SignatureHighlighter.Parse(method.DeclaringType, false));
            sb.Append('.');

            // method name
            var color = !method.IsStatic
                    ? SignatureHighlighter.METHOD_INSTANCE
                    : SignatureHighlighter.METHOD_STATIC;
            sb.Append($"<color={color}>{method.Name}</color>");

            // arguments
            sb.Append('(');
            var args = method.GetParameters();
            if (args != null && args.Any())
            {
                int i = 0;
                foreach (var param in args)
                {
                    sb.Append(SignatureHighlighter.Parse(param.ParameterType, false));
                    sb.Append(' ');
                    sb.Append($"<color={SignatureHighlighter.LOCAL_ARG}>{param.Name}</color>");
                    i++;
                    if (i < args.Length)
                        sb.Append(", ");
                }
            }
            sb.Append(')');

            var ret = sb.ToString();
            highlightedMethods.Add(sig, ret);
            return ret;
        }
    }
}
