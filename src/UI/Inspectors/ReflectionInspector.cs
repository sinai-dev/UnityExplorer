using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Explorer.UI;
using Explorer.UI.Shared;
using Explorer.CacheObject;
using Explorer.UI.Inspectors;
using Explorer.Helpers;
#if CPP
using UnhollowerBaseLib;
#endif

namespace Explorer.UI.Inspectors
{
    public abstract class ReflectionInspector : WindowBase
    {
        public override string Title => WindowManager.TabView
            ? $"<color=cyan>[R]</color> {TargetType.Name}"
            : $"Reflection Inspector ({TargetType.Name})";

        public virtual bool IsStaticInspector { get; }

        public Type TargetType;

        private CacheMember[] m_allCachedMembers;
        private CacheMember[] m_cachedMembersFiltered;

        public PageHelper Pages = new PageHelper();

        private bool m_autoUpdate = false;
        private string m_search = "";
        public MemberTypes m_typeFilter = MemberTypes.Property;
        private bool m_hideFailedReflection = false;

        private MemberScopes m_scopeFilter;
        private enum MemberScopes
        {
            Both,
            Instance,
            Static
        }

        private static readonly HashSet<string> _typeAndMemberBlacklist = new HashSet<string>
        {
            // Causes a crash
            "Type.DeclaringMethod",
            // Causes a crash
            "Rigidbody2D.Cast",
        };

        private static readonly HashSet<string> _methodStartsWithBlacklist = new HashSet<string>
        {
            // Pointless (handled by Properties)
            "get_",
            "set_",
        };

        public override void Init()
        {
            if (!IsStaticInspector)
            {
                TargetType = ReflectionHelpers.GetActualType(Target);
                CacheMembers(ReflectionHelpers.GetAllBaseTypes(Target));
            }
            else
            {
                CacheMembers(new Type[] { TargetType });
            }
        }

        public override void Update()
        {
            if (m_allCachedMembers == null)
            {
                return;
            }

            m_cachedMembersFiltered = m_allCachedMembers.Where(x => ShouldProcessMember(x)).ToArray();

            if (m_autoUpdate)
            {
                UpdateValues();
            }
        }

        private void UpdateValues()
        {
            foreach (var member in m_cachedMembersFiltered)
            {
                member.UpdateValue();
            }
        }

        public virtual bool ShouldProcessMember(CacheMember holder)
        {
            // check MemberTypes filter
            if (m_typeFilter != MemberTypes.All && m_typeFilter != holder.MemInfo?.MemberType)
                return false;

            // check scope filter
            if (m_scopeFilter == MemberScopes.Instance)
            {
                return !holder.IsStatic;
            }
            else if (m_scopeFilter == MemberScopes.Static)
            {
                return holder.IsStatic;
            }

            // hide failed reflection
            if (!string.IsNullOrEmpty(holder.ReflectionException) && m_hideFailedReflection)
                return false;

            // see if we should do name search
            if (m_search == "" || holder.MemInfo == null)
                return true;

            // ok do name search
            return (holder.MemInfo.DeclaringType.Name + "." + holder.MemInfo.Name)
                    .ToLower()
                    .Contains(m_search.ToLower());
        }

