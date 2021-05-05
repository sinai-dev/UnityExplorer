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
        //public int InsertIndex;
        //public readonly string Prefix;
        //public readonly string Addition;

        //public string Full => Prefix + Addition;

        public Suggestion(string displayText, /* string prefix, string addition, */ string underlyingValue)
        {
            DisplayText = displayText;
            //Addition = addition;
            //Prefix = prefix;
            UnderlyingValue = underlyingValue;
        }
    }
}
