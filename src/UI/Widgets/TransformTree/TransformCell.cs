using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Widgets
{
    public class TransformCell : ICell
    {
        public bool Enabled => m_enabled;
        private bool m_enabled;

        public TransformTree tree;

        public CachedTransform cachedTransform;
        public int _cellIndex;

        public GameObject uiRoot;

        public Text nameLabel;
        public Button nameButton;

        public Text expandLabel;
        public Button expandButton;

        public LayoutElement spacer;

        public TransformCell(TransformTree tree, GameObject cellUI, Button nameButton, Button expandButton, LayoutElement spacer)
        {
            this.tree = tree;
            this.uiRoot = cellUI;
            this.nameButton = nameButton;
            this.nameLabel = nameButton.GetComponentInChildren<Text>();
            this.expandButton = expandButton;
            this.expandLabel = expandButton.GetComponentInChildren<Text>();
            this.spacer = spacer;

            nameButton.onClick.AddListener(OnMainButtonClicked);
            expandButton.onClick.AddListener(OnExpandClicked);
        }

        //This is called from the SetCell method in DataSource
        public void ConfigureCell(CachedTransform cached, int cellIndex)
        {
            if (cached == null || !cached.Value)
                return;

            if (!Enabled)
                Enable();

            _cellIndex = cellIndex;
            cachedTransform = cached;

            spacer.minWidth = cached.Depth * 15;

            nameLabel.text = cached.Value.name;
            nameLabel.color = cached.Value.gameObject.activeSelf ? Color.white : Color.grey;

            int childCount = cached.Value.childCount;
            if (childCount > 0)
            {
                nameLabel.text = $"<color=grey>[{childCount}]</color> {nameLabel.text}";

                expandButton.interactable = true;
                //expandLabel.enabled = true;
                expandLabel.text = cached.Expanded ? "▼" : "►";
                expandLabel.color = cached.Expanded ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.3f, 0.3f, 0.3f);
            }
            else
            {
                expandButton.interactable = false;
                expandLabel.text = "▪";
                expandLabel.color = new Color(0.3f, 0.3f, 0.3f);
                //expandLabel.enabled = false;
            }
        }

        public void Disable()
        {
            m_enabled = false;
            uiRoot.SetActive(false);
        }

        public void Enable()
        {
            m_enabled = true;
            uiRoot.SetActive(true);
        }

        public void OnExpandClicked()
        {
            tree.ToggleExpandCell(this);
        }

        public void OnMainButtonClicked()
        {
            if (cachedTransform.Value)
                ExplorerCore.Log($"TODO Inspect {cachedTransform.Value.name}");
            else
                ExplorerCore.LogWarning("The object was destroyed!");
        }

        public static RectTransform CreatePrototypeCell(GameObject parent)
        {
            var prototype = UIFactory.CreateHorizontalGroup(parent, "PrototypeCell", true, true, true, true, 2, default,
                new Color(0.15f, 0.15f, 0.15f), TextAnchor.MiddleCenter);
            //var cell = prototype.AddComponent<TransformCell>();
            var rect = prototype.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(25, 25);
            UIFactory.SetLayoutElement(prototype, minWidth: 100, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 0);

            var spacer = UIFactory.CreateUIObject("Spacer", prototype, new Vector2(0, 0));
            UIFactory.SetLayoutElement(spacer, minWidth: 0, flexibleWidth: 0, minHeight: 0, flexibleHeight: 0);

            var expandButton = UIFactory.CreateButton(prototype, "ExpandButton", "►", null);
            UIFactory.SetLayoutElement(expandButton.gameObject, minWidth: 15, flexibleWidth: 0, minHeight: 25, flexibleHeight: 0);

            var nameButton = UIFactory.CreateButton(prototype, "NameButton", "Name", null);
            UIFactory.SetLayoutElement(nameButton.gameObject, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 0);
            var nameLabel = nameButton.GetComponentInChildren<Text>();
            nameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            nameLabel.alignment = TextAnchor.MiddleLeft;

            Color normal = new Color(0.15f, 0.15f, 0.15f);
            Color highlight = new Color(0.25f, 0.25f, 0.25f);
            Color pressed = new Color(0.05f, 0.05f, 0.05f);
            Color disabled = new Color(1, 1, 1, 0);
            RuntimeProvider.Instance.SetColorBlock(expandButton, normal, highlight, pressed, disabled);
            RuntimeProvider.Instance.SetColorBlock(nameButton, normal, highlight, pressed, disabled);

            prototype.SetActive(false);

            return rect;
        }
    }
}
