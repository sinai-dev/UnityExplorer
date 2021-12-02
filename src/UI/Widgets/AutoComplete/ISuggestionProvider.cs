using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;

namespace UnityExplorer.UI.Widgets.AutoComplete
{
    public interface ISuggestionProvider
    {
        InputFieldRef InputField { get; }
        bool AnchorToCaretPosition { get; }

        bool AllowNavigation { get; }

        void OnSuggestionClicked(Suggestion suggestion);
    }
}
