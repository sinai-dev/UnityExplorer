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

        public GameObject UIRoot => uiRoot;
        public GameObject uiRoot;

        public bool Enabled => m_enabled;
        private bool m_enabled;

        public RectTransform Rect => m_rect;
        private RectTransform m_rect;

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

        #endregion

        public GameObject CreateContent(GameObject parent)
        {
            uiRoot = UIFactory.CreateHorizontalGroup(parent, "ButtonCell", true, true, true, true, 2, default,
                new Color(0.11f, 0.11f, 0.11f), TextAnchor.MiddleCenter);
            m_rect = uiRoot.GetComponent<RectTransform>();
            m_rect.anchorMin = new Vector2(0, 1);
            m_rect.anchorMax = new Vector2(0, 1);
            m_rect.pivot = new Vector2(0.5f, 1);
            m_rect.sizeDelta = new Vector2(25, 25);
            UIFactory.SetLayoutElement(uiRoot, minWidth: 100, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 0);

            uiRoot.SetActive(false);

            this.Button = UIFactory.CreateButton(uiRoot, "NameButton", "Name");
            UIFactory.SetLayoutElement(Button.Button.gameObject, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 0);
            var buttonText = Button.Button.GetComponentInChildren<Text>();
            buttonText.horizontalOverflow = HorizontalWrapMode.Overflow;
            buttonText.alignment = TextAnchor.MiddleLeft;

            Color normal = new Color(0.11f, 0.11f, 0.11f);
            Color highlight = new Color(0.25f, 0.25f, 0.25f);
            Color pressed = new Color(0.05f, 0.05f, 0.05f);
            Color disabled = new Color(1, 1, 1, 0);
            RuntimeProvider.Instance.SetColorBlock(Button.Button, normal, highlight, pressed, disabled);

            Button.OnClick += () => { OnClick?.Invoke(CurrentDataIndex); };

            return uiRoot;
        }
    }
}
