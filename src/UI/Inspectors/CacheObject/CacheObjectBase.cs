using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityExplorer.UI.Inspectors.CacheObject.Views;
using UnityExplorer.UI.Inspectors.IValues;
using UnityExplorer.UI.ObjectPool;
using UnityExplorer.UI.Utility;

namespace UnityExplorer.UI.Inspectors.CacheObject
{
    public abstract class CacheObjectBase
    {
        public CacheObjectCell CellView { get; internal set; }

        public InteractiveValue IValue { get; private set; }
        public Type CurrentIValueType { get; private set; }
        public bool SubContentState { get; private set; }

        public object Value { get; protected set; }
        public Type FallbackType { get; protected set; }

        public string NameLabelText { get; protected set; }
        public string TypeLabelText { get; protected set; }
        public string ValueLabelText { get; protected set; }

        public abstract bool ShouldAutoEvaluate { get; }
        public abstract bool HasArguments { get; }
        public bool CanWrite { get; protected set; }
        public bool HadException { get; protected set; }
        public Exception LastException { get; protected set; }

        public virtual void Initialize(Type fallbackType)
        {
            this.FallbackType = fallbackType;
            this.TypeLabelText = SignatureHighlighter.ParseFullType(FallbackType, false);
            this.ValueLabelText = GetValueLabel();
        }

        // internals

        private static readonly Dictionary<string, MethodInfo> numberParseMethods = new Dictionary<string, MethodInfo>();

        public enum ValueState
        {
            NotEvaluated, Exception, NullValue,
            Boolean, Number, String, Enum,
            Collection, ValueStruct, Unsupported
        }

        public ValueState State = ValueState.NotEvaluated;

        protected const string NOT_YET_EVAL = "<color=grey>Not yet evaluated</color>";

        internal static GameObject InactiveIValueHolder
        {
            get
            {
                if (!inactiveIValueHolder)
                {
                    inactiveIValueHolder = new GameObject("InactiveIValueHolder");
                    GameObject.DontDestroyOnLoad(inactiveIValueHolder);
                    inactiveIValueHolder.transform.parent = UIManager.PoolHolder.transform;
                    inactiveIValueHolder.SetActive(false);
                }
                return inactiveIValueHolder;
            }
        }
        private static GameObject inactiveIValueHolder;

        // On parent destroying this

        public virtual void ReleasePooledObjects()
        {
            // TODO release IValue / Evaluate back to pool, etc
            ReleaseIValue();
        }

        // Updating and applying values

        public abstract void SetUserValue(object value);

        /// <summary>
        /// Process the CacheMember state when the value has been evaluated (or re-evaluated)
        /// </summary>
        protected virtual void ProcessOnEvaluate()
        {
            var prevState = State;

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
                else if (type == typeof(string))
                    State = ValueState.String;
                else if (type.IsEnum)
                    State = ValueState.Enum;
                else if (type.IsEnumerable() || type.IsDictionary())
                    State = ValueState.Collection;
                // todo Color and ValueStruct
                else
                    State = ValueState.Unsupported;
            }

            // Set label text
            ValueLabelText = GetValueLabel();

            if (State != prevState)
            {
                // TODO handle if subcontent / evaluate shown, check type change, etc
            }
        }

        protected string GetValueLabel()
        {
            switch (State)
            {
                case ValueState.NotEvaluated:
                    return $"<i>{NOT_YET_EVAL} ({SignatureHighlighter.ParseFullType(FallbackType, true)})</i>";
                case ValueState.Exception:
                    return $"<i><color=red>{ReflectionUtility.ReflectionExToString(LastException)}</color></i>";
                case ValueState.Boolean:
                case ValueState.Number:
                    return null;
                case ValueState.String:
                    string s = Value as string;
                    if (s.Length > 200)
                        s = $"{s.Substring(0, 200)}...";
                    return $"\"{s}\"";
                case ValueState.NullValue:
                    return $"<i>{ToStringUtility.ToStringWithType(Value, FallbackType, true)}</i>";
                case ValueState.Enum:
                case ValueState.Collection:
                case ValueState.ValueStruct:
                case ValueState.Unsupported:
                default:
                    return ToStringUtility.ToStringWithType(Value, FallbackType, true);
            }
        }

        protected abstract bool SetCellEvaluateState(CacheObjectCell cell);

