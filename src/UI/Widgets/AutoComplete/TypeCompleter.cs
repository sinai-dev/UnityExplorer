using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Models;

namespace UnityExplorer.UI.Widgets.AutoComplete
{
    public class TypeCompleter : ISuggestionProvider
    {
        private class CachedType
        {
            public string FullNameForFilter;
            public string FullNameValue;
            public string DisplayName;
        }

        public Type BaseType { get; }
        public InputField InputField { get; }
        public bool AnchorToCaretPosition => false;

        public event Action<Suggestion> SuggestionClicked;
        public void OnSuggestionClicked(Suggestion suggestion)
        {
            SuggestionClicked?.Invoke(suggestion);
            suggestions.Clear();
            AutoCompleter.Instance.SetSuggestions(suggestions);

            timeOfLastCheck = Time.time;
            InputField.text = suggestion.UnderlyingValue;
        }

        private readonly List<Suggestion> suggestions = new List<Suggestion>();

        private readonly Dictionary<string, CachedType> typeCache = new Dictionary<string, CachedType>();

        //// cached list of names for displaying (with proper case)
        //private readonly List<string> cachedTypesNames = new List<string>();
        //// cached list of lookup by index (lowercase)
        //private readonly List<string> cachedTypesFilter = new List<string>();
        //// cached hashset of names (lower case)
        //private readonly HashSet<string> cachedTypesSet = new HashSet<string>();

        public TypeCompleter(Type baseType, InputField inputField)
        {
            BaseType = baseType;
            InputField = inputField;

            inputField.onValueChanged.AddListener(OnInputFieldChanged);

            var types = ReflectionUtility.GetImplementationsOf(this.BaseType, true, false);

            var list = new List<CachedType>();

            foreach (var type in types)
            {
                string displayName = Utility.SignatureHighlighter.ParseFullSyntax(type, true);
                string fullName = RuntimeProvider.Instance.Reflection.GetDeobfuscatedType(type).FullName;

                string filteredName = fullName;

                list.Add(new CachedType
                {
                    FullNameValue = fullName,
                    FullNameForFilter = filteredName,
                    DisplayName = displayName,
                });
            }

            list.Sort((CachedType a, CachedType b) => a.FullNameForFilter.CompareTo(b.FullNameForFilter));

            foreach (var cache in list)
            {
                if (typeCache.ContainsKey(cache.FullNameForFilter))
                    continue;
                typeCache.Add(cache.FullNameForFilter, cache);
            }

        }

        private float timeOfLastCheck;

        private void OnInputFieldChanged(string value)
        {
            if (timeOfLastCheck == Time.time)
                return;

            timeOfLastCheck = Time.time;

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

            var added = new HashSet<string>();

            if (typeCache.TryGetValue(value, out CachedType cache))
                AddToDict(cache);

            foreach (var entry in typeCache.Values)
            {
                if (added.Contains(entry.FullNameValue))
                    continue;

                if (entry.FullNameForFilter.ContainsIgnoreCase(value))
                    AddToDict(entry);

                added.Add(entry.FullNameValue);
            }

            void AddToDict(CachedType entry)
            {
                added.Add(entry.FullNameValue);

                suggestions.Add(new Suggestion(entry.DisplayName,
                        value,
                        entry.FullNameForFilter.Substring(value.Length, entry.FullNameForFilter.Length - value.Length),
                        entry.FullNameValue));
            }
        }
    }
}
