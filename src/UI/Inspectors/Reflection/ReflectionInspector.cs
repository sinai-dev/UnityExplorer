using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI.CacheObject;
using UnityExplorer.UI.Main;
using UnityExplorer.UI.Main.Home;
using UnityExplorer.UI.Utility;

namespace UnityExplorer.UI.Inspectors.Reflection
{
    public class ReflectionInspector : InspectorBase
    {
        #region STATIC

        public static ReflectionInspector ActiveInstance { get; private set; }

        static ReflectionInspector()
        {
            PanelDragger.OnFinishResize += (RectTransform _) => OnContainerResized();
            SceneExplorer.OnToggleShow += OnContainerResized;
        }

        private static void OnContainerResized(bool _ = false)
        {
            if (ActiveInstance == null)
                return;

            ActiveInstance.m_widthUpdateWanted = true;
        }

        // Blacklists
        private static readonly HashSet<string> bl_typeAndMember = new HashSet<string>
        {
#if CPP
            // these cause a crash in IL2CPP
            "Type.DeclaringMethod",
            "Rigidbody2D.Cast",
            "Collider2D.Cast",
            "Collider2D.Raycast",
            "Texture2D.SetPixelDataImpl",
            "Camera.CalculateProjectionMatrixFromPhysicalProperties",
#endif
        };
        private static readonly HashSet<string> bl_memberNameStartsWith = new HashSet<string>
        {
            // these are redundant
            "get_",
            "set_",
        };

        #endregion

        #region INSTANCE

        public override string TabLabel => m_targetTypeShortName;

        internal CacheObjectBase ParentMember { get; set; }

        internal readonly Type m_targetType;
        internal readonly string m_targetTypeShortName;

        // all cached members of the target
        internal CacheMember[] m_allMembers;
        // filtered members based on current filters
        internal readonly List<CacheMember> m_membersFiltered = new List<CacheMember>();
        // actual shortlist of displayed members
        internal readonly CacheMember[] m_displayedMembers = new CacheMember[ConfigManager.Default_Page_Limit.Value];

        internal bool m_autoUpdate;

        public ReflectionInspector(object target) : base(target)
        {
            if (this is StaticInspector)
                m_targetType = target as Type;
            else
                m_targetType = ReflectionUtility.GetActualType(target);

            m_targetTypeShortName = SignatureHighlighter.ParseFullSyntax(m_targetType, false);

            ConstructUI();

            CacheMembers(m_targetType);

            FilterMembers();
        }

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

        internal void OnPageTurned()
        {
            RefreshDisplay();
        }

        internal bool IsBlacklisted(string sig) => bl_typeAndMember.Any(it => sig.Contains(it));
        internal bool IsBlacklisted(MethodInfo method) => bl_memberNameStartsWith.Any(it => method.Name.StartsWith(it));

        internal string GetSig(MemberInfo member) => $"{member.DeclaringType.Name}.{member.Name}";
        internal string AppendArgsToSig(ParameterInfo[] args)
        {
            string ret = " (";
            foreach (var param in args)
                ret += $"{param.ParameterType.Name} {param.Name}, ";
            ret += ")";
            return ret;
        }

        public void CacheMembers(Type type)
        {
            var list = new List<CacheMember>();
            var cachedSigs = new HashSet<string>();

            var types = ReflectionUtility.GetAllBaseTypes(type);

            var flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
            if (this is InstanceInspector)
                flags |= BindingFlags.Instance;

            foreach (var declaringType in types)
            {
                var target = Target;
                target = target.Cast(declaringType);

                IEnumerable<MemberInfo> infos = declaringType.GetMethods(flags);
                infos = infos.Concat(declaringType.GetProperties(flags));
                infos = infos.Concat(declaringType.GetFields(flags));

                foreach (var member in infos)
                {
                    try
                    {
                        var sig = GetSig(member);

                        //ExplorerCore.Log($"Trying to cache member {sig}...");
                        //ExplorerCore.Log(member.DeclaringType.FullName + "." + member.Name);

                        var mi = member as MethodInfo;
                        var pi = member as PropertyInfo;
                        var fi = member as FieldInfo;

                        if (IsBlacklisted(sig) || (mi != null && IsBlacklisted(mi)))
                            continue;

                        var args = mi?.GetParameters() ?? pi?.GetIndexParameters();
                        if (args != null)
                        {
                            if (!CacheMember.CanProcessArgs(args))
                                continue;

                            sig += AppendArgsToSig(args);
                        }

                        if (cachedSigs.Contains(sig))
                            continue;

                        cachedSigs.Add(sig);

                        if (mi != null)
                            list.Add(new CacheMethod(mi, target, m_scrollContent));
                        else if (pi != null)
                            list.Add(new CacheProperty(pi, target, m_scrollContent));
                        else
                            list.Add(new CacheField(fi, target, m_scrollContent));

                        list.Last().ParentInspector = this;
                    }
                    catch (Exception e)
                    {
                        ExplorerCore.LogWarning($"Exception caching member {member.DeclaringType.FullName}.{member.Name}!");
                        ExplorerCore.Log(e.ToString());
                    }
                }
            }

            var typeList = types.ToList();

            var sorted = new List<CacheMember>();
            sorted.AddRange(list.Where(it => it is CacheMethod)
                             .OrderBy(it => typeList.IndexOf(it.DeclaringType))
                             .ThenBy(it => it.NameForFiltering));
            sorted.AddRange(list.Where(it => it is CacheProperty)
                             .OrderBy(it => typeList.IndexOf(it.DeclaringType))
                             .ThenBy(it => it.NameForFiltering));
            sorted.AddRange(list.Where(it => it is CacheField)
                             .OrderBy(it => typeList.IndexOf(it.DeclaringType))
                             .ThenBy(it => it.NameForFiltering));

            m_allMembers = sorted.ToArray();
        }

