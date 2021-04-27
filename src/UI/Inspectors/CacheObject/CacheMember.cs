using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityExplorer.UI.Inspectors.CacheObject.Views;
using UnityExplorer.UI.Utility;

namespace UnityExplorer.UI.Inspectors.CacheObject
{
    public abstract class CacheMember : CacheObjectBase
    {
        public ReflectionInspector ParentInspector { get; internal set; }

        public Type DeclaringType { get; private set; }
        public string NameForFiltering { get; private set; }

        public object Value { get; protected set; }
        public Type FallbackType { get; private set; }

        public bool HasEvaluated { get; protected set; }
        public bool HasArguments { get; protected set; }
        public bool Evaluating { get; protected set; }
        public bool CanWrite { get; protected set; }
        public bool HadException { get; protected set; }
        public Exception LastException { get; protected set; }

        public string MemberLabelText { get; private set; }
        public string TypeLabelText { get; protected set; }
        public string ValueLabelText { get; protected set; }

        public virtual void Initialize(ReflectionInspector inspector, Type declaringType, MemberInfo member, Type returnType)
        {
            this.DeclaringType = declaringType;
            this.ParentInspector = inspector;
            this.FallbackType = returnType;
            this.MemberLabelText = SignatureHighlighter.ParseFullSyntax(declaringType, false, member);
            this.NameForFiltering = $"{declaringType.Name}.{member.Name}";
            this.TypeLabelText = SignatureHighlighter.HighlightTypeName(returnType);
        }

        public void SetCell(CacheMemberCell cell)
        {
            cell.MemberLabel.text = MemberLabelText;
            cell.TypeLabel.text = TypeLabelText;

            if (HasArguments && !HasEvaluated)
            {
                // todo
                cell.ValueLabel.text = "Not yet evalulated";
            }
            else if (!HasEvaluated)
                Evaluate();

            if (HadException)
            {
                cell.InspectButton.Button.gameObject.SetActive(false);
                cell.ValueLabel.gameObject.SetActive(true);
                cell.ValueLabel.supportRichText = true;
                cell.ValueLabel.text = $"<color=red>{ReflectionUtility.ReflectionExToString(LastException)}</color>";
            }
            else if (Value.IsNullOrDestroyed())
            {
                cell.InspectButton.Button.gameObject.SetActive(false);
                cell.ValueLabel.gameObject.SetActive(true);
                cell.ValueLabel.supportRichText = true;
                cell.ValueLabel.text = ValueLabelText;
            }
            else
            {
                cell.ValueLabel.supportRichText = false;
                cell.ValueLabel.text = ValueLabelText;

                var valueType = Value.GetActualType();
                if (valueType.IsPrimitive || valueType == typeof(decimal))
                {
                    cell.InspectButton.Button.gameObject.SetActive(false);
                    cell.ValueLabel.gameObject.SetActive(true);
                }
                else if (valueType == typeof(string))
                {
                    cell.InspectButton.Button.gameObject.SetActive(false);
                    cell.ValueLabel.gameObject.SetActive(true);
                }
                else
                {
                    cell.InspectButton.Button.gameObject.SetActive(true);
                    cell.ValueLabel.gameObject.SetActive(true);
                }
            }
        }

        protected abstract void TryEvaluate();

        public void Evaluate()
        {
            TryEvaluate();

            if (!HadException)
            {
                ValueLabelText = ToStringUtility.ToString(Value, FallbackType);
            }

            HasEvaluated = true;
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
                infos.AddRange(declaringType.GetMethods(flags));
                infos.AddRange(declaringType.GetProperties(flags));
                infos.AddRange(declaringType.GetFields(flags));

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
