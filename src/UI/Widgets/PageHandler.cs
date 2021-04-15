using System;
using System.Collections;
using System.Collections.Generic;
using UnityExplorer.Core.Config;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Utility;

namespace UnityExplorer.UI.Models
{
    public enum Turn
    {
        Left,
        Right
    }

    public class PageHandler : UIModel, IEnumerator
    {
        public PageHandler(SliderScrollbar scroll)
        {
            ItemsPerPage = ConfigManager.Default_Page_Limit?.Value ?? 20;
            m_scrollbar = scroll;
        }

        public event Action OnPageChanged;

        // UI members
        private readonly SliderScrollbar m_scrollbar;
        private GameObject m_pageUIHolder;
        private Text m_currentPageLabel;

        public override GameObject UIRoot => m_pageUIHolder;

        // For now this is just set when the PageHandler is created, based on config.
        // At some point I might make it possible to change this after creation again.
        public int ItemsPerPage { get; }

        // IEnumerator.Current
        public object Current => m_currentIndex;
        private int m_currentIndex = 0;

        public int CurrentPage
        {
            get => m_currentPage;
            set
            {
                if (value < PageCount)
                    m_currentPage = value;
            }
        }
        private int m_currentPage;


        // set and maintained by owner of list
        private int m_listCount;
        public int ListCount
        {
            get => m_listCount;
            set
            {
                m_listCount = value;

                if (PageCount <= 0 && m_pageUIHolder.activeSelf)
                {
                    m_pageUIHolder.SetActive(false);
                }
                else if (PageCount > 0 && !m_pageUIHolder.activeSelf)
                {
                    m_pageUIHolder.SetActive(true);
                }

                RefreshUI();
            }
        }

        public int PageCount => (int)Math.Ceiling(ListCount / (decimal)ItemsPerPage) - 1;

        // The index of the first element of the current page
        public int StartIndex
        {
            get
            {
                int offset = m_currentPage * ItemsPerPage;

                if (offset >= ListCount)
                {
                    offset = 0;
                    m_currentPage = 0;
                }

                return offset;
            }
        }

        public int EndIndex
        {
            get
            {
                int end = StartIndex + ItemsPerPage;
                if (end >= ListCount)
                    end = ListCount - 1;
                return end;
            }
        }

        // IEnumerator.MoveNext()
        public bool MoveNext()
        {
            m_currentIndex++;
            return m_currentIndex < StartIndex + ItemsPerPage;
        }

        // IEnumerator.Reset()
        public void Reset()
        {
            m_currentIndex = StartIndex - 1;
        }

        public IEnumerator<int> GetEnumerator()
        {
            Reset();
            while (MoveNext())
            {
                yield return m_currentIndex;
            }
        }

        public void TurnPage(Turn direction)
        {
            bool didTurn = false;
            if (direction == Turn.Left)
            {
                if (m_currentPage > 0)
                {
                    m_currentPage--;
                    didTurn = true;
                }
            }
            else
            {
                if (m_currentPage < PageCount)
                {
                    m_currentPage++;
                    didTurn = true;
                }
            }
            if (didTurn)
            {
                if (m_scrollbar != null)
                    m_scrollbar.m_scrollbar.value = 1;

                OnPageChanged?.Invoke();
                RefreshUI();
            }
        }

        public void Show() => m_pageUIHolder?.SetActive(true);

        public void Hide() => m_pageUIHolder?.SetActive(false);

        public void RefreshUI()
        {
            m_currentPageLabel.text = $"Page {CurrentPage + 1} / {CurrentPage + 1}";

            // TODO
        }

        public override void ConstructUI(GameObject parent)
        {
            m_pageUIHolder = UIFactory.CreateHorizontalGroup(parent, "PageHandlerButtons", false, true, true, true);

            Image image = m_pageUIHolder.GetComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            UIFactory.SetLayoutElement(m_pageUIHolder, minHeight: 25, minWidth: 100, flexibleWidth: 5000);

            var leftBtnObj = UIFactory.CreateButton(m_pageUIHolder,
                "BackBtn",
                "◄",
                () => { TurnPage(Turn.Left); },
                new Color(0.15f, 0.15f, 0.15f));

            UIFactory.SetLayoutElement(leftBtnObj.gameObject, flexibleWidth: 1500, minWidth: 25, minHeight: 25);

            m_currentPageLabel = UIFactory.CreateLabel(m_pageUIHolder, "PageLabel", "Page 1 / TODO", TextAnchor.MiddleCenter);

            UIFactory.SetLayoutElement(m_currentPageLabel.gameObject, minWidth: 100, flexibleWidth: 40);

            Button rightBtn = UIFactory.CreateButton(m_pageUIHolder,
                "RightBtn",
                "►",
                () => { TurnPage(Turn.Right); },
                new Color(0.15f, 0.15f, 0.15f));

            UIFactory.SetLayoutElement(rightBtn.gameObject, flexibleWidth: 1500, minWidth: 25, minHeight: 25);

            ListCount = 0;
        }
    }
}
