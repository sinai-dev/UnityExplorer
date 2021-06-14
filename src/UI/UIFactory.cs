using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.Core.Runtime;
using UnityExplorer.UI.Models;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI
{
    public static class UIFactory
    {
        #region Init, Core

        internal static Vector2 _largeElementSize = new Vector2(100, 30);
        internal static Vector2 _smallElementSize = new Vector2(25, 25);
        internal static Color _defaultTextColor = Color.white;
        internal static Font _defaultFont;

        public static void Init()
        {
            _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        public static GameObject CreateUIObject(string name, GameObject parent, Vector2 size = default)
        {
            if (!parent)
            {
                ExplorerCore.LogWarning($"Warning: Creating {name} but parent is null");
                ExplorerCore.Log(Environment.StackTrace);
            }

            var obj = new GameObject(name)
            {
                layer = 5,
                hideFlags = HideFlags.HideAndDontSave,
            };

            if (parent)
                obj.transform.SetParent(parent.transform, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = size;
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
        }

        #endregion


        #region Default Layout Components

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
            SetLayoutGroup<VerticalLayoutGroup>(panelObj, true, true, true, true);

            var rect = panelObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            var maskImg = panelObj.AddComponent<Image>();
            maskImg.color = Color.black;
            panelObj.AddComponent<Mask>().showMaskGraphic = true;

            contentHolder = CreateUIObject("Content", panelObj);

            Image bgImage = contentHolder.AddComponent<Image>();
            bgImage.type = Image.Type.Filled;
            if (bgColor == null)
                bgImage.color = new Color(0.07f, 0.07f, 0.07f);
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

        #endregion


        #region Default Control Elements

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

        public static ButtonRef CreateButton(GameObject parent, string name, string text, Color? normalColor = null)
        {
            var colors = new ColorBlock();
            normalColor = normalColor ?? new Color(0.25f, 0.25f, 0.25f);

            var btn = CreateButton(parent, name, text, colors);

            RuntimeProvider.Instance.SetColorBlock(btn.Component, normalColor, normalColor * 1.2f, normalColor * 0.7f);

            return btn;
        }

        public static ButtonRef CreateButton(GameObject parent, string name, string text, ColorBlock colors)
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

            SetButtonDeselectListener(button);

            return new ButtonRef(button);
        }

        public static void SetButtonDeselectListener(Button button)
        {
            button.onClick.AddListener(() =>
            {
                button.OnDeselect(null);
            });
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
        public static GameObject CreateToggle(GameObject parent, string name, out Toggle toggle, out Text text, Color bgColor = default, 
            int checkWidth = 20, int checkHeight = 20)
        {
            // Main obj
            GameObject toggleObj = CreateUIObject(name, parent, _smallElementSize);
            SetLayoutGroup<HorizontalLayoutGroup>(toggleObj, false, false, true, true, 5, 0,0,0,0, childAlignment: TextAnchor.MiddleLeft);
            toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = true;
            SetDefaultSelectableColors(toggle);
            // need a second reference so we can use it inside the lambda, since 'toggle' is an out var.
            Toggle t2 = toggle;
            toggle.onValueChanged.AddListener((bool _) => { t2.OnDeselect(null); });

            // Check mark background

            GameObject checkBgObj = CreateUIObject("Background", toggleObj);
            Image bgImage = checkBgObj.AddComponent<Image>();
            bgImage.color = bgColor == default ? new Color(0.04f, 0.04f, 0.04f, 0.75f) : bgColor;

            SetLayoutGroup<HorizontalLayoutGroup>(checkBgObj, true, true, true, true, 0, 2, 2, 2, 2);
            SetLayoutElement(checkBgObj, minWidth: checkWidth, flexibleWidth: 0, minHeight: checkHeight, flexibleHeight: 0);

            // Check mark image

            GameObject checkMarkObj = CreateUIObject("Checkmark", checkBgObj);
            Image checkImage = checkMarkObj.AddComponent<Image>();
            checkImage.color = new Color(0.8f, 1, 0.8f, 0.3f);

            // Label 

            GameObject labelObj = CreateUIObject("Label", toggleObj);
            text = labelObj.AddComponent<Text>();
            text.text = "";
            text.alignment = TextAnchor.MiddleLeft;
            SetDefaultTextValues(text);

            SetLayoutElement(labelObj, minWidth: 0, flexibleWidth: 0, minHeight: checkHeight, flexibleHeight: 0);

            // References

            toggle.graphic = checkImage;
            toggle.targetGraphic = bgImage;

            return toggleObj;
        }

        /// <summary>
        /// Create a standard InputField control.
        /// </summary>
        public static InputFieldRef CreateInputField(GameObject parent, string name, string placeHolderText)
        {
            GameObject mainObj = CreateUIObject(name, parent);

            Image mainImage = mainObj.AddComponent<Image>();
            mainImage.type = Image.Type.Sliced;
            mainImage.color = new Color(0, 0, 0, 0.5f);

            var inputField = mainObj.AddComponent<InputField>();
            Navigation nav = inputField.navigation;
            nav.mode = Navigation.Mode.None;
            inputField.navigation = nav;
            inputField.lineType = InputField.LineType.SingleLine;
            inputField.interactable = true;
            inputField.transition = Selectable.Transition.ColorTint;
            inputField.targetGraphic = mainImage;

            RuntimeProvider.Instance.SetColorBlock(inputField, new Color(1, 1, 1, 1),
                new Color(0.95f, 0.95f, 0.95f, 1.0f), new Color(0.78f, 0.78f, 0.78f, 1.0f));

            GameObject textArea = CreateUIObject("TextArea", mainObj);
            textArea.AddComponent<RectMask2D>();

            RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = Vector2.zero;
            textAreaRect.offsetMax = Vector2.zero;

            GameObject placeHolderObj = CreateUIObject("Placeholder", textArea);
            Text placeholderText = placeHolderObj.AddComponent<Text>();
            SetDefaultTextValues(placeholderText);
            placeholderText.text = placeHolderText ?? "...";
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            placeholderText.horizontalOverflow = HorizontalWrapMode.Wrap;
            placeholderText.alignment = TextAnchor.MiddleLeft;
            placeholderText.fontSize = 14;

            RectTransform placeHolderRect = placeHolderObj.GetComponent<RectTransform>();
            placeHolderRect.anchorMin = Vector2.zero;
            placeHolderRect.anchorMax = Vector2.one;
            placeHolderRect.offsetMin = Vector2.zero;
            placeHolderRect.offsetMax = Vector2.zero;

            inputField.placeholder = placeholderText;

            GameObject inputTextObj = CreateUIObject("Text", textArea);
            Text inputText = inputTextObj.AddComponent<Text>();
            SetDefaultTextValues(inputText);
            inputText.text = "";
            inputText.color = new Color(1f, 1f, 1f, 1f);
            inputText.horizontalOverflow = HorizontalWrapMode.Wrap;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.fontSize = 14;

            RectTransform inputTextRect = inputTextObj.GetComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = Vector2.zero;
            inputTextRect.offsetMax = Vector2.zero;

            inputField.textComponent = inputText;
            inputField.characterLimit = UIManager.MAX_INPUTFIELD_CHARS;

            return new InputFieldRef(inputField);
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
            dropdownImage.color = new Color(0.04f, 0.04f, 0.04f, 0.75f);
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


        #endregion


        #region Custom Scroll Components

        /// <summary>
        /// Create a ScrollPool for the <typeparamref name="T"/> ICell.
        /// </summary>
        public static ScrollPool<T> CreateScrollPool<T>(GameObject parent, string name, out GameObject uiRoot,
            out GameObject content, Color? bgColor = null) where T : ICell
        {
            var mainObj = CreateUIObject(name, parent, new Vector2(1, 1));
            mainObj.AddComponent<Image>().color = bgColor ?? new Color(0.12f, 0.12f, 0.12f);
            SetLayoutGroup<HorizontalLayoutGroup>(mainObj, false, true, true, true);

            GameObject viewportObj = CreateUIObject("Viewport", mainObj);
            SetLayoutElement(viewportObj, flexibleWidth: 9999, flexibleHeight: 9999);
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
            SetLayoutElement(sliderContainer, minWidth: 25, flexibleWidth: 0, flexibleHeight: 9999);
            sliderContainer.AddComponent<Mask>();

            CreateSliderScrollbar(sliderContainer, out Slider slider);

            RuntimeProvider.Instance.SetColorBlock(slider, disabled: new Color(0.1f, 0.1f, 0.1f));

            // finalize and create ScrollPool

            uiRoot = mainObj;
            var scrollPool = new ScrollPool<T>(scrollRect);

            return scrollPool;
        }

        /// <summary>
        /// Create a SliderScrollbar, using a Slider to mimic a scrollbar.
        /// </summary>
        public static GameObject CreateSliderScrollbar(GameObject parent, out Slider slider)
        {
            GameObject mainObj = CreateUIObject("SliderScrollbar", parent, _smallElementSize);
            mainObj.AddComponent<Mask>();
            mainObj.AddComponent<Image>().color = Color.white;

            GameObject bgImageObj = CreateUIObject("Background", mainObj);
            GameObject handleSlideAreaObj = CreateUIObject("Handle Slide Area", mainObj);
            GameObject handleObj = CreateUIObject("Handle", handleSlideAreaObj);

            Image bgImage = bgImageObj.AddComponent<Image>();
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0.05f, 0.05f, 0.05f, 1.0f);

            bgImageObj.AddComponent<Mask>();

            RectTransform bgRect = bgImageObj.GetComponent<RectTransform>();
            bgRect.pivot = new Vector2(0, 1);
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.offsetMax = new Vector2(0f, 0f);

            RectTransform handleSlideRect = handleSlideAreaObj.GetComponent<RectTransform>();
            handleSlideRect.anchorMin = Vector3.zero;
            handleSlideRect.anchorMax = Vector3.one;
            handleSlideRect.pivot = new Vector3(0.5f, 0.5f);

            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);

            var handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            SetLayoutElement(handleObj, minWidth: 21, flexibleWidth: 0);

            var sliderBarLayout = mainObj.AddComponent<LayoutElement>();
            sliderBarLayout.minWidth = 25;
            sliderBarLayout.flexibleWidth = 0;
            sliderBarLayout.minHeight = 30;
            sliderBarLayout.flexibleHeight = 9999;

            slider = mainObj.AddComponent<Slider>();
            slider.handleRect = handleObj.GetComponent<RectTransform>();
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.TopToBottom;

            SetLayoutElement(mainObj, minWidth: 25, flexibleWidth: 0, flexibleHeight: 9999);

            RuntimeProvider.Instance.SetColorBlock(slider,
                new Color(0.4f, 0.4f, 0.4f),
                new Color(0.5f, 0.5f, 0.5f),
                new Color(0.3f, 0.3f, 0.3f),
                new Color(0.5f, 0.5f, 0.5f));

            return mainObj;
        }

        /// <summary>
        /// Create a ScrollView and a SliderScrollbar for non-pooled content.
        /// </summary>
        public static GameObject CreateScrollView(GameObject parent, string name, out GameObject content, out AutoSliderScrollbar autoScrollbar,
            Color color = default)
        {
            GameObject mainObj = CreateUIObject(name, parent);
            var mainRect = mainObj.GetComponent<RectTransform>();
            mainRect.anchorMin = Vector2.zero;
            mainRect.anchorMax = Vector2.one;
            Image mainImage = mainObj.AddComponent<Image>();
            mainImage.type = Image.Type.Filled;
            mainImage.color = (color == default) ? new Color(0.3f, 0.3f, 0.3f, 1f) : color;

            GameObject viewportObj = CreateUIObject("Viewport", mainObj);
            var viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.pivot = new Vector2(0.0f, 1.0f);
            viewportRect.offsetMax = new Vector2(-28, 0);
            viewportObj.AddComponent<Image>().color = Color.white;
            viewportObj.AddComponent<Mask>().showMaskGraphic = false;

            content = CreateUIObject("Content", viewportObj);
            SetLayoutGroup<VerticalLayoutGroup>(content, true, false, true, true, childAlignment: TextAnchor.UpperLeft);
            SetLayoutElement(content, flexibleHeight: 9999);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.0f, 1.0f);
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Slider

            GameObject scrollBarObj = CreateUIObject("AutoSliderScrollbar", mainObj);
            var scrollBarRect = scrollBarObj.GetComponent<RectTransform>();
            scrollBarRect.anchorMin = new Vector2(1, 0);
            scrollBarRect.anchorMax = Vector2.one;
            scrollBarRect.offsetMin = new Vector2(-25, 0);
            SetLayoutGroup<VerticalLayoutGroup>(scrollBarObj, false, true, true, true);
            scrollBarObj.AddComponent<Image>().color = Color.white;
            scrollBarObj.AddComponent<Mask>().showMaskGraphic = false;

            GameObject hiddenBar = CreateScrollbar(scrollBarObj, "HiddenScrollviewScroller", out var hiddenScrollbar);
            hiddenScrollbar.SetDirection(Scrollbar.Direction.BottomToTop, true);

            for (int i = 0; i < hiddenBar.transform.childCount; i++)
            {
                var child = hiddenBar.transform.GetChild(i);
                child.gameObject.SetActive(false);
            }

            CreateSliderScrollbar(scrollBarObj, out Slider scrollSlider);

            autoScrollbar = new AutoSliderScrollbar(hiddenScrollbar, scrollSlider, contentRect, viewportRect);

            // Set up the ScrollRect component

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


        /// <summary>
        /// Create a Scrollable Input Field control
        /// </summary>
        public static GameObject CreateScrollInputField(GameObject parent, string name, string placeHolderText, out InputFieldScroller inputScroll,
            int fontSize = 14, Color color = default)
        {
            if (color == default)
                color = new Color(0.12f, 0.12f, 0.12f);

            GameObject mainObj = CreateUIObject(name, parent);
            SetLayoutElement(mainObj, minWidth: 100, minHeight: 30, flexibleWidth: 5000, flexibleHeight: 5000);
            SetLayoutGroup<HorizontalLayoutGroup>(mainObj, false, true, true, true, 2);
            Image mainImage = mainObj.AddComponent<Image>();
            mainImage.type = Image.Type.Filled;
            mainImage.color = (color == default) ? new Color(0.3f, 0.3f, 0.3f, 1f) : color;

            GameObject viewportObj = CreateUIObject("Viewport", mainObj);
            SetLayoutElement(viewportObj, minWidth: 1, flexibleWidth: 9999, flexibleHeight: 9999);
            var viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.pivot = new Vector2(0.0f, 1.0f);
            viewportObj.AddComponent<Image>().color = Color.white;
            viewportObj.AddComponent<Mask>().showMaskGraphic = false;

            // Input Field

            var inputField = CreateInputField(viewportObj, "InputField", placeHolderText);
            var content = inputField.UIRoot;
            var textComp = inputField.Component.textComponent;
            textComp.alignment = TextAnchor.UpperLeft;
            textComp.fontSize = fontSize;
            textComp.horizontalOverflow = HorizontalWrapMode.Wrap;
            inputField.Component.lineType = InputField.LineType.MultiLineNewline;
            inputField.Component.targetGraphic.color = color;
            inputField.PlaceholderText.alignment = TextAnchor.UpperLeft;
            inputField.PlaceholderText.fontSize = fontSize;
            inputField.PlaceholderText.horizontalOverflow = HorizontalWrapMode.Wrap;

            //var content = CreateInputField(viewportObj, name, placeHolderText ?? "...", out InputField inputField, fontSize, 0);
            SetLayoutElement(content, flexibleHeight: 9999, flexibleWidth: 9999);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.pivot = new Vector2(0, 1);
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(2, 0);
            contentRect.offsetMax = new Vector2(2, 0);
            inputField.Component.lineType = InputField.LineType.MultiLineNewline;
            inputField.Component.targetGraphic.color = color;

            // Slider

            GameObject scrollBarObj = CreateUIObject("AutoSliderScrollbar", mainObj);
            SetLayoutGroup<VerticalLayoutGroup>(scrollBarObj, true, true, true, true);
            SetLayoutElement(scrollBarObj, minWidth: 25, flexibleWidth: 0, flexibleHeight: 9999);
            scrollBarObj.AddComponent<Image>().color = Color.white;
            scrollBarObj.AddComponent<Mask>().showMaskGraphic = false;

            GameObject hiddenBar = CreateScrollbar(scrollBarObj, "HiddenScrollviewScroller", out var hiddenScrollbar);
            hiddenScrollbar.SetDirection(Scrollbar.Direction.BottomToTop, true);

            for (int i = 0; i < hiddenBar.transform.childCount; i++)
            {
                var child = hiddenBar.transform.GetChild(i);
                child.gameObject.SetActive(false);
            }

            CreateSliderScrollbar(scrollBarObj, out Slider scrollSlider);

            // Set up the AutoSliderScrollbar module

            var autoScroller = new AutoSliderScrollbar(hiddenScrollbar, scrollSlider, contentRect, viewportRect);

            var sliderContainer = autoScroller.Slider.m_HandleContainerRect.gameObject;
            SetLayoutElement(sliderContainer, minWidth: 25, flexibleWidth: 0, flexibleHeight: 9999);
            sliderContainer.AddComponent<Mask>();

            // Set up the InputFieldScroller module

            inputScroll = new InputFieldScroller(autoScroller, inputField);
            inputScroll.ProcessInputText();

            // Set up the ScrollRect component

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

        #endregion
    }
}
