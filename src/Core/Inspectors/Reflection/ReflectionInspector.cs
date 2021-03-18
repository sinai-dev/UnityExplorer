using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityExplorer.Core.Unity;
using UnityEngine;
using UnityExplorer.Core.Inspectors.Reflection;
using UnityExplorer.UI.Reusable;
using System.Reflection;
using UnityExplorer.UI;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.Core;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.Main.Home.Inspectors;

namespace UnityExplorer.Core.Inspectors
{
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

            ActiveInstance.ReflectionUI.m_widthUpdateWanted = true;
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

        internal ReflectionInspectorUI ReflectionUI;

        internal readonly Type m_targetType;
        internal readonly string m_targetTypeShortName;

        // all cached members of the target
        internal CacheMember[] m_allMembers;
        // filtered members based on current filters
        internal readonly List<CacheMember> m_membersFiltered = new List<CacheMember>();
        // actual shortlist of displayed members
        internal readonly CacheMember[] m_displayedMembers = new CacheMember[ExplorerConfig.Instance.Default_Page_Limit];

        internal bool m_autoUpdate;

        public ReflectionInspector(object target) : base(target)
        {
            if (this is StaticInspector)
                m_targetType = target as Type;
            else
                m_targetType = ReflectionUtility.GetType(target);

            m_targetTypeShortName = SignatureHighlighter.ParseFullSyntax(m_targetType, false);

            ReflectionUI.ConstructUI();

            CacheMembers(m_targetType);

            FilterMembers();
        }

        public override void CreateUIModule()
        {
            BaseUI = ReflectionUI = new ReflectionInspectorUI(this);
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

            if (this.BaseUI.Content)
                GameObject.Destroy(this.BaseUI.Content);
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
                            list.Add(new CacheMethod(mi, target, ReflectionUI.m_scrollContent));
                        else if (pi != null)
                            list.Add(new CacheProperty(pi, target, ReflectionUI.m_scrollContent));
                        else
                            list.Add(new CacheField(fi, target, ReflectionUI.m_scrollContent));

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

            if (ReflectionUI.m_widthUpdateWanted)
            {
                if (!ReflectionUI.m_widthUpdateWaiting)
                    ReflectionUI.m_widthUpdateWaiting = true;
                else
                {
                    UpdateWidths();
                    ReflectionUI.m_widthUpdateWaiting = false;
                    ReflectionUI.m_widthUpdateWanted = false;
                }
            }
        }

        internal void OnMemberFilterClicked(MemberTypes type, Button button)
        {
            if (ReflectionUI.m_lastActiveMemButton)
            {
                var lastColors = ReflectionUI.m_lastActiveMemButton.colors;
                lastColors.normalColor = new Color(0.2f, 0.2f, 0.2f);
                ReflectionUI.m_lastActiveMemButton.colors = lastColors;
            }

            ReflectionUI.m_memberFilter = type;
            ReflectionUI.m_lastActiveMemButton = button;

            var colors = ReflectionUI.m_lastActiveMemButton.colors;
            colors.normalColor = new Color(0.2f, 0.6f, 0.2f);
            ReflectionUI.m_lastActiveMemButton.colors = colors;

            FilterMembers(null, true);
            ReflectionUI.m_sliderScroller.m_slider.value = 1f;
        }

        public void FilterMembers(string nameFilter = null, bool force = false)
        {
            int lastCount = m_membersFiltered.Count;
            m_membersFiltered.Clear();

            nameFilter = nameFilter?.ToLower() ?? ReflectionUI.m_nameFilterText.text.ToLower();

            foreach (var mem in m_allMembers)
            {
                // membertype filter
                if (ReflectionUI.m_memberFilter != MemberTypes.All && mem.MemInfo.MemberType != ReflectionUI.m_memberFilter)
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
            ReflectionUI.m_pageHandler.ListCount = members.Count;

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

            foreach (var itemIndex in ReflectionUI.m_pageHandler)
            {
                if (itemIndex >= members.Count)
                    break;

                CacheMember member = members[itemIndex];
                m_displayedMembers[itemIndex - ReflectionUI.m_pageHandler.StartIndex] = member;
                member.Enable();
            }

            ReflectionUI.m_widthUpdateWanted = true;
        }

        internal void UpdateWidths()
        {
            float labelWidth = 125;

            foreach (var cache in m_displayedMembers)
            {
                if (cache == null)
                    break;

                var width = cache.GetMemberLabelWidth(ReflectionUI.m_scrollContentRect);

                if (width > labelWidth)
                    labelWidth = width;
            }

            float valueWidth = ReflectionUI.m_scrollContentRect.rect.width - labelWidth - 20;

            foreach (var cache in m_displayedMembers)
            {
                if (cache == null)
                    break;
                cache.SetWidths(labelWidth, valueWidth);
            }
        }


        #endregion // end instance
    }
}
