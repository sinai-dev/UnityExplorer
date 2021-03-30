using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Input;
using UnityExplorer.Core.Unity;

namespace UnityExplorer.UI.Main.Home.Inspectors.GameObjects
{
    public class GameObjectControls
    {
        internal static GameObjectControls Instance;

        public GameObjectControls()
        {
            Instance = this;
        }

        internal static bool Showing;

        internal static void ToggleVisibility() => SetVisibility(!Showing);

        internal static void SetVisibility(bool show)
        {
            if (show == Showing)
                return;

            Showing = show;

            m_hideShowLabel.text = show ? "Hide" : "Show";
            m_contentObj.SetActive(show);
        }

        internal static GameObject m_contentObj;
        internal static Text m_hideShowLabel;

        private static InputField s_setParentInput;

        private static ControlEditor s_positionControl;
        private static ControlEditor s_localPosControl;
        private static ControlEditor s_rotationControl;
        private static ControlEditor s_scaleControl;

        // Transform Vector editors

        internal struct ControlEditor
        {
            public InputField fullValue;
            public Slider[] sliders;
            public InputField[] inputs;
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

            s_positionControl.fullValue.text = go.transform.position.ToStringPretty();
            s_positionControl.values[0].text = go.transform.position.x.ToString("F3");
            s_positionControl.values[1].text = go.transform.position.y.ToString("F3");
            s_positionControl.values[2].text = go.transform.position.z.ToString("F3");

            s_localPosControl.fullValue.text = go.transform.localPosition.ToStringPretty();
            s_localPosControl.values[0].text = go.transform.localPosition.x.ToString("F3");
            s_localPosControl.values[1].text = go.transform.localPosition.y.ToString("F3");
            s_localPosControl.values[2].text = go.transform.localPosition.z.ToString("F3");

            s_rotationControl.fullValue.text = go.transform.eulerAngles.ToStringPretty();
            s_rotationControl.values[0].text = go.transform.eulerAngles.x.ToString("F3");
            s_rotationControl.values[1].text = go.transform.eulerAngles.y.ToString("F3");
            s_rotationControl.values[2].text = go.transform.eulerAngles.z.ToString("F3");

            s_scaleControl.fullValue.text = go.transform.localScale.ToStringPretty();
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

            InputField[] inputs = null;
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
            InputField input = inputs[(int)vectorValue];

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
            var mainGroup = UIFactory.CreateVerticalGroup(parent, "ControlsGroup", false, false, true, true, 5, new Vector4(4,4,4,4),
                new Color(0.07f, 0.07f, 0.07f));

            // ~~~~~~ Top row ~~~~~~

            var topRow = UIFactory.CreateHorizontalGroup(mainGroup, "TopRow", false, false, true, true, 5, default, new Color(1, 1, 1, 0));

            var hideButton = UIFactory.CreateButton(topRow, "ToggleShowButton", "Show", ToggleVisibility, new Color(0.16f, 0.16f, 0.16f));
            UIFactory.SetLayoutElement(hideButton.gameObject, minWidth: 40, flexibleWidth: 0, minHeight: 25, flexibleHeight: 0);
            m_hideShowLabel = hideButton.GetComponentInChildren<Text>();

            var topTitle = UIFactory.CreateLabel(topRow, "ControlsLabel", "Controls", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(topTitle.gameObject, minWidth: 100, flexibleWidth: 9500, minHeight: 25);

            //// ~~~~~~~~ Content ~~~~~~~~ //

            m_contentObj = UIFactory.CreateVerticalGroup(mainGroup, "ContentGroup", true, false, true, true, 5, default, new Color(1, 1, 1, 0));

            // transform controls
            ConstructVector3Editor(m_contentObj, "Position", ControlType.position, out s_positionControl);
            ConstructVector3Editor(m_contentObj, "Local Position", ControlType.localPosition, out s_localPosControl);
            ConstructVector3Editor(m_contentObj, "Rotation", ControlType.eulerAngles, out s_rotationControl);
            ConstructVector3Editor(m_contentObj, "Scale", ControlType.localScale, out s_scaleControl);

            // set parent 
            ConstructSetParent(m_contentObj);

            // bottom row buttons
            ConstructBottomButtons(m_contentObj);

            // set controls content inactive now that content is made (otherwise TMP font size goes way too big?)
            m_contentObj.SetActive(false);
        }

