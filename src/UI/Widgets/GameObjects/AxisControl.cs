using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.Utility;

namespace UnityExplorer.UI.Widgets
{
    // Handles the slider and +/- buttons for a specific axis of a transform property.

    public class AxisControl
    {
        public readonly Vector3Control parent;

        public readonly int axis;
        public readonly Slider slider;

        public AxisControl(int axis, Slider slider, Vector3Control parentControl)
        {
            this.parent = parentControl;
            this.axis = axis;
            this.slider = slider;
        }

        void OnVectorSliderChanged(float value)
        {
            parent.Owner.CurrentSlidingAxisControl = value == 0f ? null : this;
        }

        void OnVectorMinusClicked()
        {
            parent.Owner.AxisControlOperation(-this.parent.Increment, this.parent, this.axis);
        }

        void OnVectorPlusClicked()
        {
            parent.Owner.AxisControlOperation(this.parent.Increment, this.parent, this.axis);
        }

        public static AxisControl Create(GameObject parent, string title, int axis, Vector3Control owner)
        {
            Text label = UIFactory.CreateLabel(parent, $"Label_{title}", $"{title}:", TextAnchor.MiddleRight, Color.grey);
            UIFactory.SetLayoutElement(label.gameObject, minHeight: 25, minWidth: 30);

            GameObject sliderObj = UIFactory.CreateSlider(parent, $"Slider_{title}", out Slider slider);
            UIFactory.SetLayoutElement(sliderObj, minHeight: 25, minWidth: 75, flexibleWidth: 0);
            slider.m_FillImage.color = Color.clear;

            slider.minValue = -0.1f;
            slider.maxValue = 0.1f;

            AxisControl sliderControl = new(axis, slider, owner);

            slider.onValueChanged.AddListener(sliderControl.OnVectorSliderChanged);

            ButtonRef minusButton = UIFactory.CreateButton(parent, "MinusIncrementButton", "-");
            UIFactory.SetLayoutElement(minusButton.GameObject, minWidth: 20, flexibleWidth: 0, minHeight: 25);
            minusButton.OnClick += sliderControl.OnVectorMinusClicked;

            ButtonRef plusButton = UIFactory.CreateButton(parent, "PlusIncrementButton", "+");
            UIFactory.SetLayoutElement(plusButton.GameObject, minWidth: 20, flexibleWidth: 0, minHeight: 25);
            plusButton.OnClick += sliderControl.OnVectorPlusClicked;

            return sliderControl;
        }
    }
}
