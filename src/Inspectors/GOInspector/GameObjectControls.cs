using System;
using System.Collections.Generic;
using System.Linq;
using UnityExplorer.Helpers;
using UnityExplorer.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Input;
using UnityExplorer.Unstrip.Resources;

namespace UnityExplorer.Inspectors.GOInspector
{
    public class GameObjectControls
    {
        internal static GameObjectControls Instance;

        public GameObjectControls()
        {
            Instance = this;
        }

        private static TMP_InputField s_setParentInput;

        private static ControlEditor s_positionControl;
        private static ControlEditor s_localPosControl;
        private static ControlEditor s_rotationControl;
        private static ControlEditor s_scaleControl;

        // Transform Vector editors

        internal struct ControlEditor
        {
            public TMP_InputField fullValue;
            public Slider[] sliders;
            public TMP_InputField[] inputs;
            public Text[] values;
        }

        internal static bool s_sliderChangedWanted;
        private static Slider s_currentSlider;
        private static ControlType s_currentSliderType;
        private static VectorValue s_currentSliderValueType;
        private static float s_currentSliderValue;

        internal enum ControlType
        {
            position,
            localPosition,
            eulerAngles,
            localScale
        }

        internal enum VectorValue
        {
            x, y, z
        };

        internal void RefreshControls()
        {
            var go = GameObjectInspector.ActiveInstance.TargetGO;

            s_positionControl.fullValue.text = go.transform.position.ToStringLong();
            s_positionControl.values[0].text = go.transform.position.x.ToString("F3");
            s_positionControl.values[1].text = go.transform.position.y.ToString("F3");
            s_positionControl.values[2].text = go.transform.position.z.ToString("F3");

            s_localPosControl.fullValue.text = go.transform.localPosition.ToStringLong();
            s_localPosControl.values[0].text = go.transform.localPosition.x.ToString("F3");
            s_localPosControl.values[1].text = go.transform.localPosition.y.ToString("F3");
            s_localPosControl.values[2].text = go.transform.localPosition.z.ToString("F3");

            s_rotationControl.fullValue.text = go.transform.eulerAngles.ToStringLong();
            s_rotationControl.values[0].text = go.transform.eulerAngles.x.ToString("F3");
            s_rotationControl.values[1].text = go.transform.eulerAngles.y.ToString("F3");
            s_rotationControl.values[2].text = go.transform.eulerAngles.z.ToString("F3");

            s_scaleControl.fullValue.text = go.transform.localScale.ToStringLong();
            s_scaleControl.values[0].text = go.transform.localScale.x.ToString("F3");
            s_scaleControl.values[1].text = go.transform.localScale.y.ToString("F3");
            s_scaleControl.values[2].text = go.transform.localScale.z.ToString("F3");

        }

        internal static void OnSetParentClicked()
        {
            var go = GameObjectInspector.ActiveInstance.TargetGO;

            if (!go)
                return;

            var input = s_setParentInput.text;

            if (string.IsNullOrEmpty(input))
            {
                go.transform.parent = null;
            }
            else
            {
                if (GameObject.Find(input) is GameObject newParent)
                {
                    go.transform.parent = newParent.transform;
                }
                else
                {
                    ExplorerCore.Log($"Could not find any GameObject from name or path '{input}'! Note: The target must be enabled.");
                }
            }
        }

        internal static void OnSliderControlChanged(float value, Slider slider, ControlType controlType, VectorValue vectorValue)
        {
            if (value == 0)
                s_sliderChangedWanted = false;
            else
            {
                if (!s_sliderChangedWanted)
                {
                    s_sliderChangedWanted = true;
                    s_currentSlider = slider;
                    s_currentSliderType = controlType;
                    s_currentSliderValueType = vectorValue;
                }

                s_currentSliderValue = value;
            }
        }

