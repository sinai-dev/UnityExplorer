using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.ObjectPool;
using UnityExplorer.UI.Panels;

namespace UnityExplorer.UI.Inspectors
{
    public abstract class InspectorBase : IPooledObject
    {
        public bool IsActive { get; internal set; }
        public object Target { get; set; }

        public InspectorTab Tab { get; internal set; }

        public GameObject UIRoot { get; set; }

        public float DefaultHeight => -1f;
        public abstract GameObject CreateContent(GameObject parent);

        public abstract void Update();

        public virtual void OnBorrowedFromPool(object target)
        {
            this.Target = target;
            Tab = Pool<InspectorTab>.Borrow();
            Tab.UIRoot.transform.SetParent(InspectorPanel.Instance.NavbarHolder.transform, false);

            Tab.TabButton.OnClick += OnTabButtonClicked;
            Tab.CloseButton.OnClick += OnCloseClicked;
        }

        public virtual void OnReturnToPool()
        {
            Pool<InspectorTab>.Return(Tab);

            Tab.TabButton.OnClick -= OnTabButtonClicked;
            Tab.CloseButton.OnClick -= OnCloseClicked;
        }

        public virtual void OnSetActive()
        {
            Tab.SetTabColor(true);
            UIRoot.SetActive(true);
            IsActive = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(UIRoot.GetComponent<RectTransform>());
        }

        public virtual void OnSetInactive()
        {
            Tab.SetTabColor(false);
            UIRoot.SetActive(false);
            IsActive = false;
        }

        private void OnTabButtonClicked()
        {
            InspectorManager.SetInspectorActive(this);
        }

        protected abstract void OnCloseClicked();
    }
}
