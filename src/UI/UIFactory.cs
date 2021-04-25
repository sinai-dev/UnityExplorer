using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI.Models;
using UnityExplorer.UI.Utility;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI
{
    public static class UIFactory
    {
        internal static Vector2 _largeElementSize = new Vector2(160f, 30f);
        internal static Vector2 _smallElementSize = new Vector2(160f, 20f);
        internal static Color _defaultTextColor = Color.white;
        internal static Font _defaultFont;

        public static void Init()
        {
            _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        public static GameObject CreateUIObject(string name, GameObject parent = null, Vector2 size = default)
        {
            if (!parent)
            {
                ExplorerCore.LogWarning("Cannot create UI object as the parent is null or destroyed! (" + name + ")");
                return null;
            }

            var obj = new GameObject(name)
            {
                layer = 5,
                hideFlags = HideFlags.HideAndDontSave,
            };

            obj.transform.SetParent(parent.transform, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = size == default
                                ? _smallElementSize
                                : size;
            return obj;
        }

        internal static void SetDefaultTextValues(Text text)
        {
            text.color = _defaultTextColor;
            text.font = _defaultFont;
            text.fontSize = 14;
        }

        internal static void SetDefaultSelectableColors(Selectable selectable)
        {
            RuntimeProvider.Instance.SetColorBlock(selectable, new Color(0.2f, 0.2f, 0.2f),
                new Color(0.3f, 0.3f, 0.3f), new Color(0.15f, 0.15f, 0.15f));

            // Deselect all Buttons after they are clicked.
            if (selectable is Button button)
                button.onClick.AddListener(() => { button.OnDeselect(null); });
        }

        /// <summary>
        /// Get and/or Add a LayoutElement component to the GameObject, and set any of the values on it.
        /// </summary>
        public static LayoutElement SetLayoutElement(GameObject gameObject, int? minWidth = null, int? minHeight = null,
            int? flexibleWidth = null, int? flexibleHeight = null, int? preferredWidth = null, int? preferredHeight = null,
            bool? ignoreLayout = null)
        {
            var layout = gameObject.GetComponent<LayoutElement>();
            if (!layout)
                layout = gameObject.AddComponent<LayoutElement>();

            if (minWidth != null)
                layout.minWidth = (int)minWidth;

            if (minHeight != null)
                layout.minHeight = (int)minHeight;

            if (flexibleWidth != null)
                layout.flexibleWidth = (int)flexibleWidth;

            if (flexibleHeight != null)
                layout.flexibleHeight = (int)flexibleHeight;

            if (preferredWidth != null)
                layout.preferredWidth = (int)preferredWidth;

            if (preferredHeight != null)
                layout.preferredHeight = (int)preferredHeight;

            if (ignoreLayout != null)
                layout.ignoreLayout = (bool)ignoreLayout;

            return layout;
        }

        /// <summary>
        /// Get and/or Add a HorizontalOrVerticalLayoutGroup (must pick one) to the GameObject, and set the values on it.
        /// </summary>
        public static T SetLayoutGroup<T>(GameObject gameObject, bool? forceWidth = null, bool? forceHeight = null,
            bool? childControlWidth = null, bool? childControlHeight = null, int? spacing = null, int? padTop = null,
            int? padBottom = null, int? padLeft = null, int? padRight = null, TextAnchor? childAlignment = null)
            where T : HorizontalOrVerticalLayoutGroup
        {
            var group = gameObject.GetComponent<T>();
            if (!group)
                group = gameObject.AddComponent<T>();

            return SetLayoutGroup(group, forceWidth, forceHeight, childControlWidth, childControlHeight, spacing, padTop,
                padBottom, padLeft, padRight, childAlignment);
        }

        /// <summary>
        /// Set the values on a HorizontalOrVerticalLayoutGroup.
        /// </summary>
        public static T SetLayoutGroup<T>(T group, bool? forceWidth = null, bool? forceHeight = null,
            bool? childControlWidth = null, bool? childControlHeight = null, int? spacing = null, int? padTop = null,
            int? padBottom = null, int? padLeft = null, int? padRight = null, TextAnchor? childAlignment = null)
            where T : HorizontalOrVerticalLayoutGroup
        {
            if (forceWidth != null)
                group.childForceExpandWidth = (bool)forceWidth;
            if (forceHeight != null)
                group.childForceExpandHeight = (bool)forceHeight;
            if (childControlWidth != null)
                group.SetChildControlWidth((bool)childControlWidth);
            if (childControlHeight != null)
                group.SetChildControlHeight((bool)childControlHeight);
            if (spacing != null)
                group.spacing = (int)spacing;
            if (padTop != null)
                group.padding.top = (int)padTop;
            if (padBottom != null)
                group.padding.bottom = (int)padBottom;
            if (padLeft != null)
                group.padding.left = (int)padLeft;
            if (padRight != null)
                group.padding.right = (int)padRight;
            if (childAlignment != null)
                group.childAlignment = (TextAnchor)childAlignment;

            return group;
        }

        /// <summary>
        /// Create a Panel on the UI Canvas.
        /// </summary>
        public static GameObject CreatePanel(string name, out GameObject contentHolder, Color? bgColor = null)
        {
            var panelObj = CreateUIObject(name, UIManager.PanelHolder);
            var rect = panelObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            var maskImg = panelObj.AddComponent<Image>();
            maskImg.color = Color.white;
            panelObj.AddComponent<Mask>().showMaskGraphic = false;

            SetLayoutGroup<VerticalLayoutGroup>(panelObj, true, true, true, true);

            contentHolder = CreateUIObject("Content", panelObj);

            Image bgImage = contentHolder.AddComponent<Image>();
            bgImage.type = Image.Type.Filled;
            if (bgColor == null)
                bgImage.color = new Color(0.1f, 0.1f, 0.1f);
            else
                bgImage.color = (Color)bgColor;

            SetLayoutGroup<VerticalLayoutGroup>(contentHolder, true, true, true, true, 3, 3, 3, 3, 3);

            return panelObj;
        }

        /// <summary>
        /// Create a VerticalLayoutGroup object.
        /// </summary>
        public static GameObject CreateVerticalGroup(GameObject parent, string name, bool forceWidth, bool forceHeight,
            bool childControlWidth, bool childControlHeight, int spacing = 0, Vector4 padding = default, Color bgColor = default,
            TextAnchor? childAlignment = null)
        {
            GameObject groupObj = CreateUIObject(name, parent);

            SetLayoutGroup<VerticalLayoutGroup>(groupObj, forceWidth, forceHeight, childControlWidth, childControlHeight,
                spacing, (int)padding.x, (int)padding.y, (int)padding.z, (int)padding.w, childAlignment);

            Image image = groupObj.AddComponent<Image>();
            image.color = bgColor == default
                            ? new Color(0.17f, 0.17f, 0.17f)
                            : bgColor;

            return groupObj;
        }

        /// <summary>
        /// Create a HorizontalLayoutGroup object.
        /// </summary>
        public static GameObject CreateHorizontalGroup(GameObject parent, string name, bool forceExpandWidth, bool forceExpandHeight,
            bool childControlWidth, bool childControlHeight, int spacing = 0, Vector4 padding = default, Color bgColor = default,
            TextAnchor? childAlignment = null)
        {
            GameObject groupObj = CreateUIObject(name, parent);

            SetLayoutGroup<HorizontalLayoutGroup>(groupObj, forceExpandWidth, forceExpandHeight, childControlWidth, childControlHeight,
                spacing, (int)padding.x, (int)padding.y, (int)padding.z, (int)padding.w, childAlignment);

            Image image = groupObj.AddComponent<Image>();
            image.color = bgColor == default
                            ? new Color(0.17f, 0.17f, 0.17f)
                            : bgColor;

            return groupObj;
        }

        /// <summary>
        /// Create a GridLayoutGroup object.
        /// </summary>
        public static GameObject CreateGridGroup(GameObject parent, string name, Vector2 cellSize, Vector2 spacing, Color bgColor = default)
        {
            var groupObj = CreateUIObject(name, parent);

            GridLayoutGroup gridGroup = groupObj.AddComponent<GridLayoutGroup>();
            gridGroup.childAlignment = TextAnchor.UpperLeft;
            gridGroup.cellSize = cellSize;
            gridGroup.spacing = spacing;

            Image image = groupObj.AddComponent<Image>();

            image.color = bgColor == default
                ? new Color(0.17f, 0.17f, 0.17f)
                : bgColor;

            return groupObj;
        }

        /// <summary>
        /// Create a Label object.
        /// </summary>
        public static Text CreateLabel(GameObject parent, string name, string text, TextAnchor alignment,
            Color color = default, bool supportRichText = true, int fontSize = 14)
        {
            var obj = CreateUIObject(name, parent);
            var textComp = obj.AddComponent<Text>();

            SetDefaultTextValues(textComp);

            textComp.text = text;
            textComp.color = color == default ? _defaultTextColor : color;
            textComp.supportRichText = supportRichText;
            textComp.alignment = alignment;
            textComp.fontSize = fontSize;

            return textComp;
        }

        public static Button CreateButton(GameObject parent, string name, string text, Action onClick = null, Color? normalColor = null)
        {
            var colors = new ColorBlock();
            normalColor = normalColor ?? new Color(0.25f, 0.25f, 0.25f);

            var btn = CreateButton(parent, name, text, onClick, colors);

            RuntimeProvider.Instance.SetColorBlock(btn, normalColor, new Color(0.4f, 0.4f, 0.4f), new Color(0.15f, 0.15f, 0.15f));

            return btn;
        }

        public static Button CreateButton(GameObject parent, string name, string text, Action onClick, ColorBlock colors)
        {
            GameObject buttonObj = CreateUIObject(name, parent, _smallElementSize);

            var textObj = CreateUIObject("Text", buttonObj);

            Image image = buttonObj.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = new Color(1, 1, 1, 1);

            var button = buttonObj.AddComponent<Button>();
            SetDefaultSelectableColors(button);

            colors.colorMultiplier = 1;
            RuntimeProvider.Instance.SetColorBlock(button, colors);

            Text textComp = textObj.AddComponent<Text>();
            textComp.text = text;
            SetDefaultTextValues(textComp);
            textComp.alignment = TextAnchor.MiddleCenter;

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            if (onClick != null)
                button.onClick.AddListener(onClick);

            return button;
        }

        /// <summary>
        /// Create a Slider control.
        /// </summary>
        public static GameObject CreateSlider(GameObject parent, string name, out Slider slider)
        {
            GameObject sliderObj = CreateUIObject(name, parent, _smallElementSize);

            GameObject bgObj = CreateUIObject("Background", sliderObj);
            GameObject fillAreaObj = CreateUIObject("Fill Area", sliderObj);
            GameObject fillObj = CreateUIObject("Fill", fillAreaObj);
            GameObject handleSlideAreaObj = CreateUIObject("Handle Slide Area", sliderObj);
            GameObject handleObj = CreateUIObject("Handle", handleSlideAreaObj);

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0.15f, 0.15f, 0.15f, 1.0f);

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.25f);
            bgRect.anchorMax = new Vector2(1f, 0.75f);
            bgRect.sizeDelta = new Vector2(0f, 0f);

            RectTransform fillAreaRect = fillAreaObj.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.anchoredPosition = new Vector2(-5f, 0f);
            fillAreaRect.sizeDelta = new Vector2(-20f, 0f);

            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.type = Image.Type.Sliced;
            fillImage.color = new Color(0.3f, 0.3f, 0.3f, 1.0f);

            fillObj.GetComponent<RectTransform>().sizeDelta = new Vector2(10f, 0f);

            RectTransform handleSlideRect = handleSlideAreaObj.GetComponent<RectTransform>();
            handleSlideRect.sizeDelta = new Vector2(-20f, 0f);
            handleSlideRect.anchorMin = new Vector2(0f, 0f);
            handleSlideRect.anchorMax = new Vector2(1f, 1f);

            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);

            handleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(20f, 0f);

            slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillObj.GetComponent<RectTransform>();
            slider.handleRect = handleObj.GetComponent<RectTransform>();
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;

            RuntimeProvider.Instance.SetColorBlock(slider, new Color(0.4f, 0.4f, 0.4f),
                new Color(0.55f, 0.55f, 0.55f), new Color(0.3f, 0.3f, 0.3f));

            return sliderObj;
        }

        /// <summary>
        /// Create a Scrollbar control.
        /// </summary>
        public static GameObject CreateScrollbar(GameObject parent, string name, out Scrollbar scrollbar)
        {
            GameObject scrollObj = CreateUIObject(name, parent, _smallElementSize);

            GameObject slideAreaObj = CreateUIObject("Sliding Area", scrollObj);
            GameObject handleObj = CreateUIObject("Handle", slideAreaObj);

            Image scrollImage = scrollObj.AddComponent<Image>();
            scrollImage.type = Image.Type.Sliced;
            scrollImage.color = new Color(0.1f, 0.1f, 0.1f);

            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.type = Image.Type.Sliced;
            handleImage.color = new Color(0.4f, 0.4f, 0.4f);

            RectTransform slideAreaRect = slideAreaObj.GetComponent<RectTransform>();
            slideAreaRect.sizeDelta = new Vector2(-20f, -20f);
            slideAreaRect.anchorMin = Vector2.zero;
            slideAreaRect.anchorMax = Vector2.one;

            RectTransform handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20f, 20f);

            scrollbar = scrollObj.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;

            SetDefaultSelectableColors(scrollbar);

            return scrollObj;
        }

        /// <summary>
        /// Create a Toggle control.
        /// </summary>
        public static GameObject CreateToggle(GameObject parent, string name, out Toggle toggle, out Text text, Color bgColor = default)
        {
            GameObject toggleObj = CreateUIObject(name, parent, _smallElementSize);

            GameObject bgObj = CreateUIObject("Background", toggleObj);
            GameObject checkObj = CreateUIObject("Checkmark", bgObj);
            GameObject labelObj = CreateUIObject("Label", toggleObj);

            toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = true;
            Toggle toggleComp = toggle;

            toggle.onValueChanged.AddListener(Deselect);
            void Deselect(bool _)
            {
                toggleComp.OnDeselect(null);
            }

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = bgColor == default
                ? new Color(0.2f, 0.2f, 0.2f, 1.0f)
                : bgColor;

            Image checkImage = checkObj.AddComponent<Image>();
            checkImage.color = new Color(0.3f, 0.5f, 0.3f, 1.0f);

            text = labelObj.AddComponent<Text>();
            text.text = "Toggle";
            SetDefaultTextValues(text);

            toggle.graphic = checkImage;
            toggle.targetGraphic = bgImage;
            SetDefaultSelectableColors(toggle);

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 1f);
            bgRect.anchorMax = new Vector2(0f, 1f);
            bgRect.anchoredPosition = new Vector2(13f, -13f);
            bgRect.sizeDelta = new Vector2(20f, 20f);

            RectTransform checkRect = checkObj.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.5f, 0.5f);
            checkRect.anchorMax = new Vector2(0.5f, 0.5f);
            checkRect.anchoredPosition = Vector2.zero;
            checkRect.sizeDelta = new Vector2(14f, 14f);

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(28f, 2f);
            labelRect.offsetMax = new Vector2(-5f, -5f);
            return toggleObj;
        }

        /// <summary>
        /// Create a Scrollable Input Field control (custom InputFieldScroller).
        /// </summary>
        public static GameObject CreateSrollInputField(GameObject parent, string name, string placeHolderText,
            out InputFieldScroller inputScroll, int fontSize = 14, Color color = default)
        {
            if (color == default)
                color = new Color(0.15f, 0.15f, 0.15f);

            var mainObj = CreateScrollView(parent, "InputFieldScrollView", out GameObject scrollContent, out SliderScrollbar scroller, color);

            CreateInputField(scrollContent, name, placeHolderText ?? "...", out InputField inputField, fontSize, 0);

            inputField.lineType = InputField.LineType.MultiLineNewline;
            inputField.targetGraphic.color = color;

            inputScroll = new InputFieldScroller(scroller, inputField);

            return mainObj;
        }

        // Little helper class to force rebuild of an input field's layout on value change.
        // This is limited to once per frame per input field, so its not too expensive.
        private class InputFieldRefresher
        {
            private float timeOfLastRebuild;
            private readonly RectTransform rectTransform;

            public InputFieldRefresher(InputField inputField)
            {
                if (!inputField)
                    return;

                rectTransform = inputField.GetComponent<RectTransform>();

                inputField.onValueChanged.AddListener((string val) =>
                {
                    if (Time.time > timeOfLastRebuild)
                    {
                        timeOfLastRebuild = Time.time;
                        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                    }
                });
            }
        }

        /// <summary>
        /// Create a standard InputField control.
        /// </summary>
        public static GameObject CreateInputField(GameObject parent, string name, string placeHolderText, out InputField inputField,
            int fontSize = 14, int alignment = 3, int wrap = 0)
        {
            GameObject mainObj = CreateUIObject(name, parent);

            Image mainImage = mainObj.AddComponent<Image>();
            mainImage.type = Image.Type.Sliced;
            mainImage.color = new Color(0.12f, 0.12f, 0.12f);

            inputField = mainObj.AddComponent<InputField>();
            Navigation nav = inputField.navigation;
            nav.mode = Navigation.Mode.None;
            inputField.navigation = nav;
            inputField.lineType = InputField.LineType.SingleLine;
            inputField.interactable = true;
            inputField.transition = Selectable.Transition.ColorTint;
            inputField.targetGraphic = mainImage;

            RuntimeProvider.Instance.SetColorBlock(inputField, new Color(1, 1, 1, 1),
                new Color(0.95f, 0.95f, 0.95f, 1.0f), new Color(0.78f, 0.78f, 0.78f, 1.0f));

            SetLayoutGroup<VerticalLayoutGroup>(mainObj, true, true, true, true);

            GameObject textArea = CreateUIObject("TextArea", mainObj);
            textArea.AddComponent<RectMask2D>();

            RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = Vector2.zero;
            textAreaRect.offsetMax = Vector2.zero;

            // mainInput.textViewport = textArea.GetComponent<RectTransform>();

            GameObject placeHolderObj = CreateUIObject("Placeholder", textArea);
            Text placeholderText = placeHolderObj.AddComponent<Text>();
            SetDefaultTextValues(placeholderText);
            placeholderText.text = placeHolderText ?? "...";
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            placeholderText.horizontalOverflow = (HorizontalWrapMode)wrap;
            placeholderText.alignment = (TextAnchor)alignment;
            placeholderText.fontSize = fontSize;

            RectTransform placeHolderRect = placeHolderObj.GetComponent<RectTransform>();
            placeHolderRect.anchorMin = Vector2.zero;
            placeHolderRect.anchorMax = Vector2.one;
            placeHolderRect.offsetMin = Vector2.zero;
            placeHolderRect.offsetMax = Vector2.zero;

            SetLayoutElement(placeHolderObj, minWidth: 500, flexibleWidth: 5000);

            inputField.placeholder = placeholderText;

            GameObject inputTextObj = CreateUIObject("Text", textArea);
            Text inputText = inputTextObj.AddComponent<Text>();
            SetDefaultTextValues(inputText);
            inputText.text = "";
            inputText.color = new Color(1f, 1f, 1f, 1f);
            inputText.horizontalOverflow = (HorizontalWrapMode)wrap;
            inputText.alignment = (TextAnchor)alignment;
            inputText.fontSize = fontSize;

            RectTransform inputTextRect = inputTextObj.GetComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = Vector2.zero;
            inputTextRect.offsetMax = Vector2.zero;

            SetLayoutElement(inputTextObj, minWidth: 500, flexibleWidth: 5000);

            inputField.textComponent = inputText;

            new InputFieldRefresher(inputField);

            return mainObj;
        }

        /// <summary>
        /// Create a DropDown control.
        /// </summary>
        public static GameObject CreateDropdown(GameObject parent, out Dropdown dropdown, string defaultItemText, int itemFontSize,
            Action<int> onValueChanged, string[] defaultOptions = null)
        {
            GameObject dropdownObj = CreateUIObject("Dropdown", parent, _largeElementSize);

            GameObject labelObj = CreateUIObject("Label", dropdownObj);
            GameObject arrowObj = CreateUIObject("Arrow", dropdownObj);
            GameObject templateObj = CreateUIObject("Template", dropdownObj);
            GameObject viewportObj = CreateUIObject("Viewport", templateObj);
            GameObject contentObj = CreateUIObject("Content", viewportObj);
            GameObject itemObj = CreateUIObject("Item", contentObj);
            GameObject itemBgObj = CreateUIObject("Item Background", itemObj);
            GameObject itemCheckObj = CreateUIObject("Item Checkmark", itemObj);
            GameObject itemLabelObj = CreateUIObject("Item Label", itemObj);

            GameObject scrollbarObj = CreateScrollbar(templateObj, "DropdownScroll", out Scrollbar scrollbar);
            scrollbar.SetDirection(Scrollbar.Direction.BottomToTop, true);
            RuntimeProvider.Instance.SetColorBlock(scrollbar, new Color(0.45f, 0.45f, 0.45f), new Color(0.6f, 0.6f, 0.6f), new Color(0.4f, 0.4f, 0.4f));

            RectTransform scrollRectTransform = scrollbarObj.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.right;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.pivot = Vector2.one;
            scrollRectTransform.sizeDelta = new Vector2(scrollRectTransform.sizeDelta.x, 0f);

            Text itemLabelText = itemLabelObj.AddComponent<Text>();
            SetDefaultTextValues(itemLabelText);
            itemLabelText.alignment = TextAnchor.MiddleLeft;
            itemLabelText.text = defaultItemText;
            itemLabelText.fontSize = itemFontSize;

            var arrowText = arrowObj.AddComponent<Text>();
            SetDefaultTextValues(arrowText);
            arrowText.text = "▼";
            var arrowRect = arrowObj.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1f, 0.5f);
            arrowRect.anchorMax = new Vector2(1f, 0.5f);
            arrowRect.sizeDelta = new Vector2(20f, 20f);
            arrowRect.anchoredPosition = new Vector2(-15f, 0f);

            Image itemBgImage = itemBgObj.AddComponent<Image>();
            itemBgImage.color = new Color(0.25f, 0.35f, 0.25f, 1.0f);

            Toggle itemToggle = itemObj.AddComponent<Toggle>();
            itemToggle.targetGraphic = itemBgImage;
            itemToggle.isOn = true;
            RuntimeProvider.Instance.SetColorBlock(itemToggle,
                new Color(0.35f, 0.35f, 0.35f, 1.0f), new Color(0.25f, 0.55f, 0.25f, 1.0f));

            itemToggle.onValueChanged.AddListener((bool val) => { itemToggle.OnDeselect(null); });
            Image templateImage = templateObj.AddComponent<Image>();
            templateImage.type = Image.Type.Sliced;
            templateImage.color = Color.black;

            var scrollRect = templateObj.AddComponent<ScrollRect>();
            scrollRect.scrollSensitivity = 35;
            scrollRect.content = contentObj.GetComponent<RectTransform>();
            scrollRect.viewport = viewportObj.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = -3f;

            viewportObj.AddComponent<Mask>().showMaskGraphic = false;

            Image viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.type = Image.Type.Sliced;

            Text labelText = labelObj.AddComponent<Text>();
            SetDefaultTextValues(labelText);
            labelText.alignment = TextAnchor.MiddleLeft;

            Image dropdownImage = dropdownObj.AddComponent<Image>();
            dropdownImage.color = new Color(0.07f, 0.07f, 0.07f, 1);
            dropdownImage.type = Image.Type.Sliced;

            dropdown = dropdownObj.AddComponent<Dropdown>();
            dropdown.targetGraphic = dropdownImage;
            dropdown.template = templateObj.GetComponent<RectTransform>();
            dropdown.captionText = labelText;
            dropdown.itemText = itemLabelText;
            //itemLabelText.text = "DEFAULT";

            dropdown.RefreshShownValue();

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 2f);
            labelRect.offsetMax = new Vector2(-28f, -2f);

            RectTransform templateRect = templateObj.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, 2f);
            templateRect.sizeDelta = new Vector2(0f, 150f);

            RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(1f, 1f);
            viewportRect.sizeDelta = new Vector2(-18f, 0f);
            viewportRect.pivot = new Vector2(0f, 1f);

            RectTransform contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = new Vector2(0f, 0f);
            contentRect.sizeDelta = new Vector2(0f, 28f);

            RectTransform itemRect = itemObj.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, 25f);

            RectTransform itemBgRect = itemBgObj.GetComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.sizeDelta = Vector2.zero;

            RectTransform itemLabelRect = itemLabelObj.GetComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(20f, 1f);
            itemLabelRect.offsetMax = new Vector2(-10f, -2f);
            templateObj.SetActive(false);

            if (onValueChanged != null)
                dropdown.onValueChanged.AddListener(onValueChanged);

            if (defaultOptions != null)
            {
                foreach (var option in defaultOptions)
                    dropdown.options.Add(new Dropdown.OptionData(option));
            }

            return dropdownObj;
        }

        public static ScrollPool CreateScrollPool(GameObject parent, string name, out GameObject uiRoot,
            out GameObject content, Color? bgColor = null)
        {
            var mainObj = CreateUIObject(name, parent, new Vector2(1, 1));
            mainObj.AddComponent<Image>().color = bgColor ?? new Color(0.12f, 0.12f, 0.12f);
            SetLayoutGroup<HorizontalLayoutGroup>(mainObj, false, true, true, true);

            GameObject viewportObj = CreateUIObject("Viewport", mainObj);
            SetLayoutElement(viewportObj, flexibleWidth: 9999);
            var viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.pivot = new Vector2(0.0f, 1.0f);
            viewportRect.sizeDelta = new Vector2(0f, 0.0f);
            viewportRect.offsetMax = new Vector2(-10.0f, 0.0f);
            viewportObj.AddComponent<Image>().color = Color.white;
            viewportObj.AddComponent<Mask>().showMaskGraphic = false;

            content = CreateUIObject("Content", viewportObj);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 0f);
            contentRect.offsetMax = new Vector2(0f, 0f);

            SetLayoutGroup<VerticalLayoutGroup>(content, true, false, true, true, 0, 2, 2, 2, 2, TextAnchor.UpperCenter);

            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = mainObj.AddComponent<ScrollRect>();
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            //scrollRect.inertia = false;
            scrollRect.inertia = true;
            scrollRect.elasticity = 0.125f;
            scrollRect.scrollSensitivity = 25;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            // Slider

            var sliderContainer = CreateVerticalGroup(mainObj, "SliderContainer",
                false, false, true, true, 0, default, new Color(0.05f, 0.05f, 0.05f));
            SetLayoutElement(sliderContainer, minWidth: 25, flexibleWidth:0, flexibleHeight: 9999);
            sliderContainer.AddComponent<Mask>();

            var sliderObj = SliderScrollbar.CreateSliderScrollbar(sliderContainer, out Slider slider);
            slider.direction = Slider.Direction.TopToBottom;
            SetLayoutElement(sliderObj, minWidth: 25, flexibleWidth: 0, flexibleHeight: 9999);

            RuntimeProvider.Instance.SetColorBlock(slider, disabled: new Color(0.1f, 0.1f, 0.1f));

            slider.handleRect.offsetMin = new Vector2(slider.handleRect.offsetMin.x, 0);
            slider.handleRect.offsetMax = new Vector2(slider.handleRect.offsetMax.x, 0);
            slider.handleRect.pivot = new Vector2(0.5f, 0.5f);

            var container = slider.m_HandleContainerRect;
            container.anchorMin = Vector3.zero;
            container.anchorMax = Vector3.one;
            container.pivot = new Vector3(0.5f, 0.5f);

            // finalize and create ScrollPool

            uiRoot = mainObj;
            var scrollPool = new ScrollPool(scrollRect);

            //viewportObj.GetComponent<Mask>().enabled = false;

            return scrollPool;
        }

        /// <summary>
        /// Create a ScrollView element.
        /// </summary>
        public static GameObject CreateScrollView(GameObject parent, string name, out GameObject content, out SliderScrollbar scroller,
            Color color = default)
        {
            GameObject mainObj = CreateUIObject("DynamicScrollView", parent);

            SetLayoutElement(mainObj, minWidth: 100, minHeight: 30, flexibleWidth: 5000, flexibleHeight: 5000);

            Image mainImage = mainObj.AddComponent<Image>();
            mainImage.type = Image.Type.Filled;
            mainImage.color = (color == default) ? new Color(0.3f, 0.3f, 0.3f, 1f) : color;

            GameObject viewportObj = CreateUIObject("Viewport", mainObj);

            var viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.pivot = new Vector2(0.0f, 1.0f);
            viewportRect.sizeDelta = new Vector2(-15.0f, 0.0f);
            viewportRect.offsetMax = new Vector2(-20.0f, 0.0f);

            viewportObj.AddComponent<Image>().color = Color.white;
            viewportObj.AddComponent<Mask>().showMaskGraphic = false;

            content = CreateUIObject("Content", viewportObj);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.0f, 1.0f);
            contentRect.anchorMax = new Vector2(1.0f, 1.0f);
            contentRect.pivot = new Vector2(0.0f, 1.0f);
            contentRect.sizeDelta = new Vector2(5f, 0f);
            contentRect.offsetMax = new Vector2(0f, 0f);
            var contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            SetLayoutGroup<VerticalLayoutGroup>(content, true, true, true, true, 5, 5, 5, 5, 5);

            CreateSliderScrollbar(mainObj, out scroller, out Scrollbar hiddenScrollbar);

            // Back to the main scrollview ScrollRect, setting it up now that we have all references.

            var scrollRect = mainObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.verticalScrollbar = hiddenScrollbar;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 35;
            scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            return mainObj;
        }

        public static GameObject CreateSliderScrollbar(GameObject mainObj, out SliderScrollbar scroller, out Scrollbar hiddenScrollbar)
        {
            GameObject scrollBarObj = CreateUIObject("DynamicScrollbar", mainObj);

            var scrollbarLayout = scrollBarObj.AddComponent<VerticalLayoutGroup>();
            scrollbarLayout.childForceExpandHeight = true;
            scrollbarLayout.SetChildControlHeight(true);

            RectTransform scrollBarRect = scrollBarObj.GetComponent<RectTransform>();
            scrollBarRect.anchorMin = new Vector2(1.0f, 0.0f);
            scrollBarRect.anchorMax = new Vector2(1.0f, 1.0f);
            scrollBarRect.sizeDelta = new Vector2(15.0f, 0.0f);
            scrollBarRect.offsetMin = new Vector2(-15.0f, 0.0f);

            GameObject hiddenBar = CreateScrollbar(scrollBarObj, "HiddenScrollviewScroller", out hiddenScrollbar);
            hiddenScrollbar.SetDirection(Scrollbar.Direction.BottomToTop, true);

            for (int i = 0; i < hiddenBar.transform.childCount; i++)
            {
                var child = hiddenBar.transform.GetChild(i);
                child.gameObject.SetActive(false);
            }

            SliderScrollbar.CreateSliderScrollbar(scrollBarObj, out Slider scrollSlider);

            scroller = new SliderScrollbar(hiddenScrollbar, scrollSlider);

            return scrollBarObj;
        }
    }
}
