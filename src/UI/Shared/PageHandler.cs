using System;
using System.Collections;
using System.Collections.Generic;
using UnityExplorer.Config;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Helpers;

namespace UnityExplorer.UI.Shared
{
    public enum Turn
    {
        Left,
        Right
    }

    // TODO:
    // - Input for setting page directly

    public class PageHandler : IEnumerator
    {
        public PageHandler(SliderScrollbar scroll)
        {
            ItemsPerPage = ModConfig.Instance?.Default_Page_Limit ?? 20;
            m_scrollbar = scroll;
        }

        public event Action OnPageChanged;

        private readonly SliderScrollbar m_scrollbar;

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

        // ui
        private GameObject m_pageUIHolder;
        private Text m_currentPageLabel;

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

        #region UI CONSTRUCTION

        public void Show() => m_pageUIHolder?.SetActive(true);

        public void Hide() => m_pageUIHolder?.SetActive(false);

        public void RefreshUI()
        {
            m_currentPageLabel.text = $"Page {m_currentPage + 1} / {PageCount + 1}";
        }

        public void ConstructUI(GameObject parent)
        {
            m_pageUIHolder = UIFactory.CreateHorizontalGroup(parent);

            Image image = m_pageUIHolder.GetComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            HorizontalLayoutGroup mainGroup = m_pageUIHolder.GetComponent<HorizontalLayoutGroup>();
            mainGroup.childForceExpandHeight = true;
            mainGroup.childForceExpandWidth = false;
            mainGroup.childControlWidth = true;
            mainGroup.childControlHeight = true;

            LayoutElement mainLayout = m_pageUIHolder.AddComponent<LayoutElement>();
            mainLayout.minHeight = 25;
            mainLayout.flexibleHeight = 0;
            mainLayout.minWidth = 100;
            mainLayout.flexibleWidth = 5000;

            GameObject leftBtnObj = UIFactory.CreateButton(m_pageUIHolder, new Color(0.15f, 0.15f, 0.15f));
            Button leftBtn = leftBtnObj.GetComponent<Button>();

            leftBtn.onClick.AddListener(() => { TurnPage(Turn.Left); });

            Text leftBtnText = leftBtnObj.GetComponentInChildren<Text>();
            leftBtnText.text = "◄";
            LayoutElement leftBtnLayout = leftBtnObj.AddComponent<LayoutElement>();
            leftBtnLayout.flexibleHeight = 0f;
            leftBtnLayout.flexibleWidth = 1500f;
            leftBtnLayout.minWidth = 25f;
            leftBtnLayout.minHeight = 25f;

            GameObject labelObj = UIFactory.CreateLabel(m_pageUIHolder, TextAnchor.MiddleCenter);
            m_currentPageLabel = labelObj.GetComponent<Text>();
            m_currentPageLabel.text = "Page 1 / TODO";
            LayoutElement textLayout = labelObj.AddComponent<LayoutElement>();
            textLayout.minWidth = 100f;
            textLayout.flexibleWidth = 40f;

            GameObject rightBtnObj = UIFactory.CreateButton(m_pageUIHolder, new Color(0.15f, 0.15f, 0.15f));
            Button rightBtn = rightBtnObj.GetComponent<Button>();

            rightBtn.onClick.AddListener(() => { TurnPage(Turn.Right); });

            Text rightBtnText = rightBtnObj.GetComponentInChildren<Text>();
            rightBtnText.text = "►";
            LayoutElement rightBtnLayout = rightBtnObj.AddComponent<LayoutElement>();
            rightBtnLayout.flexibleHeight = 0;
            rightBtnLayout.flexibleWidth = 1500f;
            rightBtnLayout.minWidth = 25f;
            rightBtnLayout.minHeight = 25;

            ListCount = 0;
        }

        #endregion
    }
}
