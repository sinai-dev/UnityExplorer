using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI.CacheObject.Views;
using UnityExplorer.UI.IValues;
using UnityExplorer.UI.ObjectPool;
using UnityExplorer.UI.Utility;

namespace UnityExplorer.UI.CacheObject
{
    public enum ValueState
    {
        NotEvaluated,
        Exception,
        NullValue,
        Boolean,
        Number,
        String,
        Enum,
        Collection,
        Dictionary,
        ValueStruct,
        Color,
        Unsupported
    }

    public abstract class CacheObjectBase
    {
        public ICacheObjectController Owner { get; set; }

        public CacheObjectCell CellView { get; internal set; }

        public object Value { get; protected set; }
        public Type FallbackType { get; protected set; }

        public InteractiveValue IValue { get; private set; }
        public Type CurrentIValueType { get; private set; }
        public bool SubContentShowWanted { get; private set; }

        public string NameLabelText { get; protected set; }
        public string ValueLabelText { get; protected set; }

        public abstract bool ShouldAutoEvaluate { get; }
        public abstract bool HasArguments { get; }
        public abstract bool CanWrite { get; }
        public bool HadException { get; protected set; }
        public Exception LastException { get; protected set; }

        public virtual void SetFallbackType(Type fallbackType)
        {
            this.FallbackType = fallbackType;
            GetValueLabel();
        }

        // internals

        private static readonly Dictionary<string, MethodInfo> numberParseMethods = new Dictionary<string, MethodInfo>();

        public ValueState State = ValueState.NotEvaluated;

        protected const string NOT_YET_EVAL = "<color=grey>Not yet evaluated</color>";


        public virtual void ReleasePooledObjects()
        {
            if (this.IValue != null)
                ReleaseIValue();

            if (this.CellView != null)
                UnlinkFromView();
        }

        public virtual void SetView(CacheObjectCell cellView)
        {
            this.CellView = cellView;
            cellView.Occupant = this;
        }

        public virtual void UnlinkFromView()
        {
            if (this.CellView == null)
                return;

            this.CellView.Occupant = null;
            this.CellView = null;

            if (this.IValue != null)
                this.IValue.UIRoot.transform.SetParent(InactiveIValueHolder.transform, false);
        }

        // Updating and applying values

        public abstract void SetUserValue(object value);

        public virtual void SetValueFromSource(object value)
        {
            this.Value = value;

            if (!Value.IsNullOrDestroyed())
                Value = Value.TryCast();

            var prevState = State;
            ProcessOnEvaluate();

            if (State != prevState)
            {
                if (this.IValue != null)
                {
                    // State has changed, need to return IValue
                    ReleaseIValue();
                    SubContentShowWanted = false;
                }
            }

            if (this.IValue != null)
                this.IValue.SetValue(Value);
        }

        /// <summary>
        /// Process the CacheMember state when the value has been evaluated (or re-evaluated)
        /// </summary>
        protected virtual void ProcessOnEvaluate()
        {
            if (HadException)
                State = ValueState.Exception;
            else if (Value.IsNullOrDestroyed())
                State = ValueState.NullValue;
            else
            {
                var type = Value.GetActualType();

                if (type == typeof(bool))
                    State = ValueState.Boolean;
                else if (type.IsPrimitive || type == typeof(decimal))
                    State = ValueState.Number;
                else if (ReflectionProvider.Instance.IsString(Value))
                    State = ValueState.String;
                else if (type.IsEnum)
                    State = ValueState.Enum;

                // todo Color and ValueStruct

                else if (type.IsDictionary())
                    State = ValueState.Dictionary;
                else if (type.IsEnumerable())
                    State = ValueState.Collection;
                else
                    State = ValueState.Unsupported;
            }

            // Set label text
            GetValueLabel();
        }

