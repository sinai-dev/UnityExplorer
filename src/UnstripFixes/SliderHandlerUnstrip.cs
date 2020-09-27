#if CPP
using System;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace Explorer
{
    public struct SliderHandlerUnstrip
	{
		private readonly Rect position;
		private readonly float currentValue;
		private readonly float size;
		private readonly float start;
		private readonly float end;
		private readonly GUIStyle slider;
		private readonly GUIStyle thumb;
		private readonly bool horiz;
		private readonly int id;

		public SliderHandlerUnstrip(Rect position, float currentValue, float size, float start, float end, GUIStyle slider, GUIStyle thumb, bool horiz, int id)
		{
			this.position = position;
			this.currentValue = currentValue;
			this.size = size;
			this.start = start;
			this.end = end;
			this.slider = slider;
			this.thumb = thumb;
			this.horiz = horiz;
			this.id = id;
		}

		public float Handle()
		{
			float result;
			if (this.slider == null || this.thumb == null)
			{
				result = this.currentValue;
			}
			else
			{
				switch (this.CurrentEventType())
				{
					case EventType.MouseDown:
						return this.OnMouseDown();
					case EventType.MouseUp:
						return this.OnMouseUp();
					case EventType.MouseDrag:
						return this.OnMouseDrag();
					case EventType.Repaint:
						return this.OnRepaint();
				}
				result = this.currentValue;
			}
			return result;
		}

		private float OnMouseDown()
		{
			float result;
			if (!this.position.Contains(this.CurrentEvent().mousePosition) || this.IsEmptySlider())
			{
				result = this.currentValue;
			}
			else
			{
				GUI.scrollTroughSide = 0;
				GUIUtility.hotControl = this.id;
				this.CurrentEvent().Use();
				if (this.ThumbSelectionRect().Contains(this.CurrentEvent().mousePosition))
				{
					this.StartDraggingWithValue(this.ClampedCurrentValue());
					result = this.currentValue;
				}
				else
				{
					GUI.changed = true;
					if (this.SupportsPageMovements())
					{
						this.SliderState().isDragging = false;
						GUIUnstrip.nextScrollStepTime = DateTime.Now.AddMilliseconds(250.0);
						GUI.scrollTroughSide = this.CurrentScrollTroughSide();
						result = this.PageMovementValue();
					}
					else
					{
						float num = this.ValueForCurrentMousePosition();
						this.StartDraggingWithValue(num);
						result = this.Clamp(num);
					}
				}
			}
			return result;
		}

		private float OnMouseDrag()
		{
			float result;
			if (GUIUtility.hotControl != this.id)
			{
				result = this.currentValue;
			}
			else
			{
				SliderState sliderState = this.SliderState();
				if (!sliderState.isDragging)
				{
					result = this.currentValue;
				}
				else
				{
					GUI.changed = true;
					this.CurrentEvent().Use();
					float num = this.MousePosition() - sliderState.dragStartPos;
					float value = sliderState.dragStartValue + num / this.ValuesPerPixel();
					result = this.Clamp(value);
				}
			}
			return result;
		}

		private float OnMouseUp()
		{
			if (GUIUtility.hotControl == this.id)
			{
				this.CurrentEvent().Use();
				GUIUtility.hotControl = 0;
			}
			return this.currentValue;
		}

		private float OnRepaint()
		{
			this.slider.Draw(this.position, GUIContent.none, this.id);
			if (!this.IsEmptySlider() && this.currentValue >= this.MinValue() && this.currentValue <= this.MaxValue())
			{
				this.thumb.Draw(this.ThumbRect(), GUIContent.none, this.id);
			}
			float result;
			if (GUIUtility.hotControl != this.id || !this.position.Contains(this.CurrentEvent().mousePosition) || this.IsEmptySlider())
			{
				result = this.currentValue;
			}
			else if (this.ThumbRect().Contains(this.CurrentEvent().mousePosition))
			{
				if (GUI.scrollTroughSide != 0)
				{
					GUIUtility.hotControl = 0;
				}
				result = this.currentValue;
			}
			else
			{
				GUI.InternalRepaintEditorWindow();
				if (DateTime.Now < GUIUnstrip.nextScrollStepTime)
				{
					result = this.currentValue;
				}
				else if (this.CurrentScrollTroughSide() != GUI.scrollTroughSide)
				{
					result = this.currentValue;
				}
				else
				{
					GUIUnstrip.nextScrollStepTime = DateTime.Now.AddMilliseconds(30.0);
					if (this.SupportsPageMovements())
					{
						this.SliderState().isDragging = false;
						GUI.changed = true;
						result = this.PageMovementValue();
					}
					else
					{
						result = this.ClampedCurrentValue();
					}
				}
			}
			return result;
		}

		private EventType CurrentEventType()
		{
			return this.CurrentEvent().GetTypeForControl(this.id);
		}


		private int CurrentScrollTroughSide()
		{
			float num = (!this.horiz) ? this.CurrentEvent().mousePosition.y : this.CurrentEvent().mousePosition.x;
			float num2 = (!this.horiz) ? this.ThumbRect().y : this.ThumbRect().x;
			return (num <= num2) ? -1 : 1;
		}

		private bool IsEmptySlider()
		{
			return this.start == this.end;
		}

		private bool SupportsPageMovements()
		{
			return this.size != 0f && GUI.usePageScrollbars;
		}

		private float PageMovementValue()
		{
			float num = this.currentValue;
			int num2 = (this.start <= this.end) ? 1 : -1;
			if (this.MousePosition() > this.PageUpMovementBound())
			{
				num += this.size * (float)num2 * 0.9f;
			}
			else
			{
				num -= this.size * (float)num2 * 0.9f;
			}
			return this.Clamp(num);
		}

		private float PageUpMovementBound()
		{
			float result;
			if (this.horiz)
			{
				result = this.ThumbRect().xMax - this.position.x;
			}
			else
			{
				result = this.ThumbRect().yMax - this.position.y;
			}
			return result;
		}

		private Event CurrentEvent()
		{
			return Event.current;
		}

		private float ValueForCurrentMousePosition()
		{
			float result;
			if (this.horiz)
			{
				result = (this.MousePosition() - this.ThumbRect().width * 0.5f) / this.ValuesPerPixel() + this.start - this.size * 0.5f;
			}
			else
			{
				result = (this.MousePosition() - this.ThumbRect().height * 0.5f) / this.ValuesPerPixel() + this.start - this.size * 0.5f;
			}
			return result;
		}

		private float Clamp(float value)
		{
			return Mathf.Clamp(value, this.MinValue(), this.MaxValue());
		}

		private Rect ThumbSelectionRect()
		{
			return this.ThumbRect();
		}

		private void StartDraggingWithValue(float dragStartValue)
		{
			SliderState sliderState = this.SliderState();
			sliderState.dragStartPos = this.MousePosition();
			sliderState.dragStartValue = dragStartValue;
			sliderState.isDragging = true;
		}

		private SliderState SliderState()
		{
			return (SliderState)GUIUtility.GetStateObject(Il2CppType.Of<SliderState>(), this.id).TryCast<SliderState>();
		}

		private Rect ThumbRect()
		{
			return (!this.horiz) ? this.VerticalThumbRect() : this.HorizontalThumbRect();
		}

		private Rect VerticalThumbRect()
		{
			float num = this.ValuesPerPixel();
			Rect result;
			if (this.start < this.end)
			{
				result = new Rect(this.position.x + (float)this.slider.padding.left, (this.ClampedCurrentValue() - this.start) * num + this.position.y + (float)this.slider.padding.top, this.position.width - (float)this.slider.padding.horizontal, this.size * num + this.ThumbSize());
			}
			else
			{
				result = new Rect(this.position.x + (float)this.slider.padding.left, (this.ClampedCurrentValue() + this.size - this.start) * num + this.position.y + (float)this.slider.padding.top, this.position.width - (float)this.slider.padding.horizontal, this.size * -num + this.ThumbSize());
			}
			return result;
		}

		private Rect HorizontalThumbRect()
		{
			float num = this.ValuesPerPixel();
			Rect result;
			if (this.start < this.end)
			{
				result = new Rect((this.ClampedCurrentValue() - this.start) * num + this.position.x + (float)this.slider.padding.left, this.position.y + (float)this.slider.padding.top, this.size * num + this.ThumbSize(), this.position.height - (float)this.slider.padding.vertical);
			}
			else
			{
				result = new Rect((this.ClampedCurrentValue() + this.size - this.start) * num + this.position.x + (float)this.slider.padding.left, this.position.y, this.size * -num + this.ThumbSize(), this.position.height);
			}
			return result;
		}

		private float ClampedCurrentValue()
		{
			return this.Clamp(this.currentValue);
		}

		private float MousePosition()
		{
			float result;
			if (this.horiz)
			{
				result = this.CurrentEvent().mousePosition.x - this.position.x;
			}
			else
			{
				result = this.CurrentEvent().mousePosition.y - this.position.y;
			}
			return result;
		}

		private float ValuesPerPixel()
		{
			float result;
			if (this.horiz)
			{
				result = (this.position.width - (float)this.slider.padding.horizontal - this.ThumbSize()) / (this.end - this.start);
			}
			else
			{
				result = (this.position.height - (float)this.slider.padding.vertical - this.ThumbSize()) / (this.end - this.start);
			}
			return result;
		}

		private float ThumbSize()
		{
			float result;
			if (this.horiz)
			{
				result = ((this.thumb.fixedWidth == 0f) ? ((float)this.thumb.padding.horizontal) : this.thumb.fixedWidth);
			}
			else
			{
				result = ((this.thumb.fixedHeight == 0f) ? ((float)this.thumb.padding.vertical) : this.thumb.fixedHeight);
			}
			return result;
		}

		private float MaxValue()
		{
			return Mathf.Max(this.start, this.end) - this.size;
		}

		private float MinValue()
		{
			return Mathf.Min(this.start, this.end);
		}

	}
}
#endif