        public virtual void SetCell(CacheObjectCell cell)
        {
            cell.NameLabel.text = NameLabelText;
            cell.ValueLabel.gameObject.SetActive(true);

            cell.SubContentHolder.gameObject.SetActive(SubContentState);
            if (IValue != null)
                IValue.UIRoot.transform.SetParent(cell.SubContentHolder.transform, false);

            if (SetCellEvaluateState(cell))
                return;

            switch (State)
            {
                case ValueState.Exception:
                case ValueState.NullValue:
                    ReleaseIValue();
                    SetValueState(cell, true, true, Color.white, false, false, false, false, false, false);
                    break;
                case ValueState.Boolean:
                    SetValueState(cell, false, false, default, false, toggleActive: true, false, CanWrite, false, false);
                    break;
                case ValueState.Number:
                    SetValueState(cell, false, true, Color.white, true, false, inputActive: true, CanWrite, false, false);
                    break;
                case ValueState.String:
                    UpdateIValueOnValueUpdate();
                    SetValueState(cell, true, false, SignatureHighlighter.StringOrange, false, false, false, false, false, true);
                    break;
                case ValueState.Enum:
                    UpdateIValueOnValueUpdate();
                    SetValueState(cell, true, true, Color.white, false, false, false, false, false, true);
                    break;
                case ValueState.Collection:
                case ValueState.ValueStruct:
                    UpdateIValueOnValueUpdate();
                    SetValueState(cell, true, true, Color.white, false, false, false, false, true, true);
                    break;
                case ValueState.Unsupported:
                    SetValueState(cell, true, true, Color.white, false, false, false, false, true, false);
                    break;
            }
        }

        protected virtual void SetValueState(CacheObjectCell cell, bool valueActive, bool valueRichText, Color valueColor,
            bool typeLabelActive, bool toggleActive, bool inputActive, bool applyActive, bool inspectActive, bool subContentActive)
        {
            //cell.ValueLabel.gameObject.SetActive(valueActive);
            if (valueActive)
            {
                cell.ValueLabel.text = ValueLabelText;
                cell.ValueLabel.supportRichText = valueRichText;
                cell.ValueLabel.color = valueColor;
            }
            else
                cell.ValueLabel.text = "";

            cell.TypeLabel.gameObject.SetActive(typeLabelActive);
            if (typeLabelActive)
                cell.TypeLabel.text = TypeLabelText;

            cell.Toggle.gameObject.SetActive(toggleActive);
            if (toggleActive)
            {
                cell.Toggle.isOn = (bool)Value;
                cell.ToggleText.text = Value.ToString();
            }

            cell.InputField.gameObject.SetActive(inputActive);
            if (inputActive)
            {
                cell.InputField.text = Value.ToString();
                cell.InputField.readOnly = !CanWrite;
            }

            cell.ApplyButton.Button.gameObject.SetActive(applyActive);
            cell.InspectButton.Button.gameObject.SetActive(inspectActive);
            cell.SubContentButton.Button.gameObject.SetActive(subContentActive);
        }

        // IValues

        public virtual void OnCellSubContentToggle()
        {
            if (this.IValue == null)
            {
                IValue = (InteractiveValue)Pool.Borrow(typeof(InteractiveValue));
                CurrentIValueType = IValue.GetType();
                IValue.SetOwner(this);
                IValue.UIRoot.transform.SetParent(CellView.SubContentHolder.transform, false);
                CellView.SubContentHolder.SetActive(true);
                SubContentState = true;
            }
            else
            {
                SubContentState = !SubContentState;
                CellView.SubContentHolder.SetActive(SubContentState); 
            }
        }

        public virtual void ReleaseIValue()
        {
            if (IValue == null)
                return;

            IValue.OnOwnerReleased();
            Pool.Return(CurrentIValueType, IValue);

            IValue = null;
        }

        internal void HideIValue()
        {
            if (this.IValue == null)
                return;

            this.IValue.UIRoot.transform.SetParent(InactiveIValueHolder.transform, false);
        }

        public void UpdateIValueOnValueUpdate()
        {
            if (this.IValue == null)
                return;

            IValue.SetValue(Value);
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
                if (!numberParseMethods.ContainsKey(FallbackType.AssemblyQualifiedName))
                {
                    var method = FallbackType.GetMethod("Parse", new Type[] { typeof(string) });
                    numberParseMethods.Add(FallbackType.AssemblyQualifiedName, method);
                }

                var val = numberParseMethods[FallbackType.AssemblyQualifiedName]
                    .Invoke(null, new object[] { CellView.InputField.text });
                SetUserValue(val);
            }

            SetCell(this.CellView);
        }
    }
}
