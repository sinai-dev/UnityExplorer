using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ExplorerBeta.Config;

namespace ExplorerBeta.UI.Shared
{
    public enum Turn
    {
        Left,
        Right
    }

    public class PageHandler
    {
        public PageHandler(int listCount)
        {
            ListCount = listCount;
            m_itemsPerPage = ModConfig.Instance?.Default_Page_Limit ?? 20;
        }

        // callback for when the page is turned
        public event Action OnPageChanged;

        // set and maintained by owner of list
        public int ListCount { get; set; }

        // For now this is just set when the PageHandler is created, based on config.
        // At some point I might make it possible to change this after creation again.
        private readonly int m_itemsPerPage;

        private int m_currentPage;

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

        private GameObject m_pageUIHolder;
        private Text m_currentPageLabel;

        public void Show() => m_pageUIHolder?.SetActive(true);

        public void Hide() => m_pageUIHolder?.SetActive(false);

        public void ConstructUI(GameObject parent)
        {
            m_pageUIHolder = UIFactory.CreateHorizontalGroup(parent);

            var image = m_pageUIHolder.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);

            var parentGroup = m_pageUIHolder.GetComponent<HorizontalLayoutGroup>();
            parentGroup.childForceExpandHeight = true;
            parentGroup.childForceExpandWidth = false;
            parentGroup.childControlWidth = true;
            parentGroup.childControlHeight = true;

            var parentLayout = m_pageUIHolder.AddComponent<LayoutElement>();
            parentLayout.minHeight = 45;
            parentLayout.preferredHeight = 45;
            parentLayout.flexibleHeight = 3;
            parentLayout.minWidth = 300;
            parentLayout.preferredWidth = 300;
            parentLayout.flexibleWidth = 0;

            var leftBtnObj = UIFactory.CreateButton(m_pageUIHolder);
            var leftBtn = leftBtnObj.GetComponent<Button>();
#if CPP
            leftBtn.onClick.AddListener(new Action(() => { TurnPage(Turn.Left); }));
#else
            leftBtn.onClick.AddListener(() => { TurnPage(Turn.Left); });
#endif
            var leftBtnText = leftBtnObj.GetComponentInChildren<Text>();
            leftBtnText.text = "<";
            var leftBtnLayout = leftBtnObj.AddComponent<LayoutElement>();
            leftBtnLayout.flexibleHeight = 0;
            leftBtnLayout.flexibleWidth = 0;
            leftBtnLayout.minWidth = 30;
            leftBtnLayout.preferredWidth = 30;
            leftBtnLayout.minHeight = 30;
            leftBtnLayout.preferredHeight = 30;

            var labelObj = UIFactory.CreateLabel(m_pageUIHolder, TextAnchor.MiddleCenter);
            m_currentPageLabel = labelObj.GetComponent<Text>();
            m_currentPageLabel.text = "Page 1 / TODO";
            var textLayout = labelObj.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1.5f;
            textLayout.preferredWidth = 200;

            var rightBtnObj = UIFactory.CreateButton(m_pageUIHolder);
            var rightBtn = rightBtnObj.GetComponent<Button>();
#if CPP
            rightBtn.onClick.AddListener(new Action(() => { TurnPage(Turn.Right); }));
#else
            rightBtn.onClick.AddListener(() => { TurnPage(Turn.Right); });
#endif
            var rightBtnText = rightBtnObj.GetComponentInChildren<Text>();
            rightBtnText.text = ">";
            var rightBtnLayout = rightBtnObj.AddComponent<LayoutElement>();
            rightBtnLayout.flexibleHeight = 0;
            rightBtnLayout.flexibleWidth = 0;
            rightBtnLayout.preferredWidth = 30;
            rightBtnLayout.minWidth = 30;
            rightBtnLayout.minHeight = 30;
            rightBtnLayout.preferredHeight = 30;

            RefreshUI();
        }

        public void RefreshUI()
        {
            m_currentPageLabel.text = $"Page {m_currentPage + 1} / {LastPage + 1}";
        }

#endregion
    }
}
