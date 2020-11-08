using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityExplorer;
using UnityExplorer.Helpers;
using UnityExplorer.UI;

// Basically just to fix an issue with Scrollbars, instead we use a Slider as the scrollbar.
public class SliderScrollbar
{
	internal static readonly List<SliderScrollbar> Instances = new List<SliderScrollbar>();

	internal readonly Scrollbar m_scrollbar;
	internal readonly Slider m_slider;

	public SliderScrollbar(Scrollbar scrollbar, Slider slider)
    {
		Instances.Add(this);

		this.m_scrollbar = scrollbar;
		this.m_slider = slider;

#if MONO
		this.m_scrollbar.onValueChanged.AddListener(this.OnScrollbarValueChanged);
		this.m_slider.onValueChanged.AddListener(this.OnSliderValueChanged);
#else
		this.m_scrollbar.onValueChanged.AddListener(new Action<float>(this.OnScrollbarValueChanged));
		this.m_slider.onValueChanged.AddListener(new Action<float>(this.OnSliderValueChanged));
#endif

		this.RefreshVisibility();
		this.m_slider.Set(1f, false);
	}

	~SliderScrollbar()
    {
		Instances.Remove(this);
    }

	internal void Update()
	{
		this.RefreshVisibility();
	}

	internal void RefreshVisibility()
	{
		if (this.m_slider && this.m_scrollbar)
		{
            bool shouldShow = !Mathf.Approximately(this.m_scrollbar.size, 1);
            var obj = this.m_slider.handleRect.gameObject;

            if (obj.activeSelf != shouldShow)
            {
                obj.SetActive(shouldShow);

                if (shouldShow)
					this.m_slider.Set(this.m_scrollbar.value, false);
				else
                    m_slider.Set(1f, false);
            }
        }
	}

	public void OnScrollbarValueChanged(float _value)
	{
		//this.RefreshVisibility();
		if (this.m_slider && this.m_slider.value != _value)
		{
			this.m_slider.Set(_value, false);
		}
	}

	public void OnSliderValueChanged(float _value)
	{
		if (this.m_scrollbar)
		{
			this.m_scrollbar.value = _value;
		}
	}

    #region UI CONSTRUCTION

    public static GameObject CreateSliderScrollbar(GameObject parent, out Slider slider)
    {
        GameObject sliderObj = UIFactory.CreateUIObject("Slider", parent, UIFactory.thinSize);

        GameObject bgObj = UIFactory.CreateUIObject("Background", sliderObj);
        GameObject fillAreaObj = UIFactory.CreateUIObject("Fill Area", sliderObj);
        GameObject fillObj = UIFactory.CreateUIObject("Fill", fillAreaObj);
        GameObject handleSlideAreaObj = UIFactory.CreateUIObject("Handle Slide Area", sliderObj);
        GameObject handleObj = UIFactory.CreateUIObject("Handle", handleSlideAreaObj);

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.type = Image.Type.Sliced;
        bgImage.color = new Color(0.05f, 0.05f, 0.05f, 1.0f);

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.offsetMax = new Vector2(-10f, 0f);

        RectTransform fillAreaRect = fillAreaObj.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.anchoredPosition = new Vector2(-5f, 0f);
        fillAreaRect.sizeDelta = new Vector2(-20f, 0f);

        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.type = Image.Type.Sliced;
        fillImage.color = Color.clear;

        fillObj.GetComponent<RectTransform>().sizeDelta = new Vector2(10f, 0f);

        RectTransform handleSlideRect = handleSlideAreaObj.GetComponent<RectTransform>();
        handleSlideRect.anchorMin = new Vector2(0f, 0f);
        handleSlideRect.anchorMax = new Vector2(1f, 1f);
        handleSlideRect.offsetMin = new Vector2(15f, 30f);
        handleSlideRect.offsetMax = new Vector2(-15f, 0f);
        handleSlideRect.sizeDelta = new Vector2(-30f, -30f);

        Image handleImage = handleObj.AddComponent<Image>();
        handleImage.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);

        var handleRect = handleObj.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(15f, 30f);
        handleRect.offsetMin = new Vector2(-13f, -28f);
        handleRect.offsetMax = new Vector2(3f, -2f);

        var sliderBarLayout = sliderObj.AddComponent<LayoutElement>();
        sliderBarLayout.minWidth = 25;
        sliderBarLayout.flexibleWidth = 0;
        sliderBarLayout.minHeight = 30;
        sliderBarLayout.flexibleHeight = 5000;

        slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fillObj.GetComponent<RectTransform>();
        slider.handleRect = handleObj.GetComponent<RectTransform>();
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.BottomToTop;
        UIFactory.SetDefaultColorTransitionValues(slider);

        return sliderObj;
    }

    #endregion
}

#if MONO
public static class SliderExtensions
{
	// il2cpp can just use the orig method directly (forced public)

	private static MethodInfo m_setMethod;
	private static MethodInfo SetMethod
    {
		get
        {
			if (m_setMethod == null)
            {
				m_setMethod = typeof(Slider).GetMethod("Set", ReflectionHelpers.CommonFlags, null, new[] { typeof(float), typeof(bool) }, null);
            }
			return m_setMethod;
        }
	}

	public static void Set(this Slider slider, float value, bool invokeCallback)
	{
		SetMethod.Invoke(slider, new object[] { value, invokeCallback });
	}
}
#endif