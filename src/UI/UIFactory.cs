using System;
//using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Helpers;
using UnityExplorer.UI.Shared;

namespace UnityExplorer.UI
{
    public static class UIFactory
    {
        internal static Vector2 thickSize = new Vector2(160f, 30f);
        internal static Vector2 thinSize = new Vector2(160f, 20f);
        internal static Color defaultTextColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        internal static Font s_defaultFont;

        public static GameObject CreateUIObject(string name, GameObject parent, Vector2 size = default)
        {
            GameObject obj = new GameObject(name);

            RectTransform rect = obj.AddComponent<RectTransform>();
            if (size != default)
            {
                rect.sizeDelta = size;
            }

            SetParentAndAlign(obj, parent);

            return obj;
        }

        private static void SetParentAndAlign(GameObject child, GameObject parent)
        {
            if (parent == null)
            {
                return;
            }
            child.transform.SetParent(parent.transform, false);
            SetLayerRecursively(child);
        }

        public static void SetLayerRecursively(GameObject go)
        {
            go.layer = 5;
            Transform transform = go.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                SetLayerRecursively(transform.GetChild(i).gameObject);
            }
        }

        private static void SetDefaultTextValues(Text lbl)
        {
            lbl.color = defaultTextColor;

            if (!s_defaultFont)
                s_defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            if (s_defaultFont)
                lbl.font = s_defaultFont;
        }

        public static void SetDefaultColorTransitionValues(Selectable selectable)
        {
            ColorBlock colors = selectable.colors;
            colors.normalColor = new Color(0.35f, 0.35f, 0.35f);
            colors.highlightedColor = new Color(0.45f, 0.45f, 0.45f);
            colors.pressedColor = new Color(0.25f, 0.25f, 0.25f);
            //colors.disabledColor = new Color(0.6f, 0.6f, 0.6f);

            // fix to make all buttons become de-selected after being clicked.
            // this is because i'm not setting any ColorBlock.selectedColor, because it is commonly stripped.
            if (selectable is Button button)
            {
                button.onClick.AddListener(Deselect);
                void Deselect()
                {
					button.OnDeselect(null);
                }

            }

            selectable.colors = colors;
        }

        public static GameObject CreatePanel(GameObject parent, string name, out GameObject content)
        {
            GameObject panelObj = CreateUIObject($"Panel_{name}", parent, thickSize);

            RectTransform rect = panelObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            var img = panelObj.AddComponent<Image>();
            img.color = Color.white;

            VerticalLayoutGroup group = panelObj.AddComponent<VerticalLayoutGroup>();
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = true;
            group.childForceExpandWidth = true;

            content = new GameObject("Content");
            content.transform.parent = panelObj.transform;

            Image image2 = content.AddComponent<Image>();
            image2.type = Image.Type.Filled;
            image2.color = new Color(0.1f, 0.1f, 0.1f);

            VerticalLayoutGroup group2 = content.AddComponent<VerticalLayoutGroup>();
            group2.padding.left = 3;
            group2.padding.right = 3;
            group2.padding.bottom = 3;
            group2.padding.top = 3;
            group2.spacing = 3;
            group2.childControlHeight = true;
            group2.childControlWidth = true;
            group2.childForceExpandHeight = false;
            group2.childForceExpandWidth = true;

            return panelObj;
        }

        public static GameObject CreateGridGroup(GameObject parent, Vector2 cellSize, Vector2 spacing, Color color = default)
        {
            GameObject groupObj = CreateUIObject("GridLayout", parent);

            GridLayoutGroup gridGroup = groupObj.AddComponent<GridLayoutGroup>();
            gridGroup.childAlignment = TextAnchor.UpperLeft;
            gridGroup.cellSize = cellSize;
            gridGroup.spacing = spacing;

            Image image = groupObj.AddComponent<Image>();
            if (color != default)
            {
                image.color = color;
            }
            else
            {
                image.color = new Color(44f / 255f, 44f / 255f, 44f / 255f);
            }

            return groupObj;
        }