        internal static void UpdateSliderControl()
        {
            if (!InputManager.GetMouseButton(0))
            {
                s_sliderChangedWanted = false;
                s_currentSlider.value = 0;

                return;
            }

            if (GameObjectInspector.ActiveInstance == null) return;

            var transform = GameObjectInspector.ActiveInstance.TargetGO.transform;

            // get the current vector for the control type
            Vector3 vector = Vector2.zero;
            switch (s_currentSliderType)
            {
                case ControlType.position:
                    vector = transform.position; break;
                case ControlType.localPosition:
                    vector = transform.localPosition; break;
                case ControlType.eulerAngles:
                    vector = transform.eulerAngles; break;
                case ControlType.localScale:
                    vector = transform.localScale; break;
            }

            // apply vector value change
            switch (s_currentSliderValueType)
            {
                case VectorValue.x:
                    vector.x += s_currentSliderValue; break;
                case VectorValue.y:
                    vector.y += s_currentSliderValue; break;
                case VectorValue.z:
                    vector.z += s_currentSliderValue; break;
            }

            // set vector to transform member
            switch (s_currentSliderType)
            {
                case ControlType.position:
                    transform.position = vector; break;
                case ControlType.localPosition:
                    transform.localPosition = vector; break;
                case ControlType.eulerAngles:
                    transform.eulerAngles = vector; break;
                case ControlType.localScale:
                    transform.localScale = vector; break;
            }
        }

        internal static void OnVectorControlInputApplied(ControlType controlType, VectorValue vectorValue)
        {
            if (!(InspectorManager.Instance.m_activeInspector is GameObjectInspector instance)) return;

            // get relevant input for controltype + value

            TMP_InputField[] inputs = null;
            switch (controlType)
            {
                case ControlType.position:
                    inputs = s_positionControl.inputs; break;
                case ControlType.localPosition:
                    inputs = s_localPosControl.inputs; break;
                case ControlType.eulerAngles:
                    inputs = s_rotationControl.inputs; break;
                case ControlType.localScale:
                    inputs = s_scaleControl.inputs; break;
            }
            TMP_InputField input = inputs[(int)vectorValue];

            float val = float.Parse(input.text);

            // apply transform value

            Vector3 vector = Vector3.zero;
            var transform = instance.TargetGO.transform;
            switch (controlType)
            {
                case ControlType.position:
                    vector = transform.position; break;
                case ControlType.localPosition:
                    vector = transform.localPosition; break;
                case ControlType.eulerAngles:
                    vector = transform.eulerAngles; break;
                case ControlType.localScale:
                    vector = transform.localScale; break;
            }

            switch (vectorValue)
            {
                case VectorValue.x:
                    vector.x = val; break;
                case VectorValue.y:
                    vector.y = val; break;
                case VectorValue.z:
                    vector.z = val; break;
            }

            // set back to transform
            switch (controlType)
            {
                case ControlType.position:
                    transform.position = vector; break;
                case ControlType.localPosition:
                    transform.localPosition = vector; break;
                case ControlType.eulerAngles:
                    transform.eulerAngles = vector; break;
                case ControlType.localScale:
                    transform.localScale = vector; break;
            }
        }

        #region UI CONSTRUCTION

