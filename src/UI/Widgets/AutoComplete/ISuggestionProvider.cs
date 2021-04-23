using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace UnityExplorer.UI.Widgets.AutoComplete
{
    public interface ISuggestionProvider
    {
        InputField InputField { get; }

        void OnSuggestionClicked(Suggestion suggestion);
    }
}
