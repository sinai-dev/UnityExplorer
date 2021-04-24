using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI;
using UnityExplorer.Core.Runtime;
using UnityExplorer.Core;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.InteractiveValues;
using UnityExplorer.UI.Inspectors.Reflection;
using UnityExplorer.UI.Panels;

namespace UnityExplorer.UI.CacheObject
{
    public abstract class CacheMember : CacheObjectBase
    {
        public override bool IsMember => true;

        public override Type FallbackType { get; }

        public ReflectionInspector ParentInspector { get; set; }
        public MemberInfo MemInfo { get; set; }
        public Type DeclaringType { get; set; }
        public object DeclaringInstance { get; set; }
        public virtual bool IsStatic { get; private set; }

        public string ReflectionException { get; set; }

        public override bool CanWrite => m_canWrite ?? GetCanWrite();
        private bool? m_canWrite;

        public override bool HasParameters => ParamCount > 0;
        public virtual int ParamCount => m_arguments.Length;
        public override bool HasEvaluated => m_evaluated;
        public bool m_evaluated = false;
        public bool m_isEvaluating;
        public ParameterInfo[] m_arguments = new ParameterInfo[0];
        public string[] m_argumentInput = new string[0];

        public string NameForFiltering => m_nameForFilter ?? (m_nameForFilter = $"{MemInfo.DeclaringType.Name}.{MemInfo.Name}".ToLower());
        private string m_nameForFilter;

        public string RichTextName => m_richTextName ?? GetRichTextName();
        private string m_richTextName;

        public CacheMember(MemberInfo memberInfo, object declaringInstance, GameObject parentContent)
        {
            MemInfo = memberInfo;
            DeclaringType = memberInfo.DeclaringType;
            DeclaringInstance = declaringInstance;
            this.m_parentContent = parentContent;

            DeclaringInstance = ReflectionProvider.Instance.Cast(declaringInstance, DeclaringType);
        }

        public override void Enable()
        {
            base.Enable();

            ParentInspector.displayedMembers.Add(this);

            memberLabelElement.minWidth = 0.4f * InspectorPanel.CurrentPanelWidth;
        }

        public override void Disable()
        {
            base.Disable();

            ParentInspector.displayedMembers.Remove(this);
        }

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

        public override void CreateIValue(object value, Type fallbackType)
        {
            IValue = InteractiveValue.Create(value, fallbackType);
            IValue.Owner = this;
            IValue.m_mainContentParent = this.ContentGroup;
            IValue.m_subContentParent = this.SubContentGroup;
        }

        public override void UpdateValue()
        {
            if (!HasParameters || m_isEvaluating)
            {
                try
                {
                    Type baseType = ReflectionUtility.GetActualType(IValue.Value) ?? FallbackType;

                    if (!ReflectionProvider.Instance.IsReflectionSupported(baseType))
                        throw new Exception("Type not supported with reflection");

                    UpdateReflection();

                    if (IValue.Value != null)
                        IValue.Value = IValue.Value.TryCast(ReflectionUtility.GetActualType(IValue.Value));
                }
                catch (Exception e)
                {
                    ReflectionException = e.ReflectionExToString(true);
                }
            }

            base.UpdateValue();
        }

        public abstract void UpdateReflection();

        public override void SetValue()
        {
            // no implementation for base class
        }

        public object[] ParseArguments()
        {
            if (m_arguments.Length < 1)
                return new object[0];

            var parsedArgs = new List<object>();
            for (int i = 0; i < m_arguments.Length; i++)
            {
                var input = m_argumentInput[i];
                var type = m_arguments[i].ParameterType;

                if (type.IsByRef)
                    type = type.GetElementType();

                if (!string.IsNullOrEmpty(input))
                {
                    if (type == typeof(string))
                    {
                        parsedArgs.Add(input);
                        continue;
                    }
                    else
                    {
                        try
                        {
                            var arg = type.GetMethod("Parse", new Type[] { typeof(string) })
                                          .Invoke(null, new object[] { input });

                            parsedArgs.Add(arg);
                            continue;
                        }
                        catch
                        {
                            ExplorerCore.Log($"Could not parse input '{input}' for argument #{i} '{m_arguments[i].Name}' ({type.FullName})");
                        }
                    }
                }

                // No input, see if there is a default value.
                if (m_arguments[i].IsOptional)
                {
                    parsedArgs.Add(m_arguments[i].DefaultValue);
                    continue;
                }

                // Try add a null arg I guess
                parsedArgs.Add(null);
            }

