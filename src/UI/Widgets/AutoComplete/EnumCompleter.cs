using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityExplorer.CacheObject.IValues;
using UniverseLib;
using UniverseLib.UI;

namespace UnityExplorer.UI.Widgets.AutoComplete
{
    public class EnumCompleter : ISuggestionProvider
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

        public Type EnumType { get; set; }

        public InputFieldRef InputField { get; }
        public bool AnchorToCaretPosition => false;

        private readonly List<Suggestion> suggestions = new List<Suggestion>();
        private readonly HashSet<string> suggestedValues = new HashSet<string>();

        private OrderedDictionary enumValues;

        internal string chosenSuggestion;

        bool ISuggestionProvider.AllowNavigation => false;

        public EnumCompleter(Type enumType, InputFieldRef inputField)
        {
            EnumType = enumType;
            InputField = inputField;

            inputField.OnValueChanged += OnInputFieldChanged;

            if (EnumType != null)
                CacheEnumValues();
        }

        public void CacheEnumValues()
        {
            enumValues = InteractiveEnum.GetEnumValues(EnumType);
        }

        private string GetLastSplitInput(string fullInput)
        {
            string ret = fullInput;
            
            int lastSplit = fullInput.LastIndexOf(',');
            if (lastSplit >= 0)
            {
                lastSplit++;
                if (lastSplit == fullInput.Length)
                    ret = "";
                else
                    ret = fullInput.Substring(lastSplit);
            }

            return ret;
        }

        public void OnSuggestionClicked(Suggestion suggestion)
        {
            chosenSuggestion = suggestion.UnderlyingValue;

            string lastInput = GetLastSplitInput(InputField.Text);

            if (lastInput != suggestion.UnderlyingValue)
            {
                string valueToSet = InputField.Text;

                if (valueToSet.Length > 0)
                    valueToSet = valueToSet.Substring(0, InputField.Text.Length - lastInput.Length);

                valueToSet += suggestion.UnderlyingValue;

                InputField.Text = valueToSet;

                //InputField.Text += suggestion.UnderlyingValue.Substring(lastInput.Length);
            }

            SuggestionClicked?.Invoke(suggestion);

            suggestions.Clear();
            AutoCompleteModal.Instance.SetSuggestions(suggestions);
        }

        public void HelperButtonClicked()
        {
            GetSuggestions("");
            AutoCompleteModal.Instance.TakeOwnership(this);
            AutoCompleteModal.Instance.SetSuggestions(suggestions);
        }

        private void OnInputFieldChanged(string value)
        {
            if (!Enabled)
                return;

            if (string.IsNullOrEmpty(value) || GetLastSplitInput(value) == chosenSuggestion)
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
            suggestedValues.Clear();

            if (EnumType == null)
            {
                ExplorerCore.LogWarning("Autocompleter Base enum type is null!");
                return;
            }

            value = GetLastSplitInput(value);

            for (int i = 0; i < this.enumValues.Count; i++)
            {
                var enumValue = (CachedEnumValue)enumValues[i];
                if (enumValue.Name.ContainsIgnoreCase(value))
                    AddSuggestion(enumValue.Name);
            }
        }

        internal static readonly Dictionary<string, string> sharedValueToLabel = new Dictionary<string, string>(4096);

        void AddSuggestion(string value)
        {
            if (suggestedValues.Contains(value))
                return;
            suggestedValues.Add(value);

            if (!sharedValueToLabel.ContainsKey(value))
                sharedValueToLabel.Add(value, $"<color={SignatureHighlighter.CONST}>{value}</color>");

            suggestions.Add(new Suggestion(sharedValueToLabel[value], value));
        }
    }
}