        public static GameObject CreateVerticalGroup(GameObject parent, Color color = default)
        {
            GameObject groupObj = CreateUIObject("VerticalLayout", parent);

            VerticalLayoutGroup horiGroup = groupObj.AddComponent<VerticalLayoutGroup>();
            horiGroup.childAlignment = TextAnchor.UpperLeft;
            horiGroup.childControlWidth = true;
            horiGroup.childControlHeight = true;

            Image image = groupObj.AddComponent<Image>();
            if (color != default)
            {
                image.color = color;
            }
            else
            {
                image.color = new Color(44f / 255f, 44f / 255f, 44f / 255f);
            }

            return groupObj;
        }

        public static GameObject CreateHorizontalGroup(GameObject parent, Color color = default)
        {
            GameObject groupObj = CreateUIObject("HorizontalLayout", parent);

            HorizontalLayoutGroup horiGroup = groupObj.AddComponent<HorizontalLayoutGroup>();
            horiGroup.childAlignment = TextAnchor.UpperLeft;
            horiGroup.childControlWidth = true;
            horiGroup.childControlHeight = true;

            Image image = groupObj.AddComponent<Image>();
            if (color != default)
            {
                image.color = color;
            }
            else
            {
                image.color = new Color(44f / 255f, 44f / 255f, 44f / 255f);
            }

            return groupObj;
        }

        //public static GameObject CreateTMPLabel(GameObject parent, TextAlignmentOptions alignment)
        //{
        //    GameObject labelObj = CreateUIObject("Label", parent, thinSize);

        //    TextMeshProUGUI text = labelObj.AddComponent<TextMeshProUGUI>();

        //    text.alignment = alignment;
        //    text.richText = true;

        //    return labelObj;
        //}

        public static GameObject CreateLabel(GameObject parent, TextAnchor alignment)
        {
            GameObject labelObj = CreateUIObject("Label", parent, thinSize);

            Text text = labelObj.AddComponent<Text>();
            SetDefaultTextValues(text);
            text.alignment = alignment;
            text.supportRichText = true;

            return labelObj;
        }

        public static GameObject CreateButton(GameObject parent, Color normalColor = default)
        {
            GameObject buttonObj = CreateUIObject("Button", parent, thinSize);

            GameObject textObj = new GameObject("Text");
            textObj.AddComponent<RectTransform>();
            SetParentAndAlign(textObj, buttonObj);

            Image image = buttonObj.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = new Color(1, 1, 1, 0.75f);

            SetDefaultColorTransitionValues(buttonObj.AddComponent<Button>());

            if (normalColor != default)
            {
                var btn = buttonObj.GetComponent<Button>();
                var colors = btn.colors;
                colors.normalColor = normalColor;
                btn.colors = colors;
            }

            Text text = textObj.AddComponent<Text>();
            text.text = "Button";
            SetDefaultTextValues(text);
            text.alignment = TextAnchor.MiddleCenter;

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            return buttonObj;
        }

        public static GameObject CreateSlider(GameObject parent)
        {
            GameObject sliderObj = CreateUIObject("Slider", parent, thinSize);

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

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillObj.GetComponent<RectTransform>();
            slider.handleRect = handleObj.GetComponent<RectTransform>();
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            SetDefaultColorTransitionValues(slider);

            return sliderObj;
        }

        public static GameObject CreateScrollbar(GameObject parent)
        {
            GameObject scrollObj = CreateUIObject("Scrollbar", parent, thinSize);

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

            Scrollbar scrollbar = scrollObj.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            SetDefaultColorTransitionValues(scrollbar);

            return scrollObj;
        }