        internal void ConstructSetParent(GameObject contentObj)
        {
            var setParentGroupObj = UIFactory.CreateHorizontalGroup(contentObj, "SetParentRow", false, false, true, true, 5, default, 
                new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(setParentGroupObj, minHeight: 25, flexibleHeight: 0);

            var title = UIFactory.CreateLabel(setParentGroupObj, "SetParentLabel", "Set Parent:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(title.gameObject, minWidth: 110, minHeight: 25, flexibleHeight: 0);

            var inputFieldObj = UIFactory.CreateInputField(setParentGroupObj, "SetParentInputField", "Enter a GameObject name or path...");
            s_setParentInput = inputFieldObj.GetComponent<InputField>();
            UIFactory.SetLayoutElement(inputFieldObj, minHeight: 25, preferredWidth: 400, flexibleWidth: 9999);

            var applyButton = UIFactory.CreateButton(setParentGroupObj, "SetParentButton", "Apply", OnSetParentClicked);
            UIFactory.SetLayoutElement(applyButton.gameObject, minWidth: 55, flexibleWidth: 0, minHeight: 25, flexibleHeight: 0);
        }

        internal void ConstructVector3Editor(GameObject parent, string titleText, ControlType type, out ControlEditor editor)
        {
            editor = new ControlEditor();

            var topBarObj = UIFactory.CreateHorizontalGroup(parent, "Vector3Editor", false, false, true, true, 5, default, new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(topBarObj, minHeight: 25, flexibleHeight: 0);

            var title = UIFactory.CreateLabel(topBarObj, "Title", titleText, TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(title.gameObject, minWidth: 110, flexibleWidth: 0, minHeight: 25);

            // expand button
            var expandButton = UIFactory.CreateButton(topBarObj, "ExpandArrow", "▼");
            var expandText = expandButton.GetComponentInChildren<Text>();
            expandText.fontSize = 12;
            UIFactory.SetLayoutElement(expandButton.gameObject, minWidth: 35, flexibleWidth: 0, minHeight: 25, flexibleHeight: 0);

            // readonly value input

            var valueInputObj = UIFactory.CreateInputField(topBarObj, "ValueInput", "...");
            var valueInput = valueInputObj.GetComponent<InputField>();
            valueInput.readOnly = true;
            UIFactory.SetLayoutElement(valueInputObj, minHeight: 25, flexibleHeight: 0, preferredWidth: 400, flexibleWidth: 9999);

            editor.fullValue = valueInput;

            editor.sliders = new Slider[3];
            editor.inputs = new InputField[3];
            editor.values = new Text[3];

            var xRow = ConstructEditorRow(parent, editor, type, VectorValue.x);
            xRow.SetActive(false);
            var yRow = ConstructEditorRow(parent, editor, type, VectorValue.y);
            yRow.SetActive(false);
            var zRow = ConstructEditorRow(parent, editor, type, VectorValue.z);
            zRow.SetActive(false);

            // add expand callback now that we have group reference
            expandButton.onClick.AddListener(ToggleExpand);
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
            var rowObject = UIFactory.CreateHorizontalGroup(parent, "EditorRow", false, false, true, true, 5, default, new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(rowObject, minHeight: 25, flexibleHeight: 0, minWidth: 100);

            // Value labels

            var valueTitle = UIFactory.CreateLabel(rowObject, "ValueTitle", $"{vectorValue.ToString().ToUpper()}:", TextAnchor.MiddleLeft, Color.cyan);
            UIFactory.SetLayoutElement(valueTitle.gameObject, minHeight: 25, flexibleHeight: 0, minWidth: 25, flexibleWidth: 0);

            // actual value label
            var valueLabel = UIFactory.CreateLabel(rowObject, "ValueLabel", "<notset>", TextAnchor.MiddleLeft);
            editor.values[(int)vectorValue] = valueLabel;
            UIFactory.SetLayoutElement(valueLabel.gameObject, minWidth: 85, flexibleWidth: 0, minHeight: 25);

            // input field

            var inputHolder = UIFactory.CreateVerticalGroup(rowObject, "InputFieldGroup", false, false, true, true, 0, default, new Color(1, 1, 1, 0));

            var inputObj = UIFactory.CreateInputField(inputHolder, "InputField", "...");
            var input = inputObj.GetComponent<InputField>();
            //input.characterValidation = InputField.CharacterValidation.Decimal;

            UIFactory.SetLayoutElement(inputObj, minHeight: 25, flexibleHeight: 0, minWidth: 90, flexibleWidth: 50);

            editor.inputs[(int)vectorValue] = input;

            // apply button

            var applyBtn = UIFactory.CreateButton(rowObject, "ApplyButton", "Apply", () => { OnVectorControlInputApplied(type, vectorValue); });
            UIFactory.SetLayoutElement(applyBtn.gameObject, minWidth: 60, minHeight: 25);


            // Slider

            var sliderObj = UIFactory.CreateSlider(rowObject, "VectorSlider", out Slider slider);
            UIFactory.SetLayoutElement(sliderObj, minHeight: 20, flexibleHeight: 0, minWidth: 200, flexibleWidth: 9000);
            sliderObj.transform.Find("Fill Area").gameObject.SetActive(false);
            var sliderColors = slider.colors;
            sliderColors.normalColor = new Color(0.65f, 0.65f, 0.65f);
            slider.colors = sliderColors;
            slider.minValue = -2;
            slider.maxValue = 2;
            slider.value = 0;
            slider.onValueChanged.AddListener((float val) => { OnSliderControlChanged(val, slider, type, vectorValue); });
            editor.sliders[(int)vectorValue] = slider;

            return rowObject;
        }

        internal void ConstructBottomButtons(GameObject contentObj)
        {
            var bottomRow = UIFactory.CreateHorizontalGroup(contentObj, "BottomButtons", true, true, false, false, 4, default, new Color(1, 1, 1, 0));

            var instantiateBtn = UIFactory.CreateButton(bottomRow, "InstantiateBtn", "Instantiate", InstantiateBtn, new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(instantiateBtn.gameObject, minWidth: 150);

            void InstantiateBtn()
            {
                var go = GameObjectInspector.ActiveInstance.TargetGO;
                if (!go)
                    return;

                var clone = GameObject.Instantiate(go);
                InspectorManager.Instance.Inspect(clone);
            }

            var dontDestroyBtn = UIFactory.CreateButton(bottomRow, "DontDestroyButton", "Set DontDestroyOnLoad", DontDestroyOnLoadBtn,
                new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(dontDestroyBtn.gameObject, flexibleWidth: 5000);

            void DontDestroyOnLoadBtn()
            {
                var go = GameObjectInspector.ActiveInstance.TargetGO;
                if (!go)
                    return;

                GameObject.DontDestroyOnLoad(go);
            }

            var destroyBtn = UIFactory.CreateButton(bottomRow, "DestroyButton", "Destroy", DestroyBtn, new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(destroyBtn.gameObject, minWidth: 150);
            var destroyText = destroyBtn.GetComponentInChildren<Text>();
            destroyText.color = Color.red;

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
