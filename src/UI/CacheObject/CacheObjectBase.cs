using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityExplorer.UI;
using UnityExplorer.Core.Unity;
using UnityEngine.UI;
using UnityExplorer.Core;
using UnityExplorer.UI.InteractiveValues;

namespace UnityExplorer.UI.CacheObject
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
            if (m_mainContent)
                m_mainContent.SetActive(false);
        }

        public void Destroy()
        {
            if (this.m_mainContent)
                GameObject.Destroy(this.m_mainContent);
        }

        public virtual void UpdateValue()
        {
            var value = IValue.Value;

            // if the type has changed fundamentally, make a new interactivevalue for it
            var type = value == null 
                ? FallbackType
                : ReflectionUtility.GetType(value);

            var ivalueType = InteractiveValue.GetIValueForType(type);

            if (ivalueType != IValue.GetType())
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

            m_mainContent = UIFactory.CreateVerticalGroup(m_parentContent, "CacheObjectBase.MainContent", true, true, true, true, 0, default,
                new Color(0.1f, 0.1f, 0.1f));
            m_mainRect = m_mainContent.GetComponent<RectTransform>();
            m_mainRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 25);

            UIFactory.SetLayoutElement(m_mainContent, minHeight: 25, flexibleHeight: 9999, minWidth: 200, flexibleWidth: 5000);

            // subcontent

            m_subContent = UIFactory.CreateVerticalGroup(m_mainContent, "CacheObjectBase.SubContent", true, false, true, true, 0, default, 
                new Color(0.085f, 0.085f, 0.085f));
            UIFactory.SetLayoutElement(m_subContent, minHeight: 30, flexibleHeight: 9999, minWidth: 125, flexibleWidth: 9000);

            m_subContent.SetActive(false);

            IValue.m_subContentParent = m_subContent;
        }

        #endregion

    }
}
