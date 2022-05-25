using UniverseLib.UI;
using UniverseLib.UI.Panels;

namespace UnityExplorer.UI
{
    internal class ExplorerUIBase : UIBase
    {
        public ExplorerUIBase(string id, Action updateMethod) : base(id, updateMethod) { }

        protected override PanelManager CreatePanelManager()
        {
            return new UEPanelManager(this);
        }
    }
}
