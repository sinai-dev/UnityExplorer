using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExplorerBeta;
using ExplorerBeta.Input;
using ExplorerBeta.UI;
using ExplorerBeta.UI.Main;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if CPP
using UnhollowerRuntimeLib;
#endif

namespace Explorer.UI.Main.Pages.Console
{
    public class AutoCompleter
    {
        public static AutoCompleter Instance;

        public const int MAX_LABELS = 500;
        private const int UPDATES_PER_BATCH = 100;

        public static GameObject m_mainObj;
        private static RectTransform m_thisRect;

        private static readonly List<GameObject> m_suggestionButtons = new List<GameObject>();
        private static readonly List<Text> m_suggestionTexts = new List<Text>();
        private static readonly List<Text> m_hiddenSuggestionTexts = new List<Text>();

        private static bool m_suggestionsDirty;
        private static AutoComplete[] m_suggestions = new AutoComplete[0];
        private static int m_lastBatchIndex;

        private static string m_prevInput = "NULL";
        private static int m_lastCaretPos;

        public static void Init()
        {
            ConstructUI();

            m_mainObj.SetActive(false);
        }

        public static void Update()
        {
            if (!m_mainObj)
                return;

            if (!ConsolePage.EnableSuggestions)
            {
                if (m_mainObj.activeSelf)
                    m_mainObj.SetActive(false);

                return;
            }

            RefreshButtons();

            UpdatePosition();
        }

        public static void SetSuggestions(AutoComplete[] suggestions)
        {
            m_suggestions = suggestions;

            m_suggestionsDirty = true;
            m_lastBatchIndex = 0;
        }

        private static void RefreshButtons()
        {
            if (!m_suggestionsDirty)
                return;

            if (m_suggestions.Length < 1)
            {
                if (m_mainObj.activeSelf)
                    m_mainObj?.SetActive(false);
                return;
            }

            if (!m_mainObj.activeSelf)
            {
                m_mainObj.SetActive(true);
            }

            if (m_suggestions.Length < 1 || m_lastBatchIndex >= MAX_LABELS)
            {
                m_suggestionsDirty = false;
                return;
            }

            int end = m_lastBatchIndex + UPDATES_PER_BATCH;
            for (int i = m_lastBatchIndex; i < end && i < MAX_LABELS; i++)
            {
                if (i >= m_suggestions.Length)
                {
                    if (m_suggestionButtons[i].activeSelf)
                        m_suggestionButtons[i].SetActive(false);
                }
                else
                {
                    if (!m_suggestionButtons[i].activeSelf)
                        m_suggestionButtons[i].SetActive(true);

                    var suggestion = m_suggestions[i];
                    var label = m_suggestionTexts[i];
                    var hiddenLabel = m_hiddenSuggestionTexts[i];

                    label.text = suggestion.Full;
                    hiddenLabel.text = suggestion.Addition;

                    if (suggestion.Context == AutoComplete.Contexts.Namespace)
                    {
                        label.color = new Color(0.75f, 0.75f, 0.75f, 1.0f);
                    }
                    else
                    {
                        label.color = Color.white;
                    }
                }

                m_lastBatchIndex = i;
            }

            m_lastBatchIndex++;
        }

        private static void UpdatePosition()
        {
            var editor = ConsolePage.Instance.m_codeEditor;

            if (editor.InputField.text.Length < 1)
                return;

            var caretPos = editor.InputField.caretPosition;
            while (caretPos >= editor.inputText.textInfo.characterInfo.Length)
                caretPos--;

            if (caretPos == m_lastCaretPos)
                return;

            m_lastCaretPos = caretPos;

            if (caretPos >= 0 && caretPos < editor.inputText.textInfo.characterInfo.Length)
            {
                var pos = editor.inputText.textInfo.characterInfo[caretPos].bottomLeft;

                pos = MainMenu.Instance.MainPanel.transform.TransformPoint(pos);

                m_mainObj.transform.position = new Vector3(pos.x + m_thisRect.rect.width / 2, pos.y - 12, 0);
            }
        }

        private static readonly char[] splitChars = new[] { '{', '}', ',', ';', '<', '>', '(', ')', '[', ']', '=', '|', '&' };

