using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityExplorer.UI;
using UnityExplorer.UI.Shared;
using UnityExplorer.Helpers;
using UnityEngine.UI;

namespace UnityExplorer.Inspectors.Reflection
{
    public class CacheObjectBase
    {
        public InteractiveValue IValue;

        public virtual bool CanWrite => false;
        public virtual bool HasParameters => false;
        public virtual bool IsMember => false;
        public virtual bool HasEvaluated => true;

        // TODO
        public virtual void InitValue(object value, Type valueType)
        {
            if (valueType == null && value == null)
            {
                return;
            }

            // TEMP
            IValue = new InteractiveValue
            {
                OwnerCacheObject = this,
                ValueType = ReflectionHelpers.GetActualType(value) ?? valueType,
            };
            UpdateValue();
        }

        public virtual void Enable()
        {
            if (!m_constructedUI)
            {
                ConstructUI();
                IValue.ConstructUI(m_topContent);
                UpdateValue();
            }

            m_mainContent.SetActive(true);
        }

        public virtual void Disable()
        {
            m_mainContent.SetActive(false);
        }

        public virtual void UpdateValue()
        {
            IValue.UpdateValue();
        }

        public virtual void SetValue() => throw new NotImplementedException();

        #region UI CONSTRUCTION

        internal bool m_constructedUI;
        internal GameObject m_parentContent;
        internal GameObject m_mainContent;
        internal GameObject m_topContent;
        //internal GameObject m_subContent;

        // Make base UI holder for CacheObject, this doesnt actually display anything.
        internal virtual void ConstructUI()
        {
            m_constructedUI = true;

            m_mainContent = UIFactory.CreateVerticalGroup(m_parentContent, new Color(0.1f, 0.1f, 0.1f));
            var rowGroup = m_mainContent.GetComponent<VerticalLayoutGroup>();
            rowGroup.childForceExpandWidth = true;
            rowGroup.childControlWidth = true;
            rowGroup.childForceExpandHeight = false;
            rowGroup.childControlHeight = true;
            var rowLayout = m_mainContent.AddComponent<LayoutElement>();
            rowLayout.minHeight = 25;
            rowLayout.flexibleHeight = 0;
            rowLayout.minWidth = 200;
            rowLayout.flexibleWidth = 5000;

            m_topContent = UIFactory.CreateHorizontalGroup(m_mainContent, new Color(1, 1, 1, 0));
            var topLayout = m_topContent.AddComponent<LayoutElement>();
            topLayout.minHeight = 25;
            topLayout.flexibleHeight = 0;
            var topGroup = m_topContent.GetComponent<HorizontalLayoutGroup>();
            topGroup.childForceExpandHeight = false;
            topGroup.childForceExpandWidth = true;
            topGroup.childControlHeight = true;
            topGroup.childControlWidth = true;
            topGroup.spacing = 4;

            //m_subContent = UIFactory.CreateHorizontalGroup(m_parentContent, new Color(1, 1, 1, 0));
            //var subGroup = m_subContent.GetComponent<HorizontalLayoutGroup>();
            //subGroup.childForceExpandWidth = true;
            //subGroup.childControlWidth = true;
            //var subLayout = m_subContent.AddComponent<LayoutElement>();
            //subLayout.minHeight = 25;
            //subLayout.flexibleHeight = 500;
            //subLayout.minWidth = 125;
            //subLayout.flexibleWidth = 9000;

            //m_subContent.SetActive(false);
        }

        #endregion

    }
}
