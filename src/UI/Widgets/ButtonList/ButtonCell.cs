using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.ObjectPool;

namespace UnityExplorer.UI.Widgets
{
    public class ButtonCell : ICell
    {
        public float DefaultHeight => 25f;

        public Action<int> OnClick;
        public int CurrentDataIndex;

        public ButtonRef Button;

        #region ICell

        public bool Enabled => m_enabled;
        private bool m_enabled;

        public GameObject UIRoot { get; set; }
        public RectTransform Rect { get; set; }

        public void Disable()
        {
            m_enabled = false;
            UIRoot.SetActive(false);
        }

        public void Enable()
        {
            m_enabled = true;
            UIRoot.SetActive(true);
        }

        #endregion

        public virtual GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateHorizontalGroup(parent, "ButtonCell", true, false, true, true, 2, default,
                new Color(0.11f, 0.11f, 0.11f), TextAnchor.MiddleCenter);
            Rect = UIRoot.GetComponent<RectTransform>();
            Rect.anchorMin = new Vector2(0, 1);
            Rect.anchorMax = new Vector2(0, 1);
            Rect.pivot = new Vector2(0.5f, 1);
            Rect.sizeDelta = new Vector2(25, 25);
            UIFactory.SetLayoutElement(UIRoot, minWidth: 100, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 0);

            UIRoot.SetActive(false);

            this.Button = UIFactory.CreateButton(UIRoot, "NameButton", "Name");
            UIFactory.SetLayoutElement(Button.Component.gameObject, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 0);
            var buttonText = Button.Component.GetComponentInChildren<Text>();
            buttonText.horizontalOverflow = HorizontalWrapMode.Overflow;
            buttonText.alignment = TextAnchor.MiddleLeft;

            Color normal = new Color(0.11f, 0.11f, 0.11f);
            Color highlight = new Color(0.16f, 0.16f, 0.16f);
            Color pressed = new Color(0.05f, 0.05f, 0.05f);
            Color disabled = new Color(1, 1, 1, 0);
            RuntimeProvider.Instance.SetColorBlock(Button.Component, normal, highlight, pressed, disabled);

            Button.OnClick += () => { OnClick?.Invoke(CurrentDataIndex); };

            return UIRoot;
        }
    }
}
