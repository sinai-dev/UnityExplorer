//using System;
//using UnityEngine;
//using UnityEngine.UI;
//using UnityExplorer.UI.Panels;

//namespace UnityExplorer.Inspectors
//{
//    public abstract class InspectorBase
//    {
//        public object Target;
//        public bool IsActive { get; private set; }

//        public abstract string TabLabel { get; }

//        internal bool m_pendingDestroy;

//        public InspectorBase(object target)
//        {
//            Target = target;

//            if (Target.IsNullOrDestroyed(false))
//            {
//                Destroy();
//                return;
//            }

//            AddInspectorTab(this);
//        }

//        public virtual void SetActive()
//        {
//            this.IsActive = true;
//            Content?.SetActive(true);
//        }

//        public virtual void SetInactive()
//        {
//            this.IsActive = false;
//            Content?.SetActive(false);
//        }

//        public virtual void Update()
//        {
//            if (Target.IsNullOrDestroyed(false))
//            {
//                Destroy();
//                return;
//            }

//            m_tabText.text = TabLabel;
//        }

//        public virtual void Destroy()
//        {
//            m_pendingDestroy = true;

//            GameObject tabGroup = m_tabButton?.transform.parent.gameObject;

//            if (tabGroup)
//                GameObject.Destroy(tabGroup);

//            int thisIndex = -1;
//            if (InspectorManager.ActiveInspectors.Contains(this))
//            {
//                thisIndex = InspectorManager.ActiveInspectors.IndexOf(this);
//                InspectorManager.ActiveInspectors.Remove(this);
//            }

//            if (ReferenceEquals(InspectorManager.m_activeInspector, this))
//            {
//                InspectorManager.UnsetInspectorTab();

//                if (InspectorManager.ActiveInspectors.Count > 0)
//                {
//                    var prevTab = InspectorManager.ActiveInspectors[thisIndex > 0 ? thisIndex - 1 : 0];
//                    InspectorManager.SetInspectorTab(prevTab);
//                }
//            }
//        }

//        #region UI

//        public abstract GameObject Content { get; set; }
//        public Button m_tabButton;
//        public Text m_tabText;

//        public void AddInspectorTab(InspectorBase parent)
//        {
//            var tabContent = InspectorPanel.Instance.NavbarHolder;

//            var tabGroupObj = UIFactory.CreateHorizontalGroup(tabContent, "TabObject", true, true, true, true);
//            UIFactory.SetLayoutElement(tabGroupObj, minWidth: 185, flexibleWidth: 0);
//            tabGroupObj.AddComponent<Mask>();

//            m_tabButton = UIFactory.CreateButton(tabGroupObj,
//                "TabButton",
//                "",
//                () => { InspectorManager.SetInspectorTab(parent); });

//            UIFactory.SetLayoutElement(m_tabButton.gameObject, minWidth: 165, flexibleWidth: 0);

//            m_tabText = m_tabButton.GetComponentInChildren<Text>();
//            m_tabText.horizontalOverflow = HorizontalWrapMode.Overflow;
//            m_tabText.alignment = TextAnchor.MiddleLeft;

//            var closeBtn = UIFactory.CreateButton(tabGroupObj,
//                "CloseButton",
//                "X",
//                () => { InspectorManager.DestroyInspector(parent); },
//                new Color(0.2f, 0.2f, 0.2f, 1));

//            UIFactory.SetLayoutElement(closeBtn.gameObject, minWidth: 20, flexibleWidth: 0);

//            var closeBtnText = closeBtn.GetComponentInChildren<Text>();
//            closeBtnText.color = new Color(1, 0, 0, 1);
//        }

//        public virtual void OnPanelResized() { }

//        #endregion
//    }
//}
