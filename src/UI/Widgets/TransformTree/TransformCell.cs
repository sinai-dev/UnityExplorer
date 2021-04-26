using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Inspectors;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Widgets
{
    public class TransformCell : ICell
    {
        public float DefaultHeight => 25f;

        public bool Enabled => m_enabled;
        private bool m_enabled;

        public Action<CachedTransform> OnExpandToggled;

        public CachedTransform cachedTransform;
        public int _cellIndex;

        public GameObject UIRoot => uiRoot;
        public GameObject uiRoot;
        public RectTransform Rect => m_rect;
        private RectTransform m_rect;

        public ButtonRef ExpandButton;
        public ButtonRef NameButton;

        public LayoutElement spacer;

        public void ConfigureCell(CachedTransform cached, int cellIndex)
        {
            if (cached == null)
            {
                ExplorerCore.LogWarning("Setting TransformTree cell but the CachedTransform is null!");
                return;
            }

            if (!Enabled)
                Enable();

            _cellIndex = cellIndex;
            cachedTransform = cached;

            spacer.minWidth = cached.Depth * 15;

            if (cached.Value)
            {
                NameButton.ButtonText.text = cached.Value.name;
                NameButton.ButtonText.color = cached.Value.gameObject.activeSelf ? Color.white : Color.grey;

                int childCount = cached.Value.childCount;
                if (childCount > 0)
                {
                    NameButton.ButtonText.text = $"<color=grey>[{childCount}]</color> {NameButton.ButtonText.text}";

                    ExpandButton.Button.interactable = true;
                    ExpandButton.ButtonText.text = cached.Expanded ? "▼" : "►";
                    ExpandButton.ButtonText.color = cached.Expanded ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.3f, 0.3f, 0.3f);
                }
                else
                {
                    ExpandButton.Button.interactable = false;
                    ExpandButton.ButtonText.text = "▪";
                    ExpandButton.ButtonText.color = new Color(0.3f, 0.3f, 0.3f);
                }
            }
            else
            {
                NameButton.ButtonText.text = $"[Destroyed]";
                NameButton.ButtonText.color = Color.red;
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
            OnExpandToggled?.Invoke(cachedTransform);
        }

        public void OnMainButtonClicked()
        {
            if (cachedTransform.Value)
                InspectorManager.Inspect(cachedTransform.Value.gameObject);
            else
                ExplorerCore.LogWarning("The object was destroyed!");
        }

        public GameObject CreateContent(GameObject parent)
        {
            uiRoot = UIFactory.CreateUIObject("TransformCell", parent);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(uiRoot, true, true, true, true, 2, childAlignment: TextAnchor.MiddleCenter);
            m_rect = uiRoot.GetComponent<RectTransform>();
            m_rect.anchorMin = new Vector2(0, 1);
            m_rect.anchorMax = new Vector2(0, 1);
            m_rect.pivot = new Vector2(0.5f, 1);
            m_rect.sizeDelta = new Vector2(25, 25);
            UIFactory.SetLayoutElement(uiRoot, minWidth: 100, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 0);

            var spacerObj = UIFactory.CreateUIObject("Spacer", uiRoot, new Vector2(0, 0));
            UIFactory.SetLayoutElement(spacerObj, minWidth: 0, flexibleWidth: 0, minHeight: 0, flexibleHeight: 0);
            this.spacer = spacerObj.GetComponent<LayoutElement>();

            ExpandButton = UIFactory.CreateButton(this.uiRoot, "ExpandButton", "►");
            UIFactory.SetLayoutElement(ExpandButton.Button.gameObject, minWidth: 15, flexibleWidth: 0, minHeight: 25, flexibleHeight: 0);

            NameButton = UIFactory.CreateButton(this.uiRoot, "NameButton", "Name", null);
            UIFactory.SetLayoutElement(NameButton.Button.gameObject, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 0);
            var nameLabel = NameButton.Button.GetComponentInChildren<Text>();
            nameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            nameLabel.alignment = TextAnchor.MiddleLeft;

            Color normal = new Color(0.11f, 0.11f, 0.11f);
            Color highlight = new Color(0.25f, 0.25f, 0.25f);
            Color pressed = new Color(0.05f, 0.05f, 0.05f);
            Color disabled = new Color(1, 1, 1, 0);
            RuntimeProvider.Instance.SetColorBlock(ExpandButton.Button, normal, highlight, pressed, disabled);
            RuntimeProvider.Instance.SetColorBlock(NameButton.Button, normal, highlight, pressed, disabled);

            NameButton.OnClick += OnMainButtonClicked;
            ExpandButton.OnClick += OnExpandClicked;

            uiRoot.SetActive(false);

            return this.uiRoot;
        }
    }
}
