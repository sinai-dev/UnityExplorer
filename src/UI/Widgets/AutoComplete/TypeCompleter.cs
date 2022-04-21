using System;
using System.Collections.Generic;
using System.Linq;
using UnityExplorer.UI.Panels;
using UniverseLib;
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

        public InputFieldRef InputField { get; }
        public bool AnchorToCaretPosition => false;

        readonly bool allowAbstract;
        readonly bool allowEnum;
        readonly bool allowGeneric;

        private HashSet<Type> allowedTypes;
        private HashSet<Type> allAllowedTypes;

        readonly List<Suggestion> suggestions = new();
        readonly HashSet<string> suggestedNames = new();
        private string chosenSuggestion;

        bool ISuggestionProvider.AllowNavigation => false;

        static readonly Dictionary<string, Type> shorthandToType = new()
        {
            { "object", typeof(object) },
            { "string", typeof(string) },
            { "bool", typeof(bool) },
            { "byte", typeof(byte) },
            { "sbyte", typeof(sbyte) },
            { "char", typeof(char) },
            { "decimal", typeof(decimal) },
            { "double", typeof(double) },
            { "float", typeof(float) },
            { "int", typeof(int) },
            { "uint", typeof(uint) },
            { "long", typeof(long) },
            { "ulong", typeof(ulong) },
            { "short", typeof(short) },
            { "ushort", typeof(ushort) },
            { "void", typeof(void) },
        };

        public TypeCompleter(Type baseType, InputFieldRef inputField) : this(baseType, inputField, true, true, true) { }

        public TypeCompleter(Type baseType, InputFieldRef inputField, bool allowAbstract, bool allowEnum, bool allowGeneric)
        {
            BaseType = baseType;
            InputField = inputField;

            this.allowAbstract = allowAbstract;
            this.allowEnum = allowEnum;
            this.allowGeneric = allowGeneric;

            inputField.OnValueChanged += OnInputFieldChanged;

            if (BaseType != null)
                CacheTypes();
        }

        public void CacheTypes()
        {
            if (!AllTypes)
                allowedTypes = ReflectionUtility.GetImplementationsOf(BaseType, allowAbstract, allowGeneric, allowEnum);
            else
                allowedTypes = GetAllAllowedTypes();
        }

        HashSet<Type> GetAllAllowedTypes()
        {
            if (allAllowedTypes == null)
            {
                allAllowedTypes = new();
                foreach (KeyValuePair<string, Type> entry in ReflectionUtility.AllTypes)
                {
                    // skip <PrivateImplementationDetails> and <AnonymousClass> classes
                    Type type = entry.Value;
                    if (type.FullName.Contains("PrivateImplementationDetails")
                             || type.FullName.Contains("DisplayClass")
                             || type.FullName.Contains('<'))
                    {
                        continue;
                    }
                    allAllowedTypes.Add(type);
                }
            }

            return allAllowedTypes;
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

                AutoCompleteModal.TakeOwnership(this);
                AutoCompleteModal.Instance.SetSuggestions(suggestions);
            }
        }

        private void GetSuggestions(string input)
        {
            suggestions.Clear();
            suggestedNames.Clear();

            if (!AllTypes && BaseType == null)
            {
                ExplorerCore.LogWarning("Autocompleter Base type is null!");
                return;
            }

            // shorthand types all inherit from System.Object
            if (AllTypes || BaseType == typeof(object))
            {
                if (shorthandToType.TryGetValue(input, out Type shorthand))
                    AddSuggestion(shorthand);

                foreach (KeyValuePair<string, Type> entry in shorthandToType)
                {
                    if (entry.Key.StartsWith(input, StringComparison.InvariantCultureIgnoreCase))
                        AddSuggestion(entry.Value);
                }
            }

            // Check for exact match first
            if (ReflectionUtility.GetTypeByName(input) is Type t && allowedTypes.Contains(t))
                AddSuggestion(t);

            foreach (Type entry in allowedTypes)
            {
                if (entry.FullName.ContainsIgnoreCase(input))
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
