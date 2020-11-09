using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityExplorer.Helpers;
using UnityEngine;
using UnityExplorer.Inspectors.Reflection;
using UnityExplorer.UI.Shared;
using System.Reflection;
using UnityExplorer.UI;
using UnityEngine.UI;
using TMPro;

namespace UnityExplorer.Inspectors
{
    public class ReflectionInspector : InspectorBase
    {
        public override string TabLabel => m_targetTypeShortName;

        internal readonly Type m_targetType;
        internal readonly string m_targetTypeShortName;

        // all cached members of the target
        internal CacheMember[] m_allMembers;
        // filtered members based on current filters
        internal CacheMember[] m_membersFiltered;
        // actual shortlist of displayed members
        internal CacheMember[] m_membersShortlist;

        // UI members

        private GameObject m_content;
        public override GameObject Content
        {
            get => m_content;
            set => m_content = value;
        }

        internal PageHandler m_pageHandler;

        // Blacklists
        private static readonly HashSet<string> s_typeAndMemberBlacklist = new HashSet<string>
        {
            "Type.DeclaringMethod",
            "Rigidbody2D.Cast",
        };
        private static readonly HashSet<string> s_methodStartsWithBlacklist = new HashSet<string>
        {
            "get_",
            "set_",
        };

        // Ctor

        public ReflectionInspector(object target) : base(target)
        {
            if (this is StaticInspector)
                m_targetType = target as Type;
            else
                m_targetType = ReflectionHelpers.GetActualType(target);

            m_targetTypeShortName = m_targetType.Name;

            CacheMembers(m_targetType);

            ConstructUI();
        }

        // Methods

        public override void Update()
        {
            base.Update();

            // todo
        }

        public override void Destroy()
        {
            base.Destroy();

            if (this.Content)
                GameObject.Destroy(this.Content);
        }

