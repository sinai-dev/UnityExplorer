using System;
using System.Reflection;
using UnityEngine;
using UnityExplorer.UI;
using UnityEngine.UI;
using System.IO;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI.Main.Home.Inspectors;

namespace UnityExplorer.Core.Inspectors.Reflection
{
    public enum MemberScopes
    {
        All,
        Instance,
        Static
    }

    public class InstanceInspector : ReflectionInspector
    {
        public override string TabLabel => $" <color=cyan>[R]</color> {base.TabLabel}";

        internal MemberScopes m_scopeFilter;
        internal Button m_lastActiveScopeButton;

        public InstanceInspector(object target) : base(target) { }

        internal InstanceInspectorUI InstanceUI;
        public void CreateInstanceUIModule()
        {
            InstanceUI = new InstanceInspectorUI(this);
        }

        internal void OnScopeFilterClicked(MemberScopes type, Button button)
        {
            if (m_lastActiveScopeButton)
            {
                var lastColors = m_lastActiveScopeButton.colors;
                lastColors.normalColor = new Color(0.2f, 0.2f, 0.2f);
                m_lastActiveScopeButton.colors = lastColors;
            }

            m_scopeFilter = type;
            m_lastActiveScopeButton = button;

            var colors = m_lastActiveScopeButton.colors;
            colors.normalColor = new Color(0.2f, 0.6f, 0.2f);
            m_lastActiveScopeButton.colors = colors;

            FilterMembers(null, true);
            base.ReflectionUI.m_sliderScroller.m_slider.value = 1f;
        }
    }
}
