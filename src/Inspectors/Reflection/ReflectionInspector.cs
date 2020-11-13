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
using UnityExplorer.Config;

namespace UnityExplorer.Inspectors
{
    // TODO:
    // - Filters
    // - Helper tools for Target object (for UnityEngine.Objects, Components, Textures, and maybe a general ToString helper)

    public class ReflectionInspector : InspectorBase
    {
        #region STATIC

        public static ReflectionInspector ActiveInstance { get; private set; }

        static ReflectionInspector()
        {
            PanelDragger.OnFinishResize += OnContainerResized;
            SceneExplorer.OnToggleShow += OnContainerResized;
        }

        private static void OnContainerResized()
        {
            if (ActiveInstance == null)
                return;

            ActiveInstance.m_widthUpdateWanted = true;
        }

        // Blacklists
        private static readonly HashSet<string> s_typeAndMemberBlacklist = new HashSet<string>
        {
#if CPP
            // these cause a crash in IL2CPP
            "Type.DeclaringMethod",
            "Rigidbody2D.Cast",
            "Collider2D.Cast",
            "Collider2D.Raycast",
            "Texture2D.SetPixelDataImpl",
#endif
        };
        private static readonly HashSet<string> s_methodStartsWithBlacklist = new HashSet<string>
        {
            // these are redundant
            "get_",
            "set_",
        };

#endregion

#region INSTANCE

        public override string TabLabel => m_targetTypeShortName;

        public bool AutoUpdate { get; set; }

        internal readonly Type m_targetType;
        internal readonly string m_targetTypeShortName;

        // all cached members of the target
        internal CacheMember[] m_allMembers;
        // filtered members based on current filters
        internal CacheMember[] m_membersFiltered;
        // actual shortlist of displayed members
        internal readonly CacheMember[] m_displayedMembers = new CacheMember[ModConfig.Instance.Default_Page_Limit];

        // UI members

        private GameObject m_content;
        public override GameObject Content
        {
            get => m_content;
            set => m_content = value;
        }

        internal Text m_nameFilterText;

        internal PageHandler m_pageHandler;
        internal SliderScrollbar m_sliderScroller;
        internal GameObject m_scrollContent;
        internal RectTransform m_scrollContentRect;

        internal bool m_widthUpdateWanted;
        internal bool m_widthUpdateWaiting;

        // Ctor

        public ReflectionInspector(object target) : base(target)
        {
            if (this is StaticInspector)
                m_targetType = target as Type;
            else
                m_targetType = ReflectionHelpers.GetActualType(target);

            m_targetTypeShortName = UISyntaxHighlight.ParseFullSyntax(m_targetType, false);

            ConstructUI();

            CacheMembers(m_targetType);

            FilterMembers();
        }

        // Methods

        public override void SetActive()
        {
            base.SetActive();
            ActiveInstance = this;
        }

        public override void SetInactive()
        {
            base.SetInactive();
            ActiveInstance = null;
        }

        public override void Destroy()
        {
            base.Destroy();

            if (this.Content)
                GameObject.Destroy(this.Content);
        }

        private void OnPageTurned()
        {
            RefreshDisplay();
        }

        public override void Update()
        {
            base.Update();

            if (AutoUpdate)
            {
                foreach (var member in m_displayedMembers)
                {
                    member.UpdateValue();
                }
            }

            if (m_widthUpdateWanted)
            {
                if (!m_widthUpdateWaiting)
                    m_widthUpdateWaiting = true;
                else
                {
                    UpdateWidths();
                    m_widthUpdateWaiting = false;
                    m_widthUpdateWanted = false;
                }
            }
        }

        public void FilterMembers(string nameFilter = null)
        {
            var list = new List<CacheMember>();

            nameFilter = nameFilter?.ToLower() ?? m_nameFilterText.text.ToLower();

            foreach (var mem in m_allMembers)
            {
                // name filter
                if (!string.IsNullOrEmpty(nameFilter) && !mem.NameForFiltering.Contains(nameFilter))
                    continue;

                list.Add(mem);
            }

            if (m_membersFiltered == null || m_membersFiltered.Length != list.Count)
            {
                m_membersFiltered = list.ToArray();
                RefreshDisplay();
            }
        }

