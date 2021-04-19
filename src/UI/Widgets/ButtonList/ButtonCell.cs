using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace UnityExplorer.UI.Widgets
{
    public class ButtonCell<T> : ICell
    {
        public bool Enabled => m_enabled;
        private bool m_enabled;

        public Action<ButtonCell<T>> OnClick;

        public ButtonListSource<T> list;

        public GameObject uiRoot;
        public Text buttonText;
        public Button button;

        public ButtonCell(ButtonListSource<T> list, GameObject uiRoot, Button button, Text text)
        {
            this.list = list;
            this.uiRoot = uiRoot;
            this.buttonText = text;
            this.button = button;

            button.onClick.AddListener(() => { OnClick?.Invoke(this); });
        }

        public void Disable()
        {
            m_enabled = false;
            uiRoot.SetActive(false);
        }

        public void Enable()
        {
            m_enabled = true;
            uiRoot.SetActive(true);
        }

        public static GameObject CreatePrototypeCell(GameObject parent)
        {
            var prototype = UIFactory.CreateHorizontalGroup(parent, "PrototypeCell", true, true, true, true, 2, default,
                new Color(0.15f, 0.15f, 0.15f), TextAnchor.MiddleCenter);
            //var cell = prototype.AddComponent<TransformCell>();
            var rect = prototype.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(25, 25);
            UIFactory.SetLayoutElement(prototype, minWidth: 100, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 0);

            var nameButton = UIFactory.CreateButton(prototype, "NameButton", "Name", null);
            UIFactory.SetLayoutElement(nameButton.gameObject, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 0);
            var nameLabel = nameButton.GetComponentInChildren<Text>();
            nameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            nameLabel.alignment = TextAnchor.MiddleLeft;

            Color normal = new Color(0.15f, 0.15f, 0.15f);
            Color highlight = new Color(0.25f, 0.25f, 0.25f);
            Color pressed = new Color(0.05f, 0.05f, 0.05f);
            Color disabled = new Color(1, 1, 1, 0);
            RuntimeProvider.Instance.SetColorBlock(nameButton, normal, highlight, pressed, disabled);

            prototype.SetActive(false);

            return prototype;
        }
    }
}
