using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI;
using UnityExplorer.Core.Unity;
using UnityExplorer.Core.Runtime;
using UnityExplorer.Core;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.InteractiveValues;
using UnityExplorer.UI.Main.Home.Inspectors.Reflection;

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
            IValue.m_mainContentParent = this.m_rightGroup;
            IValue.m_subContentParent = this.m_subContent;
        }

        public override void UpdateValue()
        {
            if (!HasParameters || m_isEvaluating)
            {
                try
                {
                    Type baseType = ReflectionUtility.GetType(IValue.Value) ?? FallbackType;

                    if (!ReflectionProvider.Instance.IsReflectionSupported(baseType))
                        throw new Exception("Type not supported with reflection");

                    UpdateReflection();

                    if (IValue.Value != null)
                        IValue.Value = IValue.Value.Cast(ReflectionUtility.GetType(IValue.Value));
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

        internal float GetMemberLabelWidth(RectTransform scrollRect)
        {
            var textGenSettings = m_memLabelText.GetGenerationSettings(m_topRowRect.rect.size);
            textGenSettings.scaleFactor = InputFieldScroller.canvasScaler.scaleFactor;

            var textGen = m_memLabelText.cachedTextGeneratorForLayout;
            float preferredWidth = textGen.GetPreferredWidth(RichTextName, textGenSettings);

            float max = scrollRect.rect.width * 0.4f;

            if (preferredWidth > max) preferredWidth = max;

            return preferredWidth < 125f ? 125f : preferredWidth;
        }

        internal void SetWidths(float labelWidth, float valueWidth)
        {
            m_leftLayout.preferredWidth = labelWidth;
            m_rightLayout.preferredWidth = valueWidth;
        }

        internal RectTransform m_topRowRect;
        internal Text m_memLabelText;
        internal GameObject m_leftGroup;
        internal LayoutElement m_leftLayout;
        internal GameObject m_rightGroup;
        internal LayoutElement m_rightLayout;

        internal override void ConstructUI()
        {
            base.ConstructUI();

            var topGroupObj = UIFactory.CreateHorizontalGroup(m_mainContent, "CacheMemberGroup", false, false, true, true, 10, new Vector4(0, 0, 3, 3),
                new Color(1, 1, 1, 0));

            m_topRowRect = topGroupObj.GetComponent<RectTransform>();

            UIFactory.SetLayoutElement(topGroupObj, minHeight: 25, flexibleHeight: 0, minWidth: 300, flexibleWidth: 5000);

            // left group

            m_leftGroup = UIFactory.CreateHorizontalGroup(topGroupObj, "LeftGroup", false, true, true, true, 4, default, new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(m_leftGroup, minHeight: 25, flexibleHeight: 0, minWidth: 125, flexibleWidth: 200);

            // member label

            m_memLabelText = UIFactory.CreateLabel(m_leftGroup, "MemLabelText", RichTextName, TextAnchor.MiddleLeft);
            m_memLabelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            var leftRect = m_memLabelText.GetComponent<RectTransform>();
            leftRect.anchorMin = Vector2.zero;
            leftRect.anchorMax = Vector2.one;
            leftRect.offsetMin = Vector2.zero;
            leftRect.offsetMax = Vector2.zero;
            leftRect.sizeDelta = Vector2.zero;
            m_leftLayout = m_memLabelText.gameObject.AddComponent<LayoutElement>();
            m_leftLayout.preferredWidth = 125;
            m_leftLayout.minHeight = 25;
            m_leftLayout.flexibleHeight = 100;
            var labelFitter = m_memLabelText.gameObject.AddComponent<ContentSizeFitter>();
            labelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            labelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            // right group

            m_rightGroup = UIFactory.CreateVerticalGroup(topGroupObj, "RightGroup", false, true, true, true, 2, new Vector4(4,2,0,0),
                new Color(1, 1, 1, 0));

            m_rightLayout = m_rightGroup.AddComponent<LayoutElement>();
            m_rightLayout.minHeight = 25;
            m_rightLayout.flexibleHeight = 480;
            m_rightLayout.minWidth = 125;
            m_rightLayout.flexibleWidth = 5000;

            ConstructArgInput(out GameObject argsHolder);

            ConstructEvaluateButtons(argsHolder);

            IValue.m_mainContentParent = m_rightGroup;
        }

        internal void ConstructArgInput(out GameObject argsHolder)
        {
            argsHolder = null;

            if (HasParameters)
            {
                argsHolder = UIFactory.CreateVerticalGroup(m_rightGroup, "ArgsHolder", true, false, true, true, 4, new Color(1, 1, 1, 0));

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

            var argInputObj = UIFactory.CreateInputField(rowObj, "ArgInput", "...", 14, (int)TextAnchor.MiddleLeft, 1);
            UIFactory.SetLayoutElement(argInputObj, flexibleWidth: 1200, preferredWidth: 150, minWidth: 20, minHeight: 25, flexibleHeight: 0);

            var argInput = argInputObj.GetComponent<InputField>();
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
                var evalGroupObj = UIFactory.CreateHorizontalGroup(m_rightGroup, "EvalGroup", false, false, true, true, 5, 
                    default, new Color(1, 1, 1, 0));
                UIFactory.SetLayoutElement(evalGroupObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 5000);

                var colors = new ColorBlock();
                colors = RuntimeProvider.Instance.SetColorBlock(colors, new Color(0.4f, 0.4f, 0.4f),
                    new Color(0.4f, 0.7f, 0.4f), new Color(0.3f, 0.3f, 0.3f));

                var evalButton = UIFactory.CreateButton(evalGroupObj, 
                    "EvalButton", 
                    $"Evaluate ({ParamCount})",
                    null,
                    colors);

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
                        evalButton.colors = RuntimeProvider.Instance.SetColorBlock(evalButton.colors, new Color(0.3f, 0.6f, 0.3f));

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
                    evalButton.colors = RuntimeProvider.Instance.SetColorBlock(evalButton.colors, new Color(0.4f, 0.4f, 0.4f));
                });
            }
            else if (this is CacheMethod)
            {
                // simple method evaluate button

                var colors = new ColorBlock();
                colors = RuntimeProvider.Instance.SetColorBlock(colors, new Color(0.4f, 0.4f, 0.4f),
                    new Color(0.4f, 0.7f, 0.4f), new Color(0.3f, 0.3f, 0.3f));

                var evalButton = UIFactory.CreateButton(m_rightGroup, "EvalButton", "Evaluate", () => { (this as CacheMethod).Evaluate(); }, colors);
                UIFactory.SetLayoutElement(evalButton.gameObject, minWidth: 100, minHeight: 22, flexibleWidth: 0);
            }
        }

        #endregion
    }
}
