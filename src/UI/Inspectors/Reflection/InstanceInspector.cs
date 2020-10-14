using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Explorer.UI;
using Explorer.UI.Shared;
using Explorer.CacheObject;
#if CPP
using UnhollowerBaseLib;
#endif

namespace Explorer.UI.Inspectors
{
    public class InstanceInspector : ReflectionInspector
    {
        public override bool IsStaticInspector => false;

        // some extra cast-caching
        public UnityEngine.Object m_uObj;
        private Component m_component;

        public override void Init()
        {            
            // cache the extra cast-caching
#if CPP
            if (!IsStaticInspector && Target is Il2CppSystem.Object ilObject)
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
            if (!IsStaticInspector)
            {
                m_uObj = Target as UnityEngine.Object;
                m_component = Target as Component;
            }
#endif

            base.Init();
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

            base.Update();
        }
        
        public void DrawInstanceControls(Rect rect)
        {
            //if (m_uObj)
            //{
            //    GUILayout.Label("Name: " + m_uObj.name, new GUILayoutOption[0]);
            //}
            //GUILayout.EndHorizontal();

            if (m_uObj)
            {
                GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
                GUILayout.Label("<b>Tools:</b>", new GUILayoutOption[] { GUILayout.Width(80) });
                Buttons.InstantiateButton(m_uObj);
                if (m_component && m_component.gameObject is GameObject obj)
                {
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label("GameObject:", new GUILayoutOption[] { GUILayout.Width(135) });
                    var charWidth = obj.name.Length * 15;
                    var maxWidth = rect.width - 350;
                    var btnWidth = charWidth < maxWidth ? charWidth : maxWidth;
                    if (GUILayout.Button("<color=#00FF00>" + obj.name + "</color>", new GUILayoutOption[] { GUILayout.Width(btnWidth) }))
                    {
                        WindowManager.InspectObject(obj, out bool _);
                    }
                    GUI.skin.label.alignment = TextAnchor.UpperLeft;
                }
                else
                {
                    GUILayout.Label("Name: " + m_uObj.name, new GUILayoutOption[0]);
                }
                GUILayout.EndHorizontal();
            }
        }
    }
}
