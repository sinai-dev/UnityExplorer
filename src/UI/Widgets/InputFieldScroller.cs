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
                if (inputField)
                    return inputField.gameObject;
                return null;
            }
        }

        internal SliderScrollbar sliderScroller;
        internal InputField inputField;

        internal RectTransform inputRect;
        internal LayoutElement layoutElement;
        internal VerticalLayoutGroup parentLayoutGroup;

        internal static CanvasScaler canvasScaler;

        public InputFieldScroller(SliderScrollbar sliderScroller, InputField inputField)
        {
            //Instances.Add(this);

            this.sliderScroller = sliderScroller;
            this.inputField = inputField;

            sliderScroller.m_parentInputScroller = this;

            inputField.onValueChanged.AddListener(OnTextChanged);

            inputRect = inputField.GetComponent<RectTransform>();
            layoutElement = inputField.gameObject.AddComponent<LayoutElement>();
            parentLayoutGroup = inputField.transform.parent.GetComponent<VerticalLayoutGroup>();

            layoutElement.minHeight = 25;
            layoutElement.minWidth = 100;

            if (!canvasScaler)
                canvasScaler = UIManager.CanvasRoot.GetComponent<CanvasScaler>();
        }

        internal string m_lastText;
        internal bool m_updateWanted;

        // only done once, to fix height on creation.
        internal bool heightInitAfterLayout;

        public override void Update()
        {
            if (!heightInitAfterLayout)
            {
                heightInitAfterLayout = true;
                var height = sliderScroller.m_scrollRect.parent.parent.GetComponent<RectTransform>().rect.height;
                layoutElement.preferredHeight = height;
            }

            if (m_updateWanted && inputField.gameObject.activeInHierarchy)
            {
                m_updateWanted = false;
                RefreshUI();
            }
        }

        //internal bool CheckDestroyed()
        //{
        //    if (sliderScroller == null || sliderScroller.CheckDestroyed())
        //    {
        //        Instances.Remove(this);
        //        return true;
        //    }

        //    return false;
        //}

        internal void OnTextChanged(string text)
        {
            m_lastText = text;
            m_updateWanted = true;
        }

        internal void RefreshUI()
        {
            var curInputRect = inputField.textComponent.rectTransform.rect;
            var scaleFactor = canvasScaler.scaleFactor;

            // Current text settings
            var texGenSettings = inputField.textComponent.GetGenerationSettings(curInputRect.size);
            texGenSettings.generateOutOfBounds = false;
            texGenSettings.scaleFactor = scaleFactor;

            // Preferred text rect height
            var textGen = inputField.textComponent.cachedTextGeneratorForLayout;
            float preferredHeight = textGen.GetPreferredHeight(m_lastText, texGenSettings) + 10;

            // Default text rect height (fit to scroll parent or expand to fit text)
            float minHeight = Mathf.Max(preferredHeight, sliderScroller.m_scrollRect.rect.height - 25);

            layoutElement.preferredHeight = minHeight;

            if (inputField.caretPosition == inputField.text.Length
                && inputField.text.Length > 0
                && inputField.text[inputField.text.Length - 1] == '\n')
            {
                sliderScroller.m_slider.value = 0f;
            }
        }

        public override void ConstructUI(GameObject parent)
        {
            throw new NotImplementedException();
        }
    }
}