        public static GameObject CreateToggle(GameObject parent, out Toggle toggle, out Text text, Color bgColor = default)
        {
            GameObject toggleObj = CreateUIObject("Toggle", parent, thinSize);

            GameObject bgObj = CreateUIObject("Background", toggleObj);
            GameObject checkObj = CreateUIObject("Checkmark", bgObj);
            GameObject labelObj = CreateUIObject("Label", toggleObj);

            toggle = toggleObj.AddComponent<Toggle>();
            //toggle.isOn = true;
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
            SetDefaultColorTransitionValues(toggle);

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

        public static GameObject CreateSrollInputField(GameObject parent, out InputFieldScroller inputScroll, int fontSize = 14, Color color = default)
        {
            if (color == default)
                color = new Color(0.15f, 0.15f, 0.15f);

            var mainObj = CreateScrollView(parent, out GameObject scrollContent, out SliderScrollbar scroller, color);

            var inputObj = CreateInputField(scrollContent, fontSize, 0);

            var inputField = inputObj.GetComponent<InputField>();
            inputField.lineType = InputField.LineType.MultiLineNewline;
            inputField.targetGraphic.color = color;

            inputScroll = new InputFieldScroller(scroller, inputField);

            return mainObj;
        }

        public static GameObject CreateInputField(GameObject parent, int fontSize = 14, int alignment = 3, int wrap = 0)
        {
            GameObject mainObj = CreateUIObject("InputField", parent);

            Image mainImage = mainObj.AddComponent<Image>();
            mainImage.type = Image.Type.Sliced;
            mainImage.color = new Color(0.15f, 0.15f, 0.15f);

            InputField mainInput = mainObj.AddComponent<InputField>();
            Navigation nav = mainInput.navigation;
            nav.mode = Navigation.Mode.None;
            mainInput.navigation = nav;
            mainInput.lineType = InputField.LineType.SingleLine;
            mainInput.interactable = true;
            mainInput.transition = Selectable.Transition.ColorTint;
            mainInput.targetGraphic = mainImage;

            ColorBlock mainColors = mainInput.colors;
            mainColors.normalColor = new Color(1, 1, 1, 1);
            mainColors.highlightedColor = new Color(245f / 255f, 245f / 255f, 245f / 255f, 1.0f);
            mainColors.pressedColor = new Color(200f / 255f, 200f / 255f, 200f / 255f, 1.0f);
            mainColors.highlightedColor = new Color(245f / 255f, 245f / 255f, 245f / 255f, 1.0f);
            mainInput.colors = mainColors;

            VerticalLayoutGroup mainGroup = mainObj.AddComponent<VerticalLayoutGroup>();
            mainGroup.childControlHeight = true;
            mainGroup.childControlWidth = true;
            mainGroup.childForceExpandWidth = true;
            mainGroup.childForceExpandHeight = true;

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
            placeholderText.text = "...";
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            placeholderText.horizontalOverflow = (HorizontalWrapMode)wrap;
            placeholderText.alignment = (TextAnchor)alignment;
            placeholderText.fontSize = fontSize;

            RectTransform placeHolderRect = placeHolderObj.GetComponent<RectTransform>();
            placeHolderRect.anchorMin = Vector2.zero;
            placeHolderRect.anchorMax = Vector2.one;
            placeHolderRect.offsetMin = Vector2.zero;
            placeHolderRect.offsetMax = Vector2.zero;

            LayoutElement placeholderLayout = placeHolderObj.AddComponent<LayoutElement>();
            placeholderLayout.minWidth = 500;
            placeholderLayout.flexibleWidth = 5000;

            mainInput.placeholder = placeholderText;

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

            LayoutElement inputTextLayout = inputTextObj.AddComponent<LayoutElement>();
            inputTextLayout.minWidth = 500;
            inputTextLayout.flexibleWidth = 5000;

            mainInput.textComponent = inputText;

            return mainObj;
        }

        public static GameObject CreateDropdown(GameObject parent, out Dropdown dropdown)
        {
            GameObject dropdownObj = CreateUIObject("Dropdown", parent, thickSize);

            GameObject labelObj = CreateUIObject("Label", dropdownObj);
            GameObject arrowObj = CreateUIObject("Arrow", dropdownObj);
            GameObject templateObj = CreateUIObject("Template", dropdownObj);
            GameObject viewportObj = CreateUIObject("Viewport", templateObj);
            GameObject contentObj = CreateUIObject("Content", viewportObj);
            GameObject itemObj = CreateUIObject("Item", contentObj);
            GameObject itemBgObj = CreateUIObject("Item Background", itemObj);
            GameObject itemCheckObj = CreateUIObject("Item Checkmark", itemObj);
            GameObject itemLabelObj = CreateUIObject("Item Label", itemObj);

            GameObject scrollbarObj = CreateScrollbar(templateObj);
            scrollbarObj.name = "Scrollbar";
            Scrollbar scrollbar = scrollbarObj.GetComponent<Scrollbar>();
            scrollbar.SetDirection(Scrollbar.Direction.BottomToTop, true);

            RectTransform scrollRectTransform = scrollbarObj.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.right;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.pivot = Vector2.one;
            scrollRectTransform.sizeDelta = new Vector2(scrollRectTransform.sizeDelta.x, 0f);

            Text itemLabelText = itemLabelObj.AddComponent<Text>();
            SetDefaultTextValues(itemLabelText);
            itemLabelText.alignment = TextAnchor.MiddleLeft;

            var arrowText = arrowObj.AddComponent<Text>();
            SetDefaultTextValues(arrowText);
            arrowText.text = "▼";
            var arrowRect = arrowObj.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1f, 0.5f);
            arrowRect.anchorMax = new Vector2(1f, 0.5f);
            arrowRect.sizeDelta = new Vector2(20f, 20f);
            arrowRect.anchoredPosition = new Vector2(-15f, 0f);

            Image itemBgImage = itemBgObj.AddComponent<Image>();
            itemBgImage.color = new Color(0.25f, 0.45f, 0.25f, 1.0f);

            Toggle itemToggle = itemObj.AddComponent<Toggle>();
            itemToggle.targetGraphic = itemBgImage;
            itemToggle.isOn = true;
            ColorBlock colors = itemToggle.colors;
            colors.normalColor = new Color(0.35f, 0.35f, 0.35f, 1.0f);
            colors.highlightedColor = new Color(0.25f, 0.45f, 0.25f, 1.0f);
            itemToggle.colors = colors;

            itemToggle.onValueChanged.AddListener((bool val) => { itemToggle.OnDeselect(null); });
            Image templateImage = templateObj.AddComponent<Image>();
            templateImage.type = Image.Type.Sliced;
            templateImage.color = new Color(0.15f, 0.15f, 0.15f, 1.0f);

            ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
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
            dropdownImage.color = new Color(0.2f, 0.2f, 0.2f, 1);
            dropdownImage.type = Image.Type.Sliced;

            dropdown = dropdownObj.AddComponent<Dropdown>();
            dropdown.targetGraphic = dropdownImage;
            dropdown.template = templateObj.GetComponent<RectTransform>();
            dropdown.captionText = labelText;
            dropdown.itemText = itemLabelText;
            itemLabelText.text = "DEFAULT";

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

            return dropdownObj;
        }

