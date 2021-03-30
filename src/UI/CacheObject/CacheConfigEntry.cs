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
            IValue.m_mainContentParent = m_mainGroup;
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

        internal GameObject m_mainGroup;
        //internal GameObject m_leftGroup;
        //internal GameObject m_rightGroup;
        //internal GameObject m_secondRow;

        internal override void ConstructUI()
        {
            base.ConstructUI();

            m_mainGroup = UIFactory.CreateVerticalGroup(m_mainContent, "ConfigHolder", true, false, true, true, 5, new Vector4(2, 2, 2, 2));

            var horiGroup = UIFactory.CreateHorizontalGroup(m_mainGroup, "ConfigEntryHolder", false, false, true, true, childAlignment: TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(horiGroup, minHeight: 30, flexibleHeight: 0);

            //// left group

            //m_leftGroup = UIFactory.CreateHorizontalGroup(horiGroup, "ConfigTitleGroup", false, false, true, true, 4, default, new Color(1, 1, 1, 0));
            //UIFactory.SetLayoutElement(m_leftGroup, minHeight: 25, flexibleHeight: 0, minWidth: 200, flexibleWidth: 0);

            // config entry label

            var configLabel = UIFactory.CreateLabel(horiGroup, "ConfigLabel", this.RefConfig.Name, TextAnchor.MiddleLeft);
            var leftRect = configLabel.GetComponent<RectTransform>();
            leftRect.anchorMin = Vector2.zero;
            leftRect.anchorMax = Vector2.one;
            leftRect.offsetMin = Vector2.zero;
            leftRect.offsetMax = Vector2.zero;
            leftRect.sizeDelta = Vector2.zero;
            UIFactory.SetLayoutElement(configLabel.gameObject, minWidth: 250, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);

            // Default button

            var defaultButton = UIFactory.CreateButton(horiGroup,
                "RevertDefaultButton",
                "Default",
                () => { RefConfig.RevertToDefaultValue(); },
                new Color(0.3f, 0.3f, 0.3f));
            UIFactory.SetLayoutElement(defaultButton.gameObject, minWidth: 80, minHeight: 22, flexibleWidth: 0);

            //// right group

            //m_rightGroup = UIFactory.CreateVerticalGroup(horiGroup, "ConfigValueGroup", false, false, true, true, 4, default, new Color(1, 1, 1, 0));
            //UIFactory.SetLayoutElement(m_rightGroup, minHeight: 25, minWidth: 150, flexibleHeight: 0, flexibleWidth: 5000);

            // Description label

            var desc = UIFactory.CreateLabel(m_mainGroup, "Description", $"<i>{RefConfig.Description}</i>", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(desc.gameObject, minWidth: 250, minHeight: 20, flexibleWidth: 9999, flexibleHeight: 0);

            //// Second row (IValue)

            //m_secondRow = UIFactory.CreateHorizontalGroup(m_mainGroup, "DescriptionRow", false, false, true, true, 4, new Color(0.08f, 0.08f, 0.08f));

            // IValue

            if (IValue != null)
            {
                IValue.m_mainContentParent = m_mainGroup;
                IValue.m_subContentParent = this.m_subContent;
            }

            // makes the subcontent look nicer
            m_subContent.transform.SetParent(m_mainGroup.transform, false);
        }
    }
}
