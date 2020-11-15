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
    public abstract class CacheObjectBase
    {
        public InteractiveValue IValue;

        public virtual bool CanWrite => false;
        public virtual bool HasParameters => false;
        public virtual bool IsMember => false;
        public virtual bool HasEvaluated => true;

        public abstract Type FallbackType { get; }

        public abstract void CreateIValue(object value, Type fallbackType);

        public virtual void Enable()
        {
            if (!m_constructedUI)
            {
                ConstructUI();
                UpdateValue();
            }

            m_mainContent.SetActive(true);
            m_mainContent.transform.SetAsLastSibling();
        }

        public virtual void Disable()
        {
            m_mainContent?.SetActive(false);
        }

        public void Destroy()
        {
            GameObject.Destroy(this.m_mainContent);
        }

        public virtual void UpdateValue()
        {
            var value = IValue.Value;

            // see if current value has changed types fundamentally
            var type = value == null 
                ? FallbackType
                : ReflectionHelpers.GetActualType(value);
            var ivalueType = InteractiveValue.GetIValueForType(type);

            if (ivalueType != IValue.IValueType)
            {
                IValue.OnDestroy();
                CreateIValue(value, FallbackType);
                m_subContent.SetActive(false);
            }

            IValue.OnValueUpdated();

            IValue.RefreshElementsAfterUpdate();
        }

        public virtual void SetValue() => throw new NotImplementedException();

        #region UI CONSTRUCTION

        internal bool m_constructedUI;
        internal GameObject m_parentContent;
        internal RectTransform m_mainRect;
        internal GameObject m_mainContent;
        internal GameObject m_subContent;

        // Make base UI holder for CacheObject, this doesnt actually display anything.
        internal virtual void ConstructUI()
        {
            m_constructedUI = true;

            m_mainContent = UIFactory.CreateVerticalGroup(m_parentContent, new Color(0.1f, 0.1f, 0.1f));
            m_mainContent.name = "CacheObjectBase.MainContent";
            m_mainRect = m_mainContent.GetComponent<RectTransform>();
            m_mainRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 25);
            var mainGroup = m_mainContent.GetComponent<VerticalLayoutGroup>();
            mainGroup.childForceExpandWidth = true;
            mainGroup.childControlWidth = true;
            mainGroup.childForceExpandHeight = true;
            mainGroup.childControlHeight = true;
            var mainLayout = m_mainContent.AddComponent<LayoutElement>();
            mainLayout.minHeight = 25;
            mainLayout.flexibleHeight = 9999;
            mainLayout.minWidth = 200;
            mainLayout.flexibleWidth = 5000;

            // subcontent

            m_subContent = UIFactory.CreateVerticalGroup(m_mainContent, new Color(0.085f, 0.085f, 0.085f));
            m_subContent.name = "CacheObjectBase.SubContent";
            var subGroup = m_subContent.GetComponent<VerticalLayoutGroup>();
            subGroup.childForceExpandWidth = true;
            subGroup.childForceExpandHeight = false;
            var subLayout = m_subContent.AddComponent<LayoutElement>();
            subLayout.minHeight = 30;
            subLayout.flexibleHeight = 9999;
            subLayout.minWidth = 125;
            subLayout.flexibleWidth = 9000;

            m_subContent.SetActive(false);

            IValue.m_subContentParent = m_subContent;
        }

        #endregion

    }
}
