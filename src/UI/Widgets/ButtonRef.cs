using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UI;

namespace UnityExplorer.UI.Widgets
{
    // A simple helper class to handle a button's OnClick more effectively.

    public class ButtonRef
    {
        public Action OnClick;

        public Button Button { get; }
        public Text ButtonText { get; }

        public ButtonRef(Button button)
        {
            this.Button = button;
            this.ButtonText = button.GetComponentInChildren<Text>();

            button.onClick.AddListener(() => { OnClick?.Invoke(); });
        }
    }
}
