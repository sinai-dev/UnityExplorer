using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.CacheObject;
using UnityExplorer.UI;
using UniverseLib.UI;
using UniverseLib.UI.Models;

namespace UnityExplorer.CacheObject.IValues
{
    public abstract class InteractiveValue : IPooledObject
    {
        public static Type GetIValueTypeForState(ValueState state)
        {
            switch (state)
            {
                case ValueState.String:
                    return typeof(InteractiveString);
                case ValueState.Enum:
                    return typeof(InteractiveEnum);
                case ValueState.Collection:
                    return typeof(InteractiveList);
                case ValueState.Dictionary:
                    return typeof(InteractiveDictionary);
                case ValueState.ValueStruct:
                    return typeof(InteractiveValueStruct);
                case ValueState.Color:
                    return typeof(InteractiveColor);
                default: return null;
            }
        }

        public GameObject UIRoot { get; set; }
        public float DefaultHeight => -1f;

        public virtual bool CanWrite => this.CurrentOwner.CanWrite;

        public CacheObjectBase CurrentOwner => m_owner;
        private CacheObjectBase m_owner;

        public bool PendingValueWanted;

        public virtual void OnBorrowed(CacheObjectBase owner)
        {
            if (this.m_owner != null)
            {
                ExplorerCore.LogWarning("Setting an IValue's owner but there is already one set. Maybe it wasn't cleaned up?");
                ReleaseFromOwner();
            }

            this.m_owner = owner;
        }

        public virtual void ReleaseFromOwner()
        {
            if (this.m_owner == null)
                return;

            this.m_owner = null;
        }

        public abstract void SetValue(object value);

        public virtual void SetLayout() { }

        public abstract GameObject CreateContent(GameObject parent);
    }
}
