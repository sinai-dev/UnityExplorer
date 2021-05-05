using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
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

        public InputField InputField { get; }
        public bool AnchorToCaretPosition => false;

        private readonly List<Suggestion> suggestions = new List<Suggestion>();
        private float timeOfLastCheck;

        private HashSet<Type> allowedTypes;

        public TypeCompleter(Type baseType, InputField inputField)
        {
            BaseType = baseType;
            InputField = inputField;

            inputField.onValueChanged.AddListener(OnInputFieldChanged);

            if (BaseType != null)
                CacheTypes();
        }

        public void CacheTypes()
        {
            allowedTypes = ReflectionUtility.GetImplementationsOf(BaseType, true, false);
        }

        public void OnSuggestionClicked(Suggestion suggestion)
        {
            timeOfLastCheck = Time.realtimeSinceStartup;

            InputField.text = suggestion.UnderlyingValue;
            SuggestionClicked?.Invoke(suggestion);

            suggestions.Clear();
            AutoCompleter.Instance.SetSuggestions(suggestions);
        }

        private void OnInputFieldChanged(string value)
        {
            if (!timeOfLastCheck.OccuredEarlierThanDefault())
                return;

            timeOfLastCheck = Time.realtimeSinceStartup;

            value = value ?? "";

            if (string.IsNullOrEmpty(value))
            {
                AutoCompleter.Instance.ReleaseOwnership(this);
            }
            else
            {
                GetSuggestions(value);

                AutoCompleter.Instance.TakeOwnership(this);
                AutoCompleter.Instance.SetSuggestions(suggestions);
            }
        }

        private void GetSuggestions(string value)
        {
            suggestions.Clear();

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
            if (!sharedTypeToLabel.ContainsKey(type.FullName))
                sharedTypeToLabel.Add(type.FullName, SignatureHighlighter.Parse(type, true));

            suggestions.Add(new Suggestion(sharedTypeToLabel[type.FullName], type.FullName));
        }
    }
}
