using System;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Helpers;
using UnityExplorer.UI;

namespace UnityExplorer.Inspectors
{
    public abstract class InspectorBase
    {
        public object Target;

        public abstract string TabLabel { get; }

        public bool IsActive { get; private set; }
        public abstract GameObject Content { get; set; }
        public Button tabButton;
        public Text tabText;

        internal bool m_pendingDestroy;

        public InspectorBase(object target)
        {
            Target = target;

            if (Target.IsNullOrDestroyed(false))
            {
                Destroy();
                return;
            }

            AddInspectorTab();
        }

        public virtual void SetActive()
        {
            this.IsActive = true;
            Content?.SetActive(true);
        }

        public virtual void SetInactive()
        {
            this.IsActive = false;
            Content?.SetActive(false);
        }

        public virtual void Update()
        {
            if (Target.IsNullOrDestroyed(false))
            {
                Destroy();
                return;
            }

            tabText.text = TabLabel;
        }

        public virtual void Destroy()
        {
            m_pendingDestroy = true;

            GameObject tabGroup = tabButton?.transform.parent.gameObject;

            if (tabGroup)
            {
                GameObject.Destroy(tabGroup);
            }

            int thisIndex = -1;
            if (InspectorManager.Instance.m_currentInspectors.Contains(this))
            {
                thisIndex = InspectorManager.Instance.m_currentInspectors.IndexOf(this);
                InspectorManager.Instance.m_currentInspectors.Remove(this);
            }

            if (ReferenceEquals(InspectorManager.Instance.m_activeInspector, this))
            {
                InspectorManager.Instance.UnsetInspectorTab();

                if (InspectorManager.Instance.m_currentInspectors.Count > 0)
                {
                    var prevTab = InspectorManager.Instance.m_currentInspectors[thisIndex > 0 ? thisIndex - 1 : 0];
                    InspectorManager.Instance.SetInspectorTab(prevTab);
                }
            }
        }

        

        #region UI CONSTRUCTION

        public void AddInspectorTab()
        {
            var tabContent = InspectorManager.Instance.m_tabBarContent;

            var tabGroupObj = UIFactory.CreateHorizontalGroup(tabContent);
            var tabGroup = tabGroupObj.GetComponent<HorizontalLayoutGroup>();
            tabGroup.childForceExpandWidth = true;
            tabGroup.childControlWidth = true;
            var tabLayout = tabGroupObj.AddComponent<LayoutElement>();
            tabLayout.minWidth = 185;
            tabLayout.flexibleWidth = 0;
            tabGroupObj.AddComponent<Mask>();

            var targetButtonObj = UIFactory.CreateButton(tabGroupObj);
            targetButtonObj.AddComponent<Mask>();
            var targetButtonLayout = targetButtonObj.AddComponent<LayoutElement>();
            targetButtonLayout.minWidth = 165;
            targetButtonLayout.flexibleWidth = 0;

            tabText = targetButtonObj.GetComponentInChildren<Text>();
            tabText.horizontalOverflow = HorizontalWrapMode.Overflow;
            tabText.alignment = TextAnchor.MiddleLeft;

            tabButton = targetButtonObj.GetComponent<Button>();

            tabButton.onClick.AddListener(() => { InspectorManager.Instance.SetInspectorTab(this); });

            var closeBtnObj = UIFactory.CreateButton(tabGroupObj);
            var closeBtnLayout = closeBtnObj.AddComponent<LayoutElement>();
            closeBtnLayout.minWidth = 20;
            closeBtnLayout.flexibleWidth = 0;
            var closeBtnText = closeBtnObj.GetComponentInChildren<Text>();
            closeBtnText.text = "X";
            closeBtnText.color = new Color(1, 0, 0, 1);

            var closeBtn = closeBtnObj.GetComponent<Button>();

            closeBtn.onClick.AddListener(Destroy);

            var closeColors = closeBtn.colors;
            closeColors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1);
            closeBtn.colors = closeColors;
        }

#endregion
    }
}
