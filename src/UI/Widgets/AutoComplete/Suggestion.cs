using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityExplorer.Core;

namespace UnityExplorer.UI.Widgets.AutoComplete
{
    public struct Suggestion
    {
        public readonly string DisplayText;
        public readonly string UnderlyingValue;

        public string Combined => DisplayText + UnderlyingValue;

        public Suggestion(string displayText, string underlyingValue)
        {
            DisplayText = displayText;
            UnderlyingValue = underlyingValue;
        }
    }
}
