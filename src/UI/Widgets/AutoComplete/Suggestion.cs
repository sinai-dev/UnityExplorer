namespace UnityExplorer.UI.Widgets.AutoComplete
{
    public struct Suggestion
    {
        public readonly string DisplayText;
        public readonly string UnderlyingValue;

        public Suggestion(string displayText, string underlyingValue)
        {
            DisplayText = displayText;
            UnderlyingValue = underlyingValue;
        }
    }
}
