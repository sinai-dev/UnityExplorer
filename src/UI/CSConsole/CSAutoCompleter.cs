using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExplorer.UI.CSharpConsole.Lexers;
using UnityExplorer.UI.Widgets.AutoComplete;

namespace UnityExplorer.UI.CSharpConsole
{
    public class CSAutoCompleter : ISuggestionProvider
    {
        public InputFieldRef InputField => CSConsole.Input;

        public bool AnchorToCaretPosition => true;

        public void OnSuggestionClicked(Suggestion suggestion)
        {
            CSConsole.InsertSuggestionAtCaret(suggestion.UnderlyingValue);
        }

        private readonly HashSet<char> delimiters = new HashSet<char>
        {
            '{', '}', ',', ';', '<', '>', '(', ')', '[', ']', '=', '|', '&', '?'
        };

        //private readonly HashSet<string> expressions = new HashSet<string>
        //{
        //    "new", 
        //    "is", "as",
        //    "return", "yield", "throw", "in",
        //    "do", "for", "foreach", 
        //    "else",
        //};

        public void CheckAutocompletes()
        {
            
            if (string.IsNullOrEmpty(InputField.Text))
            {
                AutoCompleteModal.Instance.ReleaseOwnership(this);
                return;
            }

            int caret = Math.Max(0, Math.Min(InputField.Text.Length - 1, InputField.Component.caretPosition - 1));
            int i = caret;

            while (i > 0)
            {
                i--;
                char c = InputField.Text[i];
                if (char.IsWhiteSpace(c) || delimiters.Contains(c))
                {
                    i++;
                    break;
                }
            }

            i = Math.Max(0, i);

            string input = InputField.Text.Substring(i, (caret - i + 1));

            string[] evaluatorCompletions = CSConsole.Evaluator.GetCompletions(input, out string prefix);

            if (evaluatorCompletions != null && evaluatorCompletions.Any())
            {
                var suggestions = from completion in evaluatorCompletions
                                  select new Suggestion($"<color=cyan>{prefix}</color>{completion}", completion);

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