        public override void Update()
        {
            base.Update();

            if (m_autoUpdate)
            {
                foreach (var member in m_displayedMembers)
                {
                    if (member == null) break;
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

        internal void OnMemberFilterClicked(MemberTypes type, Button button)
        {
            if (m_lastActiveMemButton)
                m_lastActiveMemButton.colors = RuntimeProvider.Instance.SetColorBlock(m_lastActiveMemButton.colors, new Color(0.2f, 0.2f, 0.2f));

            m_memberFilter = type;
            m_lastActiveMemButton = button;

            m_lastActiveMemButton.colors = RuntimeProvider.Instance.SetColorBlock(m_lastActiveMemButton.colors, new Color(0.2f, 0.6f, 0.2f));

            FilterMembers(null, true);
            m_sliderScroller.m_slider.value = 1f;
        }

        public void FilterMembers(string nameFilter = null, bool force = false)
        {
            int lastCount = m_membersFiltered.Count;
            m_membersFiltered.Clear();

            nameFilter = nameFilter?.ToLower() ?? m_nameFilterText.text.ToLower();

            foreach (var mem in m_allMembers)
            {
                // membertype filter
                if (m_memberFilter != MemberTypes.All && mem.MemInfo.MemberType != m_memberFilter)
                    continue;

                if (this is InstanceInspector ii && ii.m_scopeFilter != MemberScopes.All)
                {
                    if (mem.IsStatic && ii.m_scopeFilter != MemberScopes.Static)
                        continue;
                    else if (!mem.IsStatic && ii.m_scopeFilter != MemberScopes.Instance)
                        continue;
                }

                // name filter
                if (!string.IsNullOrEmpty(nameFilter) && !mem.NameForFiltering.Contains(nameFilter))
                    continue;

                m_membersFiltered.Add(mem);
            }

            if (force || lastCount != m_membersFiltered.Count)
                RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            var members = m_membersFiltered;
            m_pageHandler.ListCount = members.Count;

            // disable current members
            for (int i = 0; i < m_displayedMembers.Length; i++)
            {
                var mem = m_displayedMembers[i];
                if (mem != null)
                    mem.Disable();
                else
                    break;
            }

            if (members.Count < 1)
                return;

            foreach (var itemIndex in m_pageHandler)
            {
                if (itemIndex >= members.Count)
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


        #endregion // end instance

        #region UI

        private GameObject m_content;
        public override GameObject Content
        {
            get => m_content;
            set => m_content = value;
        }

        internal Text m_nameFilterText;
        internal MemberTypes m_memberFilter;
        internal Button m_lastActiveMemButton;

        internal PageHandler m_pageHandler;
        internal SliderScrollbar m_sliderScroller;
        internal GameObject m_scrollContent;
        internal RectTransform m_scrollContentRect;

        internal bool m_widthUpdateWanted;
        internal bool m_widthUpdateWaiting;

        internal GameObject m_filterAreaObj;
        internal GameObject m_updateRowObj;
        internal GameObject m_memberListObj;

        internal void ConstructUI()
        {
            var parent = InspectorManager.m_inspectorContent;

            this.Content = UIFactory.CreateVerticalGroup(parent, "ReflectionInspector", true, false, true, true, 5, new Vector4(4,4,4,4),
                new Color(0.15f, 0.15f, 0.15f));

            ConstructTopArea();

            ConstructMemberList();
        }

        internal void ConstructTopArea()
        {
            var nameRowObj = UIFactory.CreateHorizontalGroup(Content, "NameRowObj", true, true, true, true, 2, default, new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(nameRowObj, minHeight: 25, flexibleHeight: 0, minWidth: 200, flexibleWidth: 5000);

            var typeLabelText = UIFactory.CreateLabel(nameRowObj, "TypeLabel", "Type:", TextAnchor.MiddleLeft);
            typeLabelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.SetLayoutElement(typeLabelText.gameObject, minWidth: 40, flexibleWidth: 0, minHeight: 25);

            var typeDisplay = UIFactory.CreateLabel(nameRowObj, "TypeDisplayText", SignatureHighlighter.ParseFullSyntax(m_targetType, true), 
                TextAnchor.MiddleLeft);

            UIFactory.SetLayoutElement(typeDisplay.gameObject, minHeight: 25, flexibleWidth: 5000);

            // instance helper tools

            if (this is InstanceInspector instanceInspector)
            {
                instanceInspector.ConstructUnityInstanceHelpers();
            }

            ConstructFilterArea();

            ConstructUpdateRow();
        }

        internal void ConstructFilterArea()
        {
            // Filters

            m_filterAreaObj = UIFactory.CreateVerticalGroup(Content, "FilterGroup", true, true, true, true, 4, new Vector4(4,4,4,4),
                new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(m_filterAreaObj, minHeight: 60);

            // name filter

            var nameFilterRowObj = UIFactory.CreateHorizontalGroup(m_filterAreaObj, "NameFilterRow", false, false, true, true, 5, default,
                new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(nameFilterRowObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 5000);

            var nameLabel = UIFactory.CreateLabel(nameFilterRowObj, "NameLabel", "Filter names:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(nameLabel.gameObject, minWidth: 100, minHeight: 25, flexibleWidth: 0);

            var nameInputObj = UIFactory.CreateInputField(nameFilterRowObj, "NameInput", "...", 14, (int)TextAnchor.MiddleLeft, (int)HorizontalWrapMode.Overflow);
            UIFactory.SetLayoutElement(nameInputObj, flexibleWidth: 5000, minWidth: 100, minHeight: 25);
            var nameInput = nameInputObj.GetComponent<InputField>();
            nameInput.onValueChanged.AddListener((string val) => { FilterMembers(val); });
            m_nameFilterText = nameInput.textComponent;

            // membertype filter

            var memberFilterRowObj = UIFactory.CreateHorizontalGroup(m_filterAreaObj, "MemberFilter", false, false, true, true, 5, default,
                new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(memberFilterRowObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 5000);

            var memLabel = UIFactory.CreateLabel(memberFilterRowObj, "MemberFilterLabel", "Filter members:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(memLabel.gameObject, minWidth: 100, minHeight: 25, flexibleWidth: 0);

            AddFilterButton(memberFilterRowObj, MemberTypes.All);
            AddFilterButton(memberFilterRowObj, MemberTypes.Method);
            AddFilterButton(memberFilterRowObj, MemberTypes.Property, true);
            AddFilterButton(memberFilterRowObj, MemberTypes.Field);

            // Instance filters

            if (this is InstanceInspector instanceInspector)
            {
                instanceInspector.ConstructInstanceFilters(m_filterAreaObj);
            }
        }

        private void AddFilterButton(GameObject parent, MemberTypes type, bool setEnabled = false)
        {
            var btn = UIFactory.CreateButton(parent,
                "FilterButton_" + type, 
                type.ToString(), 
                null,
                new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(btn.gameObject, minHeight: 25, minWidth: 70);
            btn.onClick.AddListener(() => { OnMemberFilterClicked(type, btn); });

            btn.colors = RuntimeProvider.Instance.SetColorBlock(btn.colors, highlighted: new Color(0.3f, 0.7f, 0.3f));

            if (setEnabled)
            {
                btn.colors = RuntimeProvider.Instance.SetColorBlock(btn.colors, new Color(0.2f, 0.6f, 0.2f));
                m_memberFilter = type;
                m_lastActiveMemButton = btn;
            }
        }

        internal void ConstructUpdateRow()
        {
            m_updateRowObj = UIFactory.CreateHorizontalGroup(Content, "UpdateRow", false, true, true, true, 10, default, new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(m_updateRowObj, minHeight: 25);

            // update button

            var updateBtn = UIFactory.CreateButton(m_updateRowObj, "UpdateButton", "Update Values", null, new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(updateBtn.gameObject, minWidth: 110, flexibleWidth: 0);
            updateBtn.onClick.AddListener(() =>
            {
                bool orig = m_autoUpdate;
                m_autoUpdate = true;
                Update();
                if (!orig) 
                    m_autoUpdate = orig;
            });

            // auto update

            var autoUpdateObj = UIFactory.CreateToggle(m_updateRowObj, "UpdateToggle", out Toggle autoUpdateToggle, out Text autoUpdateText);
            var autoUpdateLayout = autoUpdateObj.AddComponent<LayoutElement>();
            autoUpdateLayout.minWidth = 150;
            autoUpdateLayout.minHeight = 25;
            autoUpdateText.text = "Auto-update?";
            autoUpdateToggle.isOn = false;
            autoUpdateToggle.onValueChanged.AddListener((bool val) => { m_autoUpdate = val; });
        }

        internal void ConstructMemberList()
        {
            m_memberListObj = UIFactory.CreateScrollView(Content, "MemberList", out m_scrollContent, out m_sliderScroller, new Color(0.05f, 0.05f, 0.05f));

            m_scrollContentRect = m_scrollContent.GetComponent<RectTransform>();

            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(m_scrollContent, forceHeight: true, spacing: 3, padLeft: 0, padRight: 0);

            m_pageHandler = new PageHandler(m_sliderScroller);
            m_pageHandler.ConstructUI(Content);
            m_pageHandler.OnPageChanged += OnPageTurned;
        }
    }

    #endregion
}