        internal void ConstructControls(GameObject parent)
        {
            var controlsObj = UIFactory.CreateVerticalGroup(parent, new Color(0.07f, 0.07f, 0.07f));
            var controlsGroup = controlsObj.GetComponent<VerticalLayoutGroup>();
            controlsGroup.childForceExpandWidth = false;
            controlsGroup.childControlWidth = true;
            controlsGroup.childForceExpandHeight = false;
            controlsGroup.spacing = 5;
            controlsGroup.padding.top = 4;
            controlsGroup.padding.left = 4;
            controlsGroup.padding.right = 4;
            controlsGroup.padding.bottom = 4;

            // ~~~~~~ Top row ~~~~~~

            var topRow = UIFactory.CreateHorizontalGroup(controlsObj, new Color(1, 1, 1, 0));
            var topRowGroup = topRow.GetComponent<HorizontalLayoutGroup>();
            topRowGroup.childForceExpandWidth = false;
            topRowGroup.childControlWidth = true;
            topRowGroup.childForceExpandHeight = false;
            topRowGroup.childControlHeight = true;
            topRowGroup.spacing = 5;

            var hideButtonObj = UIFactory.CreateButton(topRow);
            var hideButton = hideButtonObj.GetComponent<Button>();
            var hideColors = hideButton.colors;
            hideColors.normalColor = new Color(0.16f, 0.16f, 0.16f);
            hideButton.colors = hideColors;
            var hideText = hideButtonObj.GetComponentInChildren<Text>();
            hideText.text = "Show";
            hideText.fontSize = 14;
            var hideButtonLayout = hideButtonObj.AddComponent<LayoutElement>();
            hideButtonLayout.minWidth = 40;
            hideButtonLayout.flexibleWidth = 0;
            hideButtonLayout.minHeight = 25;
            hideButtonLayout.flexibleHeight = 0;

            var topTitle = UIFactory.CreateLabel(topRow, TextAnchor.MiddleLeft);
            var topText = topTitle.GetComponent<Text>();
            topText.text = "Controls";
            var titleLayout = topTitle.AddComponent<LayoutElement>();
            titleLayout.minWidth = 100;
            titleLayout.flexibleWidth = 9500;
            titleLayout.minHeight = 25;

            //// ~~~~~~~~ Content ~~~~~~~~ //

            var contentObj = UIFactory.CreateVerticalGroup(controlsObj, new Color(1, 1, 1, 0));
            var contentGroup = contentObj.GetComponent<VerticalLayoutGroup>();
            contentGroup.childForceExpandHeight = false;
            contentGroup.childControlHeight = true;
            contentGroup.spacing = 5;
            contentGroup.childForceExpandWidth = true;
            contentGroup.childControlWidth = true;

            // ~~ add hide button callback now that we have scroll reference ~~
#if CPP
            hideButton.onClick.AddListener(new Action(OnHideClicked));
#else
            hideButton.onClick.AddListener(OnHideClicked);
#endif
            void OnHideClicked()
            {
                if (hideText.text == "Show")
                {
                    hideText.text = "Hide";
                    contentObj.SetActive(true);
                }
                else
                {
                    hideText.text = "Show";
                    contentObj.SetActive(false);
                }
            }

            // set parent 
            ConstructSetParent(contentObj);

            // transform controls
            ConstructVector3Editor(contentObj, "Position", ControlType.position, out s_positionControl);
            ConstructVector3Editor(contentObj, "Local Position", ControlType.localPosition, out s_localPosControl);
            ConstructVector3Editor(contentObj, "Rotation", ControlType.eulerAngles, out s_rotationControl);
            ConstructVector3Editor(contentObj, "Scale", ControlType.localScale, out s_scaleControl);

            // bottom row buttons
            ConstructBottomButtons(contentObj);

            // set controls content inactive now that content is made (otherwise TMP font size goes way too big?)
            contentObj.SetActive(false);
        }

