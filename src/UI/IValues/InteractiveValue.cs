using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.CacheObject;
using UnityExplorer.UI.ObjectPool;

namespace UnityExplorer.UI.IValues
{
    public class InteractiveValue : IPooledObject
    {
        public GameObject UIRoot { get; set; }

        public float DefaultHeight => -1f;

        public virtual bool CanWrite => this.CurrentOwner.CanWrite;

        public CacheObjectBase CurrentOwner => m_owner;
        private CacheObjectBase m_owner;

        //public object EditedValue { get; private set; }

        public virtual void SetLayout() { }

        public static Type GetIValueTypeForState(ValueState state)
        {
            switch (state)
            {
                //case ValueState.String:
                //    return null;
                //case ValueState.Enum:
                //    return null;
                case ValueState.Collection:
                    return typeof(InteractiveList);
                case ValueState.Dictionary:
                    return typeof(InteractiveDictionary);
                //case ValueState.ValueStruct:
                //    return null;
                //case ValueState.Color:
                //    return null;
                default: return typeof(InteractiveValue);
            }
        }

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

        public virtual void SetValue(object value) { }

        //public virtual void SetValue(object value)
        //{
        //    this.EditedValue = value;
        //}

        public virtual GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateUIObject(this.GetType().Name, parent);
            UIRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(UIRoot, true, true, true, true, 3, childAlignment: TextAnchor.MiddleLeft);

            UIFactory.CreateLabel(UIRoot, "Label", "this is an ivalue", TextAnchor.MiddleLeft);
            UIFactory.CreateInputField(UIRoot, "InputFIeld", "...", out var input);
            UIFactory.SetLayoutElement(input.gameObject, minHeight: 25, flexibleHeight: 500);
            input.lineType = InputField.LineType.MultiLineNewline;
            input.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return UIRoot;
        }
    }
}