        protected void GetValueLabel()
        {
            string label;
            switch (State)
            {
                case ValueState.NotEvaluated:
                    label = $"<i>{NOT_YET_EVAL} ({SignatureHighlighter.Parse(FallbackType, true)})</i>"; break;
                case ValueState.Exception:
                    label = $"<i><color=red>{ReflectionUtility.ReflectionExToString(LastException)}</color></i>"; break;
                case ValueState.Boolean:
                case ValueState.Number:
                    label = null; break;
                case ValueState.String:
                    string s = ReflectionProvider.Instance.UnboxString(Value);
                    if (s.Length > 200)
                        s = $"{s.Substring(0, 200)}...";
                    label = $"\"{s}\""; break;
                case ValueState.NullValue:
                    label = $"<i>{ToStringUtility.ToStringWithType(Value, FallbackType, true)}</i>"; break;
                case ValueState.Enum:
                case ValueState.Collection:
                case ValueState.Dictionary:
                case ValueState.ValueStruct:
                case ValueState.Color:
                case ValueState.Unsupported:
                default:
                    label = ToStringUtility.ToStringWithType(Value, FallbackType, true); break;
            }
            this.ValueLabelText = label;
        }

        // Setting cell state from our model

        /// <summary>Return true if SetCell should abort, false if it should continue.</summary>
        protected abstract bool SetCellEvaluateState(CacheObjectCell cell);

        public virtual void SetDataToCell(CacheObjectCell cell)
        {
            cell.NameLabel.text = NameLabelText;
            cell.ValueLabel.gameObject.SetActive(true);

            cell.SubContentHolder.gameObject.SetActive(SubContentShowWanted);
            if (IValue != null)
            { 
                IValue.UIRoot.transform.SetParent(cell.SubContentHolder.transform, false);
                IValue.SetLayout();
            }

            if (SetCellEvaluateState(cell))
                return;

            switch (State)
            {
                case ValueState.Exception:
                case ValueState.NullValue:
                    SetValueState(cell, ValueStateArgs.Default);
                    break;
                case ValueState.Boolean:
                    SetValueState(cell, new ValueStateArgs(false, toggleActive:true, applyActive: CanWrite));
                    break;
                case ValueState.Number:
                    SetValueState(cell, new ValueStateArgs(false, typeLabelActive: true, inputActive: true, applyActive: CanWrite));
                    break;
                case ValueState.String:
                    SetValueState(cell, new ValueStateArgs(true, false, SignatureHighlighter.StringOrange, subContentButtonActive: true));
                    break;
                case ValueState.Enum:
                    SetValueState(cell, new ValueStateArgs(true, subContentButtonActive: CanWrite));
                    break;
                case ValueState.Collection:
                case ValueState.Dictionary:
                case ValueState.ValueStruct:
                case ValueState.Color:
                    SetValueState(cell, new ValueStateArgs(true, inspectActive: true, subContentButtonActive: true));
                    break;
                case ValueState.Unsupported:
                    SetValueState(cell, new ValueStateArgs(true, inspectActive: true));
                    break;
            }

            cell.RefreshSubcontentButton();
        }

        protected virtual void SetValueState(CacheObjectCell cell, ValueStateArgs args)
        {
            if (args.valueActive)
            {
                cell.ValueLabel.text = ValueLabelText;
                cell.ValueLabel.supportRichText = args.valueRichText;
                cell.ValueLabel.color = args.valueColor;
            }
            else
                cell.ValueLabel.text = "";

            cell.TypeLabel.gameObject.SetActive(args.typeLabelActive);
            if (args.typeLabelActive)
                cell.TypeLabel.text = SignatureHighlighter.Parse(Value.GetActualType(), false);

            cell.Toggle.gameObject.SetActive(args.toggleActive);
            if (args.toggleActive)
            {
                cell.Toggle.interactable = CanWrite;
                cell.Toggle.isOn = (bool)Value;
                cell.ToggleText.text = Value.ToString();
            }

            cell.InputField.gameObject.SetActive(args.inputActive);
            if (args.inputActive)
            {
                cell.InputField.text = Value.ToString();
                cell.InputField.readOnly = !CanWrite;
            }

            cell.ApplyButton.Button.gameObject.SetActive(args.applyActive);
            cell.InspectButton.Button.gameObject.SetActive(args.inspectActive);
            cell.SubContentButton.Button.gameObject.SetActive(args.subContentButtonActive);
        }

