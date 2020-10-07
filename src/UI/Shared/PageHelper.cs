using UnityEngine;

namespace Explorer.UI.Shared
{
    public enum Turn
    {
        Left,
        Right
    }

    public class PageHelper
    {
        public int PageOffset { get; set; }

        public int ItemsPerPage
        {
            get => m_itemsPerPage;
            set
            {
                m_itemsPerPage = value;
                CalculateMaxOffset();
            }
        }
        private int m_itemsPerPage = Config.ModConfig.Instance.Default_Page_Limit;

        public int ItemCount
        {
            get => m_count;
            set
            {
                m_count = value;
                CalculateMaxOffset();
            }
        }
        private int m_count;

        public int MaxPageOffset { get; private set; } = -1;

        private int CalculateMaxOffset()
        {
            return MaxPageOffset = (int)Mathf.Ceil((float)(ItemCount / (decimal)ItemsPerPage)) - 1;
        }

        public void CurrentPageLabel()
        {
            var orig = GUI.skin.label.alignment;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;

            GUILayout.Label($"Page {PageOffset + 1}/{MaxPageOffset + 1}", new GUILayoutOption[] { GUILayout.Width(80) });

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
                if (PageOffset < MaxPageOffset)
                {
                    PageOffset++;
                    scroll = Vector2.zero;
                }
            }
        }

        public int CalculateOffsetIndex()
        {
            int offset = PageOffset * ItemsPerPage;

            if (offset >= ItemCount)
            {
                offset = 0;
                PageOffset = 0;
            }

            return offset;
        }

        public void DrawLimitInputArea()
        {
            GUILayout.Label("Limit: ", new GUILayoutOption[] { GUILayout.Width(50) });
            var limit = this.ItemsPerPage.ToString();
            limit = GUIUnstrip.TextField(limit, new GUILayoutOption[] { GUILayout.Width(50) });
            if (limit != ItemsPerPage.ToString() && int.TryParse(limit, out int i))
            {
                ItemsPerPage = i;
            }
        }
    }
}
