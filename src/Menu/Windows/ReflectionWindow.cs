using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MelonLoader;
using UnhollowerBaseLib;
using UnityEngine;

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
        }

        public override void Update()
        {
            if (Target == null)
            {
                DestroyWindow();
                return;
            }
            else if (Target is UnityEngine.Object uObj)
            {
                if (!uObj)
                {
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
                    MelonLogger.Log($"Exception getting members for type: {declaringType.FullName}");
                    continue;
                }

                object target = Target;
                string exception = null;

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

                foreach (var member in infos)
                {
                    // make sure member type is Field, Method of Property (4 / 8 / 16)
                    int m = (int)member.MemberType;
                    if (m < 4 || m > 16)
                        continue;

                    // check blacklisted members
                    var name = member.DeclaringType.Name + "." + member.Name;
                    if (_typeAndMemberBlacklist.Any(it => it == name))
                        continue;

                    if (_methodStartsWithBlacklist.Any(it => member.Name.StartsWith(it)))
                        continue;

                    // compare signature to already cached members
                    var signature = $"{member.DeclaringType.Name}.{member.Name}";
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
                        signature += " (";
                        foreach (var param in _args)
                        {
                            signature += $"{param.ParameterType.Name} {param.Name}, ";
                        }
                        signature += ")";
                    }

                    if (cachedSigs.Contains(signature))
                    {
                        continue;
                    }

                    // MelonLogger.Log($"Trying to cache member {signature}...");

                    try
                    {
                        var cached = CacheObjectBase.GetCacheObject(member, target);
                        if (cached != null)
                        {
                            cachedSigs.Add(signature);
                            list.Add(cached);
                            cached.ReflectionException = exception;
                        }
                    }
                    catch (Exception e)
                    {
                        MelonLogger.LogWarning($"Exception caching member {signature}!");
                        MelonLogger.Log(e.ToString());
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

                GUIUnstrip.BeginHorizontal();
                GUIUnstrip.Label("<b>Type:</b> <color=cyan>" + TargetType.FullName + "</color>", new GUILayoutOption[] { GUILayout.Width(245f) });
                if (m_uObj)
                {
                    GUIUnstrip.Label("Name: " + m_uObj.name);
                }
                GUIUnstrip.EndHorizontal();

                if (m_uObj)
                {
                    GUIUnstrip.BeginHorizontal();
                    GUIUnstrip.Label("<b>Tools:</b>", new GUILayoutOption[] { GUILayout.Width(80) });
                    UIHelpers.InstantiateButton(m_uObj);
                    if (m_component && m_component.gameObject is GameObject obj)
                    {
                        GUI.skin.label.alignment = TextAnchor.MiddleRight;
                        GUIUnstrip.Label("GameObject:", new GUILayoutOption[] { GUILayout.Width(135) });
                        var charWidth = obj.name.Length * 15;
                        var maxWidth = rect.width - 350;
                        var labelWidth = charWidth < maxWidth ? charWidth : maxWidth; 
                        if (GUIUnstrip.Button("<color=#00FF00>" + obj.name + "</color>", new GUILayoutOption[] { GUILayout.Width(labelWidth) }))
                        {
                            WindowManager.InspectObject(obj, out bool _);
                        }
                        GUI.skin.label.alignment = TextAnchor.UpperLeft;
                    }
                    GUIUnstrip.EndHorizontal();
                }

                UIStyles.HorizontalLine(Color.grey);

                GUIUnstrip.BeginHorizontal();
                GUIUnstrip.Label("<b>Search:</b>", new GUILayoutOption[] { GUILayout.Width(75) });
                m_search = GUIUnstrip.TextField(m_search);                
                GUIUnstrip.EndHorizontal();

                GUIUnstrip.BeginHorizontal();
                GUIUnstrip.Label("<b>Filter:</b>", new GUILayoutOption[] { GUILayout.Width(75) });
                FilterToggle(MemberTypes.All, "All");
                FilterToggle(MemberTypes.Property, "Properties");
                FilterToggle(MemberTypes.Field, "Fields");
                FilterToggle(MemberTypes.Method, "Methods");
                GUIUnstrip.EndHorizontal();

                GUIUnstrip.BeginHorizontal();
                GUIUnstrip.Label("<b>Values:</b>", new GUILayoutOption[] { GUILayout.Width(75) });
                if (GUIUnstrip.Button("Update", new GUILayoutOption[] { GUILayout.Width(100) }))
                {
                    UpdateValues();
                }
                GUI.color = m_autoUpdate ? Color.green : Color.red;
                m_autoUpdate = GUIUnstrip.Toggle(m_autoUpdate, "Auto-update?", new GUILayoutOption[] { GUILayout.Width(100) });
                GUI.color = m_hideFailedReflection ? Color.green : Color.red;
                m_hideFailedReflection = GUIUnstrip.Toggle(m_hideFailedReflection, "Hide failed Reflection?", new GUILayoutOption[] { GUILayout.Width(150) });
                GUI.color = Color.white;
                GUIUnstrip.EndHorizontal();

                GUIUnstrip.Space(10);

                Pages.ItemCount = m_cachedMembersFiltered.Length;

                // prev/next page buttons
                GUIUnstrip.BeginHorizontal();

                Pages.DrawLimitInputArea();

                if (Pages.ItemCount > Pages.ItemsPerPage)
                {
                    if (GUIUnstrip.Button("< Prev", new GUILayoutOption[] { GUILayout.Width(80) }))
                    {
                        Pages.TurnPage(Turn.Left, ref this.scroll);
                    }

                    Pages.CurrentPageLabel();

                    if (GUIUnstrip.Button("Next >", new GUILayoutOption[] { GUILayout.Width(80) }))
                    {
                        Pages.TurnPage(Turn.Right, ref this.scroll);
                    }
                }
                GUIUnstrip.EndHorizontal();

                // ====== BODY ======

                scroll = GUIUnstrip.BeginScrollView(scroll);

                GUIUnstrip.Space(10);

                UIStyles.HorizontalLine(Color.grey);

                GUIUnstrip.BeginVertical(GUI.skin.box, null);

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
                        GUIUnstrip.EndHorizontal();
                        continue;
                    }
                    GUIUnstrip.EndHorizontal();

                    // if not last element
                    if (!(j == (start + Pages.ItemsPerPage - 1) || j == (members.Length - 1)))
                        UIStyles.HorizontalLine(new Color(0.07f, 0.07f, 0.07f), true);
                }

                GUIUnstrip.EndVertical();
                GUIUnstrip.EndScrollView();

                if (!WindowManager.TabView)
                {
                    m_rect = ResizeDrag.ResizeWindow(rect, windowID);

                    GUIUnstrip.EndArea();
                }
            }
            catch (Il2CppException e)
            {
                if (!e.Message.Contains("in a group with only"))
                {
                    throw;
                }
            }
            catch (Exception e)
            {
                MelonLogger.LogWarning("Exception drawing ReflectionWindow: " + e.GetType() + ", " + e.Message);
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
            if (GUIUnstrip.Button(label, new GUILayoutOption[] { GUILayout.Width(100) }))
            {
                m_filter = mode;
                Pages.PageOffset = 0;
                scroll = Vector2.zero;
            }
            GUI.color = Color.white;
        }
    }
}