            return parsedArgs.ToArray();
        }

        private bool GetCanWrite()
        {
            if (MemInfo is FieldInfo fi)
                m_canWrite = !(fi.IsLiteral && !fi.IsInitOnly);
            else if (MemInfo is PropertyInfo pi)
                m_canWrite = pi.CanWrite;
            else
                m_canWrite = false;

            return (bool)m_canWrite;
        }

        private string GetRichTextName()
        {
            return m_richTextName = SignatureHighlighter.ParseFullSyntax(MemInfo.DeclaringType, false, MemInfo);
        }

        #region UI 

        internal Text memberLabelText;
        internal GameObject ContentGroup;

        internal LayoutElement memberLabelElement;

        internal override void ConstructUI()
        {
            base.ConstructUI();

            var horiGroup = UIFactory.CreateUIObject("HoriGroup", UIRoot);
            var groupRect = horiGroup.GetComponent<RectTransform>();
            groupRect.pivot = new Vector2(0, 1);
            groupRect.anchorMin = Vector2.zero;
            groupRect.anchorMax = Vector2.one;
            UIFactory.SetLayoutElement(horiGroup, minHeight: 30, flexibleHeight: 9999);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(horiGroup, true, true, true, true, 2, 2, 2, 2, 2, childAlignment: TextAnchor.UpperLeft);

            memberLabelText = UIFactory.CreateLabel(horiGroup, "MemLabelText", RichTextName, TextAnchor.UpperLeft);
            memberLabelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(memberLabelText.gameObject, minHeight: 25, flexibleHeight: 9999, minWidth: 150, flexibleWidth: 0);

            memberLabelElement = memberLabelText.GetComponent<LayoutElement>();

            ContentGroup = UIFactory.CreateUIObject("ContentGroup", horiGroup, default);
            UIFactory.SetLayoutElement(ContentGroup, minHeight: 30, flexibleWidth: 9999);
            var contentRect = ContentGroup.GetComponent<RectTransform>();
            contentRect.pivot = new Vector2(0, 1);
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(ContentGroup, false, false, true, true, childAlignment: TextAnchor.MiddleLeft);

            ConstructArgInput(out GameObject argsHolder);

            ConstructEvaluateButtons(argsHolder);

            IValue.m_mainContentParent = this.ContentGroup;

            //RightContentGroup.SetActive(false);

            // ParentInspector.CacheObjectContents.Add(this.m_mainContent);
        }

        internal void ConstructArgInput(out GameObject argsHolder)
        {
            argsHolder = null;

            if (HasParameters)
            {
                argsHolder = UIFactory.CreateVerticalGroup(ContentGroup, "ArgsHolder", true, false, true, true, 4, new Color(1, 1, 1, 0));

                if (this is CacheMethod cm && cm.GenericArgs.Length > 0)
                    cm.ConstructGenericArgInput(argsHolder);

                if (m_arguments.Length > 0)
                {
                    UIFactory.CreateLabel(argsHolder, "ArgumentsLabel", "Arguments:", TextAnchor.MiddleLeft);

                    for (int i = 0; i < m_arguments.Length; i++)
                        AddArgRow(i, argsHolder);
                }

                argsHolder.SetActive(false);
            }
        }