        internal void ConstructSetParent(GameObject contentObj)
        {
            var setParentGroupObj = UIFactory.CreateHorizontalGroup(contentObj, new Color(1, 1, 1, 0));
            var setParentGroup = setParentGroupObj.GetComponent<HorizontalLayoutGroup>();
            setParentGroup.childForceExpandHeight = false;
            setParentGroup.childControlHeight = true;
            setParentGroup.childForceExpandWidth = false;
            setParentGroup.childControlWidth = true;
            setParentGroup.spacing = 5;
            var setParentLayout = setParentGroupObj.AddComponent<LayoutElement>();
            setParentLayout.minHeight = 25;
            setParentLayout.flexibleHeight = 0;

            var setParentLabelObj = UIFactory.CreateLabel(setParentGroupObj, TextAnchor.MiddleLeft);
            var setParentLabel = setParentLabelObj.GetComponent<Text>();
            setParentLabel.text = "Set Parent:";
            setParentLabel.color = Color.grey;
            setParentLabel.fontSize = 14;
            var setParentLabelLayout = setParentLabelObj.AddComponent<LayoutElement>();
            setParentLabelLayout.minWidth = 110;
            setParentLabelLayout.minHeight = 25;
            setParentLabelLayout.flexibleWidth = 0;

            var setParentInputObj = UIFactory.CreateTMPInput(setParentGroupObj, 14, 0, (int)TextAlignmentOptions.MidlineLeft);
            s_setParentInput = setParentInputObj.GetComponent<TMP_InputField>();
            var placeholderInput = setParentInputObj.transform.Find("TextArea/Placeholder").GetComponent<TextMeshProUGUI>();
            placeholderInput.text = "Enter a GameObject name or path...";
            var setParentInputLayout = setParentInputObj.AddComponent<LayoutElement>();
            setParentInputLayout.minHeight = 25;
            setParentInputLayout.preferredWidth = 400;
            setParentInputLayout.flexibleWidth = 9999;

            var applyButtonObj = UIFactory.CreateButton(setParentGroupObj);
            var applyButton = applyButtonObj.GetComponent<Button>();
#if CPP
            applyButton.onClick.AddListener(new Action(OnSetParentClicked));
#else
            applyButton.onClick.AddListener(OnSetParentClicked);
#endif
            var applyText = applyButtonObj.GetComponentInChildren<Text>();
            applyText.text = "Apply";
            var applyLayout = applyButtonObj.AddComponent<LayoutElement>();
            applyLayout.minWidth = 55;
            applyLayout.flexibleWidth = 0;
            applyLayout.minHeight = 25;
            applyLayout.flexibleHeight = 0;
        }

        internal void ConstructVector3Editor(GameObject parent, string title, ControlType type, out ControlEditor editor)
        {
            editor = new ControlEditor();

            var topBarObj = UIFactory.CreateHorizontalGroup(parent, new Color(1, 1, 1, 0));
            var topGroup = topBarObj.GetComponent<HorizontalLayoutGroup>();
            topGroup.childForceExpandWidth = false;
            topGroup.childControlWidth = true;
            topGroup.childForceExpandHeight = false;
            topGroup.childControlHeight = true;
            topGroup.spacing = 5;
            var topLayout = topBarObj.AddComponent<LayoutElement>();
            topLayout.minHeight = 25;
            topLayout.flexibleHeight = 0;

            var titleObj = UIFactory.CreateLabel(topBarObj, TextAnchor.MiddleLeft);
            var titleText = titleObj.GetComponent<Text>();
            titleText.text = title;
            titleText.color = Color.grey;
            titleText.fontSize = 14;
            var titleLayout = titleObj.AddComponent<LayoutElement>();
            titleLayout.minWidth = 110;
            titleLayout.flexibleWidth = 0;
            titleLayout.minHeight = 25;

            // expand button
            var expandButtonObj = UIFactory.CreateButton(topBarObj);
            var expandButton = expandButtonObj.GetComponent<Button>();
            var expandText = expandButtonObj.GetComponentInChildren<Text>();
            expandText.text = "▼";
            expandText.fontSize = 12;
            var btnLayout = expandButtonObj.AddComponent<LayoutElement>();
            btnLayout.minWidth = 35;
            btnLayout.flexibleWidth = 0;
            btnLayout.minHeight = 25;
            btnLayout.flexibleHeight = 0;

            // readonly value input

            var valueInputObj = UIFactory.CreateTMPInput(topBarObj, 14, 0, (int)TextAlignmentOptions.MidlineLeft);
            var valueInput = valueInputObj.GetComponent<TMP_InputField>();
            valueInput.readOnly = true;
            var valueInputLayout = valueInputObj.AddComponent<LayoutElement>();
            valueInputLayout.minHeight = 25;
            valueInputLayout.flexibleHeight = 0;
            valueInputLayout.preferredWidth = 400;
            valueInputLayout.flexibleWidth = 9999;

            editor.fullValue = valueInput;

            editor.sliders = new Slider[3];
            editor.inputs = new TMP_InputField[3];
            editor.values = new Text[3];

            var xRow = ConstructEditorRow(parent, editor, type, VectorValue.x);
            xRow.SetActive(false);
            var yRow = ConstructEditorRow(parent, editor, type, VectorValue.y);
            yRow.SetActive(false);
            var zRow = ConstructEditorRow(parent, editor, type, VectorValue.z);
            zRow.SetActive(false);

            // add expand callback now that we have group reference
#if CPP
            expandButton.onClick.AddListener(new Action(ToggleExpand));
#else
            expandButton.onClick.AddListener(ToggleExpand);
#endif
            void ToggleExpand()
            {
                if (xRow.activeSelf)
                {
                    xRow.SetActive(false);
                    yRow.SetActive(false);
                    zRow.SetActive(false);
                    expandText.text = "▼";
                }
                else
                {
                    xRow.SetActive(true);
                    yRow.SetActive(true);
                    zRow.SetActive(true);
                    expandText.text = "▲";
                }
            }
        }

