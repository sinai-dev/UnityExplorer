using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityExplorer.Runtime;
using UnityExplorer.CacheObject.Views;
using UnityExplorer.Inspectors;
using UniverseLib.UI.Models;
using UnityExplorer.UI;
using UniverseLib;
using UniverseLib.UI;
using UnityExplorer.UI.Widgets;
using UniverseLib.Utility;
using UniverseLib.UI.ObjectPool;
using System.Collections;
using HarmonyLib;

namespace UnityExplorer.CacheObject
{
    public abstract class CacheMember : CacheObjectBase
    {
        public abstract Type DeclaringType { get; }
        public string NameForFiltering { get; protected set; }
        public object DeclaringInstance => IsStatic ? null : (m_declaringInstance ??= Owner.Target.TryCast(DeclaringType));
        private object m_declaringInstance;

        public abstract bool IsStatic { get; }
        public override bool HasArguments => Arguments?.Length > 0 || GenericArguments.Length > 0;
        public ParameterInfo[] Arguments { get; protected set; } = new ParameterInfo[0];
        public Type[] GenericArguments { get; protected set; } = ArgumentUtility.EmptyTypes;
        public EvaluateWidget Evaluator { get; protected set; }
        public bool Evaluating => Evaluator != null && Evaluator.UIRoot.activeSelf;

        public virtual void SetInspectorOwner(ReflectionInspector inspector, MemberInfo member)
        {
            this.Owner = inspector;
            this.NameLabelText = this switch
            {
                CacheMethod => SignatureHighlighter.HighlightMethod(member as MethodInfo),
                CacheConstructor => SignatureHighlighter.HighlightConstructor(member as ConstructorInfo),
                _ => SignatureHighlighter.Parse(member.DeclaringType, false, member),
            };

            this.NameForFiltering = SignatureHighlighter.RemoveHighlighting(NameLabelText);
            this.NameLabelTextRaw = NameForFiltering;
        }

        public override void ReleasePooledObjects()
        {
            base.ReleasePooledObjects();

            if (this.Evaluator != null)
            {
                this.Evaluator.OnReturnToPool();
                Pool<EvaluateWidget>.Return(this.Evaluator);
                this.Evaluator = null;
            }
        }

        public override void UnlinkFromView()
        {
            if (this.Evaluator != null)
                this.Evaluator.UIRoot.transform.SetParent(Pool<EvaluateWidget>.Instance.InactiveHolder.transform, false);

            base.UnlinkFromView();
        }

        protected abstract object TryEvaluate();

        protected abstract void TrySetValue(object value);

        /// <summary>
        /// Evaluate is called when first shown (if ShouldAutoEvaluate), or else when Evaluate button is clicked, or auto-updated.
        /// </summary>
        public void Evaluate()
        {
            SetValueFromSource(TryEvaluate());
        }

        /// <summary>
        /// Called when user presses the Evaluate button.
        /// </summary>
        public void EvaluateAndSetCell()
        {
            Evaluate();
            if (CellView != null)
                SetDataToCell(CellView);
        }

        public override void TrySetUserValue(object value)
        {
            TrySetValue(value);
            Evaluate();
        }

        protected override void SetValueState(CacheObjectCell cell, ValueStateArgs args)
        {
            base.SetValueState(cell, args);
        }

        private static readonly Color evalEnabledColor = new(0.15f, 0.25f, 0.15f);
        private static readonly Color evalDisabledColor = new(0.15f, 0.15f, 0.15f);

        protected override bool TryAutoEvaluateIfUnitialized(CacheObjectCell objectcell)
        {
            var cell = objectcell as CacheMemberCell;

            cell.EvaluateHolder.SetActive(!ShouldAutoEvaluate);
            if (!ShouldAutoEvaluate)
            {
                cell.EvaluateButton.Component.gameObject.SetActive(true);
                if (HasArguments)
                {
                    if (!Evaluating)
                        cell.EvaluateButton.ButtonText.text = $"Evaluate ({Arguments.Length + GenericArguments.Length})";
                    else
                    {
                        cell.EvaluateButton.ButtonText.text = "Hide";
                        Evaluator.UIRoot.transform.SetParent(cell.EvaluateHolder.transform, false);
                        RuntimeHelper.SetColorBlock(cell.EvaluateButton.Component, evalEnabledColor, evalEnabledColor * 1.3f);
                    }
                }
                else
                    cell.EvaluateButton.ButtonText.text = "Evaluate";

                if (!Evaluating)
                    RuntimeHelper.SetColorBlock(cell.EvaluateButton.Component, evalDisabledColor, evalDisabledColor * 1.3f);
            }

            if (State == ValueState.NotEvaluated && !ShouldAutoEvaluate)
            {
                SetValueState(cell, ValueStateArgs.Default);
                cell.RefreshSubcontentButton();

                return false;
            }

            if (State == ValueState.NotEvaluated)
                Evaluate();

            return true;
        }

        public void OnEvaluateClicked()
        {
            if (!HasArguments)
            {
                EvaluateAndSetCell();
            }
            else
            {
                if (Evaluator == null)
                {
                    this.Evaluator = Pool<EvaluateWidget>.Borrow();
                    Evaluator.OnBorrowedFromPool(this);
                    Evaluator.UIRoot.transform.SetParent((CellView as CacheMemberCell).EvaluateHolder.transform, false);
                    TryAutoEvaluateIfUnitialized(CellView);
                }
                else
                {
                    if (Evaluator.UIRoot.activeSelf)
                        Evaluator.UIRoot.SetActive(false);
                    else
                        Evaluator.UIRoot.SetActive(true);

                    TryAutoEvaluateIfUnitialized(CellView);
                }
            }
        }
    }
}