        private void CacheMembers(Type[] types)
        {
            var list = new List<CacheMember>();
            var cachedSigs = new List<string>();

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

                object target = Target;
                string exception = null;

#if CPP
                if (!IsStaticInspector && target is Il2CppSystem.Object ilObject)
                {
                    try
                    {
                        target = ilObject.Il2CppCast(declaringType);
                    }
                    catch (Exception e)
                    {
                        exception = ReflectionHelpers.ExceptionToString(e);
                    }
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

                        var fi = member as FieldInfo;
                        var pi = member as PropertyInfo;
                        var mi = member as MethodInfo;

                        if (IsStaticInspector)
                        {
                            if (fi != null && !fi.IsStatic) continue;
                            else if (pi != null && !pi.GetAccessors()[0].IsStatic) continue;
                            else if (mi != null && !mi.IsStatic) continue;
                        }

                        // check blacklisted members
                        var sig = $"{member.DeclaringType.Name}.{member.Name}";
                        if (_typeAndMemberBlacklist.Any(it => it == sig))
                            continue;

                        if (_methodStartsWithBlacklist.Any(it => member.Name.StartsWith(it)))
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
                            var cached = CacheFactory.GetCacheObject(member, target);

                            if (cached != null)
                            {
                                cachedSigs.Add(sig);
                                list.Add(cached);

                                if (string.IsNullOrEmpty(cached.ReflectionException))
                                {
                                    cached.ReflectionException = exception;
                                }
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

            m_allCachedMembers = list.ToArray();
        }

        // =========== GUI DRAW =========== //

        public override void WindowFunction(int windowID)
        {
            try
            {
                // ====== HEADER ======

                var rect = WindowManager.TabView ? TabViewWindow.Instance.m_rect : this.m_rect;

                if (!WindowManager.TabView)
                {
                    Header();
                    GUIUnstrip.BeginArea(new Rect(5, 25, rect.width - 10, rect.height - 35), GUI.skin.box);
                }

                var asInstance = this as InstanceInspector;

                GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
                var labelWidth = (asInstance != null && asInstance.m_uObj)
                    ? new GUILayoutOption[] { GUILayout.Width(245f) }
                    : new GUILayoutOption[0];
                GUILayout.Label("<b>Type:</b> <color=cyan>" + TargetType.FullName + "</color>", labelWidth);
                GUILayout.EndHorizontal();

                if (asInstance != null)
                {
                    asInstance.DrawInstanceControls(rect);
                }

                UIStyles.HorizontalLine(Color.grey);

                GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
                GUILayout.Label("<b>Search:</b>", new GUILayoutOption[] { GUILayout.Width(75) });
                m_search = GUIUnstrip.TextField(m_search, new GUILayoutOption[0]);
                GUILayout.EndHorizontal();

                GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
                GUILayout.Label("<b>Filter:</b>", new GUILayoutOption[] { GUILayout.Width(75) });
                FilterTypeToggle(MemberTypes.All, "All");
                FilterTypeToggle(MemberTypes.Property, "Properties");
                FilterTypeToggle(MemberTypes.Field, "Fields");
                FilterTypeToggle(MemberTypes.Method, "Methods");
                GUILayout.EndHorizontal();

                if (this is InstanceInspector)
                {
                    GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
                    GUILayout.Label("<b>Scope:</b>", new GUILayoutOption[] { GUILayout.Width(75) });
                    FilterScopeToggle(MemberScopes.Both, "Both");
                    FilterScopeToggle(MemberScopes.Instance, "Instance");
                    FilterScopeToggle(MemberScopes.Static, "Static");
                    GUILayout.EndHorizontal();
                }

                GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
                GUILayout.Label("<b>Values:</b>", new GUILayoutOption[] { GUILayout.Width(75) });
                if (GUILayout.Button("Update", new GUILayoutOption[] { GUILayout.Width(100) }))
                {
                    UpdateValues();
                }
                GUI.color = m_autoUpdate ? Color.green : Color.red;
                m_autoUpdate = GUILayout.Toggle(m_autoUpdate, "Auto-update?", new GUILayoutOption[] { GUILayout.Width(100) });
                GUI.color = m_hideFailedReflection ? Color.green : Color.red;
                m_hideFailedReflection = GUILayout.Toggle(m_hideFailedReflection, "Hide failed Reflection?", new GUILayoutOption[] { GUILayout.Width(150) });
                GUI.color = Color.white;
                GUILayout.EndHorizontal();

                GUIUnstrip.Space(10);

                Pages.ItemCount = m_cachedMembersFiltered.Length;

                // prev/next page buttons
                GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);

                Pages.DrawLimitInputArea();

                if (Pages.ItemCount > Pages.ItemsPerPage)
                {
                    if (GUILayout.Button("< Prev", new GUILayoutOption[] { GUILayout.Width(80) }))
                    {
                        Pages.TurnPage(Turn.Left, ref this.scroll);
                    }

                    Pages.CurrentPageLabel();

                    if (GUILayout.Button("Next >", new GUILayoutOption[] { GUILayout.Width(80) }))
                    {
                        Pages.TurnPage(Turn.Right, ref this.scroll);
                    }
                }
                GUILayout.EndHorizontal();

                // ====== BODY ======

                scroll = GUIUnstrip.BeginScrollView(scroll);

                GUIUnstrip.Space(10);

                UIStyles.HorizontalLine(Color.grey);

                GUIUnstrip.BeginVertical(GUIContent.none, GUI.skin.box, null);

                var members = this.m_cachedMembersFiltered;
                int start = Pages.CalculateOffsetIndex();

                for (int j = start; (j < start + Pages.ItemsPerPage && j < members.Length); j++)
                {
                    var holder = members[j];

                    GUIUnstrip.BeginHorizontal(new GUILayoutOption[] { GUILayout.Height(25) });
                    try
                    {
                        holder.Draw(rect, 180f);
                    }
                    catch
                    {
                        GUILayout.EndHorizontal();
                        continue;
                    }
                    GUILayout.EndHorizontal();

                    // if not last element
                    if (!(j == (start + Pages.ItemsPerPage - 1) || j == (members.Length - 1)))
                        UIStyles.HorizontalLine(new Color(0.07f, 0.07f, 0.07f), true);
                }

                GUILayout.EndVertical();
                GUIUnstrip.EndScrollView();

                if (!WindowManager.TabView)
                {
                    m_rect = ResizeDrag.ResizeWindow(rect, windowID);

                    GUIUnstrip.EndArea();
                }
            }
            catch (Exception e) when (e.Message.Contains("in a group with only"))
            {
                // suppress
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning("Exception drawing ReflectionWindow: " + e.GetType() + ", " + e.Message);
                DestroyWindow();
                return;
            }
        }

        private void FilterTypeToggle(MemberTypes mode, string label)
        {
            if (m_typeFilter == mode)
            {
                GUI.color = Color.green;
            }
            else
            {
                GUI.color = Color.white;
            }
            if (GUILayout.Button(label, new GUILayoutOption[] { GUILayout.Width(100) }))
            {
                m_typeFilter = mode;
                Pages.PageOffset = 0;
                scroll = Vector2.zero;
            }
            GUI.color = Color.white;
        }

        private void FilterScopeToggle(MemberScopes mode, string label)
        {
            if (m_scopeFilter == mode)
            {
                GUI.color = Color.green;
            }
            else
            {
                GUI.color = Color.white;
            }
            if (GUILayout.Button(label, new GUILayoutOption[] { GUILayout.Width(100) }))
            {
                m_scopeFilter = mode;
                Pages.PageOffset = 0;
                scroll = Vector2.zero;
            }
            GUI.color = Color.white;
        }
    }
}
