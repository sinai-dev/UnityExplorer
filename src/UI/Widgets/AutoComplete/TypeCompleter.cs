using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Input;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI.Models;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Utility;

namespace UnityExplorer.UI.Widgets.AutoComplete
{
    public class TypeCompleter : ISuggestionProvider
    {
        internal static readonly Dictionary<string, string> sharedTypeToLabel = new Dictionary<string, string>(4096);

        public event Action<Suggestion> SuggestionClicked;

        public Type BaseType { get; set; }
        public Type[] GenericConstraints { get; set; }

        public InputFieldRef InputField { get; }
        public bool AnchorToCaretPosition => false;

        private readonly List<Suggestion> suggestions = new List<Suggestion>();
        private readonly HashSet<string> suggestedNames = new HashSet<string>();

        private HashSet<Type> allowedTypes;

        private string chosenSuggestion;

        bool ISuggestionProvider.AllowNavigation => false;

        public TypeCompleter(Type baseType, InputFieldRef inputField)
        {
            BaseType = baseType;
            InputField = inputField;

            inputField.OnValueChanged += OnInputFieldChanged;

            if (BaseType != null)
                CacheTypes();
        }

        public void CacheTypes()
        {
            allowedTypes = ReflectionUtility.GetImplementationsOf(BaseType, true, false);
        }

        public void OnSuggestionClicked(Suggestion suggestion)
        {
            InputField.Text = suggestion.UnderlyingValue;
            SuggestionClicked?.Invoke(suggestion);

            suggestions.Clear();
            AutoCompleteModal.Instance.SetSuggestions(suggestions);
            chosenSuggestion = suggestion.UnderlyingValue;
        }

        private void OnInputFieldChanged(string value)
        {
            if (string.IsNullOrEmpty(value) || value == chosenSuggestion)
            {
                chosenSuggestion = null;
                AutoCompleteModal.Instance.ReleaseOwnership(this);
            }
            else
            {
                GetSuggestions(value);

                AutoCompleteModal.Instance.TakeOwnership(this);
                AutoCompleteModal.Instance.SetSuggestions(suggestions);
            }
        }

        private void GetSuggestions(string value)
        {
            suggestions.Clear();
            suggestedNames.Clear();

            if (BaseType == null)
            {
                ExplorerCore.LogWarning("Autocompleter Base type is null!");
                return;
            }

            // Check for exact match first
            if (ReflectionUtility.AllTypes.TryGetValue(value, out Type t) && allowedTypes.Contains(t))
                AddSuggestion(t);

            foreach (var entry in allowedTypes)
            {
                if (entry.FullName.ContainsIgnoreCase(value))
                    AddSuggestion(entry);
            }
        }

        void AddSuggestion(Type type)
        {
            if (suggestedNames.Contains(type.FullName))
                return;
            suggestedNames.Add(type.FullName);

            if (!sharedTypeToLabel.ContainsKey(type.FullName))
                sharedTypeToLabel.Add(type.FullName, SignatureHighlighter.Parse(type, true));

            suggestions.Add(new Suggestion(sharedTypeToLabel[type.FullName], type.FullName));
        }
    }
}
