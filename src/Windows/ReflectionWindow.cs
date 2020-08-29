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
        public override string Name { get => $"Reflection Inspector ({ObjectType.Name})"; }

        public Type ObjectType;

        private CacheObjectBase[] m_cachedMembers;
        private CacheObjectBase[] m_cachedMemberFiltered;
        private int m_pageOffset;
        private int m_limitPerPage = 20;

        private bool m_autoUpdate = false;
        private string m_search = "";
        public MemberTypes m_filter = MemberTypes.Property;
        private bool m_hideFailedReflection = false;

        // some extra caching
        private UnityEngine.Object m_uObj;
        private Component m_component;

        public override void Init()
        {
            var type = ReflectionHelpers.GetActualType(Target);
            if (type == null)
            {
                MelonLogger.Log($"Could not get underlying type for object..? Type: {Target?.GetType().Name}, ToString: {Target?.ToString()}");
                DestroyWindow();
                return;
            }

            ObjectType = type;

            var types = ReflectionHelpers.GetAllBaseTypes(Target);
            CacheMembers(types);

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

            m_filter = MemberTypes.All;
            m_autoUpdate = true;
            Update();

            m_autoUpdate = false;
            m_filter = MemberTypes.Property;
        }

        public override void Update()
        {
            m_cachedMemberFiltered = m_cachedMembers.Where(x => ShouldProcessMember(x)).ToArray();

            if (m_autoUpdate)
            {
                UpdateValues();
            }
        }

        private void UpdateValues()
        {
            foreach (var member in m_cachedMemberFiltered)
            {
                member.UpdateValue();
            }
        }

        private bool ShouldProcessMember(CacheObjectBase holder)
        {
            if (m_filter != MemberTypes.All && m_filter != holder.MemberInfoType) return false;

            if (!string.IsNullOrEmpty(holder.ReflectionException) && m_hideFailedReflection) return false;

            if (m_search == "" || holder.MemberInfo == null) return true;

            return holder.FullName
                .ToLower()
                .Contains(m_search.ToLower());
        }

        private void CacheMembers(Type[] types)
        {
            var list = new List<CacheObjectBase>();

            var names = new List<string>();

            foreach (var declaringType in types)
            {
                MemberInfo[] infos;
                string exception = null;

                try
                {
                    infos = declaringType.GetMembers(ReflectionHelpers.CommonFlags);
                }
                catch 
                {
                    MelonLogger.Log("Exception getting members for type: " + declaringType.Name);
                    continue;
                }

                object target = Target;

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
                    if (member.MemberType == MemberTypes.Field || member.MemberType == MemberTypes.Property || member.MemberType == MemberTypes.Method)
                    {
                        if (member.Name.Contains("Il2CppType") || member.Name.StartsWith("get_")) 
                            continue;

                        try
                        {
                            var name = member.DeclaringType.Name + "." + member.Name;
                            if (names.Contains(name)) continue;
                            names.Add(name);

                            var cached = CacheObjectBase.GetCacheObject(null, member, target);
                            if (cached != null)
                            {
                                list.Add(cached);
                                cached.ReflectionException = exception;
                            }
                        }
                        catch (Exception e) 
                        {
                            MelonLogger.LogWarning($"Exception caching member {declaringType.Name}.{member.Name}!");
                            MelonLogger.Log(e.ToString());
                        }
                    }
                }
            }

            m_cachedMembers = list.ToArray();
        }

        // =========== GUI DRAW =========== //

        public override void WindowFunction(int windowID)
        {
            try
            {
                Header();

                GUILayout.BeginArea(new Rect(5, 25, m_rect.width - 10, m_rect.height - 35), GUI.skin.box);

                GUILayout.BeginHorizontal(null);
                GUILayout.Label("<b>Type:</b> <color=cyan>" + ObjectType.FullName + "</color>", null);
                if (m_uObj)
                {
                    GUILayout.Label("Name: " + m_uObj.name, null);
                }
                GUILayout.EndHorizontal();

                if (m_uObj)
                {
                    GUILayout.BeginHorizontal(null);
                    GUILayout.Label("<b>Tools:</b>", new GUILayoutOption[] { GUILayout.Width(80) });
                    UIHelpers.InstantiateButton(m_uObj);
                    if (m_component && m_component.gameObject is GameObject obj)
                    {
                        GUI.skin.label.alignment = TextAnchor.MiddleRight;
                        GUILayout.Label("GameObject:", null);
                        if (GUILayout.Button("<color=#00FF00>" + obj.name + "</color>", new GUILayoutOption[] { GUILayout.MaxWidth(m_rect.width - 350) }))
                        {
                            WindowManager.InspectObject(obj, out bool _);
                        }
                        GUI.skin.label.alignment = TextAnchor.UpperLeft;
                    }
                    GUILayout.EndHorizontal();
                }

                UIStyles.HorizontalLine(Color.grey);

                GUILayout.BeginHorizontal(null);
                GUILayout.Label("<b>Search:</b>", new GUILayoutOption[] { GUILayout.Width(75) });
                m_search = GUILayout.TextField(m_search, null);
                GUILayout.Label("<b>Limit per page:</b>", new GUILayoutOption[] { GUILayout.Width(125) });
                var limitString = m_limitPerPage.ToString();
                limitString = GUILayout.TextField(limitString, new GUILayoutOption[] { GUILayout.Width(60) });
                if (int.TryParse(limitString, out int i))
                {
                    m_limitPerPage = i;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(null);
                GUILayout.Label("<b>Filter:</b>", new GUILayoutOption[] { GUILayout.Width(75) });
                FilterToggle(MemberTypes.All, "All");
                FilterToggle(MemberTypes.Property, "Properties");
                FilterToggle(MemberTypes.Field, "Fields");
                FilterToggle(MemberTypes.Method, "Methods");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(null);
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

                GUILayout.Space(10);

                int count = m_cachedMemberFiltered.Length;

                if (count > m_limitPerPage)
                {
                    // prev/next page buttons
                    GUILayout.BeginHorizontal(null);
                    int maxOffset = (int)Mathf.Ceil((float)(count / (decimal)m_limitPerPage)) - 1;
                    if (GUILayout.Button("< Prev", null))
                    {
                        if (m_pageOffset > 0) m_pageOffset--;
                        scroll = Vector2.zero;
                    }

                    GUILayout.Label($"Page {m_pageOffset + 1}/{maxOffset + 1}", new GUILayoutOption[] { GUILayout.Width(80) });

                    if (GUILayout.Button("Next >", null))
                    {
                        if (m_pageOffset < maxOffset) m_pageOffset++;
                        scroll = Vector2.zero;
                    }
                    GUILayout.EndHorizontal();
                }

                scroll = GUILayout.BeginScrollView(scroll, GUI.skin.scrollView);

                GUILayout.Space(10);

                DrawMembers(this.m_cachedMemberFiltered);

                GUILayout.EndScrollView();

                m_rect = ResizeDrag.ResizeWindow(m_rect, windowID);

                GUILayout.EndArea();
            }
            catch (Exception e)
            {
                MelonLogger.LogWarning("Exception on window draw. Message: " + e.Message);
                DestroyWindow();
                return;
            }
        }

        private void DrawMembers(CacheObjectBase[] members)
        {
            // todo pre-cache list based on current search, otherwise this doesnt work.

            int i = 0;
            DrawMembersInternal("Properties", MemberTypes.Property, members, ref i);
            DrawMembersInternal("Fields", MemberTypes.Field, members, ref i);
            DrawMembersInternal("Methods", MemberTypes.Method, members, ref i);
        }

        private void DrawMembersInternal(string title, MemberTypes filter, CacheObjectBase[] members, ref int index)
        {
            if (m_filter != filter && m_filter != MemberTypes.All)
            {
                return;
            }

            UIStyles.HorizontalLine(Color.grey);

            GUILayout.Label($"<size=18><b><color=gold>{title}</color></b></size>", null);

            int offset = (m_pageOffset * m_limitPerPage) + index;

            if (offset >= m_cachedMemberFiltered.Length)
            {
                m_pageOffset = 0;
                offset = 0;
            }

            for (int j = offset; j < offset + m_limitPerPage && j < members.Length; j++)
            {
                var holder = members[j];

                if (holder.MemberInfoType != filter || !ShouldProcessMember(holder)) continue;

                GUILayout.BeginHorizontal(new GUILayoutOption[] { GUILayout.Height(25) });
                try
                {
                    holder.Draw(this.m_rect, 180f);
                }
                catch // (Exception e)
                {
                    //MelonLogger.Log("Exception drawing member " + holder.MemberInfo.Name);
                    //MelonLogger.Log(e.GetType() + ", " + e.Message);
                    //MelonLogger.Log(e.StackTrace);
                }
                GUILayout.EndHorizontal();

                index++;
                if (index >= m_limitPerPage) break;
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
            }
            GUI.color = Color.white;
        }
    }
}