        // IValues

        internal static GameObject InactiveIValueHolder
        {
            get
            {
                if (!inactiveIValueHolder)
                {
                    inactiveIValueHolder = new GameObject("Temp_IValue_Holder");
                    GameObject.DontDestroyOnLoad(inactiveIValueHolder);
                    inactiveIValueHolder.transform.parent = UIManager.PoolHolder.transform;
                    inactiveIValueHolder.SetActive(false);
                }
                return inactiveIValueHolder;
            }
        }
        private static GameObject inactiveIValueHolder;

        public virtual void OnCellSubContentToggle()
        {
            if (this.IValue == null)
            {
                var ivalueType = InteractiveValue.GetIValueTypeForState(State);
                IValue = (InteractiveValue)Pool.Borrow(ivalueType);
                CurrentIValueType = ivalueType;

                IValue.OnBorrowed(this);
                IValue.SetValue(this.Value);
                IValue.UIRoot.transform.SetParent(CellView.SubContentHolder.transform, false);
                CellView.SubContentHolder.SetActive(true);
                SubContentShowWanted = true;

                // update our cell after creating the ivalue (the value may have updated, make sure its consistent)
                this.ProcessOnEvaluate();
                this.SetDataToCell(this.CellView);
            }
            else
            {
                SubContentShowWanted = !SubContentShowWanted;
                CellView.SubContentHolder.SetActive(SubContentShowWanted); 
            }

            CellView.RefreshSubcontentButton();
        }

        public virtual void ReleaseIValue()
        {
            if (IValue == null)
                return;

            IValue.ReleaseFromOwner();
            Pool.Return(CurrentIValueType, IValue);

            IValue = null;
        }

        // CacheObjectCell Apply

        public virtual void OnCellApplyClicked()
        {
            if (CellView == null)
            {
                ExplorerCore.LogWarning("Trying to apply CacheMember but current cell reference is null!");
                return;
            }

            if (State == ValueState.Boolean)
                SetUserValue(this.CellView.Toggle.isOn);
            else
            {
                var type = Value.GetActualType();
                if (!numberParseMethods.ContainsKey(type.AssemblyQualifiedName))
                {
                    var method = type.GetMethod("Parse", new Type[] { typeof(string) });
                    numberParseMethods.Add(type.AssemblyQualifiedName, method);
                }

                var val = numberParseMethods[type.AssemblyQualifiedName]
                    .Invoke(null, new object[] { CellView.InputField.text });
                SetUserValue(val);
            }

            SetDataToCell(this.CellView);
        }

        public struct ValueStateArgs
        {
            public ValueStateArgs(bool valueActive = true, bool valueRichText = true, Color? valueColor = null,
                bool typeLabelActive = false, bool toggleActive = false, bool inputActive = false, bool applyActive = false,
                bool inspectActive = false, bool subContentButtonActive = false)
            {
                this.valueActive = valueActive;
                this.valueRichText = valueRichText;
                this.valueColor = valueColor == null ? Color.white : (Color)valueColor;
                this.typeLabelActive = typeLabelActive;
                this.toggleActive = toggleActive;
                this.inputActive = inputActive;
                this.applyActive = applyActive;
                this.inspectActive = inspectActive;
                this.subContentButtonActive = subContentButtonActive;
            }

            public static ValueStateArgs Default => _default;
            private static ValueStateArgs _default = new ValueStateArgs(true);

            public bool valueActive, valueRichText, typeLabelActive, toggleActive,
                inputActive, applyActive, inspectActive, subContentButtonActive;

            public Color valueColor;
        }
    }
}
