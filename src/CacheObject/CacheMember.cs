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
            this.NameLabelText = this is CacheMethod 
                ? SignatureHighlighter.HighlightMethod(member as MethodInfo) 
                : SignatureHighlighter.Parse(member.DeclaringType, false, member);
            this.NameForFiltering = $"{member.DeclaringType.Name}.{member.Name}";
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


        #region Cache Member Util

        //public static bool CanParseArgs(ParameterInfo[] parameters)
        //{
        //    foreach (var param in parameters)
        //    {
        //        var pType = param.ParameterType;
        //
        //        if (pType.IsByRef && pType.HasElementType)
        //            pType = pType.GetElementType();
        //
        //        if (pType != null && ParseUtility.CanParse(pType))
        //            continue;
        //        else
        //            return false;
        //    }
        //    return true;
        //}

        public static List<CacheMember> GetCacheMembers(object inspectorTarget, Type _type, ReflectionInspector _inspector)
        {
            var list = new List<CacheMember>();
            var cachedSigs = new HashSet<string>();

            var types = ReflectionUtility.GetAllBaseTypes(_type);

            var flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
            if (!_inspector.StaticOnly)
                flags |= BindingFlags.Instance;

            var infos = new List<MemberInfo>();

            foreach (var declaringType in types)
            {
                var target = inspectorTarget;
                if (!_inspector.StaticOnly)
                    target = target.TryCast(declaringType);

                infos.Clear();
                infos.AddRange(declaringType.GetProperties(flags));
                infos.AddRange(declaringType.GetFields(flags));
                infos.AddRange(declaringType.GetMethods(flags));

                foreach (var member in infos)
                {
                    if (member.DeclaringType != declaringType)
                        continue;
                    TryCacheMember(member, list, cachedSigs, declaringType, _inspector);
                }
            }

            var typeList = types.ToList();

            var sorted = new List<CacheMember>();
            sorted.AddRange(list.Where(it => it is CacheProperty)
                             .OrderBy(it => typeList.IndexOf(it.DeclaringType))
                             .ThenBy(it => it.NameForFiltering));
            sorted.AddRange(list.Where(it => it is CacheField)
                             .OrderBy(it => typeList.IndexOf(it.DeclaringType))
                             .ThenBy(it => it.NameForFiltering));
            sorted.AddRange(list.Where(it => it is CacheMethod)
                             .OrderBy(it => typeList.IndexOf(it.DeclaringType))
                             .ThenBy(it => it.NameForFiltering));

            return sorted;
        }

        private static void TryCacheMember(MemberInfo member, List<CacheMember> list, HashSet<string> cachedSigs,
            Type declaringType, ReflectionInspector _inspector, bool ignorePropertyMethodInfos = true)
        {
            try
            {
                if (UERuntimeHelper.IsBlacklisted(member))
                    return;

                var sig = GetSig(member);

                // ExplorerCore.Log($"Trying to cache member {sig}... ({member.MemberType})");

                CacheMember cached;
                Type returnType;
                switch (member.MemberType)
                {
                    case MemberTypes.Method:
                        {
                            var mi = member as MethodInfo;
                            if (ignorePropertyMethodInfos
                                && (mi.Name.StartsWith("get_") || mi.Name.StartsWith("set_")))
                                return;

                            //var args = mi.GetParameters();
                            //if (!CanParseArgs(args))
                            //    return;

                            sig += GetArgumentString(mi.GetParameters());
                            if (cachedSigs.Contains(sig))
                                return;

                            cached = new CacheMethod() { MethodInfo = mi };
                            returnType = mi.ReturnType;
                            break;
                        }

                    case MemberTypes.Property:
                        {
                            var pi = member as PropertyInfo;

                            //var args = pi.GetIndexParameters();
                            //if (!CanParseArgs(args))
                            //    return;

                            if (!pi.CanRead && pi.CanWrite)
                            {
                                // write-only property, cache the set method instead.
                                var setMethod = pi.GetSetMethod(true);
                                if (setMethod != null)
                                    TryCacheMember(setMethod, list, cachedSigs, declaringType, _inspector, false);
                                return;
                            }

                            sig += GetArgumentString(pi.GetIndexParameters());
                            if (cachedSigs.Contains(sig))
                                return;

                            cached = new CacheProperty() { PropertyInfo = pi };
                            returnType = pi.PropertyType;
                            break;
                        }

                    case MemberTypes.Field:
                        {
                            var fi = member as FieldInfo;
                            cached = new CacheField() { FieldInfo = fi };
                            returnType = fi.FieldType;
                            break;
                        }

                    default: return;
                }

                cachedSigs.Add(sig);

                cached.SetFallbackType(returnType);
                cached.SetInspectorOwner(_inspector, member);

                list.Add(cached);
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"Exception caching member {member.DeclaringType.FullName}.{member.Name}!");
                ExplorerCore.Log(e.ToString());
            }
        }

        internal static string GetSig(MemberInfo member)
            => $"{member.DeclaringType.Name}.{member.Name}";

        internal static string GetArgumentString(ParameterInfo[] args)
        {
            var sb = new StringBuilder();
            sb.Append(' ');
            sb.Append('(');
            foreach (var param in args)
            {
                sb.Append(param.ParameterType.Name);
                sb.Append(' ');
                sb.Append(param.Name);
                sb.Append(',');
                sb.Append(' ');
            }
            sb.Append(')');
            return sb.ToString();
        }

        #endregion


    }
}
