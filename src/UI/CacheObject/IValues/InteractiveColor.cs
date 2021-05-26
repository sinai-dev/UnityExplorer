using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.CacheObject;

namespace UnityExplorer.UI.CacheObject.IValues
{
    public class InteractiveColor : InteractiveValue
    {
        public bool IsValueColor32;

        public Color EditedColor;

        private Image m_colorImage;
        private readonly InputFieldRef[] m_inputs = new InputFieldRef[4];
        private readonly Slider[] m_sliders = new Slider[4];

        private ButtonRef m_applyButton;

        private static readonly string[] fieldNames = new[] { "R", "G", "B", "A" };

        public override void OnBorrowed(CacheObjectBase owner)
        {
            base.OnBorrowed(owner);

            m_applyButton.Component.gameObject.SetActive(owner.CanWrite);

            foreach (var slider in m_sliders)
                slider.interactable = owner.CanWrite;
            foreach (var input in m_inputs)
                input.Component.readOnly = !owner.CanWrite;
        }

        // owner setting value to this
        public override void SetValue(object value)
        {
            OnOwnerSetValue(value);
        }

        private void OnOwnerSetValue(object value)
        {
            if (value is Color32 c32)
            {
                IsValueColor32 = true;
                EditedColor = c32;
                m_inputs[0].Text = c32.r.ToString();
                m_inputs[1].Text = c32.g.ToString();
                m_inputs[2].Text = c32.b.ToString();
                m_inputs[3].Text = c32.a.ToString();
                foreach (var slider in m_sliders)
                    slider.maxValue = 255;
            }
            else
            {
                IsValueColor32 = false;
                EditedColor = (Color)value;
                m_inputs[0].Text = EditedColor.r.ToString();
                m_inputs[1].Text = EditedColor.g.ToString();
                m_inputs[2].Text = EditedColor.b.ToString();
                m_inputs[3].Text = EditedColor.a.ToString();
                foreach (var slider in m_sliders)
                    slider.maxValue = 1;
            }

            if (m_colorImage)
                m_colorImage.color = EditedColor;
        }

        // setting value to owner

        public void SetValueToOwner()
        {
            if (IsValueColor32)
                CurrentOwner.SetUserValue((Color32)EditedColor);
            else
                CurrentOwner.SetUserValue(EditedColor);
        }

        private void SetColorField(float val, int fieldIndex)
        {
            switch (fieldIndex)
            {
                case 0: EditedColor.r = val; break;
                case 1: EditedColor.g = val; break;
                case 2: EditedColor.b = val; break;
                case 3: EditedColor.a = val; break;
            }

            if (m_colorImage)
                m_colorImage.color = EditedColor;
        }

        private void OnInputChanged(string val, int fieldIndex)
        {
            try
            {
                float f;
                if (IsValueColor32)
                {
                    byte value = byte.Parse(val);
                    m_sliders[fieldIndex].value = value;
                    f = (float)((decimal)value / 255);
                }
                else
                {
                    f = float.Parse(val);
                    m_sliders[fieldIndex].value = f;
                }

                SetColorField(f, fieldIndex);
            }
            catch (ArgumentException) { } // ignore bad user input
            catch (FormatException) { }
            catch (OverflowException) { }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning("InteractiveColor OnInput: " + ex.ToString());
            }
        }

        private void OnSliderValueChanged(float val, int fieldIndex)
        {
            try
            {
                if (IsValueColor32)
                {
                    m_inputs[fieldIndex].Text = ((byte)val).ToString();
                    val /= 255f;
                }
                else
                {
                    m_inputs[fieldIndex].Text = val.ToString();
                }

                SetColorField(val, fieldIndex);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning("InteractiveColor OnSlider: " + ex.ToString());
            }
        }

        // UI Construction

        public override GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(parent, "InteractiveColor", false, false, true, true, 3, new Vector4(4, 4, 4, 4),
                new Color(0.06f, 0.06f, 0.06f));

            // hori group

            var horiGroup = UIFactory.CreateHorizontalGroup(UIRoot, "ColorEditor", false, false, true, true, 5,
                default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);

            // sliders / inputs

            var grid = UIFactory.CreateGridGroup(horiGroup, "Grid", new Vector2(140, 25), new Vector2(2, 2), new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(grid, minWidth: 580, minHeight: 25, flexibleWidth: 0);

            for (int i = 0; i < 4; i++)
                AddEditorRow(i, grid);

            // apply button

            m_applyButton = UIFactory.CreateButton(horiGroup, "ApplyButton", "Apply", new Color(0.2f, 0.26f, 0.2f));
            UIFactory.SetLayoutElement(m_applyButton.Component.gameObject, minHeight: 25, minWidth: 90);
            m_applyButton.OnClick += SetValueToOwner;

            // image of color

            var imgObj = UIFactory.CreateUIObject("ColorImageHelper", horiGroup);
            UIFactory.SetLayoutElement(imgObj, minHeight: 25, minWidth: 50, flexibleWidth: 50);
            m_colorImage = imgObj.AddComponent<Image>();

            return UIRoot;
        }

        internal void AddEditorRow(int index, GameObject groupObj)
        {
            var row = UIFactory.CreateHorizontalGroup(groupObj, "EditorRow_" + fieldNames[index],
                false, true, true, true, 5, default, new Color(1, 1, 1, 0));

            var label = UIFactory.CreateLabel(row, "RowLabel", $"{fieldNames[index]}:", TextAnchor.MiddleRight, Color.cyan);
            UIFactory.SetLayoutElement(label.gameObject, minWidth: 17, flexibleWidth: 0, minHeight: 25);

            var input = UIFactory.CreateInputField(row, "Input", "...");
            UIFactory.SetLayoutElement(input.UIRoot, minWidth: 40, minHeight: 25, flexibleHeight: 0);
            m_inputs[index] = input;
            input.OnValueChanged += (string val) => { OnInputChanged(val, index); };

            var sliderObj = UIFactory.CreateSlider(row, "Slider", out Slider slider);
            m_sliders[index] = slider;
            UIFactory.SetLayoutElement(sliderObj, minHeight: 25, minWidth: 70, flexibleWidth: 999, flexibleHeight: 0);
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.onValueChanged.AddListener((float val) => { OnSliderValueChanged(val, index); });
        }
    }
}
