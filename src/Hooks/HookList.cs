using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Panels;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets.ScrollView;
using UniverseLib.Utility;

namespace UnityExplorer.Hooks
{
    public class HookList : ICellPoolDataSource<HookCell>
    {
        public int ItemCount => currentHooks.Count;
        
        internal static readonly HashSet<string> hookedSignatures = new();
        internal static readonly OrderedDictionary currentHooks = new();

        internal static GameObject UIRoot;
        internal static ScrollPool<HookCell> HooksScrollPool;

        public static void EnableOrDisableHookClicked(int index)
        {
            HookInstance hook = (HookInstance)currentHooks[index];
            hook.TogglePatch();

            HooksScrollPool.Refresh(true, false);
        }

        public static void DeleteHookClicked(int index)
        {
            HookInstance hook = (HookInstance)currentHooks[index];

            if (HookCreator.CurrentEditedHook == hook)
                HookCreator.EditorInputCancel();

            hook.Unpatch();
            currentHooks.RemoveAt(index);
            hookedSignatures.Remove(hook.TargetMethod.FullDescription());

            HooksScrollPool.Refresh(true, false);
        }

        public static void EditPatchClicked(int index)
        {
            HookManagerPanel.Instance.SetPage(HookManagerPanel.Pages.HookSourceEditor);
            HookInstance hook = (HookInstance)currentHooks[index];
            HookCreator.SetEditedHook(hook);
        }

        // Set current hook cell

        public void OnCellBorrowed(HookCell cell) { }

        public void SetCell(HookCell cell, int index)
        {
            if (index >= currentHooks.Count)
            {
                cell.Disable();
                return;
            }

            cell.CurrentDisplayedIndex = index;
            HookInstance hook = (HookInstance)currentHooks[index];

            cell.MethodNameLabel.text = SignatureHighlighter.HighlightMethod(hook.TargetMethod);

            cell.ToggleActiveButton.ButtonText.text = hook.Enabled ? "On" : "Off";
            RuntimeHelper.SetColorBlockAuto(cell.ToggleActiveButton.Component,
                hook.Enabled ? new Color(0.15f, 0.2f, 0.15f) : new Color(0.2f, 0.2f, 0.15f));
        }

        // UI

        internal void ConstructUI(GameObject leftGroup)
        {
            UIRoot = UIFactory.CreateUIObject("CurrentHooksPanel", leftGroup);
            UIFactory.SetLayoutElement(UIRoot, flexibleHeight: 9999, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(UIRoot, true, true, true, true);

            Text hooksLabel = UIFactory.CreateLabel(UIRoot, "HooksLabel", "Current Hooks", TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(hooksLabel.gameObject, minHeight: 30, flexibleWidth: 9999);

            HooksScrollPool = UIFactory.CreateScrollPool<HookCell>(UIRoot, "HooksScrollPool",
                out GameObject hooksScroll, out GameObject hooksContent);
            UIFactory.SetLayoutElement(hooksScroll, flexibleHeight: 9999);
            HooksScrollPool.Initialize(this);
        }
    }
}
