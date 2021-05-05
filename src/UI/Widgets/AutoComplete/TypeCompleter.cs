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

namespace UnityExplorer.UI.Widgets.AutoComplete
{
    public class TypeCompleter : ISuggestionProvider
    {
        public class CachedType
        {
            public Type Type;
            public string FullNameValue;
            public string DisplayName;
        }

        public event Action<Suggestion> SuggestionClicked;

        public Type BaseType { get; set; }
        public Type[] GenericConstraints { get; set; }

        public InputField InputField { get; }
        public bool AnchorToCaretPosition => false;

        private readonly List<Suggestion> suggestions = new List<Suggestion>();
        private float timeOfLastCheck;

        public Dictionary<string, CachedType> AllTypes = new Dictionary<string, CachedType>();

        // cached type trees from all autocompleters
        private static readonly Dictionary<string, Dictionary<string, CachedType>> typeCache = new Dictionary<string, Dictionary<string, CachedType>>();

        public TypeCompleter(Type baseType, InputField inputField)
        {
            BaseType = baseType;
            InputField = inputField;

            inputField.onValueChanged.AddListener(OnInputFieldChanged);

            if (BaseType != null)
                CacheTypes();
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

            var added = new HashSet<string>();

            // Check for exact match first
            if (AllTypes.TryGetValue(value, out CachedType cache))
                AddSuggestion(cache);

            foreach (var entry in AllTypes.Values)
                AddSuggestion(entry);

            void AddSuggestion(CachedType entry)
            {
                if (entry.FullNameValue == null)
                    entry.FullNameValue = ReflectionProvider.Instance.GetDeobfuscatedType(entry.Type).FullName;

                if (added.Contains(entry.FullNameValue))
                    return;
                added.Add(entry.FullNameValue);

                if (entry.DisplayName == null)
                    entry.DisplayName = Utility.SignatureHighlighter.ParseFullSyntax(entry.Type, true);

                suggestions.Add(new Suggestion(entry.DisplayName, entry.FullNameValue));
            }
        }

        public void CacheTypes()
        {
            var key = BaseType.AssemblyQualifiedName;

            if (typeCache.ContainsKey(key))
            {
                AllTypes = typeCache[key];
                return;
            }

            AllTypes = new Dictionary<string, CachedType>();

            var list = ReflectionUtility.GetImplementationsOf(BaseType, true, false)
                .Select(it => new CachedType() 
                {
                    Type = it, 
                    FullNameValue = ReflectionProvider.Instance.GetDeobfuscatedType(it).FullName 
                })
                .ToList();

            list.Sort((CachedType a, CachedType b) => a.FullNameValue.CompareTo(b.FullNameValue));

            foreach (var cache in list)
            {
                if (AllTypes.ContainsKey(cache.FullNameValue))
                    continue;
                AllTypes.Add(cache.FullNameValue, cache);
            }

            typeCache.Add(key, AllTypes);
        }
    }
}
