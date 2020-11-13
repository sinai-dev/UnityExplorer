using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Inspectors;

namespace UnityExplorer.UI.Modules
{
    public class HomePage : MainMenu.Page
    {
        public override string Name => "Home";

        public static HomePage Instance { get; internal set; }

        public override void Init()
        {
            Instance = this;

            ConstructMenu();

            new SceneExplorer();

            new InspectorManager();

            SceneExplorer.Instance.Init();
        }

        public override void Update()
        {
            SceneExplorer.Instance.Update();
            InspectorManager.Instance.Update();
        }

        private void ConstructMenu()
        {
            GameObject parent = MainMenu.Instance.PageViewport;

            Content = UIFactory.CreateHorizontalGroup(parent);
            var mainGroup = Content.GetComponent<HorizontalLayoutGroup>();
            mainGroup.padding.left = 1;
            mainGroup.padding.right = 1;
            mainGroup.padding.top = 1;
            mainGroup.padding.bottom = 1;
            mainGroup.spacing = 3;
            mainGroup.childForceExpandHeight = true;
            mainGroup.childForceExpandWidth = true;
            mainGroup.childControlHeight = true;
            mainGroup.childControlWidth = true;
        }
    }
}