        internal void AddArgRow(int i, GameObject parent)
        {
            var arg = m_arguments[i];

            var rowObj = UIFactory.CreateHorizontalGroup(parent, "ArgRow", true, false, true, true, 4, default, new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(rowObj, minHeight: 25, flexibleWidth: 5000);

            var argTypeTxt = SignatureHighlighter.ParseFullSyntax(arg.ParameterType, false);
            var argLabel = UIFactory.CreateLabel(rowObj, "ArgLabel", $"{argTypeTxt} <color={SignatureHighlighter.LOCAL_ARG}>{arg.Name}</color>",
                TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(argLabel.gameObject, minHeight: 25);

            var argInputObj = UIFactory.CreateInputField(rowObj, "ArgInput", "...", out InputField argInput, 14, (int)TextAnchor.MiddleLeft, 1);
            UIFactory.SetLayoutElement(argInputObj, flexibleWidth: 1200, preferredWidth: 150, minWidth: 20, minHeight: 25, flexibleHeight: 0);

            argInput.onValueChanged.AddListener((string val) => { m_argumentInput[i] = val; });

            if (arg.IsOptional)
            {
                var phInput = argInput.placeholder.GetComponent<Text>();
                phInput.text = " = " + arg.DefaultValue?.ToString() ?? "null";
            }
        }

        internal void ConstructEvaluateButtons(GameObject argsHolder)
        {
            if (HasParameters)
            {
                var evalGroupObj = UIFactory.CreateHorizontalGroup(ContentGroup, "EvalGroup", false, false, true, true, 5, 
                    default, new Color(1, 1, 1, 0));
                UIFactory.SetLayoutElement(evalGroupObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 5000);

                var evalButton = UIFactory.CreateButton(evalGroupObj, 
                    "EvalButton", 
                    $"Evaluate ({ParamCount})",
                    null);

                RuntimeProvider.Instance.SetColorBlock(evalButton, new Color(0.4f, 0.4f, 0.4f),
                    new Color(0.4f, 0.7f, 0.4f), new Color(0.3f, 0.3f, 0.3f));

                UIFactory.SetLayoutElement(evalButton.gameObject, minWidth: 100, minHeight: 22, flexibleWidth: 0);

                var evalText = evalButton.GetComponentInChildren<Text>();

                var cancelButton = UIFactory.CreateButton(evalGroupObj, "CancelButton", "Close", null, new Color(0.3f, 0.3f, 0.3f));
                UIFactory.SetLayoutElement(cancelButton.gameObject, minWidth: 100, minHeight: 22, flexibleWidth: 0);

                cancelButton.gameObject.SetActive(false);

                evalButton.onClick.AddListener(() =>
                {
                    if (!m_isEvaluating)
                    {
                        argsHolder.SetActive(true);
                        m_isEvaluating = true;
                        evalText.text = "Evaluate";
                        RuntimeProvider.Instance.SetColorBlock(evalButton, new Color(0.3f, 0.6f, 0.3f));

                        cancelButton.gameObject.SetActive(true);
                    }
                    else
                    {
                        if (this is CacheMethod cm)
                            cm.Evaluate();
                        else
                            UpdateValue();
                    }
                });

                cancelButton.onClick.AddListener(() =>
                {
                    cancelButton.gameObject.SetActive(false);
                    argsHolder.SetActive(false);
                    m_isEvaluating = false;

                    evalText.text = $"Evaluate ({ParamCount})";
                    RuntimeProvider.Instance.SetColorBlock(evalButton, new Color(0.4f, 0.4f, 0.4f));
                });
            }
            else if (this is CacheMethod)
            {
                // simple method evaluate button

                var evalButton = UIFactory.CreateButton(ContentGroup, "EvalButton", "Evaluate", () => { (this as CacheMethod).Evaluate(); });
                RuntimeProvider.Instance.SetColorBlock(evalButton, new Color(0.4f, 0.4f, 0.4f),
                    new Color(0.4f, 0.7f, 0.4f), new Color(0.3f, 0.3f, 0.3f));

                UIFactory.SetLayoutElement(evalButton.gameObject, minWidth: 100, minHeight: 22, flexibleWidth: 0);
            }
        }

        #endregion
    }
}
