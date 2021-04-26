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
        public GameObject UIRoot => uiRoot;
        private GameObject uiRoot;

        public float DefaultHeight => 25f;

        public ButtonRef TabButton;
        public Text TabText;

        public ButtonRef CloseButton;

        public GameObject CreateContent(GameObject parent)
        {
            uiRoot = UIFactory.CreateHorizontalGroup(parent, "TabObject", true, true, true, true, 0, new Vector4(0, 0, 3, 0));
            UIFactory.SetLayoutElement(uiRoot, minWidth: 185, flexibleWidth: 0);
            uiRoot.AddComponent<Mask>();

            TabButton = UIFactory.CreateButton(uiRoot, "TabButton", "");

            UIFactory.SetLayoutElement(TabButton.Button.gameObject, minWidth: 165, flexibleWidth: 0);

            TabText = TabButton.Button.GetComponentInChildren<Text>();
            TabText.horizontalOverflow = HorizontalWrapMode.Overflow;
            TabText.alignment = TextAnchor.MiddleLeft;

            CloseButton = UIFactory.CreateButton(uiRoot, "CloseButton", "X", new Color(0.2f, 0.2f, 0.2f, 1));
            UIFactory.SetLayoutElement(CloseButton.Button.gameObject, minWidth: 20, flexibleWidth: 0);
            var closeBtnText = CloseButton.Button.GetComponentInChildren<Text>();
            closeBtnText.color = Color.red;

            return uiRoot;
        }
    }
}
