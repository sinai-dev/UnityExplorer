using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Models;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Inspectors
{
    public class InspectorTab : IPooledObject
    {
        public GameObject UIRoot { get; set; }

        public float DefaultHeight => 25f;

        public ButtonRef TabButton;
        public Text TabText;

        public ButtonRef CloseButton;

        private static readonly Color _enabledTabColor = new Color(0.15f, 0.22f, 0.15f);
        private static readonly Color _disabledTabColor = new Color(0.13f, 0.13f, 0.13f);

        public void SetTabColor(bool active)
        {
            if (active)
                RuntimeProvider.Instance.SetColorBlock(TabButton.Component, _enabledTabColor, _enabledTabColor * 1.2f);
            else
                RuntimeProvider.Instance.SetColorBlock(TabButton.Component, _disabledTabColor, _disabledTabColor * 1.2f);
        }

        public GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateHorizontalGroup(parent, "TabObject", false, true, true, true, 0,
                default, new Color(0.13f, 0.13f, 0.13f), childAlignment: TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(UIRoot, minWidth: 200, flexibleWidth: 0);
            UIRoot.AddComponent<Mask>();

            TabButton = UIFactory.CreateButton(UIRoot, "TabButton", "");
            UIFactory.SetLayoutElement(TabButton.Component.gameObject, minWidth: 175, flexibleWidth: 0);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(TabButton.Component.gameObject, false, false, true, true, 0, 0, 0, 3);

            TabText = TabButton.Component.GetComponentInChildren<Text>();
            UIFactory.SetLayoutElement(TabText.gameObject, minHeight: 25, minWidth: 175, flexibleWidth: 0);
            TabText.alignment = TextAnchor.MiddleLeft;
            TabText.fontSize = 12;
            TabText.horizontalOverflow = HorizontalWrapMode.Overflow;

            CloseButton = UIFactory.CreateButton(UIRoot, "CloseButton", "X", new Color(0.2f, 0.2f, 0.2f, 1));
            UIFactory.SetLayoutElement(CloseButton.Component.gameObject, minHeight: 25, minWidth: 25, flexibleWidth: 0);
            var closeBtnText = CloseButton.Component.GetComponentInChildren<Text>();
            closeBtnText.color = Color.red;

            return UIRoot;
        }
    }
}