        internal GameObject ConstructEditorRow(GameObject parent, ControlEditor editor, ControlType type, VectorValue vectorValue)
        {
            var rowObject = UIFactory.CreateHorizontalGroup(parent, new Color(1, 1, 1, 0));
            var rowGroup = rowObject.GetComponent<HorizontalLayoutGroup>();
            rowGroup.childForceExpandWidth = false;
            rowGroup.childControlWidth = true;
            rowGroup.childForceExpandHeight = false;
            rowGroup.childControlHeight = true;
            rowGroup.spacing = 5;
            var rowLayout = rowObject.AddComponent<LayoutElement>();
            rowLayout.minHeight = 25;
            rowLayout.flexibleHeight = 0;
            rowLayout.minWidth = 100;

            // Value labels

            var labelObj = UIFactory.CreateLabel(rowObject, TextAnchor.MiddleLeft);
            var labelText = labelObj.GetComponent<Text>();
            labelText.color = Color.cyan;
            labelText.text = $"{vectorValue.ToString().ToUpper()}:";
            labelText.fontSize = 14;
            labelText.resizeTextMaxSize = 14;
            labelText.resizeTextForBestFit = true;
            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.minHeight = 25;
            labelLayout.flexibleHeight = 0;
            labelLayout.minWidth = 25;
            labelLayout.flexibleWidth = 0;

            // actual value label
            var valueLabelObj = UIFactory.CreateLabel(rowObject, TextAnchor.MiddleLeft);
            var valueLabel = valueLabelObj.GetComponent<Text>();
            editor.values[(int)vectorValue] = valueLabel;
            var valueLabelLayout = valueLabelObj.AddComponent<LayoutElement>();
            valueLabelLayout.minWidth = 85;
            valueLabelLayout.flexibleWidth = 0;
            valueLabelLayout.minHeight = 25;

            // Slider

            var sliderObj = UIFactory.CreateSlider(rowObject);
            var sliderLayout = sliderObj.AddComponent<LayoutElement>();
            sliderLayout.minHeight = 20;
            sliderLayout.flexibleHeight = 0;
            sliderLayout.minWidth = 200;
            sliderLayout.flexibleWidth = 9000;
            var slider = sliderObj.GetComponent<Slider>();
            slider.minValue = -2;
            slider.maxValue = 2;
            slider.value = 0;
#if CPP
            slider.onValueChanged.AddListener(new Action<float>((float val) => { OnSliderControlChanged(val, slider, type, vectorValue); }));
#else
            slider.onValueChanged.AddListener((float val) => { OnSliderControlChanged(val, slider, type, vectorValue); });
#endif
            editor.sliders[(int)vectorValue] = slider;

            // input field

            var inputHolder = UIFactory.CreateVerticalGroup(rowObject, new Color(1, 1, 1, 0));
            var inputHolderGroup = inputHolder.GetComponent<VerticalLayoutGroup>();
            inputHolderGroup.childForceExpandHeight = false;
            inputHolderGroup.childControlHeight = true;
            inputHolderGroup.childForceExpandWidth = false;
            inputHolderGroup.childControlWidth = true;

            var inputObj = UIFactory.CreateTMPInput(inputHolder, 14, 0, (int)TextAlignmentOptions.MidlineLeft);
            var input = inputObj.GetComponent<TMP_InputField>();
            input.characterValidation = TMP_InputField.CharacterValidation.Decimal;

            var inputLayout = inputObj.AddComponent<LayoutElement>();
            inputLayout.minHeight = 25;
            inputLayout.flexibleHeight = 0;
            inputLayout.minWidth = 90;
            inputLayout.flexibleWidth = 50;

            editor.inputs[(int)vectorValue] = input;

            // apply button

            var applyBtnObj = UIFactory.CreateButton(rowObject);
            var applyBtn = applyBtnObj.GetComponent<Button>();
            var applyText = applyBtnObj.GetComponentInChildren<Text>();
            applyText.text = "Apply";
            applyText.fontSize = 14;
            var applyLayout = applyBtnObj.AddComponent<LayoutElement>();
            applyLayout.minWidth = 60;
            applyLayout.minHeight = 25;

#if MONO
            applyBtn.onClick.AddListener(() => { OnVectorControlInputApplied(type, vectorValue); });
#else
            applyBtn.onClick.AddListener(new Action(() => { OnVectorControlInputApplied(type, vectorValue); }));
#endif

            return rowObject;
        }

