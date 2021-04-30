using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Inspectors.CacheObject;
using UnityExplorer.UI.ObjectPool;

namespace UnityExplorer.UI.Inspectors.IValues
{
    public class InteractiveValue : IPooledObject
    {
        public GameObject UIRoot => uiRoot;
        private GameObject uiRoot;

        public float DefaultHeight => -1f;

        public CacheObjectBase CurrentOwner { get; }
        private CacheObjectBase m_owner;

        public object EditedValue { get; private set; }

        public virtual void SetOwner(CacheObjectBase owner)
        {
            if (this.m_owner != null)
            {
                ExplorerCore.LogWarning("Setting an IValue's owner but there is already one set. Maybe it wasn't cleaned up?");
                OnOwnerReleased();
            }

            this.m_owner = owner;
            // ...
        }

        public virtual void SetValue(object value)
        {
            this.EditedValue = value;
        }

        public virtual void OnOwnerReleased()
        {
            if (this.m_owner == null)
                return;

            // ...
            this.m_owner = null;
        }

        public GameObject CreateContent(GameObject parent)
        {
            uiRoot = UIFactory.CreateUIObject(this.GetType().Name, parent);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(uiRoot, true, true, true, true, 3, childAlignment: TextAnchor.MiddleLeft);

            UIFactory.CreateLabel(uiRoot, "Label", "this is an ivalue", TextAnchor.MiddleLeft);

            return uiRoot;
        }
    }
}
