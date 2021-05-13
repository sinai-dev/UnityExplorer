using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.UI.CacheObject;
using UnityExplorer.UI.CacheObject.Views;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Panels
{
    public class OptionsPanel : UIPanel, ICacheObjectController, ICellPoolDataSource<ConfigEntryCell>
    {
        public override string Name => "Options";
        public override UIManager.Panels PanelType => UIManager.Panels.Options;

        public override int MinWidth => 550;
        public override int MinHeight => 200;

        public override bool ShouldSaveActiveState => false;
        public override bool ShowByDefault => false;

        // Entry holders
        private readonly List<CacheConfigEntry> configEntries = new List<CacheConfigEntry>();

        // ICacheObjectController
        public CacheObjectBase ParentCacheObject => null;
        public object Target => null;
        public Type TargetType => null;
        public bool CanWrite => true;

        // ICellPoolDataSource
        public int ItemCount => configEntries.Count;

        public OptionsPanel()
        {
            foreach (var entry in ConfigManager.ConfigElements)
            {
                var cache = new CacheConfigEntry(entry.Value);
                cache.Owner = this;
                configEntries.Add(cache);
            }
        }

        public void OnCellBorrowed(ConfigEntryCell cell)
        {
        }

        public void SetCell(ConfigEntryCell cell, int index)
        {
            CacheObjectControllerHelper.SetCell(cell, index, this.configEntries, null);
        }

        // Panel save data

        public override string GetSaveDataFromConfigManager()
        {
            return ConfigManager.OptionsPanelData.Value;
        }

        public override void DoSaveToConfigElement()
        {
            ConfigManager.OptionsPanelData.Value = this.ToSaveData();
        }

        protected internal override void DoSetDefaultPosAndAnchors()
        {
            mainPanelRect.localPosition = Vector2.zero;
            mainPanelRect.pivot = new Vector2(0.5f, 1f);
            mainPanelRect.anchorMin = new Vector2(0.5f, 0.1f);
            mainPanelRect.anchorMax = new Vector2(0.5f, 0.85f);
            mainPanelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 550f);
        }

        // UI Construction

        public override void ConstructPanelContent()
        {
            // Save button

            var saveBtn = UIFactory.CreateButton(this.content, "Save", "Save Options", new Color(0.2f, 0.3f, 0.2f));
            UIFactory.SetLayoutElement(saveBtn.Component.gameObject, flexibleWidth: 9999, minHeight: 30, flexibleHeight: 0);
            saveBtn.OnClick += ConfigManager.Handler.SaveConfig;

            // Config entries

            var scrollPool = UIFactory.CreateScrollPool<ConfigEntryCell>(this.content, "ConfigEntries", out GameObject scrollObj,
                out GameObject scrollContent);

            scrollPool.Initialize(this);

            foreach (var config in configEntries)
                config.UpdateValueFromSource();
        }
    }
}
