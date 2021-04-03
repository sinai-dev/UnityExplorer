using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Inspectors;

namespace UnityExplorer.UI.Main.Home
{
    public class HomePage : BaseMenuPage
    {
        public override string Name => "Home";

        public static HomePage Instance { get; internal set; }

        public override bool Init()
        {
            Instance = this;

            ConstructMenu();

            new SceneExplorer();

            new InspectorManager();

            SceneExplorer.Instance.Init();

            return true;
        }

        public override void Update()
        {
            SceneExplorer.Instance.Update();
            InspectorManager.Instance.Update();
        }

        private void ConstructMenu()
        {
            GameObject parent = MainMenu.Instance.PageViewport;

            Content = UIFactory.CreateHorizontalGroup(parent, "HomePage", true, true, true, true, 3, new Vector4(1,1,1,1)).gameObject;
        }
    }
}
