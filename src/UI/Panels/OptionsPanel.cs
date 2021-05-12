using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityExplorer.UI.Panels
{
    public class OptionsPanel : UIPanel
    {
        public override string Name => "Options";
        public override UIManager.Panels PanelType => UIManager.Panels.Options;

        public override int MinWidth => 400;
        public override int MinHeight => 200;

        public override void ConstructPanelContent()
        {
            throw new NotImplementedException();
        }

        public override void DoSaveToConfigElement()
        {
            throw new NotImplementedException();
        }

        public override string GetSaveData()
        {
            throw new NotImplementedException();
        }

        protected internal override void DoSetDefaultPosAndAnchors()
        {
            throw new NotImplementedException();
        }
    }
}
