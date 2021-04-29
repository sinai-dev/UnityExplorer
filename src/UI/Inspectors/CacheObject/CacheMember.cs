using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityExplorer.UI.Inspectors.CacheObject.Views;
using UnityExplorer.UI.Utility;

namespace UnityExplorer.UI.Inspectors.CacheObject
{
    // TODO some of this can be reused for CacheEnumerated / CacheKVP as well, just doing members for now.
    // Will put shared stuff in CacheObjectBase.

    public abstract class CacheMember : CacheObjectBase
    {
        public ReflectionInspector ParentInspector { get; internal set; }
        public CacheMemberCell CurrentView { get; internal set; }
        public bool AutoUpdateWanted { get; internal set; }

        public Type DeclaringType { get; private set; }
        public string NameForFiltering { get; private set; }

        public object Value { get; protected set; }
        public Type FallbackType { get; private set; }

        public abstract bool ShouldAutoEvaluate { get; }
        public bool HasArguments => Arguments?.Length > 0;
        public ParameterInfo[] Arguments { get; protected set; }
        public bool Evaluating { get; protected set; }
        public bool CanWrite { get; protected set; }
        public bool HadException { get; protected set; }
        public Exception LastException { get; protected set; }

        public string MemberLabelText { get; private set; }
        public string TypeLabelText { get; protected set; }
        public string ValueLabelText { get; protected set; }

        private static readonly Dictionary<string, MethodInfo> numberParseMethods = new Dictionary<string, MethodInfo>();

        public enum ValueState
        {
            NotEvaluated, Exception, NullValue,
            Boolean, Number, String, Enum, 
            Collection, ValueStruct, Unsupported
        }

        protected ValueState State = ValueState.NotEvaluated;

        private const string NOT_YET_EVAL = "<color=grey>Not yet evaluated</color>";

        /// <summary>
        /// Initialize the CacheMember when an Inspector is opened and caches the member
        /// </summary>
        public virtual void Initialize(ReflectionInspector inspector, Type declaringType, MemberInfo member, Type returnType)
        {
            this.DeclaringType = declaringType;
            this.ParentInspector = inspector;
            this.FallbackType = returnType;
            this.MemberLabelText = SignatureHighlighter.ParseFullSyntax(declaringType, false, member);
            this.NameForFiltering = $"{declaringType.Name}.{member.Name}";
            this.TypeLabelText = SignatureHighlighter.ParseFullType(FallbackType, false);
            this.ValueLabelText = GetValueLabel();
        }

        public virtual void OnDestroyed()
        {
            // TODO release IValue / Evaluate back to pool, etc
        }

        protected abstract void TryEvaluate();

        public void SetValue(object value)
        {
            // TODO unbox string, cast, etc

            TrySetValue(value);

            Evaluate();
        }

        protected abstract void TrySetValue(object value);

        public void OnCellApplyClicked()
        {
            if (CurrentView == null)
            {
                ExplorerCore.LogWarning("Trying to apply CacheMember but current cell reference is null!");
                return;
            }

            if (State == ValueState.Boolean)
                SetValue(this.CurrentView.Toggle.isOn);
            else
            {
                if (!numberParseMethods.ContainsKey(FallbackType.AssemblyQualifiedName))
                {
                    var method = FallbackType.GetMethod("Parse", new Type[] { typeof(string) });
                    numberParseMethods.Add(FallbackType.AssemblyQualifiedName, method);
                }

                var val = numberParseMethods[FallbackType.AssemblyQualifiedName]
                    .Invoke(null, new object[] { CurrentView.InputField.text });
                SetValue(val);
            }

            SetCell(this.CurrentView);
        }

        /// <summary>
        /// Evaluate when first shown (if ShouldAutoEvaluate), or else when Evaluate button is clicked.
        /// </summary>
        public void Evaluate()
        {
            TryEvaluate();

            if (!Value.IsNullOrDestroyed())
                Value = Value.TryCast();

            ProcessOnEvaluate();
        }

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

        private string GetValueLabel()
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

        /// <summary>
        /// Set the cell view for an enabled cell based on this CacheMember model.
        /// </summary>
        public void SetCell(CacheMemberCell cell)
        {
            cell.MemberLabel.text = MemberLabelText;
            cell.ValueLabel.gameObject.SetActive(true);

            cell.EvaluateHolder.SetActive(!ShouldAutoEvaluate);
            if (!ShouldAutoEvaluate)
            {
                cell.UpdateToggle.gameObject.SetActive(false);
                cell.EvaluateButton.Button.gameObject.SetActive(true);
                if (HasArguments)
                    cell.EvaluateButton.ButtonText.text = $"Evaluate ({Arguments.Length})";
                else
                    cell.EvaluateButton.ButtonText.text = "Evaluate";
            }
            else
            { 
                cell.UpdateToggle.gameObject.SetActive(true);
                cell.UpdateToggle.isOn = AutoUpdateWanted;
            }

            if (State == ValueState.NotEvaluated && !ShouldAutoEvaluate)
            {
                // todo evaluate buttons etc
                SetValueState(cell, true, true, Color.white, false, false, false, false, false, false);

                return;
            }

            if (State == ValueState.NotEvaluated)
                Evaluate();

            switch (State)
            {
                case ValueState.Exception:
                case ValueState.NullValue:
                    SetValueState(cell, true, true, Color.white, false, false, false, false, false, false);
                    break;
                case ValueState.Boolean:
                    SetValueState(cell, false, false, default, true, toggleActive: true, false, CanWrite, false, false); 
                    break;
                case ValueState.Number:
                    SetValueState(cell, false, true, Color.white, true, false, inputActive: true, CanWrite, false, false);
                    break;
                case ValueState.String:
                    SetValueState(cell, true, false, SignatureHighlighter.StringOrange, false, false, false, false, false, true);
                    break;
                case ValueState.Enum:
                    SetValueState(cell, true, true, Color.white, false, false, false, false, false, true);
                    break;
                case ValueState.Collection:
                case ValueState.ValueStruct:
                    SetValueState(cell, true, true, Color.white, false, false, false, false, true, true);
                    break;
                case ValueState.Unsupported:
                    SetValueState(cell, true, true, Color.white, false, false, false, false, true, false);
                    break;
            }
        }

