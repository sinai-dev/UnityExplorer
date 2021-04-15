using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Widgets.InfiniteScroll;

namespace UnityExplorer.UI.Widgets
{
    public class TransformCell : MonoBehaviour, ICell
    {
        public bool Enabled => m_enabled;
        private bool m_enabled;

        public TransformTree tree;
        internal CachedTransform cachedTransform;
        internal int _cellIndex;

        public Text nameLabel;
        public Button nameButton;

        public Text expandLabel;
        public Button expandButton;

        public LayoutElement spacer;

        internal void Start()
        {
            nameButton.onClick.AddListener(OnMainButtonClicked);
            expandButton.onClick.AddListener(OnExpandClicked);
        }

        //This is called from the SetCell method in DataSource
        public void ConfigureCell(CachedTransform cached, int cellIndex)
        {
            if (!Enabled)
                Enable();

            _cellIndex = cellIndex;
            cachedTransform = cached;

            spacer.minWidth = cached.Depth * 15;

            nameLabel.text = cached.Name;
            nameLabel.color = cached.RefTransform.gameObject.activeSelf ? Color.white : Color.grey;

            if (cached.ChildCount > 0)
            {
                nameLabel.text = $"<color=grey>[{cached.ChildCount}]</color> {nameLabel.text}";

                expandButton.interactable = true;
                expandLabel.enabled = true;
                expandLabel.text = cached.Expanded ? "▼" : "►";
                expandLabel.color = cached.Expanded ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.3f, 0.3f, 0.3f);
            }
            else
            {
                expandButton.interactable = false;
                expandLabel.enabled = false;
            }
        }

        public void Disable()
        {
            m_enabled = false;
            this.gameObject.SetActive(false);
        }

        public void Enable()
        {
            m_enabled = true;
            this.gameObject.SetActive(true);
        }

        public void OnExpandClicked()
        {
            tree.ToggleExpandCell(this);
        }

        public void OnMainButtonClicked()
        {
            Debug.Log($"TODO Inspect {cachedTransform.RefTransform.name}");
        }
    }
}
