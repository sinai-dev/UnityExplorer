using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ExplorerBeta.UI.Main
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
            mainGroup.padding.left = 3;
            mainGroup.padding.right = 3;
            mainGroup.padding.top = 3;
            mainGroup.padding.bottom = 3;
            mainGroup.spacing = 5;
            mainGroup.childForceExpandHeight = true;
            mainGroup.childForceExpandWidth = true;
            mainGroup.childControlHeight = true;
            mainGroup.childControlWidth = true;
        }
    }
}
