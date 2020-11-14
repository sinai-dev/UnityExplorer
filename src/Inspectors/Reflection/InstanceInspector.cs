using System;
using System.Reflection;
using UnityEngine;
using UnityExplorer.Helpers;
using UnityExplorer.UI;
using UnityEngine.UI;

namespace UnityExplorer.Inspectors.Reflection
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

        private void OnScopeFilterClicked(MemberScopes type, Button button)
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
            m_sliderScroller.m_slider.value = 1f;
        }

        public void ConstructInstanceHelpers()
        {
            // On second thought, I'm not sure about this, seems unnecessary (and bloaty)
            // I might do the Texture2D helper (view/save image) but idk about anything else.

            //if (typeof(Component).IsAssignableFrom(m_targetType))
            //{
            //    // component helpers (ref GO)
            //    var tempObj = UIFactory.CreateLabel(Content, TextAnchor.MiddleLeft);
            //    var text = tempObj.GetComponent<Text>();
            //    text.text = "TODO comp helpers";
            //}
            //else if (typeof(UnityEngine.Object).IsAssignableFrom(m_targetType))
            //{
            //    // unityengine.object helpers (name, instantiate, destroy?)
            //    var tempObj = UIFactory.CreateLabel(Content, TextAnchor.MiddleLeft);
            //    var text = tempObj.GetComponent<Text>();
            //    text.text = "TODO unity object helpers";
            //}
        }

        public void ConstructInstanceFilters(GameObject parent)
        {
            var memberFilterRowObj = UIFactory.CreateHorizontalGroup(parent, new Color(1, 1, 1, 0));
            var memFilterGroup = memberFilterRowObj.GetComponent<HorizontalLayoutGroup>();
            memFilterGroup.childForceExpandHeight = false;
            memFilterGroup.childForceExpandWidth = false;
            memFilterGroup.childControlWidth = true;
            memFilterGroup.childControlHeight = true;
            memFilterGroup.spacing = 5;
            var memFilterLayout = memberFilterRowObj.AddComponent<LayoutElement>();
            memFilterLayout.minHeight = 25;
            memFilterLayout.flexibleHeight = 0;
            memFilterLayout.flexibleWidth = 5000;

            var memLabelObj = UIFactory.CreateLabel(memberFilterRowObj, TextAnchor.MiddleLeft);
            var memLabelLayout = memLabelObj.AddComponent<LayoutElement>();
            memLabelLayout.minWidth = 100;
            memLabelLayout.minHeight = 25;
            memLabelLayout.flexibleWidth = 0;
            var memLabelText = memLabelObj.GetComponent<Text>();
            memLabelText.text = "Filter scope:";
            memLabelText.color = Color.grey;

            AddFilterButton(memberFilterRowObj, MemberScopes.All, true);
            AddFilterButton(memberFilterRowObj, MemberScopes.Instance);
            AddFilterButton(memberFilterRowObj, MemberScopes.Static);
        }

        private void AddFilterButton(GameObject parent, MemberScopes type, bool setEnabled = false)
        {
            var btnObj = UIFactory.CreateButton(parent, new Color(0.2f, 0.2f, 0.2f));

            var btnLayout = btnObj.AddComponent<LayoutElement>();
            btnLayout.minHeight = 25;
            btnLayout.minWidth = 70;

            var text = btnObj.GetComponentInChildren<Text>();
            text.text = type.ToString();

            var btn = btnObj.GetComponent<Button>();

            btn.onClick.AddListener(() => { OnScopeFilterClicked(type, btn); });

            var colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.7f, 0.3f);

            if (setEnabled)
            {
                colors.normalColor = new Color(0.2f, 0.6f, 0.2f);
                m_scopeFilter = type;
                m_lastActiveScopeButton = btn;
            }

            btn.colors = colors;
        }
    }
}
