using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.UI.CacheObject;
using UnityExplorer.UI.Utility;

namespace UnityExplorer.UI.Main.Options
{
    public class OptionsPage : BaseMenuPage
    {
        public override string Name => "Options";

        internal static readonly List<CacheConfigEntry> _cachedConfigEntries = new List<CacheConfigEntry>();

        public override bool Init()
        {
            ConstructUI();

            return true;
        }

        public override void Update()
        {
            // Not needed
        }

        #region UI CONSTRUCTION

        internal GameObject m_contentObj;

        internal void ConstructUI()
        {
            GameObject parent = MainMenu.Instance.PageViewport;

            Content = UIFactory.CreateVerticalGroup(parent, "OptionsPage", true, true, true, true, 5, new Vector4(4,4,4,4),
                new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(Content, minHeight: 340, flexibleHeight: 9999);

            // ~~~~~ Title ~~~~~

            var titleLabel = UIFactory.CreateLabel(Content, "Title", "Options", TextAnchor.UpperLeft, default, true, 25);
            UIFactory.SetLayoutElement(titleLabel.gameObject, minHeight: 30, flexibleHeight: 0);

            // Save button

            var btn = UIFactory.CreateButton(Content, 
                "SaveButton", 
                "Save Config File", 
                () => { ConfigManager.Handler.SaveConfig(); },
                new Color(0.25f, 0.6f, 0.25f));
            UIFactory.SetLayoutElement(btn.gameObject, flexibleWidth: 9999, minHeight: 30, flexibleHeight: 0);

            // ~~~~~ Actual options ~~~~~

            UIFactory.CreateScrollView(Content, "ConfigList", out m_contentObj, out _, new Color(0.05f, 0.05f, 0.05f));
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(m_contentObj, forceHeight: true, spacing: 3, padLeft: 3, padRight: 3);

            _cachedConfigEntries.AddRange(ConfigManager.ConfigElements.Values
                .Where(it => !it.IsInternal)
                .Select(it => new CacheConfigEntry(it, m_contentObj)));

            foreach (var entry in _cachedConfigEntries)
                entry.Enable();
        }


        #endregion
    }
}
