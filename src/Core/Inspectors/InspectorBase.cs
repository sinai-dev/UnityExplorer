using System;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Unity;
using UnityExplorer.UI;
using UnityExplorer.UI.Main.Home.Inspectors;

namespace UnityExplorer.Core.Inspectors
{
    public abstract class InspectorBase
    {
        public object Target;

        public InspectorBaseUI BaseUI;

        public abstract string TabLabel { get; }

        public bool IsActive { get; private set; }

        internal bool m_pendingDestroy;

        public InspectorBase(object target)
        {
            Target = target;

            if (Target.IsNullOrDestroyed(false))
            {
                Destroy();
                return;
            }

            CreateUIModule();

            BaseUI.AddInspectorTab(this);
        }

        public abstract void CreateUIModule();

        public virtual void SetActive()
        {
            this.IsActive = true;
            BaseUI.Content?.SetActive(true);
        }

        public virtual void SetInactive()
        {
            this.IsActive = false;
            BaseUI.Content?.SetActive(false);
        }

        public virtual void Update()
        {
            if (Target.IsNullOrDestroyed(false))
            {
                Destroy();
                return;
            }

            BaseUI.tabText.text = TabLabel;
        }

        public virtual void Destroy()
        {
            m_pendingDestroy = true;

            GameObject tabGroup = BaseUI.tabButton?.transform.parent.gameObject;

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
    }
}
