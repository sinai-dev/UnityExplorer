using HarmonyLib;
using System;
using System.Collections.Generic;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.Utility;

namespace UnityExplorer.UI.Widgets.AutoComplete
{
    public class TypeCompleter : ISuggestionProvider
    {
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (!_enabled)
                    AutoCompleteModal.Instance.ReleaseOwnership(this);
            }
        }
        private bool _enabled = true;

        public event Action<Suggestion> SuggestionClicked;

        public Type BaseType { get; set; }
        public Type[] GenericConstraints { get; set; }
        public bool AllTypes { get; set; }

        private readonly bool allowAbstract;
        private readonly bool allowEnum;

        public InputFieldRef InputField { get; }
        public bool AnchorToCaretPosition => false;

        private readonly List<Suggestion> suggestions = new();
        private readonly HashSet<string> suggestedNames = new();

        private HashSet<Type> allowedTypes;

        private string chosenSuggestion;

        bool ISuggestionProvider.AllowNavigation => false;

        public TypeCompleter(Type baseType, InputFieldRef inputField) : this(baseType, inputField, true, true) { }

        public TypeCompleter(Type baseType, InputFieldRef inputField, bool allowAbstract, bool allowEnum)
        {
            BaseType = baseType;
            InputField = inputField;

            this.allowAbstract = allowAbstract;
            this.allowEnum = allowEnum;

            inputField.OnValueChanged += OnInputFieldChanged;

            if (BaseType != null)
                CacheTypes();
        }

        public void CacheTypes()
        {
            if (!AllTypes)
                allowedTypes = ReflectionUtility.GetImplementationsOf(BaseType, allowAbstract, allowEnum, false);
            else
            {
                allowedTypes = new();
                foreach (var entry in ReflectionUtility.AllTypes)
                    allowedTypes.Add(entry.Value);
            }
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
            if (!Enabled)
                return;

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
            if (ReflectionUtility.GetTypeByName(value) is Type t && allowedTypes.Contains(t))
                AddSuggestion(t);

            foreach (var entry in allowedTypes)
            {
                if (entry.FullName.ContainsIgnoreCase(value))
                    AddSuggestion(entry);
            }
        }

        internal static readonly Dictionary<string, string> sharedTypeToLabel = new();

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
