using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MelonLoader;
using Mono.CSharp;
using UnhollowerBaseLib;
using UnityEngine;

namespace Explorer
{
    public class ReflectionWindow : UIWindow
    {
        public override string Name { get => "Object Reflection"; set => Name = value; }

        public Type ObjectType;
        //public object Target;

        private FieldInfoHolder[] m_FieldInfos;
        private PropertyInfoHolder[] m_PropertyInfos;

        private bool m_autoUpdate = false;
        private string m_search = "";
        public MemberFilter m_filter = MemberFilter.Property;

        public enum MemberFilter
        {
            Both,
            Property,
            Field
        }

        public override void Init()
        {
            var type = ReflectionHelpers.GetActualType(Target);
            if (type == null)
            {
                MelonLogger.Log("Could not get underlying type for object. ToString(): " + Target.ToString());
                return;
            }

            ObjectType = type;

            var types = ReflectionHelpers.GetAllBaseTypes(Target);

            CacheFields(types);
            CacheProperties(types);

            MelonLogger.Log("Cached properties: " + m_PropertyInfos.Length);

            UpdateValues(true);
        }

        public override void Update()
        {
            if (m_autoUpdate)
            {
                UpdateValues();
            }
        }

        private void UpdateValues(bool forceAll = false)
        {
            UpdateMemberList(forceAll, this.m_FieldInfos, MemberFilter.Field);
            UpdateMemberList(forceAll, this.m_PropertyInfos, MemberFilter.Property);
        }

        private void UpdateMemberList(bool forceAll, MemberInfoHolder[] list, MemberFilter filter)
        {
            if (forceAll || m_filter == MemberFilter.Both || m_filter == filter)
            {
                foreach (var holder in list)
                {
                    if (forceAll || ShouldUpdateMemberInfo(holder))
                    {
                        holder.UpdateValue(Target);
                    }
                }
            }
        }

        private bool ShouldUpdateMemberInfo(MemberInfoHolder holder)
        {
            var memberName = holder is FieldInfoHolder ? 
                (holder as FieldInfoHolder).fieldInfo.Name : 
                (holder as PropertyInfoHolder).propInfo.Name;

            return m_search == "" || memberName.ToLower().Contains(m_search.ToLower());
        }

        private void CacheProperties(Type[] types, List<string> names = null)
        {
            if (names == null)
            {
                names = new List<string>();
            }

            var list = new List<PropertyInfoHolder>();

            foreach (var type in types)
            {
                PropertyInfo[] propInfos = new PropertyInfo[0];

                try
                {
                    propInfos = type.GetProperties(ReflectionHelpers.CommonFlags);
                }
                catch (TypeLoadException)
                {
                    MelonLogger.Log($"Couldn't get Properties for Type '{type.Name}', it may not support Il2Cpp Reflection at the moment.");
                }

                foreach (var pi in propInfos)
                {
                    // this member causes a crash when inspected, so just skipping it for now.
                    if (pi.Name == "Il2CppType")
                    {
                        continue;
                    }

                    if (names.Contains(pi.Name))
                    {
                        continue;
                    }
                    names.Add(pi.Name);

                    var piHolder = new PropertyInfoHolder(type, pi);
                    list.Add(piHolder);
                }
            }

            m_PropertyInfos = list.ToArray();
        }

        private void CacheFields(Type[] types, List<string> names = null)
        {
            if (names == null)
            {
                names = new List<string>();
            }

            var list = new List<FieldInfoHolder>();

            foreach (var type in types)
            {
                foreach (var fi in type.GetFields(ReflectionHelpers.CommonFlags))
                {
                    if (names.Contains(fi.Name))
                    {
                        continue;
                    }
                    names.Add(fi.Name);

                    var fiHolder = new FieldInfoHolder(type, fi);
                    list.Add(fiHolder);
                }
            }

            m_FieldInfos = list.ToArray();
        }

        // =========== GUI DRAW =========== //

        public override void WindowFunction(int windowID)
        {
            try
            {
                Header();

                GUILayout.BeginArea(new Rect(5, 25, m_rect.width - 10, m_rect.height - 35), GUI.skin.box);

                GUILayout.BeginHorizontal(null);
                GUILayout.Label("<b>Type:</b> <color=cyan>" + ObjectType.Name + "</color>", null);

                bool unityObj = Target is UnityEngine.Object;

                if (unityObj)
                {
                    GUILayout.Label("Name: " + (Target as UnityEngine.Object).name, null);
                }
                GUILayout.EndHorizontal();

                if (unityObj)
                {
                    GUILayout.BeginHorizontal(null);

                    GUILayout.Label("<b>Tools:</b>", new GUILayoutOption[] { GUILayout.Width(80) });

                    UIHelpers.InstantiateButton((UnityEngine.Object)Target);

                    if (Target is Component comp && comp.gameObject is GameObject obj)
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
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(null);
                GUILayout.Label("<b>Filter:</b>", new GUILayoutOption[] { GUILayout.Width(75) });
                FilterToggle(MemberFilter.Both, "Both");
                FilterToggle(MemberFilter.Property, "Properties");
                FilterToggle(MemberFilter.Field, "Fields");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(null);
                GUILayout.Label("<b>Values:</b>", new GUILayoutOption[] { GUILayout.Width(75) });
                if (GUILayout.Button("Update", new GUILayoutOption[] { GUILayout.Width(100) }))
                {
                    UpdateValues();
                }
                GUI.color = m_autoUpdate ? Color.green : Color.red;
                m_autoUpdate = GUILayout.Toggle(m_autoUpdate, "Auto-update?", new GUILayoutOption[] { GUILayout.Width(100) });
                GUI.color = Color.white;
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                scroll = GUILayout.BeginScrollView(scroll, GUI.skin.scrollView);

                GUILayout.Space(10);

                if (m_filter == MemberFilter.Both || m_filter == MemberFilter.Field)
                {
                    DrawMembers(this.m_FieldInfos, "Fields");
                }

                if (m_filter == MemberFilter.Both || m_filter == MemberFilter.Property)
                {
                    DrawMembers(this.m_PropertyInfos, "Properties");
                }

                GUILayout.EndScrollView();

                m_rect = WindowManager.ResizeWindow(m_rect, windowID);

                GUILayout.EndArea();
            }
            catch (Exception e)
            {
                MelonLogger.LogWarning("Exception on window draw. Message: " + e.Message);
                DestroyWindow();
                return;
            }
        }

        private void DrawMembers(MemberInfoHolder[] members, string title)
        {
            UIStyles.HorizontalLine(Color.grey);

            GUILayout.Label($"<size=18><b><color=gold>{title}</color></b></size>", null);

            foreach (var holder in members)
            {
                var memberName = (holder as FieldInfoHolder)?.fieldInfo.Name ?? (holder as PropertyInfoHolder)?.propInfo.Name;

                if (m_search != "" && !memberName.ToLower().Contains(m_search.ToLower()))
                {
                    continue;
                }

                GUILayout.BeginHorizontal(new GUILayoutOption[] { GUILayout.Height(25) });
                holder.Draw(this);
                GUILayout.EndHorizontal();
            }
        }

        private void FilterToggle(MemberFilter mode, string label)
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
