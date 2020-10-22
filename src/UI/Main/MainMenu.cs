using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExplorerBeta.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ExplorerBeta.UI.Shared;

namespace ExplorerBeta.UI.Main
{
    // TODO REMAKE THIS

    public class MainMenu
    {
        public static MainMenu Instance { get; set; }

        public PanelDragger Dragger { get; private set; }
        public GameObject MainPanel { get; private set; }

        public MainMenu()
        {
            if (Instance != null)
            {
                ExplorerCore.LogWarning("An instance of MainMenu already exists, cannot create another!");
                return;
            }

            Instance = this;

            MainPanel = CreateBasePanel("MainMenu");
            CreateTitleBar();
            CreateNavbar();
            CreateViewArea();
        }

        public void Update()
        {

        }

        private void TestButtonCallback()
        {
            //if (EventSystem.current != EventSys)
            //    return;

            var go = EventSystem.current.currentSelectedGameObject;
            if (!go)
                return;

            var name = go.name;
            if (go.GetComponentInChildren<Text>() is Text text)
            {
                name = text.text;
            }
            ExplorerCore.Log($"{Time.time} | Pressed {name ?? "null"}");

            if (name == "X")
            {
                ExplorerCore.ShowMenu = false;
            }
        }

        #region UI Generator

        public virtual GameObject CreateBasePanel(string name)
        {
            var basePanel = UIFactory.CreatePanel(UIManager.CanvasRoot.gameObject, name);
            var panelRect = basePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.327f, 0.0967f);
            panelRect.anchorMax = new Vector2(0.672f, 0.904f);
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 620f);
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 800f);

            return basePanel;
        }

        private void CreateTitleBar()
        {
            // Make the horizontal group for window title area
            var titleGroup = UIFactory.CreateHorizontalGroup(MainPanel);
            var titleRect = titleGroup.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.005f, 0.96f);
            titleRect.anchorMax = new Vector2(0.995f, 0.994f);
            titleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 613);
            titleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 30);

            var group = titleGroup.GetComponent<HorizontalLayoutGroup>();
            group.childControlWidth = true;
            //group.childScaleWidth = true;
            group.childForceExpandHeight = true;
            group.childForceExpandWidth = true;

            // Create window title
            var titleLabel = UIFactory.CreateLabel(titleGroup, TextAnchor.MiddleCenter);
            var labelText = titleLabel.GetComponent<Text>();
            labelText.text = ExplorerCore.NAME;
            labelText.fontSize = 15;
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 575);
            labelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 25);

            // Add drag handler (on Title label)
            Dragger = new PanelDragger(titleLabel.transform.TryCast<RectTransform>(),
                                        MainPanel.GetComponent<RectTransform>());

            // Create X Button
            var exitBtnObj = UIFactory.CreateButton(titleGroup);
            var exitBtn = exitBtnObj.GetComponentInChildren<Button>();
            exitBtn.onClick.AddListener(new Action(TestButtonCallback));
            var exitBtnText = exitBtnObj.GetComponentInChildren<Text>();
            exitBtnText.text = "X";
            exitBtnText.fontSize = 14;
            var exitBtnRect = exitBtnObj.GetComponent<RectTransform>();
            exitBtnRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 40f);
            exitBtnRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 25f);
        }

        private void CreateNavbar()
        {
            // Make the horizontal group for the nav bar
            var navArea = UIFactory.CreateHorizontalGroup(MainPanel);
            var group = navArea.GetComponent<HorizontalLayoutGroup>();
            group.childAlignment = TextAnchor.MiddleLeft;
            group.spacing = 5;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = true;
            group.childControlWidth = true;
            group.spacing = 5;

            var padding = new RectOffset();
            padding.left = 5;
            padding.right = 5;
            padding.top = 3;
            padding.bottom = 3;
            group.padding = padding;

            var navRect = navArea.GetComponent<RectTransform>();
            navRect.anchorMin = new Vector2(0.005f, 0.93f);
            navRect.anchorMax = new Vector2(0.995f, 0.97f);
            var pos = navRect.localPosition;
            pos.y = 348;
            navRect.localPosition = pos;
            navRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 613);
            navRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 30);

            // Add the buttons for pages (this should be done programmatically)
            var names = new string[] { "Scenes", "Search", "C# Console", "Options" };
            for (int i = 0; i < names.Length; i++)
            {
                var btn = UIFactory.CreateButton(navArea);
                var text = btn.GetComponentInChildren<Text>();
                text.text = names[i];

                btn.GetComponent<Button>().onClick.AddListener(new Action(TestButtonCallback));

                if (i == 0)
                {
                    var image = btn.GetComponentInChildren<Image>();
                    image.color = new Color(0.1f, 0.35f, 0.1f);
                }
            }
        }

        private void CreateViewArea()
        {
            // Make the vertical group for the viewport
            var viewGroup = UIFactory.CreateVerticalGroup(MainPanel);
            var viewRect = viewGroup.GetComponent<RectTransform>();
            viewRect.anchorMin = new Vector2(0.005f, -0.038f);
            viewRect.anchorMax = new Vector2(0.995f, 0.954f);
            viewRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 613);
            viewRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 720f);
            viewRect.localPosition = new Vector3(0, -35f, 0);
        }

        #endregion
    }
}
