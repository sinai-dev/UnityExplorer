using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI;
using UnityExplorer.UI.Shared;
using UnityExplorer.Helpers;
#if CPP
using UnhollowerBaseLib;
#endif

namespace UnityExplorer.Inspectors.Reflection
{
    public abstract class CacheMember : CacheObjectBase
    {
        public MemberInfo MemInfo { get; set; }
        public Type DeclaringType { get; set; }
        public object DeclaringInstance { get; set; }

        public virtual bool IsStatic { get; private set; }
        
        public override bool IsMember => true;

        public override bool HasParameters => ParamCount > 0;
        public virtual int ParamCount => m_arguments.Length;

        public override bool HasEvaluated => m_evaluated;

        public string RichTextName => m_richTextName ?? GetRichTextName();
        private string m_richTextName;

        public string NameForFiltering => m_nameForFilter ?? (m_nameForFilter = $"{MemInfo.DeclaringType.Name}.{MemInfo.Name}".ToLower());
        private string m_nameForFilter;

        public override bool CanWrite => m_canWrite ?? GetCanWrite();
        private bool? m_canWrite;

        public string ReflectionException { get; set; }

        public bool m_evaluated = false;
        public bool m_isEvaluating;
        public ParameterInfo[] m_arguments = new ParameterInfo[0];
        public string[] m_argumentInput = new string[0];

        public CacheMember(MemberInfo memberInfo, object declaringInstance)
        {
            MemInfo = memberInfo;
            DeclaringType = memberInfo.DeclaringType;
            DeclaringInstance = declaringInstance;
        }

        public override void UpdateValue()
        {
            if (!HasParameters || m_isEvaluating)
            {
                try
                {
#if CPP
                    if (!IsReflectionSupported())
                        throw new Exception("Type not supported with Reflection");
#endif
                    UpdateReflection();
#if CPP
                    if (IValue.Value != null)
                        IValue.Value = IValue.Value.Il2CppCast(ReflectionHelpers.GetActualType(IValue.Value));
#endif
                }
                catch (Exception e)
                {
                    ReflectionException = ReflectionHelpers.ExceptionToString(e, true);
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
            {
                return new object[0];
            }

            var parsedArgs = new List<object>();
            for (int i = 0; i < m_arguments.Length; i++)
            {
                var input = m_argumentInput[i];
                var type = m_arguments[i].ParameterType;

                if (type.IsByRef)
                {
                    type = type.GetElementType();
                }

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
                            ExplorerCore.Log($"Argument #{i} '{m_arguments[i].Name}' ({type.Name}), could not parse input '{input}'.");
                        }
                    }
                }

                // No input, see if there is a default value.
                if (HasDefaultValue(m_arguments[i]))
                {
                    parsedArgs.Add(m_arguments[i].DefaultValue);
                    continue;
                }

                // Try add a null arg I guess
                parsedArgs.Add(null);
            }

            return parsedArgs.ToArray();
        }

        public static bool HasDefaultValue(ParameterInfo arg) => arg.DefaultValue != DBNull.Value;

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
            return m_richTextName = UISyntaxHighlight.ParseFullSyntax(MemInfo.DeclaringType, false, MemInfo);
        }

#if CPP
        internal bool IsReflectionSupported()
        {
            try
            {
                var baseType = this.IValue.ValueType;

                var gArgs = baseType.GetGenericArguments();
                if (gArgs.Length < 1)
                    return true;

                foreach (var arg in gArgs)
                {
                    if (!Check(arg))
                        return false;
                }

                return true;

                bool Check(Type type)
                {
                    if (!typeof(Il2CppSystem.Object).IsAssignableFrom(type))
                        return true;

                    if (!ReflectionHelpers.Il2CppTypeNotNull(type, out IntPtr ptr))
                        return false;

                    return Il2CppSystem.Type.internal_from_handle(IL2CPP.il2cpp_class_get_type(ptr)) is Il2CppSystem.Type;
                }
            }
            catch
            {
                return false;
            }
        }
