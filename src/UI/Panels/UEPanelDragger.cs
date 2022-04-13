using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Widgets.AutoComplete;
using UniverseLib.Input;
using UniverseLib.UI;
using UniverseLib.UI.Panels;
using UniverseLib.Utility;

namespace UnityExplorer.UI.Panels
{
    public class UEPanelDragger : PanelDragger
    {
        public UEPanelDragger(PanelBase uiPanel) : base(uiPanel) { }

        protected override bool MouseInResizeArea(Vector2 mousePos)
        {
            return !UIManager.NavBarRect.rect.Contains(UIManager.NavBarRect.InverseTransformPoint(mousePos))
                && base.MouseInResizeArea(mousePos);
        }
    }
}
