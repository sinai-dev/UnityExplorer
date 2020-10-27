using System;
using ExplorerBeta.Config;
using UnityEngine;
using UnityEngine.UI;

namespace ExplorerBeta.UI.Shared
{
    public enum Turn
    {
        Left,
        Right
    }

    public class PageHandler
    {
        public PageHandler()
        {
            m_itemsPerPage = ModConfig.Instance?.Default_Page_Limit ?? 20;
        }

        public event Action OnPageChanged;

        // For now this is just set when the PageHandler is created, based on config.
        // At some point I might make it possible to change this after creation again.
        public int ItemsPerPage => m_itemsPerPage;
        private int m_itemsPerPage;

        private int m_currentPage;

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

                if (LastPage <= 0 && m_pageUIHolder.activeSelf)
                {
                    m_pageUIHolder.SetActive(false);
                }
                else if (LastPage > 0 && !m_pageUIHolder.activeSelf)
                {
                    m_pageUIHolder.SetActive(true);
                }

                RefreshUI();
            }
        }

        // the last page index (not using "index" to avoid confusion with next property)
        public int LastPage => (int)Math.Ceiling(ListCount / (decimal)m_itemsPerPage) - 1;

        // The index of the first element of the current page
        public int IndexOffset
        {
            get
            {
                int offset = m_currentPage * m_itemsPerPage;

                if (offset >= ListCount)
                {
                    offset = 0;
                    m_currentPage = 0;
                }

                return offset;
            }
        }

        public void TurnPage(Turn direction)
        {
            if (direction == Turn.Left)
            {
                if (m_currentPage > 0)
                {
                    m_currentPage--;
                    OnPageChanged?.Invoke();
                    RefreshUI();
                }
            }
            else
            {
                if (m_currentPage < LastPage)
                {
                    m_currentPage++;
                    OnPageChanged?.Invoke();
                    RefreshUI();
                }
            }
        }

        #region UI

        public void Show() => m_pageUIHolder?.SetActive(true);

        public void Hide() => m_pageUIHolder?.SetActive(false);

        public void RefreshUI()
        {
            m_currentPageLabel.text = $"Page {m_currentPage + 1} / {LastPage + 1}";
        }

        public void ConstructUI(GameObject parent)
        {
            m_pageUIHolder = UIFactory.CreateHorizontalGroup(parent);

            Image image = m_pageUIHolder.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);

            HorizontalLayoutGroup parentGroup = m_pageUIHolder.GetComponent<HorizontalLayoutGroup>();
            parentGroup.childForceExpandHeight = true;
            parentGroup.childForceExpandWidth = false;
            parentGroup.childControlWidth = true;
            parentGroup.childControlHeight = true;

            LayoutElement parentLayout = m_pageUIHolder.AddComponent<LayoutElement>();
            parentLayout.minHeight = 20;
            parentLayout.flexibleHeight = 0;
            parentLayout.minWidth = 290;
            parentLayout.flexibleWidth = 30;

            GameObject leftBtnObj = UIFactory.CreateButton(m_pageUIHolder);
            Button leftBtn = leftBtnObj.GetComponent<Button>();
#if CPP
            leftBtn.onClick.AddListener(new Action(() => { TurnPage(Turn.Left); }));
#else
            leftBtn.onClick.AddListener(() => { TurnPage(Turn.Left); });
#endif
            Text leftBtnText = leftBtnObj.GetComponentInChildren<Text>();
            leftBtnText.text = "<";
            LayoutElement leftBtnLayout = leftBtnObj.AddComponent<LayoutElement>();
            leftBtnLayout.flexibleHeight = 0;
            leftBtnLayout.flexibleWidth = 0;
            leftBtnLayout.minWidth = 50;
            leftBtnLayout.minHeight = 20;

            GameObject labelObj = UIFactory.CreateLabel(m_pageUIHolder, TextAnchor.MiddleCenter);
            m_currentPageLabel = labelObj.GetComponent<Text>();
            m_currentPageLabel.text = "Page 1 / TODO";
            LayoutElement textLayout = labelObj.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1.5f;
            textLayout.preferredWidth = 200;

            GameObject rightBtnObj = UIFactory.CreateButton(m_pageUIHolder);
            Button rightBtn = rightBtnObj.GetComponent<Button>();
#if CPP
            rightBtn.onClick.AddListener(new Action(() => { TurnPage(Turn.Right); }));
#else
            rightBtn.onClick.AddListener(() => { TurnPage(Turn.Right); });
#endif
            Text rightBtnText = rightBtnObj.GetComponentInChildren<Text>();
            rightBtnText.text = ">";
            LayoutElement rightBtnLayout = rightBtnObj.AddComponent<LayoutElement>();
            rightBtnLayout.flexibleHeight = 0;
            rightBtnLayout.flexibleWidth = 0;
            rightBtnLayout.minWidth = 50;
            rightBtnLayout.minHeight = 20;

            ListCount = 0;
        }

        #endregion
    }
}