        public void CacheMembers(Type type)
        {
            var list = new List<CacheMember>();
            var cachedSigs = new HashSet<string>();

            var types = ReflectionHelpers.GetAllBaseTypes(type);

            foreach (var declaringType in types)
            {
                MemberInfo[] infos;
                try
                {
                    infos = declaringType.GetMembers(ReflectionHelpers.CommonFlags);
                }
                catch
                {
                    ExplorerCore.Log($"Exception getting members for type: {declaringType.FullName}");
                    continue;
                }

                var target = Target;
#if CPP
                try
                {
                    target = target.Il2CppCast(declaringType);
                }
                catch //(Exception e)
                {
                    //ExplorerCore.LogWarning("Excepting casting " + target.GetType().FullName + " to " + declaringType.FullName);
                }
#endif

                foreach (var member in infos)
                {
                    try
                    {
                        // make sure member type is Field, Method or Property (4 / 8 / 16)
                        int m = (int)member.MemberType;
                        if (m < 4 || m > 16)
                            continue;

                        var pi = member as PropertyInfo;
                        var mi = member as MethodInfo;

                        if (this is StaticInspector)
                        {
                            if (member is FieldInfo fi && !fi.IsStatic) continue;
                            else if (pi != null && !pi.GetAccessors(true)[0].IsStatic) continue;
                            else if (mi != null && !mi.IsStatic) continue;
                        }

                        // check blacklisted members
                        var sig = $"{member.DeclaringType.Name}.{member.Name}";
                        if (s_typeAndMemberBlacklist.Any(it => it == sig))
                            continue;

                        if (s_methodStartsWithBlacklist.Any(it => member.Name.StartsWith(it)))
                            continue;

                        if (mi != null)
                        {
                            AppendParams(mi.GetParameters());
                        }
                        else if (pi != null)
                        {
                            AppendParams(pi.GetIndexParameters());
                        }

                        void AppendParams(ParameterInfo[] _args)
                        {
                            sig += " (";
                            foreach (var param in _args)
                            {
                                sig += $"{param.ParameterType.Name} {param.Name}, ";
                            }
                            sig += ")";
                        }

                        if (cachedSigs.Contains(sig))
                        {
                            continue;
                        }

                        try
                        {
                            //ExplorerCore.Log($"Trying to cache member {sig}...");

                            var cached = CacheFactory.GetCacheObject(member, target);

                            if (cached != null)
                            {
                                cachedSigs.Add(sig);
                                list.Add(cached);
                            }
                        }
                        catch (Exception e)
                        {
                            ExplorerCore.LogWarning($"Exception caching member {sig}!");
                            ExplorerCore.Log(e.ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        ExplorerCore.LogWarning($"Exception caching member {member.DeclaringType.FullName}.{member.Name}!");
                        ExplorerCore.Log(e.ToString());
                    }
                }
            }

            m_allMembers = list.ToArray();

            ExplorerCore.Log("Cached " + m_allMembers.Length + " members");
        }

        #region UI CONSTRUCTION

        internal void ConstructUI()
        {
            var parent = InspectorManager.Instance.m_inspectorContent;
            this.Content = UIFactory.CreateVerticalGroup(parent, new Color(0.15f, 0.15f, 0.15f));
            var mainGroup = Content.GetComponent<VerticalLayoutGroup>();
            mainGroup.childForceExpandHeight = false;
            mainGroup.childForceExpandWidth = true;
            mainGroup.childControlHeight = true;
            mainGroup.childControlWidth = true;
            mainGroup.spacing = 5;
            mainGroup.padding.top = 4;
            mainGroup.padding.left = 4;
            mainGroup.padding.right = 4;
            mainGroup.padding.bottom = 4;

            ConstructTopArea();

            ConstructFilterArea();

            ConstructMemberList();
        }

        internal void ConstructTopArea()
        {
            var typeRowObj = UIFactory.CreateHorizontalGroup(Content, new Color(1, 1, 1, 0));
            var typeRowGroup = typeRowObj.GetComponent<HorizontalLayoutGroup>();
            typeRowGroup.childForceExpandWidth = true;
            typeRowGroup.childForceExpandHeight = true;
            typeRowGroup.childControlHeight = true;
            typeRowGroup.childControlWidth = true;
            var typeRowLayout = typeRowObj.AddComponent<LayoutElement>();
            typeRowLayout.minHeight = 25;
            typeRowLayout.flexibleHeight = 0;
            typeRowLayout.minWidth = 200;
            typeRowLayout.flexibleWidth = 5000;

            var typeLabel = UIFactory.CreateLabel(typeRowObj, TextAnchor.MiddleLeft);
            var typeLabelText = typeLabel.GetComponent<Text>();
            typeLabelText.text = "Type:";
            var typeLabelTextLayout = typeLabel.AddComponent<LayoutElement>();
            typeLabelTextLayout.minWidth = 60;
            typeLabelTextLayout.flexibleWidth = 0;
            typeLabelTextLayout.minHeight = 25;

            var typeLabelInputObj = UIFactory.CreateTMPInput(typeRowObj, 14, 0, (int)TextAlignmentOptions.MidlineLeft);
            var typeLabelInput = typeLabelInputObj.GetComponent<TMP_InputField>();
            typeLabelInput.readOnly = true;
            var typeLabelLayout = typeLabelInputObj.AddComponent<LayoutElement>();
            typeLabelLayout.minWidth = 150;
            typeLabelLayout.flexibleWidth = 5000;

            string classColor = SyntaxColors.Class_Instance;
            if (m_targetType.IsSealed && m_targetType.IsAbstract)
                classColor = SyntaxColors.Class_Static;
            else if (m_targetType.IsValueType)
                classColor = SyntaxColors.StructGreen;

            typeLabelInput.text = $"<color=grey>{m_targetType.Namespace}.</color><color={classColor}>{m_targetType.Name}</color>";
        }

        internal void ConstructFilterArea()
        {

        }

        internal void ConstructMemberList()
        {
            // TEMPORARY 

            var scrollobj = UIFactory.CreateScrollView(Content, out GameObject scrollContent, out SliderScrollbar scroller, new Color(0.1f, 0.1f, 0.1f));

            foreach (var member in this.m_allMembers)
            {
                var rowObj = UIFactory.CreateHorizontalGroup(scrollContent, new Color(1, 1, 1, 0));
                var rowGroup = rowObj.GetComponent<HorizontalLayoutGroup>();
                rowGroup.childForceExpandWidth = true;
                rowGroup.childControlWidth = true;
                var rowLayout = rowObj.AddComponent<LayoutElement>();
                rowLayout.minHeight = 25;
                rowLayout.flexibleHeight = 0;
                rowLayout.minWidth = 125;
                rowLayout.flexibleWidth = 9000;

                var labelObj = UIFactory.CreateLabel(rowObj, TextAnchor.MiddleLeft);

                var label = labelObj.GetComponent<Text>();
                label.text = member.RichTextName;

            }
        }

        #endregion
    }
}
