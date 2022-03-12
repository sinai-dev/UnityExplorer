using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Inspectors;
using UnityExplorer.UI.Widgets;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets;
using UniverseLib.UI.Widgets.ScrollView;
using UniverseLib.Utility;

namespace UnityExplorer.UI.Widgets
{
    public class TransformCell : ICell
    {
        public float DefaultHeight => 25f;

        public bool Enabled => enabled;
        private bool enabled;

        public Action<CachedTransform> OnExpandToggled;
        public Action<CachedTransform> OnEnableToggled;
        public Action<GameObject> OnGameObjectClicked;

        public CachedTransform cachedTransform;
        public int cellIndex;

        public GameObject UIRoot { get; set; }
        public RectTransform Rect { get; set; }

        public ButtonRef ExpandButton;
        public ButtonRef NameButton;
        public Toggle EnabledToggle;
        public InputFieldRef SiblingIndex;

        public LayoutElement spacer;

        public void Enable()
        {
            enabled = true;
            UIRoot.SetActive(true);
        }

        public void Disable()
        {
            enabled = false;
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

            this.cellIndex = cellIndex;
            cachedTransform = cached;

            spacer.minWidth = cached.Depth * 15;

            if (cached.Value)
            {
                string name = cached.Value.name?.Trim();
                if (string.IsNullOrEmpty(name))
                    name = "<i><color=grey>untitled</color></i>";
                NameButton.ButtonText.text = name;
                NameButton.ButtonText.color = cached.Value.gameObject.activeSelf ? Color.white : Color.grey;

                EnabledToggle.Set(cached.Value.gameObject.activeSelf, false);

                if (!cached.Value.parent)
                    SiblingIndex.GameObject.SetActive(false);
                else
                {
                    SiblingIndex.GameObject.SetActive(true);
                    if (!SiblingIndex.Component.isFocused)
                        SiblingIndex.Text = cached.Value.GetSiblingIndex().ToString();
                }

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

                SiblingIndex.GameObject.SetActive(false);
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

        private void OnSiblingIndexEndEdit(string input)
        {
            if (this.cachedTransform == null || !this.cachedTransform.Value)
                return;

            if (int.TryParse(input.Trim(), out int index))
                this.cachedTransform.Value.SetSiblingIndex(index);
            
            this.SiblingIndex.Text = this.cachedTransform.Value.GetSiblingIndex().ToString();
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

            // Sibling index input

            SiblingIndex = UIFactory.CreateInputField(this.UIRoot, "SiblingIndexInput", string.Empty);
            SiblingIndex.Component.textComponent.fontSize = 11;
            SiblingIndex.Component.textComponent.alignment = TextAnchor.MiddleRight;
            var siblingImage = SiblingIndex.GameObject.GetComponent<Image>();
            siblingImage.color = new(0f, 0f, 0f, 0.25f);
            UIFactory.SetLayoutElement(SiblingIndex.GameObject, 35, 20, 0, 0);
            SiblingIndex.Component.GetOnEndEdit().AddListener(OnSiblingIndexEndEdit);

            // Setup selectables

            Color normal = new(0.11f, 0.11f, 0.11f);
            Color highlight = new(0.25f, 0.25f, 0.25f);
            Color pressed = new(0.05f, 0.05f, 0.05f);
            Color disabled = new(1, 1, 1, 0);
            RuntimeHelper.SetColorBlock(ExpandButton.Component, normal, highlight, pressed, disabled);
            RuntimeHelper.SetColorBlock(NameButton.Component, normal, highlight, pressed, disabled);

            NameButton.OnClick += OnMainButtonClicked;
            ExpandButton.OnClick += OnExpandClicked;

            UIRoot.SetActive(false);

            return this.UIRoot;
        }
    }
}
