using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityExplorer.UI.Panels
{
    public class ConsoleLogPanel : UIPanel
    {
        public override string Name => "Console Log";
        public override UIManager.Panels PanelType => UIManager.Panels.ConsoleLog;

        public override int MinWidth => 300;
        public override int MinHeight => 75;

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