        public static void CheckAutocomplete()
        {
            var m_codeEditor = ConsolePage.Instance.m_codeEditor;
            var input = m_codeEditor.InputField.text;
            var caretIndex = m_codeEditor.InputField.caretPosition;

            if (!string.IsNullOrEmpty(input))
            {
                try
                {
                    var start = caretIndex <= 0 ? 0 : input.LastIndexOfAny(splitChars, caretIndex - 1) + 1;
                    input = input.Substring(start, caretIndex - start).Trim();
                }
                catch (ArgumentException) { }
            }

            if (!string.IsNullOrEmpty(input) && input != m_prevInput)
            {
                GetAutocompletes(input);
            }
            else
            {
                ClearAutocompletes();
            }

            m_prevInput = input;
        }

        public static void ClearAutocompletes()
        {
            if (ConsolePage.AutoCompletes.Any())
            {
                ConsolePage.AutoCompletes.Clear();
            }
        }

        public static void GetAutocompletes(string input)
        {
            try
            {
                // Credit ManylMarco
                ConsolePage.AutoCompletes.Clear();
                var completions = ConsolePage.Instance.m_evaluator.GetCompletions(input, out string prefix);
                if (completions != null)
                {
                    if (prefix == null)
                        prefix = input;

                    ConsolePage.AutoCompletes.AddRange(completions
                        .Where(x => !string.IsNullOrEmpty(x))
                        .Select(x => new AutoComplete(x, prefix, AutoComplete.Contexts.Other))
                        );
                }

                var trimmed = input.Trim();
                if (trimmed.StartsWith("using"))
                    trimmed = trimmed.Remove(0, 5).Trim();

                var namespaces = AutoCompleteHelpers.Namespaces
                    .Where(x => x.StartsWith(trimmed) && x.Length > trimmed.Length)
                    .Select(x => new AutoComplete(
                        x.Substring(trimmed.Length),
                        x.Substring(0, trimmed.Length),
                        AutoComplete.Contexts.Namespace));

                ConsolePage.AutoCompletes.AddRange(namespaces);
            }
            catch (Exception ex)
            {
                ExplorerCore.Log("C# Console error:\r\n" + ex);
                ClearAutocompletes();
            }
        }

#region UI Construction

        private static void ConstructUI()
        {
            var parent = UIManager.CanvasRoot;

            var obj = UIFactory.CreateScrollView(parent, out GameObject content, new Color(0.1f, 0.1f, 0.1f, 0.95f));

            m_mainObj = obj;

            var mainRect = obj.GetComponent<RectTransform>();
            m_thisRect = mainRect;
            mainRect.pivot = new Vector2(0.5f, 0.5f);
            mainRect.anchorMin = new Vector2(0.45f, 0.45f);
            mainRect.anchorMax = new Vector2(0.65f, 0.6f);
            mainRect.offsetMin = Vector2.zero;
            mainRect.offsetMax = Vector2.zero;

            var mainGroup = content.GetComponent<VerticalLayoutGroup>();
            mainGroup.childControlHeight = false;
            mainGroup.childControlWidth = true;
            mainGroup.childForceExpandHeight = false;
            mainGroup.childForceExpandWidth = true;

            for (int i = 0; i < MAX_LABELS; i++)
            {
                var buttonObj = UIFactory.CreateButton(content);
                var btn = buttonObj.GetComponent<Button>();
                var btnColors = btn.colors;
                btnColors.normalColor = new Color(0f, 0f, 0f, 0f);
                btnColors.highlightedColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
                btn.colors = btnColors;

                var nav = btn.navigation;
                nav.mode = Navigation.Mode.Vertical;
                btn.navigation = nav;

                var btnLayout = buttonObj.AddComponent<LayoutElement>();
                btnLayout.minHeight = 20;

                var text = btn.GetComponentInChildren<Text>();
                text.alignment = TextAnchor.MiddleLeft;
                text.color = Color.white;

                var hiddenChild = UIFactory.CreateUIObject("HiddenText", buttonObj);
                hiddenChild.SetActive(false);
                var hiddenText = hiddenChild.AddComponent<Text>();
                m_hiddenSuggestionTexts.Add(hiddenText);

#if CPP
                btn.onClick.AddListener(new Action(UseAutocompleteButton));
#else
                btn.onClick.AddListener(UseAutocompleteButton);
#endif

                void UseAutocompleteButton()
                {
                    ConsolePage.Instance.UseAutocomplete(hiddenText.text);
                    EventSystem.current.SetSelectedGameObject(ConsolePage.Instance.m_codeEditor.InputField.gameObject, 
                        null);
                }

                m_suggestionButtons.Add(buttonObj);
                m_suggestionTexts.Add(text);
            }
        }

#endregion
    }
}
