using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Explorer
{
    public enum Turn
    {
        Left,
        Right
    }

    public class PageHelper
    {
        public int PageOffset { get; set; }
        public int PageLimit { get; set; } = 20;
        public int Count { get; set; }
        public int MaxOffset { get; set; } = -1;

        public int CalculateMaxOffset()
        {
            return MaxOffset = (int)Mathf.Ceil((float)(Count / (decimal)PageLimit)) - 1;
        }

        public void CurrentPageLabel()
        {
            var orig = GUI.skin.label.alignment;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;

            GUILayout.Label($"Page {PageOffset + 1}/{MaxOffset + 1}", new GUILayoutOption[] { GUILayout.Width(80) });

            GUI.skin.label.alignment = orig;
        }

        public void TurnPage(Turn direction)
        {
            var _ = Vector2.zero;
            TurnPage(direction, ref _);
        }

        public void TurnPage(Turn direction, ref Vector2 scroll)
        {
            if (direction == Turn.Left)
            {
                if (PageOffset > 0) 
                {
                    PageOffset--;
                    scroll = Vector2.zero;
                }
            }
            else
            {
                if (PageOffset < MaxOffset)
                {
                    PageOffset++;
                    scroll = Vector2.zero;
                }
            }
        }

        public int CalculateOffsetIndex()
        {
            int offset = PageOffset * PageLimit;

            if (offset >= Count)
            {
                offset = 0;
                PageOffset = 0;
            }

            return offset;
        }

        public void DrawLimitInputArea()
        {
            GUILayout.Label("Limit: ", new GUILayoutOption[] { GUILayout.Width(50) });
            var limit = this.PageLimit.ToString();
            limit = GUILayout.TextField(limit, new GUILayoutOption[] { GUILayout.Width(50) });
            if (limit != PageLimit.ToString() && int.TryParse(limit, out int i))
            {
                PageLimit = i;
            }
        }
    }
}
