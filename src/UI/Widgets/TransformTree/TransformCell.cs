using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Inspectors;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Widgets
{
    public class TransformCell : ICell
    {
        public float DefaultHeight => 25f;

        public bool Enabled => m_enabled;
        private bool m_enabled;

        public Action<CachedTransform> OnExpandToggled;
        public Action<CachedTransform> OnEnableToggled;
        public Action<GameObject> OnGameObjectClicked;

        public CachedTransform cachedTransform;
        public int _cellIndex;

        public GameObject UIRoot { get; set; }
        public RectTransform Rect { get; set; }

        public ButtonRef ExpandButton;
        public ButtonRef NameButton;
        public Toggle EnabledToggle;

        public LayoutElement spacer;

        public void Enable()
        {
            m_enabled = true;
            UIRoot.SetActive(true);
        }

        public void Disable()
        {
            m_enabled = false;
            UIRoot.SetActive(false);
        }

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

                EnabledToggle.Set(cached.Value.gameObject.activeSelf, false);

                int childCount = cached.Value.childCount;
                if (childCount > 0)
                {
                    NameButton.ButtonText.text = $"<color=grey>[{childCount}]</color> {NameButton.ButtonText.text}";

                    ExpandButton.Component.interactable = true;
                    ExpandButton.ButtonText.text = cached.Expanded ? "▼" : "►";
                    ExpandButton.ButtonText.color = cached.Expanded ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.3f, 0.3f, 0.3f);
                }
                else
                {
                    ExpandButton.Component.interactable = false;
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

        public void OnMainButtonClicked()
        {
            if (cachedTransform.Value)
                OnGameObjectClicked?.Invoke(cachedTransform.Value.gameObject);
            else
                ExplorerCore.LogWarning("The object was destroyed!");
        }

        public void OnExpandClicked()
        {
            OnExpandToggled?.Invoke(cachedTransform);
        }

        private void OnEnableClicked(bool value)
        {
            OnEnableToggled?.Invoke(cachedTransform);
        }

        public GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateUIObject("TransformCell", parent);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(UIRoot, false, false, true, true, 2, childAlignment: TextAnchor.MiddleCenter);
            Rect = UIRoot.GetComponent<RectTransform>();
            Rect.anchorMin = new Vector2(0, 1);
            Rect.anchorMax = new Vector2(0, 1);
            Rect.pivot = new Vector2(0.5f, 1);
            Rect.sizeDelta = new Vector2(25, 25);
            UIFactory.SetLayoutElement(UIRoot, minWidth: 100, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 0);

            var spacerObj = UIFactory.CreateUIObject("Spacer", UIRoot, new Vector2(0, 0));
            UIFactory.SetLayoutElement(spacerObj, minWidth: 0, flexibleWidth: 0, minHeight: 0, flexibleHeight: 0);
            this.spacer = spacerObj.GetComponent<LayoutElement>();

            // Expand arrow

            ExpandButton = UIFactory.CreateButton(this.UIRoot, "ExpandButton", "►");
            UIFactory.SetLayoutElement(ExpandButton.Component.gameObject, minWidth: 15, flexibleWidth: 0, minHeight: 25, flexibleHeight: 0);

            // Enabled toggle

            var toggleObj = UIFactory.CreateToggle(UIRoot, "BehaviourToggle", out EnabledToggle, out var behavText, default, 17, 17);
            UIFactory.SetLayoutElement(toggleObj, minHeight: 17, flexibleHeight: 0, minWidth: 17);
            EnabledToggle.onValueChanged.AddListener(OnEnableClicked);

            // Name button

            NameButton = UIFactory.CreateButton(this.UIRoot, "NameButton", "Name", null);
            UIFactory.SetLayoutElement(NameButton.Component.gameObject, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 0);
            var nameLabel = NameButton.Component.GetComponentInChildren<Text>();
            nameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            nameLabel.alignment = TextAnchor.MiddleLeft;

            Color normal = new Color(0.11f, 0.11f, 0.11f);
            Color highlight = new Color(0.25f, 0.25f, 0.25f);
            Color pressed = new Color(0.05f, 0.05f, 0.05f);
            Color disabled = new Color(1, 1, 1, 0);
            RuntimeProvider.Instance.SetColorBlock(ExpandButton.Component, normal, highlight, pressed, disabled);
            RuntimeProvider.Instance.SetColorBlock(NameButton.Component, normal, highlight, pressed, disabled);

            NameButton.OnClick += OnMainButtonClicked;
            ExpandButton.OnClick += OnExpandClicked;

            UIRoot.SetActive(false);

            return this.UIRoot;
        }
    }
}