        public void RefreshDisplay()
        {
            var members = m_membersFiltered;
            m_pageHandler.ListCount = members.Length;

            // disable current members
            for (int i = 0; i < m_displayedMembers.Length; i++)
            {
                var mem = m_displayedMembers[i];
                if (mem != null)
                    mem.Disable();
                else
                    break;
            }

            if (members.Length < 1)
                return;

            foreach (var itemIndex in m_pageHandler)
            {
                if (itemIndex >= members.Length)
                    break;

                CacheMember member = members[itemIndex];
                m_displayedMembers[itemIndex - m_pageHandler.StartIndex] = member;
                member.Enable();
            }

            m_widthUpdateWanted = true;
        }

        internal void UpdateWidths()
        {
            float labelWidth = 125;

            foreach (var cache in m_displayedMembers)
            {
                if (cache == null)
                    break;

                var width = cache.GetMemberLabelWidth(m_scrollContentRect);

                if (width > labelWidth)
                    labelWidth = width;
            }

            float valueWidth = m_scrollContentRect.rect.width - labelWidth - 20;

            foreach (var cache in m_displayedMembers)
            {
                if (cache == null)
                    break;
                cache.SetWidths(labelWidth, valueWidth);
            }
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
                        //ExplorerCore.Log($"Trying to cache member {sig}...");
                        //ExplorerCore.Log(member.DeclaringType.FullName + "." + member.Name);

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

                        if (s_typeAndMemberBlacklist.Any(it => sig.Contains(it)))
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
                            var cached = CacheFactory.GetCacheObject(member, target, m_scrollContent);

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

            var sorted = new List<CacheMember>();
            sorted.AddRange(list.Where(x => x is CacheMethod));
            sorted.AddRange(list.Where(x => x is CacheProperty));
            sorted.AddRange(list.Where(x => x is CacheField));

            m_allMembers = sorted.ToArray();

            // ExplorerCore.Log("Cached " + m_allMembers.Length + " members");
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

            ConstructMemberList();
        }

