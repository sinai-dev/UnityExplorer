using UnityExplorer.CacheObject;
using UnityExplorer.CacheObject.Views;
using UnityExplorer.Config;
using UniverseLib.UI;
using UniverseLib.UI.Widgets.ScrollView;

namespace UnityExplorer.UI.Panels
{
    public class OptionsPanel : UEPanel, ICacheObjectController, ICellPoolDataSource<ConfigEntryCell>
    {
        public override string Name => "Options";
        public override UIManager.Panels PanelType => UIManager.Panels.Options;

        public override int MinWidth => 600;
        public override int MinHeight => 200;
        public override Vector2 DefaultAnchorMin => new(0.5f, 0.1f);
        public override Vector2 DefaultAnchorMax => new(0.5f, 0.85f);

        public override bool ShouldSaveActiveState => false;
        public override bool ShowByDefault => false;

        // Entry holders
        private readonly List<CacheConfigEntry> configEntries = new();

        // ICacheObjectController
        public CacheObjectBase ParentCacheObject => null;
        public object Target => null;
        public Type TargetType => null;
        public bool CanWrite => true;

        // ICellPoolDataSource
        public int ItemCount => configEntries.Count;

        public OptionsPanel(UIBase owner) : base(owner)
        {
            foreach (KeyValuePair<string, IConfigElement> entry in ConfigManager.ConfigElements)
            {
                CacheConfigEntry cache = new(entry.Value)
                {
                    Owner = this
                };
                configEntries.Add(cache);
            }

            foreach (CacheConfigEntry config in configEntries)
                config.UpdateValueFromSource();
        }

        public void OnCellBorrowed(ConfigEntryCell cell)
        {
        }

        public void SetCell(ConfigEntryCell cell, int index)
        {
            CacheObjectControllerHelper.SetCell(cell, index, this.configEntries, null);
        }

        // UI Construction

        public override void SetDefaultSizeAndPosition()
        {
            base.SetDefaultSizeAndPosition();

            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 600f);
        }

        protected override void ConstructPanelContent()
        {
            // Save button

            UniverseLib.UI.Models.ButtonRef saveBtn = UIFactory.CreateButton(this.ContentRoot, "Save", "Save Options", new Color(0.2f, 0.3f, 0.2f));
            UIFactory.SetLayoutElement(saveBtn.Component.gameObject, flexibleWidth: 9999, minHeight: 30, flexibleHeight: 0);
            saveBtn.OnClick += ConfigManager.Handler.SaveConfig;

            // Config entries

            ScrollPool<ConfigEntryCell> scrollPool = UIFactory.CreateScrollPool<ConfigEntryCell>(
                this.ContentRoot, 
                "ConfigEntries", 
                out GameObject scrollObj,
                out GameObject scrollContent);

            scrollPool.Initialize(this);
        }
    }
}
