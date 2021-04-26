//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using UnityEngine;
//using UnityExplorer.UI;
//using UnityEngine.UI;
//using UnityExplorer.Core;
//using UnityExplorer.UI.InteractiveValues;
//using UnityExplorer.UI.Widgets;

//namespace UnityExplorer.UI.CacheObject
//{
//    public abstract class CacheObjectBase
//    {
//        public InteractiveValue IValue;

//        public virtual bool CanWrite => false;
//        public virtual bool HasParameters => false;
//        public virtual bool IsMember => false;
//        public virtual bool HasEvaluated => true;

//        public abstract Type FallbackType { get; }

//        public abstract void CreateIValue(object value, Type fallbackType);

//        public virtual void Enable()
//        {
//            if (!m_constructedUI)
//            {
//                ConstructUI();
//                UpdateValue();
//            }
//        }

//        public virtual void Disable()
//        {
//            if (UIRoot)
//                UIRoot.SetActive(false);
//        }

//        public void Destroy()
//        {
//            if (this.UIRoot)
//                GameObject.Destroy(this.UIRoot);
//        }

//        public virtual void UpdateValue()
//        {
//            var value = IValue.Value;

//            // if the type has changed fundamentally, make a new interactivevalue for it
//            var type = value == null 
//                ? FallbackType
//                : ReflectionUtility.GetActualType(value);

//            var ivalueType = InteractiveValue.GetIValueForType(type);

//            if (ivalueType != IValue.GetType())
//            {
//                IValue.OnDestroy();
//                CreateIValue(value, FallbackType);
//                SubContentGroup.SetActive(false);
//            }

//            IValue.OnValueUpdated();

//            IValue.RefreshElementsAfterUpdate();
//        }

//        public virtual void SetValue() => throw new NotImplementedException();

//        #region UI CONSTRUCTION

//        internal bool m_constructedUI;
//        internal GameObject m_parentContent;
//        internal RectTransform m_mainRect;
//        internal GameObject UIRoot;

//        internal GameObject SubContentGroup;
//        internal bool constructedSubcontent;

//        // Make base UI holder for CacheObject, this doesnt actually display anything.
//        internal virtual void ConstructUI()
//        {
//            m_constructedUI = true;

//            //UIRoot = UIFactory.CreateVerticalGroup(m_parentContent, $"{this.GetType().Name}.MainContent", true, true, true, true, 2, 
//            //    new Vector4(0, 5, 0, 0), new Color(0.1f, 0.1f, 0.1f), TextAnchor.UpperLeft);

//            UIRoot = UIFactory.CreateUIObject($"{this.GetType().Name}.MainContent", m_parentContent);
//            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(UIRoot, true, true, true, true, 2, 0, 5, 0, 0, TextAnchor.UpperLeft);
//            m_mainRect = UIRoot.GetComponent<RectTransform>();
//            m_mainRect.pivot = new Vector2(0, 1);
//            m_mainRect.anchorMin = Vector2.zero;
//            m_mainRect.anchorMax = Vector2.one;
//            UIFactory.SetLayoutElement(UIRoot, minHeight: 30, flexibleHeight: 9999, minWidth: 200, flexibleWidth: 5000);

//            SubContentGroup = new GameObject("SubContent");
//            SubContentGroup.transform.parent = UIRoot.transform;
//            UIFactory.SetLayoutElement(SubContentGroup, minHeight: 30, flexibleHeight: 9999, minWidth: 125, flexibleWidth: 9000);
//            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(SubContentGroup, true, false, true, true);
//            SubContentGroup.SetActive(false);

//            IValue.m_subContentParent = SubContentGroup;
//        }

//        public virtual void CheckSubcontentCreation()
//        {
//            if (!constructedSubcontent)
//            {
//                SubContentGroup.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f);
//                constructedSubcontent = true;
//            }
//        }

//        #endregion

//    }
//}
