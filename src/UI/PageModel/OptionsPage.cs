using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UnityExplorer.UI.PageModel
{
    // Probably do this after I've made the CacheObjectBase / InteractiveValue classes.
    // Might not use CacheObject, but InteractiveValue would be useful here.
    
    // Maybe InteractiveValue could have an OnSetValue event, which CacheObject and this class can subscribe to separately.

    public class OptionsPage : MainMenu.Page
    {
        public override string Name => "Options";

        public override void Init()
        {
            ConstructUI();

        }

        public override void Update()
        {

        }

        #region UI CONSTRUCTION

        internal void ConstructUI()
        {
            GameObject parent = MainMenu.Instance.PageViewport;

            Content = UIFactory.CreateHorizontalGroup(parent);
            var mainGroup = Content.GetComponent<HorizontalLayoutGroup>();
            mainGroup.padding.left = 4;
            mainGroup.padding.right = 4;
            mainGroup.padding.top = 4;
            mainGroup.padding.bottom = 4;
            mainGroup.spacing = 5;
            mainGroup.childForceExpandHeight = true;
            mainGroup.childForceExpandWidth = true;
            mainGroup.childControlHeight = true;
            mainGroup.childControlWidth = true;

            ConstructTopArea();

        }

        internal void ConstructTopArea()
        {
            var topAreaObj = UIFactory.CreateVerticalGroup(Content, new Color(0.15f, 0.15f, 0.15f));
            var topGroup = topAreaObj.GetComponent<VerticalLayoutGroup>();
            topGroup.childForceExpandHeight = false;
            topGroup.childControlHeight = true;
            topGroup.childForceExpandWidth = true;
            topGroup.childControlWidth = true;
            topGroup.padding.top = 5;
            topGroup.padding.left = 5;
            topGroup.padding.right = 5;
            topGroup.padding.bottom = 5;
            topGroup.spacing = 5;

            GameObject titleObj = UIFactory.CreateLabel(Content, TextAnchor.UpperLeft);
            Text titleLabel = titleObj.GetComponent<Text>();
            titleLabel.text = "Options";
            titleLabel.fontSize = 20;
            LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
            titleLayout.minHeight = 30;
            titleLayout.flexibleHeight = 0;
        }

        

        #endregion
    }
}
