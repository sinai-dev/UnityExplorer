using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.ObjectPool;
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
                RuntimeProvider.Instance.SetColorBlock(TabButton.Button, _enabledTabColor, _enabledTabColor * 1.2f);
            else
                RuntimeProvider.Instance.SetColorBlock(TabButton.Button, _disabledTabColor, _disabledTabColor * 1.2f);
        }

        public GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateHorizontalGroup(parent, "TabObject", true, true, true, true, 0, 
                new Vector4(0, 0, 3, 0), new Color(0.13f, 0.13f, 0.13f));
            UIFactory.SetLayoutElement(UIRoot, minWidth: 185, flexibleWidth: 0);
            UIRoot.AddComponent<Mask>();

            TabButton = UIFactory.CreateButton(UIRoot, "TabButton", "");

            UIFactory.SetLayoutElement(TabButton.Button.gameObject, minWidth: 165, flexibleWidth: 0);

            TabText = TabButton.Button.GetComponentInChildren<Text>();
            TabText.horizontalOverflow = HorizontalWrapMode.Overflow;
            TabText.alignment = TextAnchor.MiddleLeft;

            CloseButton = UIFactory.CreateButton(UIRoot, "CloseButton", "X", new Color(0.2f, 0.2f, 0.2f, 1));
            UIFactory.SetLayoutElement(CloseButton.Button.gameObject, minWidth: 20, flexibleWidth: 0);
            var closeBtnText = CloseButton.Button.GetComponentInChildren<Text>();
            closeBtnText.color = Color.red;

            return UIRoot;
        }
    }
}
