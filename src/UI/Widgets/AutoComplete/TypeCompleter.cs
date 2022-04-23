using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
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
            get => enabled;
            set
            {
                enabled = value;
                if (!enabled)
                { 
                    AutoCompleteModal.Instance.ReleaseOwnership(this);
                    if (getSuggestionsCoroutine != null)
                        RuntimeHelper.StopCoroutine(getSuggestionsCoroutine);
                }
            }
        }
        bool enabled = true;

        public event Action<Suggestion> SuggestionClicked;

        public InputFieldRef InputField { get; }
        public bool AnchorToCaretPosition => false;

        readonly bool allowAbstract;
        readonly bool allowEnum;
        readonly bool allowGeneric;

        public Type BaseType { get; set; }
        HashSet<Type> allowedTypes;
        string pendingInput;
        Coroutine getSuggestionsCoroutine;
        readonly Stopwatch cacheTypesStopwatch = new();

        readonly List<Suggestion> suggestions = new();
        readonly HashSet<string> suggestedTypes = new();
        string chosenSuggestion;

        readonly List<Suggestion> loadingSuggestions = new()
        {
            new("<color=grey>Loading...</color>", "")
        };

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

            CacheTypes();
        }

        public void OnSuggestionClicked(Suggestion suggestion)
        {
            chosenSuggestion = suggestion.UnderlyingValue;
            InputField.Text = suggestion.UnderlyingValue;
            SuggestionClicked?.Invoke(suggestion);

            suggestions.Clear();
            //AutoCompleteModal.Instance.SetSuggestions(suggestions, true);
            AutoCompleteModal.Instance.ReleaseOwnership(this);
        }

        public void CacheTypes()
        {
            allowedTypes = null;
            cacheTypesStopwatch.Reset();
            cacheTypesStopwatch.Start();
            ReflectionUtility.GetImplementationsOf(BaseType, OnTypesCached, allowAbstract, allowGeneric, allowEnum);
        }

        void OnTypesCached(HashSet<Type> set)
        {
            allowedTypes = set;

            // ExplorerCore.Log($"Cached {allowedTypes.Count} TypeCompleter types in {cacheTypesStopwatch.ElapsedMilliseconds * 0.001f} seconds.");

            if (pendingInput != null)
            {
                GetSuggestions(pendingInput);
                pendingInput = null;
            }
        }

        void OnInputFieldChanged(string input)
        {
            if (!Enabled)
                return;

            if (input != chosenSuggestion)
                chosenSuggestion = null;

            if (string.IsNullOrEmpty(input) || input == chosenSuggestion)
            {
                if (getSuggestionsCoroutine != null)
                    RuntimeHelper.StopCoroutine(getSuggestionsCoroutine);
                AutoCompleteModal.Instance.ReleaseOwnership(this);
            }
            else
            {
                GetSuggestions(input);
            }
        }

        void GetSuggestions(string input)
        {
            if (allowedTypes == null)
            {
                if (pendingInput != null)
                {
                    AutoCompleteModal.TakeOwnership(this);
                    AutoCompleteModal.Instance.SetSuggestions(loadingSuggestions, true);
                }

                pendingInput = input;
                return;
            }

            if (getSuggestionsCoroutine != null)
                RuntimeHelper.StopCoroutine(getSuggestionsCoroutine);

            getSuggestionsCoroutine = RuntimeHelper.StartCoroutine(GetSuggestionsAsync(input));
        }

        IEnumerator GetSuggestionsAsync(string input)
        {
            suggestions.Clear();
            suggestedTypes.Clear();

            AutoCompleteModal.TakeOwnership(this);
            AutoCompleteModal.Instance.SetSuggestions(suggestions, true);

            // shorthand types all inherit from System.Object
            if (shorthandToType.TryGetValue(input, out Type shorthand) && allowedTypes.Contains(shorthand))
                AddSuggestion(shorthand);

            foreach (KeyValuePair<string, Type> entry in shorthandToType)
            {
                if (allowedTypes.Contains(entry.Value) && entry.Key.StartsWith(input, StringComparison.InvariantCultureIgnoreCase))
                    AddSuggestion(entry.Value);
            }

            // Check for exact match first
            if (ReflectionUtility.GetTypeByName(input) is Type t && allowedTypes.Contains(t))
                AddSuggestion(t);

            if (!suggestions.Any())
                AutoCompleteModal.Instance.SetSuggestions(loadingSuggestions, false);
            else
                AutoCompleteModal.Instance.SetSuggestions(suggestions, false);

            Stopwatch sw = new();
            sw.Start();

            // ExplorerCore.Log($"Checking {allowedTypes.Count} types...");

            foreach (Type entry in allowedTypes)
            {
                if (AutoCompleteModal.CurrentHandler == null)
                    yield break;

                if (sw.ElapsedMilliseconds > 10)
                {
                    yield return null;
                    if (suggestions.Any())
                        AutoCompleteModal.Instance.SetSuggestions(suggestions, false);

                    sw.Reset();
                    sw.Start();
                }

                if (entry.FullName.ContainsIgnoreCase(input))
                    AddSuggestion(entry);
            }

            AutoCompleteModal.Instance.SetSuggestions(suggestions, false);

            // ExplorerCore.Log($"Fetched {suggestions.Count} TypeCompleter suggestions in {sw.ElapsedMilliseconds * 0.001f} seconds.");
        }

        internal static readonly Dictionary<string, string> sharedTypeToLabel = new();

        void AddSuggestion(Type type)
        {
            if (suggestedTypes.Contains(type.FullName))
                return;
            suggestedTypes.Add(type.FullName);

            if (!sharedTypeToLabel.ContainsKey(type.FullName))
                sharedTypeToLabel.Add(type.FullName, SignatureHighlighter.Parse(type, true));

            suggestions.Add(new Suggestion(sharedTypeToLabel[type.FullName], type.FullName));
        }
    }
}
