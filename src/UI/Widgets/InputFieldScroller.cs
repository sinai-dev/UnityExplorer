using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityExplorer.UI.Models;

namespace UnityExplorer.UI.Utility
{
    // To fix an issue with Input Fields and allow them to go inside a ScrollRect nicely.

    public class InputFieldScroller : UIBehaviourModel
    {
        public override GameObject UIRoot
        {
            get
            {
                if (InputField.UIRoot)
                    return InputField.UIRoot;
                return null;
            }
        }

        public Action OnScroll;

        internal AutoSliderScrollbar Slider;
        internal InputFieldRef InputField;

        internal RectTransform ContentRect;
        internal RectTransform ViewportRect;

        internal static CanvasScaler RootScaler;

        public InputFieldScroller(AutoSliderScrollbar sliderScroller, InputFieldRef inputField)
        {
            this.Slider = sliderScroller;
            this.InputField = inputField;

            inputField.OnValueChanged += OnTextChanged;

            ContentRect = inputField.UIRoot.GetComponent<RectTransform>();
            ViewportRect = ContentRect.transform.parent.GetComponent<RectTransform>();

            if (!RootScaler)
                RootScaler = UIManager.CanvasRoot.GetComponent<CanvasScaler>();
        }

        internal string m_lastText;
        internal bool m_updateWanted;
        internal bool m_wantJumpToBottom;
        private float m_desiredContentHeight;

        private float lastContentPosition;
        private float lastViewportHeight;

        public override void Update()
        {
            if (this.ContentRect.localPosition.y != lastContentPosition)
            {
                lastContentPosition = ContentRect.localPosition.y;
                OnScroll?.Invoke();
            }

            if (ViewportRect.rect.height != lastViewportHeight)
            {
                lastViewportHeight = ViewportRect.rect.height;
                m_updateWanted = true;
            }

            if (m_updateWanted)
            {
                m_updateWanted = false;
                ProcessInputText();

                float desiredHeight = Math.Max(m_desiredContentHeight, ViewportRect.rect.height);

                if (ContentRect.rect.height < desiredHeight)
                {
                    ContentRect.sizeDelta = new Vector2(0, desiredHeight);
                    this.Slider.UpdateSliderHandle();
                }
                else if (ContentRect.rect.height > desiredHeight)
                {
                    ContentRect.sizeDelta = new Vector2(0, desiredHeight);
                    this.Slider.UpdateSliderHandle();
                }
            }

            if (m_wantJumpToBottom)
            {
                Slider.Slider.value = 1f;
                m_wantJumpToBottom = false;
            }
        }

        internal void OnTextChanged(string text)
        {
            m_lastText = text;
            m_updateWanted = true;
        }

        internal void ProcessInputText()
        {
            var curInputRect = InputField.Component.textComponent.rectTransform.rect;
            var scaleFactor = RootScaler.scaleFactor;

            // Current text settings
            var texGenSettings = InputField.Component.textComponent.GetGenerationSettings(curInputRect.size);
            texGenSettings.generateOutOfBounds = false;
            texGenSettings.scaleFactor = scaleFactor;

            // Preferred text rect height
            var textGen = InputField.Component.textComponent.cachedTextGeneratorForLayout;
            m_desiredContentHeight = textGen.GetPreferredHeight(m_lastText, texGenSettings) + 10;
        }

        public override void ConstructUI(GameObject parent)
        {
            throw new NotImplementedException();
        }
    }
}