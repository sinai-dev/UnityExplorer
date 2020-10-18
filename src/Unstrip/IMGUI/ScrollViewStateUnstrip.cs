#if CPP
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Explorer.Unstrip.IMGUI
{
    public class ScrollViewStateUnstrip
    {
        public Rect position;
        public Rect visibleRect;
        public Rect viewRect;
        public Vector2 scrollPosition;
        public bool apply;

		public void ScrollTo(Rect pos)
		{
			this.ScrollTowards(pos, float.PositiveInfinity);
		}

		public bool ScrollTowards(Rect pos, float maxDelta)
		{
			Vector2 b = this.ScrollNeeded(pos);
			bool result;
			if (b.sqrMagnitude < 0.0001f)
			{
				result = false;
			}
			else if (maxDelta == 0f)
			{
				result = true;
			}
			else
			{
				if (b.magnitude > maxDelta)
				{
					b = b.normalized * maxDelta;
				}
				this.scrollPosition += b;
				this.apply = true;
				result = true;
			}
			return result;
		}

		private Vector2 ScrollNeeded(Rect pos)
		{
			Rect rect = this.visibleRect;
			rect.x += this.scrollPosition.x;
			rect.y += this.scrollPosition.y;
			float num = pos.width - this.visibleRect.width;
			if (num > 0f)
			{
				pos.width -= num;
				pos.x += num * 0.5f;
			}
			num = pos.height - this.visibleRect.height;
			if (num > 0f)
			{
				pos.height -= num;
				pos.y += num * 0.5f;
			}
			Vector2 zero = Vector2.zero;
			if (pos.xMax > rect.xMax)
			{
				zero.x += pos.xMax - rect.xMax;
			}
			else if (pos.xMin < rect.xMin)
			{
				zero.x -= rect.xMin - pos.xMin;
			}
			if (pos.yMax > rect.yMax)
			{
				zero.y += pos.yMax - rect.yMax;
			}
			else if (pos.yMin < rect.yMin)
			{
				zero.y -= rect.yMin - pos.yMin;
			}
			Rect rect2 = this.viewRect;
			rect2.width = Mathf.Max(rect2.width, this.visibleRect.width);
			rect2.height = Mathf.Max(rect2.height, this.visibleRect.height);
			zero.x = Mathf.Clamp(zero.x, rect2.xMin - this.scrollPosition.x, rect2.xMax - this.visibleRect.width - this.scrollPosition.x);
			zero.y = Mathf.Clamp(zero.y, rect2.yMin - this.scrollPosition.y, rect2.yMax - this.visibleRect.height - this.scrollPosition.y);
			return zero;
		}
	}
}
#endif