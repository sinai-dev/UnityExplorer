using UniverseLib.UI.Panels;

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
