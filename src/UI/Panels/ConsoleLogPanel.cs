using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExplorer.Core.Config;

namespace UnityExplorer.UI.Panels
{
    public class ConsoleLogPanel : UIPanel
    {
        public override string Name => "Console Log";
        public override UIManager.Panels PanelType => UIManager.Panels.ConsoleLog;

        public override int MinWidth => 300;
        public override int MinHeight => 75;

        public override string GetSaveDataFromConfigManager()
        {
            return ConfigManager.ConsoleLogData.Value;
        }

        public override void DoSaveToConfigElement()
        {
            ConfigManager.ConsoleLogData.Value = this.ToSaveData();
        }

        protected internal override void DoSetDefaultPosAndAnchors()
        {
            mainPanelRect.localPosition = Vector2.zero;
            mainPanelRect.pivot = new Vector2(0f, 1f);
            mainPanelRect.anchorMin = new Vector2(0.5f, 0.1f);
            mainPanelRect.anchorMax = new Vector2(0.9f, 0.25f);
        }

        public override void ConstructPanelContent()
        {
            
        }
    }
}
