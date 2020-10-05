using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if CPP
using UnhollowerBaseLib;
#endif

namespace Explorer
{
    public class ReflectionWindow : UIWindow
    {
        public override string Title => WindowManager.TabView
            ? $"<color=cyan>[R]</color> {TargetType.Name}" 
            : $"Reflection Inspector ({TargetType.Name})";

        public Type TargetType;

        private CacheObjectBase[] m_allCachedMembers;
        private CacheObjectBase[] m_cachedMembersFiltered;

        public PageHelper Pages = new PageHelper();

        private bool m_autoUpdate = false;
        private string m_search = "";
        public MemberTypes m_filter = MemberTypes.Property;
        private bool m_hideFailedReflection = false;

        // some extra cast-caching
        private UnityEngine.Object m_uObj;
        private Component m_component;

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
            TargetType = ReflectionHelpers.GetActualType(Target);

            CacheMembers(ReflectionHelpers.GetAllBaseTypes(Target));

            // cache the extra cast-caching
#if CPP
            if (Target is Il2CppSystem.Object ilObject)
            {
                var unityObj = ilObject.TryCast<UnityEngine.Object>();
                if (unityObj)
                {
                    m_uObj = unityObj;

                    var component = ilObject.TryCast<Component>();
                    if (component)
                    {
                        m_component = component;
                    }
                }
            }
#else
            m_uObj = Target as UnityEngine.Object;
            m_component = Target as Component;
#endif
        }

        public override void Update()
        {
            if (Target == null)
            {
                ExplorerCore.Log("Target is null!");
                DestroyWindow();
                return;
            }
            if (Target is UnityEngine.Object uObj)
            {
                if (!uObj)
                {
                    ExplorerCore.Log("Target was destroyed!");
                    DestroyWindow();
                    return;
                }
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

        private bool ShouldProcessMember(CacheObjectBase holder)
        {
            // check MemberTypes filter
            if (m_filter != MemberTypes.All && m_filter != holder.MemInfo?.MemberType) 
                return false;

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
            var list = new List<CacheObjectBase>();
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
                if (target is Il2CppSystem.Object ilObject)
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
                    // make sure member type is Field, Method of Property (4 / 8 / 16)
                    int m = (int)member.MemberType;
                    if (m < 4 || m > 16)
                        continue;

                    // check blacklisted members
                    var sig = $"{member.DeclaringType.Name}.{member.Name}";
                    if (_typeAndMemberBlacklist.Any(it => it == sig))
                        continue;

                    if (_methodStartsWithBlacklist.Any(it => member.Name.StartsWith(it)))
                        continue;

                    if (member is MethodInfo mi)
                    {
                        AppendParams(mi.GetParameters());
                    }
                    else if (member is PropertyInfo pi)
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

                    //ExplorerCore.Log($"Trying to cache member {sig}...");

                    try
                    {
                        var cached = CacheFactory.GetTypeAndCacheObject(member, target);
                        if (cached != null)
                        {
                            cachedSigs.Add(sig);
                            list.Add(cached);
                            cached.ReflectionException = exception;
                        }
                    }
                    catch (Exception e)
                    {
                        ExplorerCore.LogWarning($"Exception caching member {sig}!");
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

                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                GUILayout.Label("<b>Type:</b> <color=cyan>" + TargetType.FullName + "</color>", new GUILayoutOption[] { GUILayout.Width(245f) });
                if (m_uObj)
                {
                    GUILayout.Label("Name: " + m_uObj.name, new GUILayoutOption[0]);
                }
                GUILayout.EndHorizontal();

                if (m_uObj)
                {
                    GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                    GUILayout.Label("<b>Tools:</b>", new GUILayoutOption[] { GUILayout.Width(80) });
                    UIHelpers.InstantiateButton(m_uObj);
                    if (m_component && m_component.gameObject is GameObject obj)
                    {
                        GUI.skin.label.alignment = TextAnchor.MiddleRight;
                        GUILayout.Label("GameObject:", new GUILayoutOption[] { GUILayout.Width(135) });
                        var charWidth = obj.name.Length * 15;
                        var maxWidth = rect.width - 350;
                        var labelWidth = charWidth < maxWidth ? charWidth : maxWidth; 
                        if (GUILayout.Button("<color=#00FF00>" + obj.name + "</color>", new GUILayoutOption[] { GUILayout.Width(labelWidth) }))
                        {
                            WindowManager.InspectObject(obj, out bool _);
                        }
                        GUI.skin.label.alignment = TextAnchor.UpperLeft;
                    }
                    GUILayout.EndHorizontal();
                }

                UIStyles.HorizontalLine(Color.grey);

                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                GUILayout.Label("<b>Search:</b>", new GUILayoutOption[] { GUILayout.Width(75) });
                m_search = GUIUnstrip.TextField(m_search, new GUILayoutOption[0]);                
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                GUILayout.Label("<b>Filter:</b>", new GUILayoutOption[] { GUILayout.Width(75) });
                FilterToggle(MemberTypes.All, "All");
                FilterToggle(MemberTypes.Property, "Properties");
                FilterToggle(MemberTypes.Field, "Fields");
                FilterToggle(MemberTypes.Method, "Methods");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
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
                GUILayout.BeginHorizontal(new GUILayoutOption[0]);

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

                GUILayout.BeginVertical(GUIContent.none, GUI.skin.box, null);

                var members = this.m_cachedMembersFiltered;
                int start = Pages.CalculateOffsetIndex();

                for (int j = start; (j < start + Pages.ItemsPerPage && j < members.Length); j++)
                {
                    var holder = members[j];

                    GUILayout.BeginHorizontal(new GUILayoutOption[] { GUILayout.Height(25) });
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

        private void FilterToggle(MemberTypes mode, string label)
        {
            if (m_filter == mode)
            {
                GUI.color = Color.green;
            }
            else
            {
                GUI.color = Color.white;
            }
            if (GUILayout.Button(label, new GUILayoutOption[] { GUILayout.Width(100) }))
            {
                m_filter = mode;
                Pages.PageOffset = 0;
                scroll = Vector2.zero;
            }
            GUI.color = Color.white;
        }
    }
}