        private void SetValueState(CacheMemberCell cell, bool valueActive, bool valueRichText, Color valueColor,
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

            cell.UpdateToggle.gameObject.SetActive(ShouldAutoEvaluate);
        }


        #region Cache Member Util

        public static bool CanProcessArgs(ParameterInfo[] parameters)
        {
            foreach (var param in parameters)
            {
                var pType = param.ParameterType;

                if (pType.IsByRef && pType.HasElementType)
                    pType = pType.GetElementType();

                if (pType != null && (pType.IsPrimitive || pType == typeof(string)))
                    continue;
                else
                    return false;
            }
            return true;
        }

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
            Type declaringType, ReflectionInspector _inspector, bool ignoreMethodBlacklist = false)
        {
            try
            {
                var sig = GetSig(member);

                if (IsBlacklisted(sig))
                    return;

                //ExplorerCore.Log($"Trying to cache member {sig}...");
                //ExplorerCore.Log(member.DeclaringType.FullName + "." + member.Name);

                CacheMember cached;
                Type returnType;
                switch (member.MemberType)
                {
                    case MemberTypes.Method:
                        {
                            var mi = member as MethodInfo;
                            if (!ignoreMethodBlacklist && IsBlacklisted(mi))
                                return;

                            var args = mi.GetParameters();
                            if (!CanProcessArgs(args))
                                return;

                            sig += AppendArgsToSig(args);
                            if (cachedSigs.Contains(sig))
                                return;

                            cached = new CacheMethod() { MethodInfo = mi };
                            returnType = mi.ReturnType;
                            break;
                        }

                    case MemberTypes.Property:
                        {
                            var pi = member as PropertyInfo;

                            var args = pi.GetIndexParameters();
                            if (!CanProcessArgs(args))
                                return;

                            if (!pi.CanRead && pi.CanWrite)
                            {
                                // write-only property, cache the set method instead.
                                var setMethod = pi.GetSetMethod(true);
                                if (setMethod != null)
                                    TryCacheMember(setMethod, list, cachedSigs, declaringType, _inspector, true);
                                return;
                            }

                            sig += AppendArgsToSig(args);
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

                cached.Initialize(_inspector, declaringType, member, returnType);

                list.Add(cached);
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"Exception caching member {member.DeclaringType.FullName}.{member.Name}!");
                ExplorerCore.Log(e.ToString());
            }
        }

        internal static string GetSig(MemberInfo member) => $"{member.DeclaringType.Name}.{member.Name}";

        internal static string AppendArgsToSig(ParameterInfo[] args)
        {
            string ret = " (";
            foreach (var param in args)
                ret += $"{param.ParameterType.Name} {param.Name}, ";
            ret += ")";
            return ret;
        }

        // Blacklists
        private static readonly HashSet<string> bl_typeAndMember = new HashSet<string>
        {
            // these cause a crash in IL2CPP
#if CPP
            "Type.DeclaringMethod",
            "Rigidbody2D.Cast",
            "Collider2D.Cast",
            "Collider2D.Raycast",
            "Texture2D.SetPixelDataImpl",
            "Camera.CalculateProjectionMatrixFromPhysicalProperties",
#endif
            // These were deprecated a long time ago, still show up in some games for some reason
            "MonoBehaviour.allowPrefabModeInPlayMode",
            "MonoBehaviour.runInEditMode",
            "Component.animation",
            "Component.audio",
            "Component.camera",
            "Component.collider",
            "Component.collider2D",
            "Component.constantForce",
            "Component.hingeJoint",
            "Component.light",
            "Component.networkView",
            "Component.particleSystem",
            "Component.renderer",
            "Component.rigidbody",
            "Component.rigidbody2D",
        };
        private static readonly HashSet<string> bl_methodNameStartsWith = new HashSet<string>
        {
            // these are redundant
            "get_",
            "set_",
        };

        internal static bool IsBlacklisted(string sig) => bl_typeAndMember.Any(it => sig.Contains(it));
        internal static bool IsBlacklisted(MethodInfo method) => bl_methodNameStartsWith.Any(it => method.Name.StartsWith(it));

        #endregion


    }
}
