using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExplorer.Core.Input;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI;
using UnityExplorer.UI.Models;

namespace UnityExplorer.UI.Widgets.AutoComplete
{
    // todo add a 'close' button if the user wants to manually hide the suggestions box

    public static class AutoCompleter
    {
        public static ISuggestionProvider CurrentHandler;

        public static GameObject UIRoot => uiRoot;
        public static GameObject uiRoot;

        public static ButtonListSource<Suggestion> dataHandler;
        public static ScrollPool scrollPool;

        private static List<Suggestion> suggestions = new List<Suggestion>();

        private static int lastCaretPos;

        public static void Update()
        {
            if (!UIRoot || !UIRoot.activeSelf)
                return;

            if (suggestions.Any() && CurrentHandler != null)
            {
                if (!CurrentHandler.InputField.gameObject.activeInHierarchy)
                    ReleaseOwnership(CurrentHandler);
                else
                {
                    lastCaretPos = CurrentHandler.InputField.caretPosition;
                    UpdatePosition();
                }
            }
        }

        public static void TakeOwnership(ISuggestionProvider provider)
        {
            CurrentHandler = provider;
        }

        public static void ReleaseOwnership(ISuggestionProvider provider)
        {
            if (CurrentHandler == null)
                return;

            if (CurrentHandler == provider)
            {
                CurrentHandler = null;
                UIRoot.SetActive(false);
            }
        }

        private static List<Suggestion> GetEntries() => suggestions;

        private static bool ShouldDisplay(Suggestion data, string filter) => true;

        public static void SetSuggestions(List<Suggestion> collection)
        {
            suggestions = collection;

            if (!suggestions.Any())
                UIRoot.SetActive(false);
            else
            {
                UIRoot.SetActive(true);
                dataHandler.RefreshData();
                scrollPool.Rebuild();
                //scrollPool.RefreshCells(true);
            }
        }

        private static void OnCellClicked(int dataIndex)
        {
            var suggestion = suggestions[dataIndex];
            CurrentHandler.OnSuggestionClicked(suggestion);
        }

        private static void SetCell(ButtonCell<Suggestion> cell, int index)
        {
            if (index < 0 || index >= suggestions.Count)
            {
                cell.Disable();
                return;
            }

            var suggestion = suggestions[index];
            cell.buttonText.text = suggestion.DisplayText;
        }

        private static void UpdatePosition()
        {
            if (CurrentHandler == null || !CurrentHandler.InputField.isFocused)
                return;

            var input = CurrentHandler.InputField;
            var textGen = input.textComponent.cachedTextGenerator;
            int caretPos = lastCaretPos;
            caretPos--;

            caretPos = Math.Max(0, caretPos);
            caretPos = Math.Min(textGen.characters.Count - 1, caretPos);

            var pos = textGen.characters[caretPos].cursorPos;
            pos = input.transform.TransformPoint(pos);

            uiRoot.transform.position = new Vector3(pos.x + 10, pos.y - 20, 0);
        }

        public static void ConstructUI()
        {
            var parent = UIManager.CanvasRoot;

            dataHandler = new ButtonListSource<Suggestion>(scrollPool, GetEntries, SetCell, ShouldDisplay, OnCellClicked);

            scrollPool = UIFactory.CreateScrollPool(parent, "AutoCompleter", out uiRoot, out GameObject scrollContent);
            var mainRect = uiRoot.GetComponent<RectTransform>();
            mainRect.pivot = new Vector2(0f, 1f);
            mainRect.anchorMin = new Vector2(0.45f, 0.45f);
            mainRect.anchorMax = new Vector2(0.65f, 0.6f);
            mainRect.offsetMin = Vector2.zero;
            mainRect.offsetMax = Vector2.zero;
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(scrollContent, true, false, true, false);

            scrollPool.Initialize(dataHandler, ButtonCell<Suggestion>.CreatePrototypeCell(parent));

            UIRoot.SetActive(false);
        }
    }
}
