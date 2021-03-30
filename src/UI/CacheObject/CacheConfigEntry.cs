using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.UI.InteractiveValues;

namespace UnityExplorer.UI.CacheObject
{
    public class CacheConfigEntry : CacheObjectBase
    {
        public IConfigElement RefConfig { get; }

        public override Type FallbackType => RefConfig.ElementType;

        public override bool HasEvaluated => true;
        public override bool HasParameters => false;
        public override bool IsMember => false;
        public override bool CanWrite => true;

        public CacheConfigEntry(IConfigElement config, GameObject parent)
        {
            RefConfig = config;

            m_parentContent = parent;

            config.OnValueChangedNotify += () => { UpdateValue(); };

            CreateIValue(config.BoxedValue, config.ElementType);
        }

        public override void CreateIValue(object value, Type fallbackType)
        {
            IValue = InteractiveValue.Create(value, fallbackType);
            IValue.Owner = this;
            IValue.m_mainContentParent = m_rightGroup;
            IValue.m_subContentParent = this.m_subContent;
        }

        public override void UpdateValue()
        {
            IValue.Value = RefConfig.BoxedValue;

            base.UpdateValue();
        }

        public override void SetValue()
        {
            RefConfig.BoxedValue = IValue.Value;
            ConfigManager.Handler.OnAnyConfigChanged();
        }

        internal GameObject m_leftGroup;
        internal GameObject m_rightGroup;

        internal override void ConstructUI()
        {
            base.ConstructUI();

            var vertGroup = UIFactory.CreateVerticalGroup(m_mainContent, "ConfigHolder", true, false, true, true, 5, new Vector4(2, 2, 2, 2));

            var horiGroup = UIFactory.CreateHorizontalGroup(vertGroup, "ConfigEntryHolder", true, false, true, true);
            UIFactory.SetLayoutElement(horiGroup, minHeight: 30, flexibleHeight: 0);

            // left group

            m_leftGroup = UIFactory.CreateHorizontalGroup(horiGroup, "ConfigTitleGroup", false, false, true, true, 4, default, new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(m_leftGroup, minHeight: 25, flexibleHeight: 0, minWidth: 125, flexibleWidth: 200);

            // config entry label

            var configLabel = UIFactory.CreateLabel(m_leftGroup, "ConfigLabel", this.RefConfig.Name, TextAnchor.MiddleLeft);
            var leftRect = configLabel.GetComponent<RectTransform>();
            leftRect.anchorMin = Vector2.zero;
            leftRect.anchorMax = Vector2.one;
            leftRect.offsetMin = Vector2.zero;
            leftRect.offsetMax = Vector2.zero;
            leftRect.sizeDelta = Vector2.zero;
            UIFactory.SetLayoutElement(configLabel.gameObject, minWidth: 250, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);

            // right group

            m_rightGroup = UIFactory.CreateVerticalGroup(horiGroup, "ConfigValueGroup", false, false, true, true, 2, new Vector4(4,2,0,0),
                new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(m_rightGroup, minHeight: 25, minWidth: 150, flexibleHeight: 0, flexibleWidth: 5000);

            if (IValue != null)
            {
                IValue.m_mainContentParent = m_rightGroup;
                IValue.m_subContentParent = this.m_subContent;
            }

            // Config description label

            UIFactory.CreateLabel(vertGroup, "Description", $"<i>{RefConfig.Description}</i>", TextAnchor.MiddleLeft, Color.grey);

            m_subContent.transform.SetAsLastSibling();
        }
    }
}