#endif

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

        internal GameObject m_leftGroup;
        internal GameObject m_rightGroup;
        internal Text m_memLabelText;
        internal RectTransform m_topRowRect;
        internal LayoutElement m_leftLayout;
        internal LayoutElement m_rightLayout;
        internal GameObject m_subContent;

        internal override void ConstructUI()
        {
            base.ConstructUI();

            var topGroupObj = UIFactory.CreateHorizontalGroup(m_mainContent, new Color(1, 1, 1, 0));
            m_topRowRect = topGroupObj.GetComponent<RectTransform>();
            var topLayout = topGroupObj.AddComponent<LayoutElement>();
            topLayout.minHeight = 25;
            topLayout.flexibleHeight = 0;
            topLayout.minWidth = 300;
            topLayout.flexibleWidth = 5000;
            var topGroup = topGroupObj.GetComponent<HorizontalLayoutGroup>();
            topGroup.childForceExpandHeight = false;
            topGroup.childForceExpandWidth = false;
            topGroup.childControlHeight = true;
            topGroup.childControlWidth = true;
            topGroup.spacing = 10;
            topGroup.padding.left = 3;
            topGroup.padding.right = 3;
            topGroup.padding.top = 0;
            topGroup.padding.bottom = 0;

            // left group

            m_leftGroup = UIFactory.CreateHorizontalGroup(topGroupObj, new Color(1, 1, 1, 0));
            var leftLayout = m_leftGroup.AddComponent<LayoutElement>();
            leftLayout.minHeight = 25;
            leftLayout.flexibleHeight = 0;
            leftLayout.minWidth = 125;
            leftLayout.flexibleWidth = 200;
            var leftGroup = m_leftGroup.GetComponent<HorizontalLayoutGroup>();
            leftGroup.childForceExpandHeight = true;
            leftGroup.childForceExpandWidth = false;
            leftGroup.childControlHeight = true;
            leftGroup.childControlWidth = true;
            leftGroup.spacing = 4;

            // member label

            var labelObj = UIFactory.CreateLabel(m_leftGroup, TextAnchor.MiddleLeft);
            var leftRect = labelObj.GetComponent<RectTransform>();
            leftRect.anchorMin = Vector2.zero;
            leftRect.anchorMax = Vector2.one;
            leftRect.offsetMin = Vector2.zero;
            leftRect.offsetMax = Vector2.zero;
            leftRect.sizeDelta = Vector2.zero;
            m_leftLayout = labelObj.AddComponent<LayoutElement>();
            m_leftLayout.preferredWidth = 125;
            m_leftLayout.minHeight = 25;
            m_leftLayout.flexibleHeight = 100;
            var labelFitter = labelObj.AddComponent<ContentSizeFitter>();
            labelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            labelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            m_memLabelText = labelObj.GetComponent<Text>();
            m_memLabelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            m_memLabelText.text = this.RichTextName;

            // right group

            m_rightGroup = UIFactory.CreateVerticalGroup(topGroupObj, new Color(1, 1, 1, 0));
            m_rightLayout = m_rightGroup.AddComponent<LayoutElement>();
            m_rightLayout.minHeight = 25;
            m_rightLayout.flexibleHeight = 480;
            m_rightLayout.minWidth = 125;
            m_rightLayout.flexibleWidth = 5000;
            var rightGroup = m_rightGroup.GetComponent<VerticalLayoutGroup>();
            rightGroup.childForceExpandHeight = true;
            rightGroup.childForceExpandWidth = false;
            rightGroup.childControlHeight = true;
            rightGroup.childControlWidth = true;
            rightGroup.spacing = 4;
            rightGroup.padding.top = 2;
            rightGroup.padding.bottom = 2;

            // evaluate button
            if (this is CacheMethod || HasParameters)
            {
                var color = HasParameters ? new Color(0.4f, 0.4f, 0.4f) : new Color(0.3f, 0.6f, 0.3f);

                var evalButtonObj = UIFactory.CreateButton(m_rightGroup, color);
                var evalLayout = evalButtonObj.AddComponent<LayoutElement>();
                evalLayout.minWidth = 100;
                evalLayout.minHeight = 22;
                evalLayout.flexibleWidth = 0;
                var evalText = evalButtonObj.GetComponentInChildren<Text>();
                evalText.text = HasParameters
                    ? $"Evaluate ({ParamCount})"
                    : "Evaluate";

                var evalButton = evalButtonObj.GetComponent<Button>();
                var colors = evalButton.colors;
                colors.highlightedColor = new Color(0.4f, 0.7f, 0.4f);
                evalButton.colors = colors;

                evalButton.onClick.AddListener(OnMainEvaluateButton);
                void OnMainEvaluateButton()
                {
                    if (HasParameters)
                    {
                        // todo show parameter input
                        ExplorerCore.Log("TODO params");
                    }
                    else
                    {
                        // method with no parameters
                        (this as CacheMethod).Evaluate();
                    }
                }
            }

            // subcontent

            // todo check if IValue actually has subcontent

            m_subContent = UIFactory.CreateHorizontalGroup(m_parentContent, new Color(1, 1, 1, 0));
            var subGroup = m_subContent.GetComponent<HorizontalLayoutGroup>();
            subGroup.childForceExpandWidth = true;
            subGroup.childControlWidth = true;
            var subLayout = m_subContent.AddComponent<LayoutElement>();
            subLayout.minHeight = 50;
            subLayout.flexibleHeight = 500;
            subLayout.minWidth = 125;
            subLayout.flexibleWidth = 9000;

            m_subContent.SetActive(false);

            // Construct InteractiveValue UI

            IValue.ConstructUI(m_rightGroup, m_subContent);
        }

        #endregion
    }
}