        internal void ConstructTopArea()
        {
            var topGroupObj = UIFactory.CreateVerticalGroup(Content, new Color(0.1f, 0.1f, 0.1f));
            var topGroup = topGroupObj.GetComponent<VerticalLayoutGroup>();
            topGroup.childForceExpandWidth = true;
            topGroup.childForceExpandHeight = true;
            topGroup.childControlWidth = true;
            topGroup.childControlHeight = true;
            topGroup.spacing = 8;
            topGroup.padding.left = 4;
            topGroup.padding.right = 4;

            var nameRowObj = UIFactory.CreateHorizontalGroup(topGroupObj, new Color(1, 1, 1, 0));
            var nameRow = nameRowObj.GetComponent<HorizontalLayoutGroup>();
            nameRow.childForceExpandWidth = true;
            nameRow.childForceExpandHeight = true;
            nameRow.childControlHeight = true;
            nameRow.childControlWidth = true;
            nameRow.padding.top = 2;
            var nameRowLayout = nameRowObj.AddComponent<LayoutElement>();
            nameRowLayout.minHeight = 25;
            nameRowLayout.flexibleHeight = 0;
            nameRowLayout.minWidth = 200;
            nameRowLayout.flexibleWidth = 5000;

            var typeLabel = UIFactory.CreateLabel(nameRowObj, TextAnchor.MiddleLeft);
            var typeLabelText = typeLabel.GetComponent<Text>();
            typeLabelText.text = "Type:";
            typeLabelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            var typeLabelTextLayout = typeLabel.AddComponent<LayoutElement>();
            typeLabelTextLayout.minWidth = 40;
            typeLabelTextLayout.flexibleWidth = 0;
            typeLabelTextLayout.minHeight = 25;

            var typeDisplayObj = UIFactory.CreateLabel(nameRowObj, TextAnchor.MiddleLeft);
            var typeDisplayText = typeDisplayObj.GetComponent<Text>();
            typeDisplayText.text = UISyntaxHighlight.ParseFullSyntax(m_targetType, true);
            var typeDisplayLayout = typeDisplayObj.AddComponent<LayoutElement>();
            typeDisplayLayout.minHeight = 25;
            typeDisplayLayout.flexibleWidth = 5000;

            // Helper tools

            if (this is InstanceInspector)
            {
                (this as InstanceInspector).ConstructInstanceHelpers(topGroupObj);
            }

            // Filters

            var filterAreaObj = UIFactory.CreateVerticalGroup(topGroupObj, new Color(1,1,1,0));
            var filterLayout = filterAreaObj.AddComponent<LayoutElement>();
            filterLayout.minHeight = 60;
            var filterGroup = filterAreaObj.GetComponent<VerticalLayoutGroup>();
            filterGroup.childForceExpandWidth = true;
            filterGroup.childForceExpandHeight = false;
            filterGroup.childControlWidth = true;
            filterGroup.childControlHeight = true;
            filterGroup.spacing = 4;

            // name filter

            var nameFilterRowObj = UIFactory.CreateHorizontalGroup(filterAreaObj, new Color(1, 1, 1, 0));
            var nameFilterGroup = nameFilterRowObj.GetComponent<HorizontalLayoutGroup>();
            nameFilterGroup.childForceExpandHeight = false;
            nameFilterGroup.childForceExpandWidth = false;
            nameFilterGroup.childControlWidth = true;
            nameFilterGroup.childControlHeight = true;
            nameFilterGroup.spacing = 5;
            var nameFilterLayout = nameFilterRowObj.AddComponent<LayoutElement>();
            nameFilterLayout.minHeight = 25;
            nameFilterLayout.flexibleHeight = 0;
            nameFilterLayout.flexibleWidth = 5000;

            var nameLabelObj = UIFactory.CreateLabel(nameFilterRowObj, TextAnchor.MiddleLeft);
            var nameLabelLayout = nameLabelObj.AddComponent<LayoutElement>();
            nameLabelLayout.minWidth = 130;
            nameLabelLayout.minHeight = 25;
            nameLabelLayout.flexibleWidth = 0;
            var nameLabelText = nameLabelObj.GetComponent<Text>();
            nameLabelText.text = "Name contains:";

            var nameInputObj = UIFactory.CreateInputField(nameFilterRowObj, 14, (int)TextAnchor.MiddleLeft, (int)HorizontalWrapMode.Overflow);
            var nameInputLayout = nameInputObj.AddComponent<LayoutElement>();
            nameInputLayout.flexibleWidth = 5000;
            nameInputLayout.minWidth = 100;
            nameInputLayout.minHeight = 25;
            var nameInput = nameInputObj.GetComponent<InputField>();
            nameInput.onValueChanged.AddListener(new Action<string>((string val) => { FilterMembers(val); }));
            m_nameFilterText = nameInput.textComponent;

            // membertype filter



            // Instance filters

            if (this is InstanceInspector)
            {
                (this as InstanceInspector).ConstructInstanceFilters(filterAreaObj);
            }
        }

        internal void ConstructMemberList()
        {
            var scrollobj = UIFactory.CreateScrollView(Content, out m_scrollContent, out m_sliderScroller, new Color(0.12f, 0.12f, 0.12f));

            m_scrollContentRect = m_scrollContent.GetComponent<RectTransform>();

            var scrollGroup = m_scrollContent.GetComponent<VerticalLayoutGroup>();
            scrollGroup.spacing = 3;
            scrollGroup.padding.left = 0;
            scrollGroup.padding.right = 0;

            m_pageHandler = new PageHandler(m_sliderScroller);
            m_pageHandler.ConstructUI(Content);
            m_pageHandler.OnPageChanged += OnPageTurned;
        }

#endregion

#endregion
    }
}
