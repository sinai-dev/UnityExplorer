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
        private struct CachedType
        {
            public string FilteredName;
            public string DisplayName;
        }

        public Type BaseType { get; }
        public InputField InputField { get; }

        public event Action<Suggestion> SuggestionClicked;
        public void OnSuggestionClicked(Suggestion suggestion)
        {
            SuggestionClicked?.Invoke(suggestion);
            suggestions.Clear();
            AutoCompleter.SetSuggestions(suggestions);

            timeOfLastCheck = Time.time;
            InputField.text = suggestion.DisplayText;
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

            var types = ReflectionUtility.GetImplementationsOf(typeof(UnityEngine.Object), true);
            foreach (var type in types.OrderBy(it => it.FullName))
            {
                var name = type.FullName;
                typeCache.Add(name.ToLower(), new CachedType 
                { 
                    DisplayName = name, 
                    FilteredName = name.ToLower() 
                });
            }
        }

        private float timeOfLastCheck;

        private void OnInputFieldChanged(string value)
        {
            if (timeOfLastCheck == Time.time)
                return;

            timeOfLastCheck = Time.time;

            value = value?.ToLower() ?? "";

            if (string.IsNullOrEmpty(value))
            {
                AutoCompleter.ReleaseOwnership(this);
            }
            else
            {
                GetSuggestions(value);

                AutoCompleter.TakeOwnership(this);
                AutoCompleter.SetSuggestions(suggestions);
            }
        }

        private void GetSuggestions(string value)
        {
            suggestions.Clear();

            var added = new HashSet<string>();

            if (typeCache.TryGetValue(value, out CachedType cache))
            {
                added.Add(value);
                suggestions.Add(new Suggestion(cache.DisplayName, 
                    value, 
                    cache.FilteredName.Substring(value.Length, cache.FilteredName.Length - value.Length),
                    Color.white));
            }

            foreach (var entry in typeCache.Values)
            {
                if (added.Contains(entry.FilteredName))
                    continue;

                if (entry.FilteredName.Contains(value))
                {
                    suggestions.Add(new Suggestion(entry.DisplayName,
                        value,
                        entry.FilteredName.Substring(value.Length, entry.FilteredName.Length - value.Length),
                        Color.white));
                }

                added.Add(value);
            }
        }
    }
}
