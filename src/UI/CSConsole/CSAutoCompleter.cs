using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExplorer.UI.CSConsole.Lexers;
using UnityExplorer.UI.Widgets.AutoComplete;

namespace UnityExplorer.UI.CSConsole
{
    public class CSAutoCompleter : ISuggestionProvider
    {
        public InputFieldRef InputField => ConsoleController.Input;

        public bool AnchorToCaretPosition => true;

        public void OnSuggestionClicked(Suggestion suggestion)
        {
            ConsoleController.InsertSuggestionAtCaret(suggestion.UnderlyingValue);
            AutoCompleteModal.Instance.ReleaseOwnership(this);
        }

        // Delimiters for completions, notably does not include '.'
        private readonly HashSet<char> delimiters = new HashSet<char>
        {
            '{', '}', ',', ';', '<', '>', '(', ')', '[', ']', '=', '|', '&', '?'
        };

        private readonly List<Suggestion> suggestions = new List<Suggestion>();

        public void CheckAutocompletes()
        {
            if (string.IsNullOrEmpty(InputField.Text))
            {
                AutoCompleteModal.Instance.ReleaseOwnership(this);
                return;
            }

            suggestions.Clear();

            int caret = Math.Max(0, Math.Min(InputField.Text.Length - 1, InputField.Component.caretPosition - 1));
            int start = caret;

            // If the character at the caret index is whitespace or delimiter,
            // or if the next character (if it exists) is not whitespace,
            // then we don't want to provide suggestions.
            if (char.IsWhiteSpace(InputField.Text[caret])
                || delimiters.Contains(InputField.Text[caret])
                || (InputField.Text.Length > caret + 1 && !char.IsWhiteSpace(InputField.Text[caret + 1])))
            {
                AutoCompleteModal.Instance.ReleaseOwnership(this);
                return;
            }

            // get the current composition string (from caret back to last delimiter or whitespace)
            while (start > 0)
            {
                start--;
                char c = InputField.Text[start];
                if (char.IsWhiteSpace(c) || delimiters.Contains(c))
                {
                    start++;
                    break;
                }
            }
            string input = InputField.Text.Substring(start, caret - start + 1);

            // Get MCS completions

            string[] evaluatorCompletions = ConsoleController.Evaluator.GetCompletions(input, out string prefix);

            if (!string.IsNullOrEmpty(prefix) && evaluatorCompletions != null && evaluatorCompletions.Any())
            {
                suggestions.AddRange(from completion in evaluatorCompletions
                                     select new Suggestion($"<color=cyan>{prefix}</color>{completion}", completion));
            }

            // Get manual keyword completions

            foreach (var kw in KeywordLexer.keywords)
            {
                if (kw.StartsWith(input))
                {
                    string completion = kw.Substring(input.Length, kw.Length - input.Length);

                    suggestions.Add(new Suggestion(
                        $"<color=cyan>{input}</color>" +
                        $"<color=#{SignatureHighlighter.keywordBlueHex}>{completion}</color>",
                        completion));
                }
            }

            if (suggestions.Any())
            {
                AutoCompleteModal.Instance.TakeOwnership(this);
                AutoCompleteModal.Instance.SetSuggestions(suggestions);
            }
            else
            {
                AutoCompleteModal.Instance.ReleaseOwnership(this);
            }
        }
    }
}
