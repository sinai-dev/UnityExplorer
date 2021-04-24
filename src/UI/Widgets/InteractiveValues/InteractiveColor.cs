using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace UnityExplorer.UI.InteractiveValues
{
    public class InteractiveColor : InteractiveValue
    {
        //~~~~~~~~~ Instance ~~~~~~~~~~

        public InteractiveColor(object value, Type valueType) : base(value, valueType) { }

        public override bool HasSubContent => true;
        public override bool SubContentWanted => true;
        public override bool WantInspectBtn => true;

        public override void RefreshUIForValue()
        {
            base.RefreshUIForValue();

            if (m_subContentConstructed)
                RefreshUI();
        }

        private void RefreshUI()
        {
            var color = (Color)this.Value;

            m_inputs[0].text = color.r.ToString();
            m_inputs[1].text = color.g.ToString();
            m_inputs[2].text = color.b.ToString();
            m_inputs[3].text = color.a.ToString();

            if (m_colorImage)
                m_colorImage.color = color;
        }

        internal override void OnToggleSubcontent(bool toggle)
        {
            base.OnToggleSubcontent(toggle);

            RefreshUI();
        }

        #region UI CONSTRUCTION

        private Image m_colorImage;

        private readonly InputField[] m_inputs = new InputField[4];
        private readonly Slider[] m_sliders = new Slider[4];

        public override void ConstructUI(GameObject parent, GameObject subGroup)
        {
            base.ConstructUI(parent, subGroup);

            //// Limit the label width for colors, they're always about the same so make use of that space.
            //UIFactory.SetLayoutElement(this.m_baseLabel.gameObject, flexibleWidth: 0, minWidth: 250);
        }

        public override void ConstructSubcontent()
        {
            base.ConstructSubcontent();

            var horiGroup = UIFactory.CreateHorizontalGroup(m_subContentParent, "ColorEditor", false, false, true, true, 5,
                default, default, TextAnchor.MiddleLeft);

            var editorContainer = UIFactory.CreateVerticalGroup(horiGroup, "EditorContent", false, true, true, true, 2, new Vector4(4, 4, 4, 4),
                new Color(0.08f, 0.08f, 0.08f));
            UIFactory.SetLayoutElement(editorContainer, minWidth: 300, flexibleWidth: 0);

            for (int i = 0; i < 4; i++)
                AddEditorRow(i, editorContainer);

            if (Owner.CanWrite)
            {
                var applyBtn = UIFactory.CreateButton(editorContainer, "ApplyButton", "Apply", OnSetValue, new Color(0.2f, 0.2f, 0.2f));
                UIFactory.SetLayoutElement(applyBtn.gameObject, minWidth: 175, minHeight: 25, flexibleWidth: 0);

                void OnSetValue()
                {
                    Owner.SetValue();
                    RefreshUIForValue();
                }
            }

            var imgHolder = UIFactory.CreateVerticalGroup(horiGroup, "ImgHolder", true, true, true, true, 0, new Vector4(1, 1, 1, 1),
                new Color(0.08f, 0.08f, 0.08f));
            UIFactory.SetLayoutElement(imgHolder, minWidth: 128, minHeight: 128, flexibleWidth: 0, flexibleHeight: 0);

            var imgObj = UIFactory.CreateUIObject("ColorImageHelper", imgHolder, new Vector2(100, 25));
            m_colorImage = imgObj.AddComponent<Image>();
            m_colorImage.color = (Color)this.Value;
        }

        private static readonly string[] s_fieldNames = new[] { "R", "G", "B", "A" };

        internal void AddEditorRow(int index, GameObject groupObj)
        {
            var row = UIFactory.CreateHorizontalGroup(groupObj, "EditorRow_" + s_fieldNames[index], 
                false, true, true, true, 5, default, new Color(1, 1, 1, 0));

            var label = UIFactory.CreateLabel(row, "RowLabel", $"{s_fieldNames[index]}:", TextAnchor.MiddleRight, Color.cyan);
            UIFactory.SetLayoutElement(label.gameObject, minWidth: 50, flexibleWidth: 0, minHeight: 25);

            var inputFieldObj = UIFactory.CreateInputField(row, "InputField", "...", out InputField inputField, 14, 3, 1);
            UIFactory.SetLayoutElement(inputFieldObj, minWidth: 120, minHeight: 25, flexibleWidth: 0);

            m_inputs[index] = inputField;
            inputField.characterValidation = InputField.CharacterValidation.Decimal;

            inputField.onValueChanged.AddListener((string value) => 
            {
                float val = float.Parse(value);
                SetValueToColor(val);
                m_sliders[index].value = val;
            });

            var sliderObj = UIFactory.CreateSlider(row, "Slider", out Slider slider);
            m_sliders[index] = slider;
            UIFactory.SetLayoutElement(sliderObj, minWidth: 200, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = GetValueFromColor();

            slider.onValueChanged.AddListener((float value) =>
            {
                inputField.text = value.ToString();
                SetValueToColor(value);
                m_inputs[index].text = value.ToString();
            });

            // methods for writing to the color for this field

            void SetValueToColor(float floatValue)
            {
                Color _color = (Color)Value;
                switch (index)
                {
                    case 0: _color.r = floatValue; break;
                    case 1: _color.g = floatValue; break;
                    case 2: _color.b = floatValue; break;
                    case 3: _color.a = floatValue; break;
                }
                Value = _color;
                m_colorImage.color = _color;
            }

            float GetValueFromColor()
            {
                Color _color = (Color)Value;
                switch (index)
                {
                    case 0: return _color.r;
                    case 1: return _color.g;
                    case 2: return _color.b;
                    case 3: return _color.a;
                    default: throw new NotImplementedException();
                }
            }
        }

        #endregion
    }
}