        public static GameObject CreateScrollView(GameObject parent, out GameObject content, out SliderScrollbar scroller, Color color = default)
        {
            GameObject mainObj = CreateUIObject("DynamicScrollView", parent);

            var mainLayout = mainObj.AddComponent<LayoutElement>();
            mainLayout.minWidth = 100;
            mainLayout.minHeight = 100;
            mainLayout.flexibleWidth = 5000;
            mainLayout.flexibleHeight = 5000;

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

            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childForceExpandHeight = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childControlWidth = true;
            contentLayout.padding.left = 5;
            contentLayout.padding.right = 5;
            contentLayout.padding.top = 5;
            contentLayout.padding.bottom = 5;
            contentLayout.spacing = 5;

            GameObject scrollBarObj = CreateUIObject("DynamicScrollbar", mainObj);

            var scrollbarLayout = scrollBarObj.AddComponent<VerticalLayoutGroup>();
            scrollbarLayout.childForceExpandHeight = true;
            scrollbarLayout.childControlHeight = true;

            RectTransform scrollBarRect = scrollBarObj.GetComponent<RectTransform>();
            scrollBarRect.anchorMin = new Vector2(1.0f, 0.0f);
            scrollBarRect.anchorMax = new Vector2(1.0f, 1.0f);
            scrollBarRect.sizeDelta = new Vector2(15.0f, 0.0f);
            scrollBarRect.offsetMin = new Vector2(-15.0f, 0.0f);

            GameObject hiddenBar = CreateScrollbar(scrollBarObj);
            var hiddenScroll = hiddenBar.GetComponent<Scrollbar>();
            hiddenScroll.SetDirection(Scrollbar.Direction.BottomToTop, true);

            for (int i = 0; i < hiddenBar.transform.childCount; i++)
            {
                var child = hiddenBar.transform.GetChild(i);
                child.gameObject.SetActive(false);
            }

            SliderScrollbar.CreateSliderScrollbar(scrollBarObj, out Slider scrollSlider);

            // Back to the main scrollview ScrollRect, setting it up now that we have all references.

            var scrollRect = mainObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.verticalScrollbar = hiddenScroll;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 35;
            scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            // Create a custom DynamicScrollbar module
            scroller = new SliderScrollbar(hiddenScroll, scrollSlider);

            return mainObj;
        }
    }
}
