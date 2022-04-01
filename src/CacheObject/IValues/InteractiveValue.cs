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
using UniverseLib.UI.ObjectPool;

namespace UnityExplorer.CacheObject.IValues
{
    public abstract class InteractiveValue : IPooledObject
    {
        public static Type GetIValueTypeForState(ValueState state)
        {
            return state switch
            {
                ValueState.Exception or ValueState.String => typeof(InteractiveString),
                ValueState.Enum => typeof(InteractiveEnum),
                ValueState.Collection => typeof(InteractiveList),
                ValueState.Dictionary => typeof(InteractiveDictionary),
                ValueState.ValueStruct => typeof(InteractiveValueStruct),
                ValueState.Color => typeof(InteractiveColor),
                _ => null,
            };
        }

        public GameObject UIRoot { get; set; }
        public float DefaultHeight => -1f;

        public virtual bool CanWrite => this.CurrentOwner.CanWrite;

        public CacheObjectBase CurrentOwner => owner;
        private CacheObjectBase owner;

        public bool PendingValueWanted;

        public virtual void OnBorrowed(CacheObjectBase owner)
        {
            if (this.owner != null)
            {
                ExplorerCore.LogWarning("Setting an IValue's owner but there is already one set. Maybe it wasn't cleaned up?");
                ReleaseFromOwner();
            }

            this.owner = owner;
        }

        public virtual void ReleaseFromOwner()
        {
            if (this.owner == null)
                return;

            this.owner = null;
        }

        public abstract void SetValue(object value);

        public virtual void SetLayout() { }

        public abstract GameObject CreateContent(GameObject parent);
    }
}