        internal void ConstructBottomButtons(GameObject contentObj)
        {
            var bottomRow = UIFactory.CreateHorizontalGroup(contentObj, new Color(1, 1, 1, 0));
            var bottomGroup = bottomRow.GetComponent<HorizontalLayoutGroup>();
            bottomGroup.childForceExpandWidth = true;
            bottomGroup.childControlWidth = true;
            bottomGroup.spacing = 4;
            var bottomLayout = bottomRow.AddComponent<LayoutElement>();
            bottomLayout.minHeight = 25;

            var instantiateBtnObj = UIFactory.CreateButton(bottomRow, new Color(0.2f, 0.2f, 0.2f));
            var instantiateBtn = instantiateBtnObj.GetComponent<Button>();
            instantiateBtn.onClick.AddListener(InstantiateBtn);
            var instantiateText = instantiateBtnObj.GetComponentInChildren<Text>();
            instantiateText.text = "Instantiate";
            instantiateText.fontSize = 14;
            var instantiateLayout = instantiateBtnObj.AddComponent<LayoutElement>();
            instantiateLayout.minWidth = 150;

            void InstantiateBtn()
            {
                var go = GameObjectInspector.ActiveInstance.TargetGO;
                if (!go)
                    return;

                var clone = GameObject.Instantiate(go);
                InspectorManager.Instance.Inspect(clone);
            }

            var dontDestroyBtnObj = UIFactory.CreateButton(bottomRow, new Color(0.2f, 0.2f, 0.2f));
            var dontDestroyBtn = dontDestroyBtnObj.GetComponent<Button>();
            dontDestroyBtn.onClick.AddListener(DontDestroyOnLoadBtn);
            var dontDestroyText = dontDestroyBtnObj.GetComponentInChildren<Text>();
            dontDestroyText.text = "Set DontDestroyOnLoad";
            dontDestroyText.fontSize = 14;
            var dontDestroyLayout = dontDestroyBtnObj.AddComponent<LayoutElement>();
            dontDestroyLayout.flexibleWidth = 5000;

            void DontDestroyOnLoadBtn()
            {
                var go = GameObjectInspector.ActiveInstance.TargetGO;
                if (!go)
                    return;

                GameObject.DontDestroyOnLoad(go);
            }

            var destroyBtnObj = UIFactory.CreateButton(bottomRow, new Color(0.2f, 0.2f, 0.2f));
            var destroyBtn = destroyBtnObj.GetComponent<Button>();
            destroyBtn.onClick.AddListener(DestroyBtn);
            var destroyText = destroyBtnObj.GetComponentInChildren<Text>();
            destroyText.text = "Destroy";
            destroyText.fontSize = 14;
            destroyText.color = Color.red;
            var destroyLayout = destroyBtnObj.AddComponent<LayoutElement>();
            destroyLayout.minWidth = 150;

            void DestroyBtn()
            {
                var go = GameObjectInspector.ActiveInstance.TargetGO;
                if (!go)
                    return;

                GameObject.Destroy(go);
            }
        }

        #endregion
    }
}
