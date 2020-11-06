using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityExplorer;
using UnityExplorer.Helpers;

// Basically just to fix an issue with Scrollbars, instead we use a Slider as the scrollbar.
// This class contains only what is needed to update and manage one after creation.
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