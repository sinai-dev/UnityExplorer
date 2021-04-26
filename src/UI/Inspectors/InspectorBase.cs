using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExplorer.UI.ObjectPool;
using UnityExplorer.UI.Panels;

namespace UnityExplorer.UI.Inspectors
{
    public abstract class InspectorBase : IPooledObject
    {
        public InspectorTab Tab { get; internal set; }
        public bool IsActive { get; internal set; }

        public abstract GameObject UIRoot { get; }

        private static readonly Color _enabledTabColor = new Color(0.2f, 0.4f, 0.2f);
        private static readonly Color _disabledTabColor = new Color(0.25f, 0.25f, 0.25f);

        public float DefaultHeight => -1f;
        public abstract GameObject CreateContent(GameObject content);

        public abstract void Update();

        public virtual void OnBorrowedFromPool(object target)
        {
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
            RuntimeProvider.Instance.SetColorBlock(Tab.TabButton.Button, _enabledTabColor, _enabledTabColor * 1.2f);
            UIRoot.SetActive(true);
            IsActive = true;
        }

        public virtual void OnSetInactive()
        {
            RuntimeProvider.Instance.SetColorBlock(Tab.TabButton.Button, _disabledTabColor, _disabledTabColor * 1.2f);
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
